"""Pass54: source RGB/material provenance, private pack safeguards and native CI smoke."""
from __future__ import annotations

import gzip
import json
import re
import subprocess
import sys
from pathlib import Path
import pytest
from conftest import ROOT


def read(path: str) -> str:
    return (ROOT/path).read_text(encoding="utf-8")


def test_source_extractor_uses_exact_owner_sha_and_bounded_luau_literal_parser():
    script=read("tools/skins/recover_private_source_skins54.py")
    assert 'SKINS_SHA = "aeff32781cc9eafdbbd5ae7bc12ff157cc32934e7dae6773c87d7bfba355a015"' in script
    assert 'PLACE_SHA = "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2"' in script
    assert '"ModuleScript.Skins.Source.txt"' in script
    assert 'zipfile' not in script.lower() or "ZipFile" in script
    assert 'etree.iterparse' in script
    assert 'models.setdefault(weapon,{})[str(position)]' in script
    assert 'sum(parts.values())!=1484 or tagged!=728' in script
    assert 'if len(parsed)!=118' in script
    assert 'set(slots)!={1,2,3,4,5}' in script
    assert 'Color3\\.(new|fromRGB)' in script
    assert 'Enum\\.Material' in script
    assert 'sorted' in script or 'sort_keys=True' in script
    assert 'FileExistsError' in script
    assert 'execute(' not in script.lower()
    assert 'os.system(' not in script


def test_runtime_pins_authentic_palette_and_owner_part_map_without_network():
    s=read("src/Twr.Godot/Scripts/Pass54SourceSkinMaterials.cs")
    for digest in (
        "aeff32781cc9eafdbbd5ae7bc12ff157cc32934e7dae6773c87d7bfba355a015",
        "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2",
        "e9881057cd8bbe9c16c9c3e567de9ccba1fe94250f4db0d55ef5c61b6871b023",
        "f38d7e6275db2689f8244af7abf0e06ac8690fd6da5a1d13edc887b05cb24afe",
    ):
        assert digest in s
    for phrase in ("SourceSkinMaterials54.json.gz","SourceWeaponSkinSlots54.json.gz",
                   "512*1024", "synthetic && !test", "OwnerPaletteVerified",
                   "OwnerSlotsVerified", "valuesRgb.Any"):
        assert phrase in s
    assert 'original_parts").GetInt32()!=1484' in s
    assert 'tagged_parts").GetInt32()!=728' in s
    assert 'source_entries").GetInt32()!=118' in s
    assert "ReadPacked(" in s and "SHA256.HashData" in s
    assert "HttpClient" not in s and "Process.Start" not in s


def test_original_source_tool_part_indices_are_never_inferred_from_name():
    tool=read("src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs")
    weapon=read("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    assert "for(var partIndex=0;partIndex<sourceParts.Length;partIndex++)" in tool
    assert "Pass54SourceSkinMaterials.TryGetPartSlot(" in tool
    assert 'originalPart.SetMeta("TwrSkinSlot54",skinSlot)' in tool
    assert 'part.HasMeta("TwrSkinSlot54")' in weapon
    assert 'SourceSkinMappedParts++' in weapon
    assert 'SourceSkinColorParts++' in weapon
    assert 'part.MaterialOverlay=surfaces[sourceSlot-1]' in weapon
    assert 'SourceSkinMissingTextureParts++' in weapon
    assert 'Pass54SourceSkinMaterials.LoadedTaggedParts>0' in weapon
    assert 'BuildCosmeticProxyTint' in weapon
    assert 'Original source part' not in weapon or 'TwrSkinSlot54' in weapon


def test_five_source_material_channels_are_render_only_and_stateful():
    s=read("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    ui=read("src/Twr.Godot/Scripts/Pass51OriginalLoadoutDisplayRuntime.cs")
    assert "BuildSourceMaterial" in s
    assert 'kind=="Glass"' in s and 'kind=="ForceField"' in s
    assert 'kind=="Neon"' in s
    assert 'EmissionEnabled=neon || forceField' in s
    assert 'SourceSkinMappedParts=0;' in s
    assert 'if(_rig is null)return;' in s
    assert 'foreach(var mesh in meshes) mesh.MaterialOverlay=null' in s
    assert 'CosmeticSourceStatus=Pass54SourceSkinMaterials.Provenance(' in s
    assert "TEXTURE/VARIANT GAPS" in ui
    assert "MaterialOverlay" in s
    assert not re.search(r'\b(?:AwardKill|PurchaseWeapon|GrantAmmo)\s*\(',s)


def test_smoke_only_profile_does_not_mutate_normal_player_profile():
    s=read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    assert "pass54-smoke-profile.json" in s
    assert "--smoke-pass54" in s and "--smoke-pass54" in boot
    assert "TWR_SMOKE_PASS54_SOURCE_SKINS_OK" in boot
    assert 'exact_source_rgb_40_17_17=true' in boot
    assert 'owner_assets_not_uploaded=true' in boot
    assert "Pass54SourceSkinMaterials.LoadedTaggedParts!=4" in boot
    assert "ShowArmory();" in boot and 'StartGame("Manor")' in boot
    assert not re.search(r'\.Credits\s*=',boot)
    assert not re.search(r'\.Xp\s*=',boot)


def test_smoke_pack_cleanup_validates_owner_materials_and_refuses_overwrites(tmp_path):
    fixture=ROOT/"tools/skins/make_pass54_smoke_fixture.py"
    root=tmp_path/"Skins"
    cmd=[sys.executable,str(fixture),"--output-dir",str(root)]
    subprocess.run(cmd+["--create"],check=True,capture_output=True,text=True)
    palette=root/"SourceSkinMaterials54.json.gz"
    tagmap=root/"SourceWeaponSkinSlots54.json.gz"
    with gzip.open(palette,"rb") as file: a=json.load(file)
    with gzip.open(tagmap,"rb") as file: b=json.load(file)
    assert a["synthetic"] is True and len(a["skins"])==3
    assert a["skins"]["Low/Cocoa"][0]["rgb"]==[40,17,17]
    assert b["models"]["Glock 17"]=={"0":1,"1":2,"2":3,"3":4}
    assert subprocess.run(cmd+["--create"],capture_output=True).returncode!=0
    subprocess.run(cmd+["--clean"],check=True,capture_output=True,text=True)
    assert not palette.exists() and not tagmap.exists()
    # Owner or unlabelled files can never be erased by test cleanup.
    root.mkdir(exist_ok=True)
    with gzip.open(palette,"wb") as file:
        file.write(b'{"format":"twr-source-skin-material-v1","synthetic":false}')
    assert subprocess.run(cmd+["--clean"],capture_output=True).returncode!=0
    assert palette.exists()


def test_windows_ci_wraps_original_source_sidecars_in_cleanup_gates():
    action=read(".github/workflows/windows-build.yml")
    assert "twr-pass54-original-skin-materials" in action
    assert "--smoke-pass54" in action
    assert "TWR_SMOKE_PASS54_SOURCE_SKINS_OK" in action
    assert 'make_pass54_smoke_fixture.py --output-dir $skinDir --create' in action
    assert 'make_pass54_smoke_fixture.py --output-dir $skinDir --clean' in action
    assert "SourceSkinMaterials54.json.gz" in action
    assert "SourceWeaponSkinSlots54.json.gz" in action
    assert 'Remove-Item -LiteralPath $sourceTools -Force' in action
    assert action.count('if: false && success()')>=14
    assert action.count('uses: actions/upload-artifact@v4')==action.count(
        'if: false && success()')
    assert not (ROOT/"content/Skins/SourceSkinMaterials54.json.gz").exists()
    assert not (ROOT/"content/Skins/SourceWeaponSkinSlots54.json.gz").exists()
