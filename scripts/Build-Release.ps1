[CmdletBinding()]
param([Parameter(Mandatory)][string]$BannerlordVersion, [switch]$Package)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
Push-Location $root
try {
    New-Item -ItemType Directory -Force artifacts | Out-Null
    & dotnet build AgricultureEstate.sln -c Release "-p:BannerlordVersion=$BannerlordVersion" -p:Platform=x64 -p:DeployToGame=false 2>&1 |
        Tee-Object artifacts/build.log | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Build failed; see artifacts/build.log.' }
    & dotnet test AgricultureEstate.Tests/AgricultureEstate.Tests.csproj -c Release --no-build --logger 'trx;LogFileName=tests.trx' --results-directory artifacts/tests 2>&1 |
        Tee-Object artifacts/tests.log | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed; see artifacts/tests.log.' }
    if (-not $Package) { return }
    # Release jobs build the reviewed merge commit in a fresh checkout.
    $staging = Join-Path $root 'artifacts/package'
    if (Test-Path $staging) { throw 'Packaging requires a fresh artifacts/package directory.' }
    $module = Join-Path $staging 'Modules/AgricultureEstate'
    New-Item -ItemType Directory -Force $module | Out-Null
    Copy-Item AgricultureEstate/_Module/* $module -Recurse
    $bin = Join-Path $module 'bin/Win64_Shipping_Client'
    New-Item -ItemType Directory -Force $bin | Out-Null
    foreach ($assembly in 'AgricultureEstate', 'AgricultureEstate.Application', 'AgricultureEstate.Domain') {
        $built = @(Get-ChildItem "${assembly}/bin" -Filter "$assembly.dll" -Recurse | Where-Object { $_.FullName -match '[\\/]Release[\\/]net48[\\/]' })
        if ($built.Count -ne 1) { throw "Expected exactly one Release/net48 output for $assembly, found $($built.Count)." }
        Copy-Item $built[0].FullName $bin
        $symbols = [IO.Path]::ChangeExtension($built[0].FullName, '.pdb')
        if (Test-Path $symbols) { Copy-Item $symbols $bin }
    }
    # Only our three assemblies are distributed. The game supplies its own DLLs;
    # users install Harmony and MCM as separate mods.
    Copy-Item LICENSE $module
    $target = Get-Content artifacts/target.json -Raw | ConvertFrom-Json
    if ($target.package -ne $BannerlordVersion) { throw 'Resolved target does not match build target.' }
    $run = if ($env:GITHUB_RUN_NUMBER) { $env:GITHUB_RUN_NUMBER } else { '0' }
    $attempt = if ($env:GITHUB_RUN_ATTEMPT) { $env:GITHUB_RUN_ATTEMPT } else { '1' }
    $tag = "bannerlord-$BannerlordVersion-run-$run-$attempt"
    $name = "AgricultureEstate-$tag.zip"
    Copy-Item artifacts/target.json (Join-Path $module 'build-target.json')
    Compress-Archive -Path "$staging/Modules" -DestinationPath "artifacts/$name"
    $hash = (Get-FileHash "artifacts/$name" -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $name" | Set-Content -Encoding utf8 "artifacts/$name.sha256"
    $sourceCommit = & git rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Could not determine release source commit.' }
    @"
AgricultureEstate for Bannerlord $($target.game)

Reference package: $BannerlordVersion
Source commit: $sourceCommit
Validation: Release build and automated domain/application tests passed.
Source changes were reviewed and merged through a version-update pull request.
Gameplay and save compatibility have not been tested by this workflow.

Installation: extract the ZIP into your Bannerlord game folder so Modules/AgricultureEstate exists.
Install Bannerlord.Harmony and Bannerlord.MBOptionScreen (MCM v5) separately.
"@ | Set-Content -Encoding utf8 artifacts/release-notes.md
    if ($env:GITHUB_OUTPUT) {
        "tag=$tag" >> $env:GITHUB_OUTPUT
        "zip=$name" >> $env:GITHUB_OUTPUT
    }
} finally { Pop-Location }
