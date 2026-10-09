"""Private release must include ten real maps without uploading game binaries."""
from conftest import ROOT

def test_package_private_accepts_all_original_maps():
    script=(ROOT/"build/Windows/package_private.ps1").read_text()
    assert '[string]$PrivateSourceZip' in script
    assert "$Maps = @('Ranch','Mill','Bypass','Cabin','Cargo','District'" in script
    assert 'twr-source-map-v2' in script
    assert "Missing recovered offline assembly pack" in script
    assert "Refusing to package an invalid or synthetic original map" in script
    assert "all_ten_source_maps_included" in script
    assert "map_sha256" in script
    assert "human_playtested_all_maps = $false" in script
    assert "github_binary_upload = $false" in script
    assert "Copy-Item -Path (Join-Path $DataFolder '*')" in script

def test_package_private_supports_only_authorized_external_images_audio():
    script=(ROOT/"build/Windows/package_private.ps1").read_text()
    assert '[string]$PrivateAssetsFolder' in script
    assert '[string]$AuthorizedAudioFolder' in script
    assert "'*.obj'" in script
    assert "'*.png'" in script
    assert "'*.wav'" in script
    assert "Get-FileHash -LiteralPath $Executable -Algorithm SHA256" in script
    assert "StartsWith($Repo +" in script
