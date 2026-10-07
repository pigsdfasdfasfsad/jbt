param(
  [switch]$BootstrapToolchain,
  [switch]$SkipTests
)
$ErrorActionPreference = 'Stop'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$GodotVersion = '4.7.2-stable'
$GodotDisplay = '4.7.2.stable.mono'
$DotnetVersion = '8.0.425'
$Tools = Join-Path $Repo '.tools\godot'
$Output = Join-Path $Repo 'build\output'
$Project = Join-Path $Repo 'src\Twr.Godot'
$Solution = Join-Path $Project 'Those Who Remain Offline.sln'

function Find-Godot {
  $candidates = @(
    (Join-Path $Tools 'Godot_v4.7.2-stable_mono_win64_console.exe'),
    (Join-Path $Tools 'Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'),
    (Join-Path $Tools 'Godot_v4.7.2-stable_mono_win64.exe'),
    (Join-Path $Tools 'Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe')
  )
  foreach ($p in $candidates) { if (Test-Path $p) { return $p } }
  $found = Get-ChildItem -Path $Tools -Filter 'Godot*_mono_win64_console.exe' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
  if (-not $found) { $found = Get-ChildItem -Path $Tools -Filter 'Godot*_mono_win64.exe' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1 }
  if ($found) { return $found.FullName }
  return $null
}

if ($BootstrapToolchain) {
  New-Item -ItemType Directory -Force -Path $Tools | Out-Null
  $godot = Find-Godot
  if (-not $godot) {
    $zip = Join-Path $Tools 'godot.zip'
    $url = "https://github.com/godotengine/godot/releases/download/$GodotVersion/Godot_v4.7.2-stable_mono_win64.zip"
    Invoke-WebRequest -Uri $url -OutFile $zip
    Expand-Archive -Path $zip -DestinationPath $Tools -Force
    Remove-Item $zip -Force
  }
  $templateRoot = Join-Path $env:APPDATA "Godot\export_templates\$GodotDisplay"
  if (!(Test-Path $templateRoot)) {
    $tpz = Join-Path $Tools 'templates.tpz'
    $url = "https://github.com/godotengine/godot/releases/download/$GodotVersion/Godot_v4.7.2-stable_mono_export_templates.tpz"
    Invoke-WebRequest -Uri $url -OutFile $tpz
    $tmp = Join-Path $Tools 'templates-unpack'
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $tmp | Out-Null
    Copy-Item $tpz (Join-Path $Tools 'templates.zip') -Force
    Expand-Archive -Path (Join-Path $Tools 'templates.zip') -DestinationPath $tmp -Force
    New-Item -ItemType Directory -Force -Path $templateRoot | Out-Null
    $inner = Get-ChildItem $tmp -Directory | Select-Object -First 1
    if ($inner) { Copy-Item (Join-Path $inner.FullName '*') $templateRoot -Recurse -Force } else { Copy-Item (Join-Path $tmp '*') $templateRoot -Recurse -Force }
  }
}

$godot = Find-Godot
if (-not $godot) { throw 'Pinned Godot 4.7.2 .NET executable not found. Use -BootstrapToolchain.' }
if (!(Test-Path $Solution)) { throw "Godot C# solution missing: $Solution" }
if ((dotnet --version) -ne $DotnetVersion) { Write-Warning "Expected .NET $DotnetVersion; found $(dotnet --version). CI pins the expected SDK." }
if (-not $SkipTests) { python (Join-Path $Repo 'tools\validation\run_all.py') }
New-Item -ItemType Directory -Force -Path $Output | Out-Null
Push-Location $Project
try {
  dotnet build $Solution -c Release

  $importOutput = & $godot --headless --verbose --path $Project --editor --quit 2>&1
  $importCode = $LASTEXITCODE
  $importOutput | ForEach-Object { Write-Host $_ }
  if ($importCode -ne 0) { throw "Godot import/editor pass failed with exit code $importCode" }
  if ($importOutput | Where-Object { "$_" -match '(^|\\s)ERROR:' }) { throw 'Godot import/editor pass reported ERROR output.' }

  $exportOutput = & $godot --headless --verbose --path $Project --export-release 'Windows Desktop' (Join-Path $Output 'ThoseWhoRemainOffline.exe') 2>&1
  $exportCode = $LASTEXITCODE
  $exportOutput | ForEach-Object { Write-Host $_ }
  if ($exportCode -ne 0) { throw "Godot Windows export failed with exit code $exportCode" }
  if ($exportOutput | Where-Object { "$_" -match '(^|\\s)ERROR:' }) { throw 'Godot Windows export reported ERROR output.' }

  # Launch the actual main scene and enter the playable Manor slice. This catches
  # startup/_Ready/runtime faults that import and export alone cannot detect.
  $smokeOutput = & $godot --headless --verbose --path $Project --quit-after 120 -- --smoke-play 2>&1
  $smokeCode = $LASTEXITCODE
  $smokeOutput | ForEach-Object { Write-Host $_ }
  if ($smokeCode -ne 0) { throw "Godot playable smoke pass failed with exit code $smokeCode" }
  if ($smokeOutput | Where-Object { "$_" -match '(^|\\s)ERROR:' }) { throw 'Godot playable smoke pass reported ERROR output.' }
} finally { Pop-Location }
if (!(Test-Path (Join-Path $Output 'ThoseWhoRemainOffline.exe'))) { throw 'Godot export did not produce ThoseWhoRemainOffline.exe' }
Write-Host 'Windows export completed.'
