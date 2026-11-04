# Offline tests for branch synchronization. No GitHub requests are made.
$ErrorActionPreference = 'Stop'
$syncScript = Join-Path (Split-Path $PSScriptRoot) 'Sync-PrereleaseBranch.ps1'
$oldRepo = $env:GH_REPO
$oldSummary = $env:GITHUB_STEP_SUMMARY
$oldExitCode = $global:LASTEXITCODE
$env:GH_REPO = 'owner/mod'
$env:GITHUB_STEP_SUMMARY = ''

function Reset-State {
    $global:PrereleaseBranchTestState = @{
        State = 'open'; HeadRepo = 'owner/mod'; Base = 'master'; Branch = 'codex/bannerlord-test'
        Head = 'a' * 40; ContainsBase = $false; Requested = $false; Polls = 0
        Delay = 1; RejectUpdate = $false; CloseAfterUpdate = $false
        Calls = [Collections.Generic.List[object]]::new()
    }
}

function gh {
    $call = @($args)
    $state = $global:PrereleaseBranchTestState
    $state.Calls.Add($call)
    $global:LASTEXITCODE = 0
    if ($call[1] -eq '--method') {
        if ($state.RejectUpdate) { $global:LASTEXITCODE = 1; return 'HTTP 422: merge conflict or unexpected head SHA' }
        $state.Requested = $true
        return '{"message":"Updating pull request branch."}'
    }
    switch -Regex ($call[1]) {
        '^repos/owner/mod$' { return '{"default_branch":"master"}' }
        '/git/ref/heads/master$' { return (@{ object = @{ sha = ('b' * 40) } } | ConvertTo-Json) }
        '/pulls/18$' {
            if ($state.Requested) {
                $state.Polls++
                if ($state.Polls -gt $state.Delay) { $state.ContainsBase = $true; $state.Head = 'c' * 40 }
                if ($state.CloseAfterUpdate) { $state.State = 'closed' }
            }
            return (@{ state = $state.State; head = @{ sha = $state.Head; ref = $state.Branch; repo = @{ full_name = $state.HeadRepo } }; base = @{ ref = $state.Base } } | ConvertTo-Json -Depth 5)
        }
        '/compare/' {
            $status = if ($state.ContainsBase) { 'ahead' } else { 'diverged' }
            return (@{ status = $status } | ConvertTo-Json)
        }
        default { throw "Unexpected GitHub API call: $($call -join ' ')" }
    }
}

# Polling is exercised without wall-clock delays.
function Start-Sleep { param([int]$Seconds) }
function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Assert-Rejected {
    $rejected = $false
    try { & $syncScript -PullRequestNumber 18 *> $null } catch { $rejected = $true }
    Assert-True $rejected 'Unsafe or failed branch update was accepted.'
}

try {
    Reset-State
    $global:PrereleaseBranchTestState.ContainsBase = $true
    & $syncScript -PullRequestNumber 18 *> $null
    Assert-True (-not $global:PrereleaseBranchTestState.Requested) 'Up-to-date branch was mutated.'

    Reset-State
    & $syncScript -PullRequestNumber 18 *> $null
    $state = $global:PrereleaseBranchTestState
    Assert-True ($state.Requested -and $state.Polls -eq 2) 'Did not wait for the asynchronous merge.'
    $updates = @($state.Calls | Where-Object { $_[1] -eq '--method' })
    Assert-True ($updates.Count -eq 1) 'Expected exactly one branch update.'
    Assert-True (($updates[0] -join ' ') -eq "api --method PUT repos/owner/mod/pulls/18/update-branch -f expected_head_sha=$('a' * 40)") 'Update must guard the expected head SHA.'

    foreach ($case in @(
        @{ Key = 'State'; Value = 'closed' },
        @{ Key = 'HeadRepo'; Value = 'fork/mod' },
        @{ Key = 'Base'; Value = 'other' },
        @{ Key = 'Branch'; Value = 'feature/test' }
    )) {
        Reset-State
        $global:PrereleaseBranchTestState[$case.Key] = $case.Value
        Assert-Rejected
        Assert-True (-not $global:PrereleaseBranchTestState.Requested) 'Ineligible PR branch was mutated.'
    }

    Reset-State
    $global:PrereleaseBranchTestState.RejectUpdate = $true
    Assert-Rejected
    Assert-True ($global:PrereleaseBranchTestState.Polls -eq 0) 'Continued after GitHub rejected the merge.'

    Reset-State
    $global:PrereleaseBranchTestState.CloseAfterUpdate = $true
    Assert-Rejected

    Reset-State
    $global:PrereleaseBranchTestState.Delay = 100
    Assert-Rejected
    Assert-True ($global:PrereleaseBranchTestState.Polls -eq 30) 'Polling did not stop at the timeout.'

    'Prerelease branch synchronization: 9 offline scenarios passed.'
} finally {
    $env:GH_REPO = $oldRepo
    $env:GITHUB_STEP_SUMMARY = $oldSummary
    $global:LASTEXITCODE = $oldExitCode
    Remove-Variable PrereleaseBranchTestState -Scope Global
}
