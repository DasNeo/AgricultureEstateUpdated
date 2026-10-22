# Offline integration tests for the privileged publisher. No GitHub requests are made.
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot)
$fixture = Join-Path $root "artifacts/prerelease-tests-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Force $fixture | Out-Null
$publisher = Join-Path $root 'scripts/Manage-Prerelease.ps1'
$source = 'a' * 40
$package = '1.4.8.119303'
$global:PrereleaseTestState = @{
    Source = $source; Package = $package; State = 'open'; RunConclusion = 'success'; Immutable = $false
    Release = $false; Reference = $false; HeadRepo = 'owner/mod'; Calls = [Collections.Generic.List[object]]::new()
}
$oldRepo = $env:GH_REPO
$oldTemp = $env:RUNNER_TEMP
$oldSummary = $env:GITHUB_STEP_SUMMARY
$oldExitCode = $global:LASTEXITCODE
$env:GH_REPO = 'owner/mod'
$env:RUNNER_TEMP = $fixture
$env:GITHUB_STEP_SUMMARY = Join-Path $fixture summary.md

function gh {
    $call = @($args)
    $state = $global:PrereleaseTestState
    $global:LASTEXITCODE = 0
    $state.Calls.Add($call)
    if ($call[0] -eq 'release') {
        if ($call[1] -eq 'create') { $state.Release = $true }
        if ($call[1] -eq 'delete') { $state.Release = $false; $state.Reference = $false }
        return
    }
    if ($call[1] -eq '--method') {
        if ($call[2] -eq 'POST' -or $call[2] -eq 'PATCH') { $state.Reference = $true }
        if ($call[2] -eq 'DELETE') { $state.Reference = $false }
        return '{}'
    }
    if ($call[1] -eq '--paginate') { return 'BannerlordVersion.props' }
    $endpoint = $call[1]
    switch -Regex ($endpoint) {
        '/pulls/18$' { return (@{ state = $state.State; head = @{ sha = $state.Source; ref = 'codex/bannerlord-test'; repo = @{ full_name = $state.HeadRepo } }; base = @{ ref = 'master' }; html_url = 'https://example.test/pr/18' } | ConvertTo-Json -Depth 5) }
        '^repos/owner/mod$' { return '{"default_branch":"master"}' }
        '/actions/runs/123$' { return (@{ conclusion = $state.RunConclusion; head_repository = @{ full_name = 'owner/mod' }; path = '.github/workflows/bannerlord-check.yml'; event = 'pull_request'; head_sha = $state.Source; head_branch = 'codex/bannerlord-test'; html_url = 'https://example.test/run/123' } | ConvertTo-Json -Depth 4) }
        '/contents/BannerlordVersion.props' { return (@{ content = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("<Project><PropertyGroup><BannerlordVersion>$($state.Package)</BannerlordVersion></PropertyGroup></Project>")) } | ConvertTo-Json) }
        '/releases/tags/pr-18-preview$' {
            if ($state.Release) { return (@{ prerelease = $true; immutable = $state.Immutable } | ConvertTo-Json) }
            $global:LASTEXITCODE = 1; return 'HTTP 404'
        }
        '/git/ref/tags/pr-18-preview$' {
            if ($state.Reference) { return '{"object":{"sha":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}}' }
            $global:LASTEXITCODE = 1; return 'HTTP 404'
        }
        default { throw "Unexpected mock API request: $endpoint" }
    }
}

function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Get-MutationCount {
    return @($global:PrereleaseTestState.Calls | Where-Object { $_[0] -eq 'release' -or $_[1] -eq '--method' }).Count
}
function Assert-Rejected([string]$Message) {
    $before = Get-MutationCount
    $rejected = $false
    try { & $publisher -PullRequestNumber 18 -RunId 123 -ArtifactDirectory $fixture *> $null } catch { $rejected = $true }
    Assert-True $rejected $Message
    Assert-True ((Get-MutationCount) -eq $before) 'A rejected build mutated GitHub.'
}

try {
    $bin = Join-Path $fixture 'package/Modules/AgricultureEstate/bin/Win64_Shipping_Client'
    New-Item -ItemType Directory -Force $bin | Out-Null
    foreach ($name in 'AgricultureEstate', 'AgricultureEstate.Application', 'AgricultureEstate.Domain') {
        Set-Content (Join-Path $bin "$name.dll") 'test fixture'
    }
    $zip = Join-Path $fixture 'AgricultureEstate-pr-18-preview.zip'
    [IO.Compression.ZipFile]::CreateFromDirectory((Join-Path $fixture package), $zip)
    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  AgricultureEstate-pr-18-preview.zip" | Set-Content "$zip.sha256"

    @{ pull_request = 18; source_commit = $source; package = $package } | ConvertTo-Json | Set-Content (Join-Path $fixture preview.json)

    & $publisher -PullRequestNumber 18 -RunId 123 -ArtifactDirectory $fixture *> $null
    Assert-True $global:PrereleaseTestState.Release 'First successful build did not create a preview.'
    & $publisher -PullRequestNumber 18 -RunId 123 -ArtifactDirectory $fixture *> $null
    $creates = @($global:PrereleaseTestState.Calls | Where-Object { $_[0] -eq 'release' -and $_[1] -eq 'create' }).Count
    $uploads = @($global:PrereleaseTestState.Calls | Where-Object { $_[0] -eq 'release' -and $_[1] -eq 'upload' }).Count
    Assert-True ($creates -eq 1 -and $uploads -eq 2) 'Repeated builds must reuse one preview release.'

    $before = Get-MutationCount
    $global:PrereleaseTestState.Source = 'b' * 40
    & $publisher -PullRequestNumber 18 -RunId 123 -ArtifactDirectory $fixture *> $null
    Assert-True ((Get-MutationCount) -eq $before) 'Outdated build changed the preview.'
    $global:PrereleaseTestState.Source = $source

    $global:PrereleaseTestState.RunConclusion = 'failure'
    Assert-Rejected 'Failed workflow was accepted.'
    $global:PrereleaseTestState.RunConclusion = 'success'
    $global:PrereleaseTestState.Immutable = $true
    Assert-Rejected 'Immutable repository was accepted.'
    $global:PrereleaseTestState.Immutable = $false
    $global:PrereleaseTestState.HeadRepo = 'fork/mod'
    Assert-Rejected 'Fork PR was accepted.'
    $global:PrereleaseTestState.HeadRepo = 'owner/mod'
    Set-Content "$zip.sha256" 'incorrect checksum'
    Assert-Rejected 'Bad checksum was accepted.'
    "$hash  AgricultureEstate-pr-18-preview.zip" | Set-Content "$zip.sha256"

    @{ pull_request = 18; source_commit = $source; package = '1.4.7.117484' } | ConvertTo-Json | Set-Content (Join-Path $fixture preview.json)
    Assert-Rejected 'Mismatched target package was accepted.'
    @{ pull_request = 18; source_commit = $source; package = $package } | ConvertTo-Json | Set-Content (Join-Path $fixture preview.json)
    $unsafeArchive = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Update)
    try { $unsafeArchive.CreateEntry('../outside.txt') | Out-Null } finally { $unsafeArchive.Dispose() }
    $unsafeHash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    "$unsafeHash  AgricultureEstate-pr-18-preview.zip" | Set-Content "$zip.sha256"
    Assert-Rejected 'Unsafe archive path was accepted.'

    $global:PrereleaseTestState.State = 'closed'
    & $publisher -PullRequestNumber 18 -Cleanup *> $null
    Assert-True (-not $global:PrereleaseTestState.Release -and -not $global:PrereleaseTestState.Reference) 'Closed PR preview/tag was not removed.'
    & $publisher -PullRequestNumber 18 -RunId 123 -ArtifactDirectory $fixture *> $null
    Assert-True (-not $global:PrereleaseTestState.Release) 'Late build recreated a closed PR preview.'
    'Prerelease publisher: 11 offline scenarios passed.'
} finally {
    $env:GH_REPO = $oldRepo
    $env:RUNNER_TEMP = $oldTemp
    $env:GITHUB_STEP_SUMMARY = $oldSummary
    $global:LASTEXITCODE = $oldExitCode
    Remove-Variable PrereleaseTestState -Scope Global
}
