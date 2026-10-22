[CmdletBinding()]
param([string]$Path = 'BannerlordVersion.props', [string]$OutputPath = 'artifacts/target.json')
$ErrorActionPreference = 'Stop'
[xml]$document = Get-Content -LiteralPath $Path -Raw
$node = $document.SelectSingleNode('/Project/PropertyGroup/BannerlordVersion')
$package = if ($node) { $node.InnerText } else { '' }
if ($package -notmatch '^\d+\.\d+\.\d+\.\d+(-beta)?$') { throw "Invalid pinned Bannerlord package in $Path." }
$target = [ordered]@{
    package = $package
    game = (($package -replace '-beta$', '').Split('.')[0..2] -join '.')
    beta = $package.EndsWith('-beta')
}
$parent = Split-Path $OutputPath
if ($parent) { New-Item -ItemType Directory -Force $parent | Out-Null }
$target | ConvertTo-Json | Set-Content -Encoding utf8 $OutputPath
if ($env:GITHUB_OUTPUT) {
    "package=$package" >> $env:GITHUB_OUTPUT
    "beta=$($target.beta.ToString().ToLowerInvariant())" >> $env:GITHUB_OUTPUT
}
Write-Host "Pinned Bannerlord reference assemblies: $package"
