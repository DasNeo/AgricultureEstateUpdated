[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateRange(1, 2147483647)][int]$PullRequestNumber
)
$ErrorActionPreference = 'Stop'
if ($env:GH_REPO -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') { throw 'GH_REPO is required.' }
$repo = $env:GH_REPO

function Invoke-GhJson {
    param([Parameter(Mandatory)][string[]]$Arguments)
    $output = & gh @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Could not update the PR from the default branch. Resolve any merge conflicts or branch restrictions and retry. GitHub: $($output -join "`n")" }
    return ($output -join "`n" | ConvertFrom-Json)
}

function Get-VersionPr {
    $pr = Invoke-GhJson -Arguments @('api', "repos/$repo/pulls/$PullRequestNumber")
    if ($pr.state -ne 'open' -or $pr.head.repo.full_name -ne $repo -or
        $pr.base.ref -ne $defaultBranch -or -not $pr.head.ref.StartsWith('codex/bannerlord-')) {
        throw 'Branch updates require an open version-update PR from this repository targeting the default branch.'
    }
    return $pr
}

function Test-ContainsBase {
    param([string]$HeadCommit)
    $comparison = Invoke-GhJson -Arguments @('api', "repos/$repo/compare/${baseCommit}...${HeadCommit}")
    return $comparison.status -in @('ahead', 'identical')
}

$repository = Invoke-GhJson -Arguments @('api', "repos/$repo")
$defaultBranch = $repository.default_branch
$pr = Get-VersionPr
$baseRef = [Uri]::EscapeDataString($defaultBranch)
$base = Invoke-GhJson -Arguments @('api', "repos/$repo/git/ref/heads/$baseRef")
$baseCommit = $base.object.sha
if (Test-ContainsBase -HeadCommit $pr.head.sha) {
    Write-Host "PR #$PullRequestNumber already includes $defaultBranch ($baseCommit)."
    return
}

# GitHub merges the current base into the PR without checking out or executing PR code.
# The expected head prevents overwriting a concurrent PR update.
Invoke-GhJson -Arguments @('api', '--method', 'PUT', "repos/$repo/pulls/$PullRequestNumber/update-branch",
    '-f', "expected_head_sha=$($pr.head.sha)") | Out-Null

# update-branch is asynchronous. Do not build until the updated PR includes the base
# observed above. The subsequent read-only build resolves the current PR head again.
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    $latest = Get-VersionPr
    if (Test-ContainsBase -HeadCommit $latest.head.sha) {
        Write-Host "Updated PR #$PullRequestNumber from $defaultBranch; ready to build $($latest.head.sha)."
        if ($env:GITHUB_STEP_SUMMARY) {
            "Updated PR #$PullRequestNumber from $defaultBranch (base $baseCommit, PR commit $($latest.head.sha))." >> $env:GITHUB_STEP_SUMMARY
        }
        return
    }
    Start-Sleep -Seconds 2
}
throw 'Timed out waiting for the PR branch update. No preview will be built; check the PR and retry.'
