[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Version,
    [string[]]$AvailableVersions,
    [string]$OutputPath = 'artifacts/target.json'
)
$ErrorActionPreference = 'Stop'
$requested = $Version.Trim().ToLowerInvariant() -replace '^v', ''
if ($requested -notmatch '^\d+\.\d+\.\d+(\.\d+)?(-beta)?$') {
    throw 'Enter a game version (1.2.12), exact package version (1.2.12.12345), or a beta version (1.5.4-beta).'
}
if (-not $AvailableVersions) {
    $AvailableVersions = (Invoke-RestMethod 'https://api.nuget.org/v3-flatcontainer/bannerlord.referenceassemblies/index.json').versions
}
$beta = $requested.EndsWith('-beta')
$numeric = $requested -replace '-beta$', ''
$exact = $numeric.Split('.').Count -eq 4
$matches = @($AvailableVersions | Where-Object {
    ($_ -match '^\d+\.\d+\.\d+\.\d+(-beta)?$') -and
    ($_.EndsWith('-beta') -eq $beta) -and
    $(if ($exact) { $_ -eq $requested } else { ($_ -replace '-beta$', '').StartsWith("$numeric.") })
} | Sort-Object { [version]($_ -replace '-beta$', '') } -Descending)
if ($matches.Count -eq 0) { throw "No published Bannerlord reference assemblies match '$Version'. No release can be built until the package exists." }
$package = $matches[0]
$target = [ordered]@{ requested = $requested; package = $package; game = (($package -replace '-beta$', '').Split('.')[0..2] -join '.'); beta = $beta }
$parent = Split-Path $OutputPath
if ($parent) { New-Item -ItemType Directory -Force $parent | Out-Null }
$target | ConvertTo-Json | Set-Content -Encoding utf8 $OutputPath
if ($env:GITHUB_OUTPUT) {
    "package=$package" >> $env:GITHUB_OUTPUT
    "beta=$($beta.ToString().ToLowerInvariant())" >> $env:GITHUB_OUTPUT
}
Write-Host "Selected Bannerlord reference assemblies $package"
