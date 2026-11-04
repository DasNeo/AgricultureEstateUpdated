# Offline regression checks for release asset validation; no editor is required.
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot)
$fixture = Join-Path $root "artifacts/module-assets-tests-$([guid]::NewGuid().ToString('N'))"
$packages = Join-Path $fixture 'AssetPackages'
New-Item -ItemType Directory -Path $packages -Force | Out-Null
$validator = Join-Path $root 'scripts/Test-ModuleAssets.ps1'

function Assert-Rejected([string]$expectedMessage) {
    $message = ''
    try { & $validator -ModulePath $fixture } catch { $message = $_.Exception.Message }
    if ($message -notlike $expectedMessage) { throw "Expected '$expectedMessage', received '$message'." }
}

Assert-Rejected 'Missing client asset packages*'

# Moving the source definition into AssetPackages must not pass validation.
Copy-Item -LiteralPath (Join-Path $root 'AgricultureEstate/_Module/Assets/GauntletUI/ui_agriculture_estate_1_tex.tpac') -Destination (Join-Path $packages 'definition.tpac')
Assert-Rejected 'Client asset packages do not contain*'

$badHeader = Join-Path $packages 'invalid.tpac'
[IO.File]::WriteAllBytes($badHeader, [Text.Encoding]::ASCII.GetBytes('invalid'))
Assert-Rejected 'Invalid TPAC header*'

# The distributable loose set must work without any exported AssetPackages.
$moduleRoot = Join-Path $root 'AgricultureEstate/_Module'
$manifestPath = 'GUI/AgricultureEstateAssets.json'
$manifest = Get-Content -LiteralPath (Join-Path $moduleRoot $manifestPath) -Raw | ConvertFrom-Json
foreach ($relativePath in @($manifestPath) + @($manifest.files | ForEach-Object path)) {
    $destination = Join-Path $fixture $relativePath
    New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $moduleRoot $relativePath) -Destination $destination
}
& $validator -ModulePath $fixture

$cacheRelativePath = ($manifest.files | Where-Object path -like 'RuntimeDataCache/*').path
$cacheFile = Join-Path $fixture $cacheRelativePath
$originalCacheBytes = [IO.File]::ReadAllBytes($cacheFile)
$corruptedCacheBytes = [byte[]]$originalCacheBytes.Clone()
$corruptedCacheBytes[$corruptedCacheBytes.Length - 1] = $corruptedCacheBytes[$corruptedCacheBytes.Length - 1] -bxor 1
[IO.File]::WriteAllBytes($cacheFile, $corruptedCacheBytes)
Assert-Rejected 'Loose UI asset checksum mismatch*'
[IO.File]::WriteAllBytes($cacheFile, $originalCacheBytes)
Move-Item -LiteralPath $cacheFile -Destination "$cacheFile.missing-test"
Assert-Rejected 'Missing loose UI asset*'

Write-Host 'Passed: complete loose assets are accepted; missing or corrupt caches and incomplete client packages are rejected.'
