"""Pass56: user-supplied wiki image evidence, safe private gallery and native smoke."""
from __future__ import annotations

import hashlib
import json
import re
import subprocess
import sys
from zipfile import ZipFile

from conftest import ROOT


def read(path: str) -> str:
    return (ROOT/path).read_text(encoding="utf-8")


def test_wiki_catalog_is_reproducible_and_separate_from_original_uv_textures():
    src=read("tools/assets/recover_wiki_reference_images56.py")
    assert "b07b058367d115b9710feb25accf0ac5cc8a1de74b4b1769a890ff0c457115f0" in src
    assert "e2ebf80af7b6591af0de885a9cee9e863da398407fdc741ac3f215077936984f" in src
    assert "ALIASES=" in src and "'Glock 17':'G17'" in src
    assert "SKIN_SUFFIXES=" in src
    assert "source_docs" in src
    assert "if kind.lower()!='skin'" in src
    assert "canonical(subject)!=canonical(skin.split('/',1)[1])" in src
    assert "if len(weapon_refs)!=91 or defaults!=90 or variants!=1563 or poses!=270" in src
    assert "if len(output_images)!=2033 or len(skin_icons)!=110" in src
    assert "FileExistsError" not in src or "ZipFile(output,'x'" in src
    assert "ZipFile(output,'x'" in src
    assert "sourceImageZipSha256" in src
    assert "NOT Roblox UV textures" in src
    assert "urllib" not in src and "requests.get(" not in src


def test_runtime_accepts_only_bounded_pinned_reference_assets():
    src=read("src/Twr.Godot/Scripts/Pass56WikiVisualReferences.cs")
    for hash_value in (
        "c06abbaefcaa33430a5a37ddb10fe7540fb27ac39c863fd05c420cad0d2cd2c1",
        "b07b058367d115b9710feb25accf0ac5cc8a1de74b4b1769a890ff0c457115f0",
        "e2ebf80af7b6591af0de885a9cee9e863da398407fdc741ac3f215077936984f",
    ):
        assert hash_value in src
    assert 'SyntheticFixture' in src
    assert 'if(synthetic && !smoke)' in src
    assert "MaxArchiveBytes=32*1024*1024" in src
    assert "MaxManifestBytes=1_500_000" in src
    assert "MaxSingleImageBytes=1_500_000" in src
    assert "Files.Count!=2033" in src
    assert "Weapons.Count!=91" in src
    assert "Icons.Count!=110" in src
    assert "SHA256.HashData(bytes)" in src
    assert "SHA256.HashData(bytes)).ToLowerInvariant()" in src
    assert "image.LoadPngFromBuffer(bytes)" in src
    assert "image.LoadJpgFromBuffer(bytes)" in src
    assert "Cache.Count>=8" in src
    assert "ZipFile.OpenRead(_packPath)" in src
    assert "HttpClient" not in src and "WebRequest" not in src


def test_reference_gallery_is_explicitly_not_weapon_material_and_not_a_purchase():
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    assert 'private void ShowWikiReferenceGallery(string weapon,string? skin)' in boot
    assert '"ArmoryWikiReference"' in boot
    assert '"InventoryWikiReference"' in boot
    assert '"CloseWikiReferenceGallery"' in boot
    assert '"Pass56SourceWikiScreenshot"' in boot
    assert '"Pass56SkinReferenceIcon"' in boot
    assert 'Pass56WikiVisualReferences.TryWeaponImage(' in boot
    assert 'Pass56WikiVisualReferences.TrySkinIcon(' in boot
    assert "NOT Roblox UV texture files" in boot
    assert "CloseWikiReferenceGallery();" in boot
    assert 'private void RunPass56Smoke()' in boot
    assert 'TWR_SMOKE_PASS56_WIKI_REFERENCE_OK' in boot
    assert not re.search(r'\.Credits\s*=',boot)
    assert 'MaterialOverlay=picture' not in boot


def test_synthetic_wiki_gallery_fixture_is_safe_and_deterministic(tmp_path):
    script=ROOT/"tools/assets/make_pass56_wiki_smoke_fixture.py"
    p=tmp_path/"Content/References/SourceWikiReferenceImages56.zip"
    cmd=[sys.executable,str(script),"--output",str(p)]
    subprocess.run(cmd+["--create"],check=True,capture_output=True,text=True)
    first=p.read_bytes()
    with ZipFile(p) as archive:
        data=json.loads(archive.read("manifest.json"))
        assert data["format"]=="twr-wiki-weapon-reference-v1"
        assert data["synthetic"] is True
        assert len(data["files"])==3
        assert data["weapons"]["Glock 17"]["skins"]["Neon/Ego"]=="reference-images/G17-Ego.png"
        for filename,metadata in data["files"].items():
            blob=archive.read(filename)
            assert blob.startswith(b"\x89PNG\r\n\x1a\n")
            assert hashlib.sha256(blob).hexdigest()==metadata["sha256"]
            assert metadata["width"]==metadata["height"]==4
    assert subprocess.run(cmd+["--create"],capture_output=True).returncode!=0
    assert p.read_bytes()==first
    subprocess.run(cmd+["--clean"],check=True,capture_output=True,text=True)
    assert not p.exists()
    p.write_bytes(b"pretend-owner-reference-data")
    assert subprocess.run(cmd+["--clean"],capture_output=True).returncode!=0
    assert p.read_bytes()==b"pretend-owner-reference-data"


def test_native_ci_smoke_uses_synthetic_gallery_and_deletes_private_references():
    action=read(".github/workflows/windows-build.yml")
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    assert "twr-pass56-source-wiki-gallery" in action
    assert "--smoke-pass56" in action and "--smoke-pass56" in boot
    assert "TWR_SMOKE_PASS56_WIKI_REFERENCE_OK" in action
    assert "TWR_SMOKE_PASS56_WIKI_REFERENCE_OK" in boot
    assert "make_pass56_wiki_smoke_fixture.py --output $gallery --create" in action
    assert "make_pass56_wiki_smoke_fixture.py --output $gallery --clean" in action
    assert "make_pass49_lobby_smoke_fixture.py --output $sourceLobby --create" in action
    assert "make_pass49_lobby_smoke_fixture.py --output $sourceLobby --clean" in action
    assert "SourceWikiReferenceImages56.zip" in action
    assert "pass56-smoke-profile.json" in session
    assert "if(smoke54 || smoke55 || smoke56)" in session
    for path in ("Pass49OriginalLobbyRuntime.cs","Pass50SourceLobbySignsRuntime.cs",
                 "OriginalWeaponSourceRuntime.cs","Pass54SourceSkinMaterials.cs",
                 "Pass55SourceSkinLayers.cs"):
        assert "--smoke-pass56" in read("src/Twr.Godot/Scripts/"+path)
    assert action.count("if: false && success()")>=14
    assert action.count("uses: actions/upload-artifact@v4")==action.count(
        "if: false && success()"
    )
    assert not (ROOT/"content/References/SourceWikiReferenceImages56.zip").exists()
