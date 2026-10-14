"""Pass117: verify owner-private source import without publishing original data."""
from conftest import ROOT
import hashlib
import importlib.util
import json
import pathlib
import zipfile

SCRIPT = ROOT / "tools/maps/install_private_laboratory117.py"


def load_installer():
    spec = importlib.util.spec_from_file_location("private_laboratory117", SCRIPT)
    obj = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(obj)
    return obj


def test_owner_source_installer_is_sha_pinned_to_original_laboratory():
    importer = load_installer()
    assert importer.XML_SHA256 == (
        "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2"
    )
    assert importer.SCENE_SHA256 == (
        "b92efe792c93062a0741724f252949ec13cc828d601e277e21a1b13b1d02bb8c"
    )
    assert importer.COLLISION_SHA256 == (
        "a3067649db39d5f07be6600b642763fd846050c33b913f0d03dbfbed16468727"
    )
    assert importer.EXPECTED == {
        "geometry": 34268, "collision": 1517,
        "lights": 836, "infected_spawns": 15,
        "player_spawns": 8, "item_markers": 127,
        "fortification_markers": 47,
    }
    assert importer.SCENE_MEMBER == "Content/Maps/Laboratory.scene.jsonl.gz"
    assert importer.COLLISION_MEMBER == "Content/Collision/Laboratory.col26.gz"


def test_truncated_or_synthetic_pack_cannot_write_any_files(tmp_path):
    importer = load_installer()
    archive = tmp_path / "synthetic.zip"
    with zipfile.ZipFile(archive, "w") as z:
        z.writestr(importer.SCENE_MEMBER, b"this is not a source scene")
        z.writestr(importer.COLLISION_MEMBER, b"not a verified collision cache")
    root = tmp_path / "game"
    root.mkdir()
    (root / "ThoseWhoRemainOffline.exe").write_bytes(b"CI synthetic non-executable")
    try:
        importer.install(archive, root)
    except ValueError as e:
        assert "digest" in str(e)
    else:
        raise AssertionError("Unverified source pack installed")
    assert not (root / importer.SCENE_MEMBER).exists()
    assert not (root / importer.COLLISION_MEMBER).exists()


def test_installer_requires_compiled_game_and_all_source_members(tmp_path):
    importer = load_installer()
    target = tmp_path / "empty-game"
    target.mkdir()
    file = tmp_path / "empty.zip"
    with zipfile.ZipFile(file, "w") as z:
        z.writestr("README.txt", "empty")
    try:
        importer.install(file, target)
    except ValueError as e:
        assert "compiled Windows executable" in str(e)
    else:
        raise AssertionError("Installed without any compiled game")
    (target / "ThoseWhoRemainOffline.exe").write_bytes(b"synthetic")
    try:
        importer.install(file, target)
    except ValueError as e:
        assert "lacks the original Laboratory scene/collision" in str(e)
    else:
        raise AssertionError("Incomplete owner pack installed")


def test_private_windows_packager_accepts_authenticated_source_and_collision():
    s = (ROOT / "build/Windows/package_private.ps1").read_text(encoding="utf8")
    assert "[string]$LaboratoryPack" in s
    assert "[string]$LaboratoryCollision" in s
    assert "twr-laboratory-scene-v1" in s
    assert "twr-source-map-v2" in s
    assert "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2" in s
    for count in ("34268", "1517", "836", "15", "127", "47", "18130", "4501", "180"):
        assert count in s
    assert "TWRCOL26" in s
    assert "cacheSceneSha -ne $MapHashes['Laboratory']" in s
    assert "Copy-Item -LiteralPath $LaboratoryCollision" in s
    assert "Compress-Archive" in s
    assert "header.synthetic -eq $true" in s
    assert "StartsWith($Repo +" in s


def test_original_map_prioritized_and_existing_fast_collision_consumes_pack():
    src = (ROOT / "src/Twr.Godot/Scripts/LaboratorySourceLoader.cs").read_text(encoding="utf8")
    world = (ROOT / "src/Twr.Godot/Scripts/GameplayRoot.cs").read_text(encoding="utf8")
    fast = (ROOT / "src/Twr.Godot/Scripts/Pass26CollisionRuntime.cs").read_text(encoding="utf8")
    assert 'var packName = mapName + ".scene.jsonl.gz";' in src
    assert "Pass26CollisionRuntime.TryBuild(stage, mapName, filePath)" in src
    assert 'Str(header, "format") == "twr-source-map-v2"' in src
    assert 'Str(header, "map") != mapName' in src
    assert "LaboratorySourceLoader.TryBuild(this, MapName, out var recovered)" in world
    assert "_mapLayout = originalWorldAvailable" in world
    assert ": MapBlockoutBuilder.Build(this, _mapDefinition)" in world
    assert "var name = map + \".col26.gz\";" in fast
    assert "declaredSha.SequenceEqual(sceneSha)" in fast
    assert "chunkCount is < 1 or > MaxChunks" in fast


def test_public_repo_does_not_contain_private_map_or_uploaded_original_xml():
    assert not (ROOT / "src/Twr.Godot/Content/Maps/Laboratory.scene.jsonl.gz").exists()
    assert not (ROOT / "src/Twr.Godot/Content/Collision/Laboratory.col26.gz").exists()
    assert not (ROOT / "TestPlace.zip").exists()
    ci = (ROOT / ".github/workflows/windows-build.yml").read_text(encoding="utf8")
    assert "twr-pass117-original-laboratory-private-installer" in ci
    assert "Preflight Pass 117 authenticated private Laboratory installer refusal" in ci
    assert "TWR_PASS117_PRIVATE_INSTALLER_SAFETY_OK" in ci
    assert "TestPlace.zip" not in ci
    assert ci.count("uses: actions/upload-artifact@v4") == ci.count(
        "if: false && success()"
    )
