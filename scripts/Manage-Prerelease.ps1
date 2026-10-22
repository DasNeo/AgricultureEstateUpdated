[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateRange(1, 2147483647)][int]$PullRequestNumber,
    [long]$RunId = 0,
    [string]$ArtifactDirectory = 'preview',
    [switch]$Cleanup
)
$ErrorActionPreference = 'Stop'
if ($env:GH_REPO -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') { throw 'GH_REPO is required.' }
$repo = $env:GH_REPO
$tag = "pr-$PullRequestNumber-preview"

function Invoke-Gh {
    param([Parameter(Mandatory)][string[]]$Arguments)
    $output = & gh @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "GitHub command failed: $($output -join "`n")" }
    return ($output -join "`n")
}

function Get-GhObject {
    param([string]$Endpoint, [switch]$AllowMissing)
    $output = & gh api $Endpoint 2>&1
    if ($LASTEXITCODE -ne 0) {
        if ($AllowMissing -and ($output -join "`n") -match 'HTTP 404') { return $null }
        throw "GitHub API request failed: $($output -join "`n")"
    }
    return ($output -join "`n" | ConvertFrom-Json)
}

$pr = Get-GhObject "repos/$repo/pulls/$PullRequestNumber"
$repository = Get-GhObject "repos/$repo"
if ($pr.head.repo.full_name -ne $repo -or $pr.base.ref -ne $repository.default_branch -or
    -not $pr.head.ref.StartsWith('codex/bannerlord-')) { throw 'Not a version-update PR from this repository.' }

$release = Get-GhObject "repos/$repo/releases/tags/$tag" -AllowMissing
if ($release -and (-not $release.prerelease -or $release.immutable)) { throw 'Refusing to modify a non-preview or immutable release.' }
if ($Cleanup -or $pr.state -ne 'open') {
    # Recheck live state inside the shared per-PR concurrency group.
    if ($pr.state -eq 'open') { throw 'Refusing to clean up an open PR.' }
    if ($release) {
        Invoke-Gh -Arguments @('release', 'delete', $tag, '--repo', $repo, '--yes', '--cleanup-tag') | Out-Host
    } else {
        $reference = Get-GhObject "repos/$repo/git/ref/tags/$tag" -AllowMissing
        if ($reference) { Invoke-Gh -Arguments @('api', '--method', 'DELETE', "repos/$repo/git/refs/tags/$tag") | Out-Host }
    }
    Write-Host "Removed preview for closed PR #$PullRequestNumber (if present)."
    return
}

if ($RunId -lt 1) { throw 'A completed source workflow run is required.' }
$run = Get-GhObject "repos/$repo/actions/runs/$RunId"
$allowedPaths = @('.github/workflows/bannerlord-update.yml', '.github/workflows/bannerlord-check.yml')
if ($run.conclusion -ne 'success' -or $run.head_repository.full_name -ne $repo -or $run.path -notin $allowedPaths) {
    throw 'Preview artifacts must come from a successful compatibility workflow in this repository.'
}
$metadata = Get-Content -LiteralPath (Join-Path $ArtifactDirectory 'preview.json') -Raw | ConvertFrom-Json
$source = [string]$metadata.source_commit
$package = [string]$metadata.package
if ($metadata.pull_request -ne $PullRequestNumber -or $source -notmatch '^[a-f0-9]{40}$' -or
    $package -notmatch '^\d+\.\d+\.\d+\.\d+(-beta)?$') { throw 'Invalid preview metadata.' }
if ($pr.head.sha -ne $source) {
    Write-Host 'Skipping an outdated build; the PR has newer commits.'
    return
}
if ($run.event -eq 'pull_request') {
    if ($run.head_sha -ne $source -or $run.head_branch -ne $pr.head.ref) { throw 'Workflow source does not match this PR.' }
} elseif ($run.event -eq 'workflow_dispatch') {
    if ($run.head_branch -ne $repository.default_branch) { throw 'Manual builds must originate on the default branch.' }
    if ($run.path -eq '.github/workflows/bannerlord-update.yml' -and
        $pr.head.ref -notmatch "-$RunId-\d+$") { throw 'This PR was not created by the source preparation run.' }
} else { throw 'Unsupported source workflow event.' }

$files = Invoke-Gh -Arguments @('api', '--paginate', "repos/$repo/pulls/$PullRequestNumber/files", '--jq', '.[].filename')
if ('BannerlordVersion.props' -notin ($files -split "`n")) { throw 'PR does not update the pinned reference version.' }
$pin = Get-GhObject "repos/$repo/contents/BannerlordVersion.props?ref=$source"
[xml]$xml = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($pin.content))
$versionNode = $xml.SelectSingleNode('/Project/PropertyGroup/BannerlordVersion')
if (-not $versionNode -or $versionNode.InnerText -ne $package) { throw 'Artifact target does not match the PR source.' }

$zipName = "AgricultureEstate-$tag.zip"
$zipPath = Join-Path $ArtifactDirectory $zipName
$checksumPath = "$zipPath.sha256"
$actualHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$checksum = (Get-Content -LiteralPath $checksumPath -Raw).Trim()
if ($checksum -ne "$actualHash  $zipName") { throw 'Preview checksum mismatch.' }
# Inspect archive paths as data. Never extract or execute PR-controlled artifacts here.
$archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $zipPath).Path)
try {
    $dlls = @()
    foreach ($entry in $archive.Entries) {
        if (-not $entry.FullName.StartsWith('Modules/AgricultureEstate/') -or
            $entry.FullName -match '(^|[/\\])\.\.([/\\]|$)|\\|:') { throw "Unsafe archive path: $($entry.FullName)" }
        if ($entry.FullName.EndsWith('.dll', [StringComparison]::OrdinalIgnoreCase)) { $dlls += $entry.FullName }
    }
    $expectedDlls = @('AgricultureEstate', 'AgricultureEstate.Application', 'AgricultureEstate.Domain') |
        ForEach-Object { "Modules/AgricultureEstate/bin/Win64_Shipping_Client/$_.dll" }
    if (@(Compare-Object ($dlls | Sort-Object) ($expectedDlls | Sort-Object)).Count -ne 0) { throw 'Unexpected preview assemblies.' }
} finally { $archive.Dispose() }
# A late build must not recreate a preview after the PR closes or moves forward.
$latest = Get-GhObject "repos/$repo/pulls/$PullRequestNumber"
if ($latest.state -ne 'open' -or $latest.head.sha -ne $source) { Write-Host 'PR changed during validation; skipping publication.'; return }
$notes = @"
Unmerged test build for PR #${PullRequestNumber}: $($pr.html_url)

Bannerlord reference package: $package
Built PR commit: $source
Build and automated tests passed: $($run.html_url)

Gameplay, DLC, Harmony patches, and save compatibility still need in-game testing.
This is the latest successful build of this PR, not necessarily its latest commit.
Successful builds replace these downloads. Failed builds keep the previous successful preview.
"@
$notesPath = Join-Path $env:RUNNER_TEMP "pr-$PullRequestNumber-preview-notes.md"
$notes | Set-Content -Encoding utf8 $notesPath
$reference = Get-GhObject "repos/$repo/git/ref/tags/$tag" -AllowMissing
if ($reference) {
    Invoke-Gh -Arguments @('api', '--method', 'PATCH', "repos/$repo/git/refs/tags/$tag", '-f', "sha=$source", '-F', 'force=true') | Out-Host
} else {
    Invoke-Gh -Arguments @('api', '--method', 'POST', "repos/$repo/git/refs", '-f', "ref=refs/tags/$tag", '-f', "sha=$source") | Out-Host
}
$title = "PR #$PullRequestNumber preview - Bannerlord $package"
if (-not $release) {
    Invoke-Gh -Arguments @('release', 'create', $tag, '--repo', $repo, '--verify-tag', '--draft', '--prerelease', '--latest=false', '--title', $title, '--notes-file', $notesPath) | Out-Host
}
Invoke-Gh -Arguments @('release', 'upload', $tag, $zipPath, $checksumPath, '--repo', $repo, '--clobber') | Out-Host
Invoke-Gh -Arguments @('release', 'edit', $tag, '--repo', $repo, '--draft=false', '--prerelease', '--latest=false', '--title', $title, '--notes-file', $notesPath) | Out-Host
$url = "https://github.com/$repo/releases/tag/$tag"
Write-Host "Preview: $url"
if ($env:GITHUB_STEP_SUMMARY) { "PR prerelease: $url (commit $source)" >> $env:GITHUB_STEP_SUMMARY }
