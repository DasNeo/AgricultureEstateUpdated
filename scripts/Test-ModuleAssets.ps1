[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ModulePath
)
$ErrorActionPreference = 'Stop'

# Loose assets are also distributable, but the client needs their compiled cache.
# Validate the whole set together so an updated PNG cannot ship with an old cache.
$definitionPath = 'Assets/GauntletUI/ui_agriculture_estate_1_tex.tpac'
$atlasPath = 'AssetSources/GauntletUI/ui_agriculture_estate_1.png'
if ((Test-Path -LiteralPath (Join-Path $ModulePath 'Assets')) -and
    (Test-Path -LiteralPath (Join-Path $ModulePath 'AssetSources'))) {
    $manifestPath = Join-Path $ModulePath 'GUI/AgricultureEstateAssets.json'
    if (-not (Test-Path -LiteralPath $manifestPath)) { throw 'Missing loose UI asset manifest: GUI/AgricultureEstateAssets.json.' }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.schema -ne 1 -or $manifest.texture -ne 'ui_agriculture_estate_1') { throw 'Invalid loose UI asset manifest.' }
    $definition = [IO.File]::ReadAllBytes((Join-Path $ModulePath $definitionPath))
    if ($definition.Length -lt 68 -or [Text.Encoding]::ASCII.GetString($definition, 0, 4) -ne 'TPAC' -or
        [BitConverter]::ToUInt32($definition, 24) -ne 1) { throw 'Invalid loose UI texture definition.' }
    $packageGuid = [Guid]::new([byte[]]$definition[8..23])
    $cachePath = "RuntimeDataCache/$($packageGuid.ToString().ToUpperInvariant()).rdc"
    $requiredPaths = @($definitionPath, $atlasPath, $cachePath)
    if (@($manifest.files).Count -ne $requiredPaths.Count) { throw 'Incomplete loose UI asset manifest.' }
    foreach ($requiredPath in $requiredPaths) {
        $records = @($manifest.files | Where-Object { $_.path -ceq $requiredPath })
        if ($records.Count -ne 1) { throw "Missing or duplicate loose UI asset manifest entry: $requiredPath" }
        $assetPath = Join-Path $ModulePath $requiredPath
        if (-not (Test-Path -LiteralPath $assetPath -PathType Leaf)) { throw "Missing loose UI asset: $requiredPath" }
        if ((Get-FileHash -LiteralPath $assetPath -Algorithm SHA256).Hash -ine $records[0].sha256) {
            throw "Loose UI asset checksum mismatch: $requiredPath. Regenerate the texture/cache together and update GUI/AgricultureEstateAssets.json."
        }
    }
    $cache = [IO.File]::ReadAllBytes((Join-Path $ModulePath $cachePath))
    if ($cache.Length -lt 165 -or [Text.Encoding]::ASCII.GetString($cache, 0, 4) -ne 'RDC0' -or
        [BitConverter]::ToUInt32($cache, 8) -eq 0) { throw 'Invalid or empty loose UI runtime cache.' }
    $ownerGuid = [Guid]::new([byte[]]$definition[52..67])
    $cachedOwnerGuid = [Guid]::new([byte[]]$cache[20..35])
    $dataOffset = [BitConverter]::ToUInt64($cache, 68)
    $dataSize = [BitConverter]::ToUInt64($cache, 84)
    if ($ownerGuid -ne $cachedOwnerGuid -or $dataOffset -lt 165 -or $dataOffset -gt $cache.Length -or
        $dataSize -eq 0 -or $dataSize -gt ($cache.Length - $dataOffset)) { throw 'Loose UI runtime cache has a mismatched owner or missing texture payload.' }
    Write-Host 'Custom UI definition, source image, and matching runtime cache verified.'
    return
}

# Packed client exports carry their texture data inline instead.
$packagesPath = Join-Path $ModulePath 'AssetPackages'
$packages = @(Get-ChildItem -LiteralPath $packagesPath -Filter '*.tpac' -File -ErrorAction SilentlyContinue)
if ($packages.Count -eq 0) {
    throw 'Missing client asset packages. Publish AgricultureEstate with Client enabled in the Modding Kit, then copy its AssetPackages folder into AgricultureEstate/_Module. See docs/BUILDING.md.'
}

$textureName = 'ui_agriculture_estate_1'
$found = $false
foreach ($package in $packages) {
    $bytes = [IO.File]::ReadAllBytes($package.FullName)
    if ($bytes.Length -lt 4 -or [Text.Encoding]::ASCII.GetString($bytes, 0, 4) -ne 'TPAC') {
        throw "Invalid TPAC header: $($package.FullName)"
    }
    # This only establishes presence of a cooked package, not in-game compatibility.
    # Reject the tiny editable definition if it was copied here by mistake.
    if ($bytes.Length -gt 1024 -and [Text.Encoding]::ASCII.GetString($bytes).Contains($textureName)) {
        $found = $true
    }
}
if (-not $found) {
    throw "Client asset packages do not contain the $textureName texture with cooked data. Re-export AgricultureEstate for Client; do not move the editable Assets definition into AssetPackages."
}
Write-Host 'Custom UI client asset package is present.'
