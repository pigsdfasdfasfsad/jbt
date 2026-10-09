param(
  [string]$LaboratoryPack,
  [string]$PrivateSourceZip,
  [string]$PrivateAssetsFolder,
  [string]$AuthorizedAudioFolder,
  [string]$Destination = (Join-Path $HOME 'Downloads\ThoseWhoRemainOffline-Private-Dev.zip')
)
$ErrorActionPreference = 'Stop'
$Repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$Output = Join-Path $Repo 'build\output'
$Executable = Join-Path $Output 'ThoseWhoRemainOffline.exe'
if (!(Test-Path -LiteralPath $Executable)) {
  throw "Windows executable not found. Run build\Windows\build.ps1 locally first."
}
if ([bool]$LaboratoryPack -eq [bool]$PrivateSourceZip) {
  throw 'Specify exactly one of -PrivateSourceZip (recommended all ten maps) or -LaboratoryPack (legacy only).'
}

function Read-SceneHeader([string]$Path) {
  $src = [IO.File]::OpenRead((Resolve-Path -LiteralPath $Path).Path)
  try {
    $gzip = [IO.Compression.GZipStream]::new($src, [IO.Compression.CompressionMode]::Decompress)
    $reader = [IO.StreamReader]::new($gzip)
    try {
      return ($reader.ReadLine() | ConvertFrom-Json)
    } finally { $reader.Dispose() }
  } finally { $src.Dispose() }
}

$Archive = [IO.Path]::GetFullPath($Destination)
if ($Archive.StartsWith($Repo + [IO.Path]::DirectorySeparatorChar,
                        [StringComparison]::OrdinalIgnoreCase)) {
  throw 'Private executable packages must be written outside the public repository.'
}
$Parent = Split-Path -Parent $Archive
New-Item -ItemType Directory -Force -Path $Parent | Out-Null
$Stage = Join-Path ([IO.Path]::GetTempPath()) ('twr-private-' + [Guid]::NewGuid().ToString('N'))
$Maps = @('Ranch','Mill','Bypass','Cabin','Cargo','District',
          'Expressway','Prison','Laboratory','Manor')
$MapHashes = [ordered]@{}
try {
  New-Item -ItemType Directory -Force -Path $Stage | Out-Null
  # Include the actual Windows executable and Godot/.NET support files but
  # exclude CI smoke logs and synthetic fixture content.
  Get-ChildItem -LiteralPath $Output -File |
    Where-Object { $_.Name -notmatch '\.(stdout|stderr)\.txt$' -and
                   $_.Extension -ne '.zip' } |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $Stage -Force }
  Get-ChildItem -LiteralPath $Output -Directory |
    Where-Object { $_.Name -ne 'Content' } |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $Stage -Recurse -Force }

  $Content = Join-Path $Stage 'Content'
  $MapDir = Join-Path $Content 'Maps'
  New-Item -ItemType Directory -Force -Path $MapDir | Out-Null

  if ($PrivateSourceZip) {
    if (!(Test-Path -LiteralPath $PrivateSourceZip)) {
      throw "Private ten-map source ZIP not found: $PrivateSourceZip"
    }
    $InputStage = Join-Path $Stage '_private-source-input'
    Expand-Archive -LiteralPath $PrivateSourceZip -DestinationPath $InputStage -Force
    $DataFolder = Join-Path $InputStage 'Content'
    if (!(Test-Path -LiteralPath $DataFolder)) {
      throw 'Private source ZIP has no Content folder.'
    }
    # Never package CI fixtures, partial map sets, or renamed source files.
    foreach ($Map in $Maps) {
      $File = Join-Path $DataFolder "Maps\$Map.scene.jsonl.gz"
      if (!(Test-Path -LiteralPath $File)) {
        throw "The private source ZIP is missing original map $Map."
      }
      $header = Read-SceneHeader $File
      if ($header.format -ne 'twr-source-map-v2' -or
          $header.map -ne $Map -or
          $header.synthetic -eq $true -or
          $header.counts.geometry -lt 10 -or
          $header.counts.player_spawns -lt 1 -or
          $header.counts.infected_spawns -lt 1) {
        throw "Refusing to package an invalid or synthetic original map: $Map"
      }
      $MapHashes[$Map] = (Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    foreach ($Model in @('Enemies\InfectedSourceVariants.json.gz',
                          'Weapons\SourceWeaponModels.json.gz')) {
      if (!(Test-Path -LiteralPath (Join-Path $DataFolder $Model))) {
        throw "Missing recovered offline assembly pack: $Model"
      }
    }
    Copy-Item -Path (Join-Path $DataFolder '*') -Destination $Content -Recurse -Force
    Remove-Item -LiteralPath $InputStage -Force -Recurse
  } else {
    if (!(Test-Path -LiteralPath $LaboratoryPack)) {
      throw "Private Laboratory.scene.jsonl.gz pack not found: $LaboratoryPack"
    }
    $header = Read-SceneHeader $LaboratoryPack
    if ($header.format -ne 'twr-laboratory-scene-v1' -or
        $header.map -ne 'Laboratory' -or
        $header.synthetic -eq $true) {
      throw 'Refusing to package an invalid or synthetic Laboratory scene.'
    }
    Copy-Item -LiteralPath $LaboratoryPack -Destination (Join-Path $MapDir 'Laboratory.scene.jsonl.gz') -Force
    $MapHashes['Laboratory'] = (Get-FileHash -LiteralPath $LaboratoryPack -Algorithm SHA256).Hash.ToLowerInvariant()
  }

  if ($PrivateAssetsFolder) {
    if (!(Test-Path -LiteralPath $PrivateAssetsFolder)) { throw 'PrivateAssetsFolder does not exist.' }
    $AssetsFolder = Join-Path $Content 'Assets'
    New-Item -ItemType Directory -Force -Path $AssetsFolder | Out-Null
    foreach ($Kind in @('Meshes','Textures')) {
      $source = Join-Path $PrivateAssetsFolder $Kind
      if (Test-Path -LiteralPath $source) {
        New-Item -ItemType Directory -Force -Path (Join-Path $AssetsFolder $Kind) | Out-Null
        $pattern = if ($Kind -eq 'Meshes') { '*.obj' } else { '*.png' }
        Get-ChildItem -LiteralPath $source -File -Filter $pattern |
          Where-Object { $_.BaseName -match '^[0-9]+$' } |
          Copy-Item -Destination (Join-Path $AssetsFolder $Kind) -Force
      }
    }
  }
  if ($AuthorizedAudioFolder) {
    if (!(Test-Path -LiteralPath $AuthorizedAudioFolder)) { throw 'AuthorizedAudioFolder does not exist.' }
    $SoundDest = Join-Path $Content 'Audio'
    New-Item -ItemType Directory -Force -Path $SoundDest | Out-Null
    Get-ChildItem -LiteralPath $AuthorizedAudioFolder -File -Filter '*.wav' |
      Copy-Item -Destination $SoundDest -Force
  }

  $Manifest = [ordered]@{
    distribution = 'PRIVATE DEVELOPMENT BUILD - NOT FIDELITY COMPLETE'
    engine = 'Godot C# offline'
    original_source_map_count = $MapHashes.Count
    all_ten_source_maps_included = ($MapHashes.Count -eq 10)
    map_sha256 = $MapHashes
    executable_sha256 = (Get-FileHash -LiteralPath $Executable -Algorithm SHA256).Hash.ToLowerInvariant()
    github_binary_upload = $false
    human_playtested_all_maps = $false
    original_meshes_may_be_missing = $true
  }
  $Manifest | ConvertTo-Json -Depth 6 |
    Set-Content -Path (Join-Path $Stage 'BUILD-MANIFEST.json') -Encoding UTF8
  if (Test-Path -LiteralPath $Archive) { Remove-Item -LiteralPath $Archive -Force }
  Compress-Archive -Path (Join-Path $Stage '*') -DestinationPath $Archive -CompressionLevel Optimal -Force
  Write-Host "TWR_PRIVATE_PACKAGE_OK path=$Archive maps=$($MapHashes.Count)"
  Write-Host 'This private build is not yet graphically certified to match the original.'
} finally {
  Remove-Item -LiteralPath $Stage -Force -Recurse -ErrorAction SilentlyContinue
}
