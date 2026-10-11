"""Owner-held map binaries stay private; a synthetic-only Pass29 CI build is allowed."""
from conftest import ROOT

def test_private_packaging_refuses_ci_scene_and_repo_destination():
    source = (ROOT / 'build/Windows/package_private.ps1').read_text()
    assert "header.synthetic -eq $true" in source
    assert 'twr-laboratory-scene-v1' in source
    assert 'Compress-Archive' in source
    assert 'BUILD-MANIFEST.json' in source
    assert 'executable_sha256' in source
    assert 'StartsWith($Repo +' in source
    assert 'TWR_PRIVATE_PACKAGE_OK' in source
    workflow = (ROOT / '.github/workflows/windows-build.yml').read_text()
    assert "github.ref_name == 'twr-pass29-integration'" in workflow
    assert "TWR-Pass36-Windows-x64-NoPrivateAssets" in workflow
    assert "PrivateSourceZip" not in workflow
    assert 'create-release' not in workflow
