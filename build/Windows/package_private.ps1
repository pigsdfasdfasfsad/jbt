param(
  [Parameter(Mandatory=$true)][string]$LaboratoryPack,
  [string]$Destination = (Join-Path $HOME 'Downloads\ThoseWhoRemainOffline-Private-Dev.zip')
)
$ErrorActionPreference = 'Stop'
$Repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$Output = Join-Path $Repo 'build\output'
$Executable = Join-Path $Output 'ThoseWhoRemainOffline.exe'
if (!(Test-Path -LiteralPath $Executable)) {
  throw "Windows executable not found. Run build\Windows\build.ps1 locally first."
}
if (!(Test-Path -LiteralPath $LaboratoryPack)) {
  throw "Private Laboratory.scene.jsonl.gz pack not found: $LaboratoryPack"
}

# Prevent a synthetic CI fixture from being mistaken for the owner's map.
$src = [IO.File]::OpenRead((Resolve-Path -LiteralPath $LaboratoryPack).Path)
try {
  $gzip = [IO.Compression.GZipStream]::new($src, [IO.Compression.CompressionMode]::Decompress)
  $reader = [IO.StreamReader]::new($gzip)
  try {
    $line = $reader.ReadLine()
    $header = $line | ConvertFrom-Json
    if ($header.format -ne 'twr-laboratory-scene-v1' -or
        $header.map -ne 'Laboratory' -or
        $header.synthetic -eq $true) {
      throw 'Refusing to package an invalid or synthetic Laboratory scene.'
    }
  } finally {
    $reader.Dispose()
  }
} finally {
  $src.Dispose()
}

$Archive = [IO.Path]::GetFullPath($Destination)
if ($Archive.StartsWith($Repo + [IO.Path]::DirectorySeparatorChar,
                        [StringComparison]::OrdinalIgnoreCase)) {
  throw 'Private executable packages must be written outside the public repository.'
}
$Parent = Split-Path -Parent $Archive
New-Item -ItemType Directory -Force -Path $Parent | Out-Null
$Stage = Join-Path ([IO.Path]::GetTempPath()) ('twr-private-' + [Guid]::NewGuid().ToString('N'))
try {
  New-Item -ItemType Directory -Force -Path $Stage | Out-Null
  # Preserve the exported Godot/.NET runtime, but not test logs or CI fixtures.
  Get-ChildItem -LiteralPath $Output -File |
    Where-Object { $_.Name -notmatch '^(smoke|completion-smoke|lab-source-smoke)\.' -and $_.Extension -ne '.zip' } |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $Stage -Force }
  Get-ChildItem -LiteralPath $Output -Directory |
    Where-Object { $_.Name -ne 'Content' } |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $Stage -Recurse -Force }

  $MapDir = Join-Path $Stage 'Content\Maps'
  New-Item -ItemType Directory -Force -Path $MapDir | Out-Null
  Copy-Item -LiteralPath $LaboratoryPack -Destination (Join-Path $MapDir 'Laboratory.scene.jsonl.gz') -Force

  $Manifest = [ordered]@{
    distribution = 'PRIVATE DEVELOPMENT BUILD - NOT FIDELITY COMPLETE'
    engine = 'Godot C# offline'
    laboratory_pack_format = 'twr-laboratory-scene-v1'
    executable_sha256 = (Get-FileHash -LiteralPath $Executable -Algorithm SHA256).Hash.ToLowerInvariant()
    laboratory_pack_sha256 = (Get-FileHash -LiteralPath $LaboratoryPack -Algorithm SHA256).Hash.ToLowerInvariant()
    github_binary_upload = $false
  }
  $Manifest | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $Stage 'BUILD-MANIFEST.json') -Encoding UTF8
  if (Test-Path -LiteralPath $Archive) { Remove-Item -LiteralPath $Archive -Force }
  Compress-Archive -Path (Join-Path $Stage '*') -DestinationPath $Archive -CompressionLevel Optimal -Force
  Write-Host "TWR_PRIVATE_PACKAGE_OK path=$Archive"
  Write-Host 'This is a development package; original mesh binaries may still be missing.'
} finally {
  Remove-Item -LiteralPath $Stage -Force -Recurse -ErrorAction SilentlyContinue
}
