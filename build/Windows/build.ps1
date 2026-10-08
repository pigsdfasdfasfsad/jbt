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
  if ($importOutput | Where-Object { "$_" -match '(^|\s)ERROR:' }) { throw 'Godot import/editor pass reported ERROR output.' }

  $exportExe = Join-Path $Output 'ThoseWhoRemainOffline.exe'
  $exportOutput = & $godot --headless --verbose --path $Project --export-release 'Windows Desktop' $exportExe 2>&1
  $exportCode = $LASTEXITCODE
  $exportOutput | ForEach-Object { Write-Host $_ }
  if ($exportCode -ne 0) { throw "Godot Windows export failed with exit code $exportCode" }
  if ($exportOutput | Where-Object { "$_" -match '(^|\s)ERROR:' }) { throw 'Godot Windows export reported ERROR output.' }
  if (!(Test-Path $exportExe)) { throw 'Godot export did not produce ThoseWhoRemainOffline.exe' }

  # Smoke-test the exported artifact itself. A normal source-tree launch looks
  # for the editor Debug assembly, while this workflow intentionally builds Release.
  $smokeStdout = Join-Path $Output 'smoke.stdout.txt'
  $smokeStderr = Join-Path $Output 'smoke.stderr.txt'
  Remove-Item $smokeStdout,$smokeStderr -Force -ErrorAction SilentlyContinue
  $smoke = Start-Process -FilePath $exportExe -ArgumentList @('--headless','--verbose','--quit-after','120','--','--smoke-play') -Wait -PassThru -NoNewWindow -RedirectStandardOutput $smokeStdout -RedirectStandardError $smokeStderr

  $smokeOutput = @()
  if (Test-Path $smokeStdout) { $smokeOutput += Get-Content $smokeStdout }
  if (Test-Path $smokeStderr) { $smokeOutput += Get-Content $smokeStderr }
  $smokeOutput | ForEach-Object { Write-Host $_ }
  if ($smoke.ExitCode -ne 0) { throw "Exported playable smoke pass failed with exit code $($smoke.ExitCode)" }
  if ($smokeOutput | Where-Object { "$_" -match '(^|\s)ERROR:' }) { throw 'Exported playable smoke pass reported ERROR output.' }

  # Full release contract smoke: drive every release map through all 15 domain
  # waves inside the exported executable and require committed completion state.
  $fullStdout = Join-Path $Output 'completion-smoke.stdout.txt'
  $fullStderr = Join-Path $Output 'completion-smoke.stderr.txt'
  Remove-Item $fullStdout,$fullStderr -Force -ErrorAction SilentlyContinue
  $full = Start-Process -FilePath $exportExe -ArgumentList @('--headless','--verbose','--quit-after','600','--','--smoke-complete') -Wait -PassThru -NoNewWindow -RedirectStandardOutput $fullStdout -RedirectStandardError $fullStderr

  $fullOutput = @()
  if (Test-Path $fullStdout) { $fullOutput += Get-Content $fullStdout }
  if (Test-Path $fullStderr) { $fullOutput += Get-Content $fullStderr }
  $fullOutput | ForEach-Object { Write-Host $_ }
  if ($full.ExitCode -ne 0) { throw "Exported 15-wave completion smoke failed with exit code $($full.ExitCode)" }
  if ($fullOutput | Where-Object { "$_" -match '(^|\s)ERROR:' }) { throw 'Exported 15-wave completion smoke reported ERROR output.' }
  if (-not ($fullOutput | Where-Object { "$_" -match 'TWR_SMOKE_COMPLETE_OK maps=10 waves=150' })) {
    throw 'Exported 15-wave completion smoke did not report the required completion marker.'
  }
  # Instantiate representative categories of offline first-person weapon models.
  $weaponStdout = Join-Path $Output 'weapon-models.stdout.txt'
  $weaponStderr = Join-Path $Output 'weapon-models.stderr.txt'
  Remove-Item $weaponStdout,$weaponStderr -Force -ErrorAction SilentlyContinue
  $weaponModels = Start-Process -FilePath $exportExe -ArgumentList @('--headless','--verbose','--quit-after','120','--','--smoke-weapon-models') -Wait -PassThru -NoNewWindow -RedirectStandardOutput $weaponStdout -RedirectStandardError $weaponStderr
  $weaponOutput = @()
  if (Test-Path $weaponStdout) { $weaponOutput += Get-Content $weaponStdout }
  if (Test-Path $weaponStderr) { $weaponOutput += Get-Content $weaponStderr }
  $weaponOutput | ForEach-Object { Write-Host $_ }
  if ($weaponModels.ExitCode -ne 0) { throw "Weapon visual smoke failed with exit code $($weaponModels.ExitCode)" }
  if (-not ($weaponOutput | Where-Object { "$_" -match 'TWR_SMOKE_WEAPON_VISUALS_OK categories=6 throwables=1' })) {
    throw 'Weapon visual smoke did not verify representative models.'
  }
  if (-not ($weaponOutput | Where-Object { "$_" -match 'TWR_SMOKE_FIDELITY_METADATA_OK' })) {
    throw 'Fidelity camera metadata serialization did not pass.'
  }

  # Instantiate all eight offline infected presentation types in exported Godot.
  $modelStdout = Join-Path $Output 'infected-models.stdout.txt'
  $modelStderr = Join-Path $Output 'infected-models.stderr.txt'
  Remove-Item $modelStdout,$modelStderr -Force -ErrorAction SilentlyContinue
  $models = Start-Process -FilePath $exportExe -ArgumentList @('--headless','--verbose','--quit-after','120','--','--smoke-infected-models') -Wait -PassThru -NoNewWindow -RedirectStandardOutput $modelStdout -RedirectStandardError $modelStderr
  $modelOutput = @()
  if (Test-Path $modelStdout) { $modelOutput += Get-Content $modelStdout }
  if (Test-Path $modelStderr) { $modelOutput += Get-Content $modelStderr }
  $modelOutput | ForEach-Object { Write-Host $_ }
  if ($models.ExitCode -ne 0) { throw "Infected visual smoke failed with exit code $($models.ExitCode)" }
  if (-not ($modelOutput | Where-Object { "$_" -match 'TWR_SMOKE_INFECTED_VISUALS_OK types=8' })) {
    throw 'Infected visual smoke did not report all eight assembly types.'
  }

  # Check offline source-derived R6 zombie model assembly using a generated
  # six-type fixture. Never publish private original zombie model bytes.
  $privateZombieFixture = Join-Path $Output 'Content\Enemies\InfectedSourceVariants.json.gz'
  try {
    python (Join-Path $Repo 'tools\maps\make_source_infected_smoke_fixture.py') --output $privateZombieFixture
    $sourceZombieStdout = Join-Path $Output 'source-infected.stdout.txt'
    $sourceZombieStderr = Join-Path $Output 'source-infected.stderr.txt'
    Remove-Item $sourceZombieStdout,$sourceZombieStderr -Force -ErrorAction SilentlyContinue
    $sourceZombie = Start-Process -FilePath $exportExe -ArgumentList @('--headless','--verbose','--quit-after','120','--','--smoke-source-infected') -Wait -PassThru -NoNewWindow -RedirectStandardOutput $sourceZombieStdout -RedirectStandardError $sourceZombieStderr
    $sourceZombieOutput = @()
    if (Test-Path $sourceZombieStdout) { $sourceZombieOutput += Get-Content $sourceZombieStdout }
    if (Test-Path $sourceZombieStderr) { $sourceZombieOutput += Get-Content $sourceZombieStderr }
    $sourceZombieOutput | ForEach-Object { Write-Host $_ }
    if ($sourceZombie.ExitCode -ne 0) { throw "Source-infected smoke failed: exit $($sourceZombie.ExitCode)" }
    if (-not ($sourceZombieOutput | Where-Object { "$_" -match 'TWR_SMOKE_SOURCE_INFECTED_OK types=6' })) {
      throw 'Source-derived original infected body assembly smoke failed.'
    }
  } finally {
    Remove-Item $privateZombieFixture -Force -ErrorAction SilentlyContinue
  }

  # Require the exported Windows executable to construct the entire
  # source-guided Expressway environment without runtime script errors.
  $roadStdout = Join-Path $Output 'expressway-scene.stdout.txt'
  $roadStderr = Join-Path $Output 'expressway-scene.stderr.txt'
  Remove-Item $roadStdout,$roadStderr -Force -ErrorAction SilentlyContinue
  $road = Start-Process -FilePath $exportExe -ArgumentList @('--headless','--verbose','--quit-after','180','--','--smoke-expressway-scene') -Wait -PassThru -NoNewWindow -RedirectStandardOutput $roadStdout -RedirectStandardError $roadStderr
  $roadOutput = @()
  if (Test-Path $roadStdout) { $roadOutput += Get-Content $roadStdout }
  if (Test-Path $roadStderr) { $roadOutput += Get-Content $roadStderr }
  $roadOutput | ForEach-Object { Write-Host $_ }
  if ($road.ExitCode -ne 0) { throw "Expressway scene smoke failed with exit code $($road.ExitCode)" }
  if ($roadOutput | Where-Object { "$_" -match '(^|\s)ERROR:' }) {
    throw 'Expressway scene smoke reported ERROR output.'
  }
  if (-not ($roadOutput | Where-Object { "$_" -match 'TWR_SMOKE_EXPRESSWAY_SCENE_OK' })) {
    throw 'Expressway scene smoke did not report required reconstruction marker.'
  }

  # The real source pack is owner-only and must never be uploaded to GitHub.
  # Exercise the exported loader with a clearly marked synthetic CI fixture.
  $fixture = Join-Path $Output 'Content\Maps\Laboratory.scene.jsonl.gz'
  $fixtureStdout = Join-Path $Output 'lab-source-smoke.stdout.txt'
  $fixtureStderr = Join-Path $Output 'lab-source-smoke.stderr.txt'
  try {
    python (Join-Path $Repo 'tools\maps\make_lab_smoke_fixture.py') --output $fixture
    Remove-Item $fixtureStdout,$fixtureStderr -Force -ErrorAction SilentlyContinue
    $lab = Start-Process -FilePath $exportExe -ArgumentList @('--headless','--verbose','--quit-after','240','--','--smoke-lab-source') -Wait -PassThru -NoNewWindow -RedirectStandardOutput $fixtureStdout -RedirectStandardError $fixtureStderr
    $labOutput = @()
    if (Test-Path $fixtureStdout) { $labOutput += Get-Content $fixtureStdout }
    if (Test-Path $fixtureStderr) { $labOutput += Get-Content $fixtureStderr }
    $labOutput | ForEach-Object { Write-Host $_ }
    if ($lab.ExitCode -ne 0) { throw "Synthetic Laboratory source-load smoke failed with exit code $($lab.ExitCode)" }
    if ($labOutput | Where-Object { "$_" -match '(^|\s)ERROR:' }) { throw 'Synthetic Laboratory source-load smoke reported ERROR output.' }
    if (-not ($labOutput | Where-Object { "$_" -match 'TWR_LAB_SOURCE_LOADED' })) {
      throw 'Laboratory source loader did not report a completed scene load.'
    }
    if (-not ($labOutput | Where-Object { "$_" -match 'TWR_SMOKE_LAB_SOURCE_OK infected_spawns=15' })) {
      throw 'Laboratory source-pack smoke did not report required marker.'
    }
  } finally {
    # Never leave synthetic test map data inside the normal build output.
    Remove-Item $fixture -Force -ErrorAction SilentlyContinue
  }

  # Also exercise the new v2 full-map importer for EVERY map with a tiny,
  # generated and explicitly synthetic fixture (no original asset bytes).
  $sourceMapsDir = Join-Path $Output 'Content\Maps'
  $mapNames = @('Ranch','Mill','Bypass','Cabin','Cargo','District',
                'Expressway','Prison','Laboratory','Manor')
  $testMapFiles = @($mapNames | ForEach-Object {
    Join-Path $sourceMapsDir ($_.ToString() + '.scene.jsonl.gz')
  })
  try {
    python (Join-Path $Repo 'tools\maps\make_source_map_smoke_fixtures.py') --output-dir $sourceMapsDir
    $sourceStdout = Join-Path $Output 'all-source-maps.stdout.txt'
    $sourceStderr = Join-Path $Output 'all-source-maps.stderr.txt'
    Remove-Item $sourceStdout,$sourceStderr -Force -ErrorAction SilentlyContinue
    $originalMaps = Start-Process -FilePath $exportExe -ArgumentList @('--headless','--verbose','--quit-after','240','--','--smoke-all-source-maps') -Wait -PassThru -NoNewWindow -RedirectStandardOutput $sourceStdout -RedirectStandardError $sourceStderr
    $sourceOutput = @()
    if (Test-Path $sourceStdout) { $sourceOutput += Get-Content $sourceStdout }
    if (Test-Path $sourceStderr) { $sourceOutput += Get-Content $sourceStderr }
    $sourceOutput | ForEach-Object { Write-Host $_ }
    if ($originalMaps.ExitCode -ne 0) { throw "Ten-map source smoke failed: exit $($originalMaps.ExitCode)" }
    if ($sourceOutput | Where-Object { "$_" -match '(^|\s)ERROR:' }) {
      throw 'Ten-map source smoke emitted Godot errors.'
    }
    if (-not ($sourceOutput | Where-Object { "$_" -match 'TWR_SMOKE_ALL_SOURCE_MAPS_OK maps=10' })) {
      throw 'Ten-map source loader did not finish every synthetic scene.'
    }
  } finally {
    # Never include any synthetic map data in private Windows distribution.
    Remove-Item $testMapFiles -Force -ErrorAction SilentlyContinue
  }
} finally { Pop-Location }

Write-Host 'Windows export and playable smoke pass completed.'
