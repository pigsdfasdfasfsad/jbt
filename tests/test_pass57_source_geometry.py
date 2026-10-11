"""Pass57: source mesh ID canonicalization and safe original geometry fallbacks."""
from __future__ import annotations

import gzip
import hashlib
import json
import subprocess
import sys

from conftest import ROOT


def source(path):
    return (ROOT/path).read_text(encoding="utf8")


def test_mesh_asset_url_canonicalization_is_strict_and_only_local():
    data=source("src/Twr.Godot/Scripts/OfflineAssetResolver.cs")
    assert "public static string CanonicalAssetId(string? id)" in data
    assert 'const string prefix="rbxassetid://";' in data
    assert 'id.StartsWith(prefix,StringComparison.Ordinal)' in data
    assert "digit is <'0' or >'9'" in data
    assert "id=CanonicalAssetId(id)" in data
    assert data.count("id=CanonicalAssetId(id)") == 2
    assert "Content\", \"Assets\", folder, name" in data
    assert 'FindPrivateFile("Meshes", id + ".obj", MaxMeshBytes)' in data
    assert 'FindPrivateFile("Textures", id + ".png", MaxTextureBytes)' in data
    assert "MaxTriangles = 300_000" in data
    assert "HttpClient" not in data
    assert "WebRequest" not in data


def test_missing_source_mesh_uses_bounded_authored_shape_and_preserves_part_transform():
    geometry=source("src/Twr.Godot/Scripts/Pass57SourceGeometryProxy.cs")
    original=source("src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs")
    assert "CircularSides=24" in geometry
    assert "IdentifiableTube(originalName)" in geometry
    assert "TryTubeAxis(sourceSize,out var axis)" in geometry
    assert 'originalClass=="WedgePart"' in geometry
    assert "RobloxPrimitiveGeometry.WedgeMesh()" in geometry
    assert '"barrel" or "forebarrel"' in geometry
    assert '"muzzle" or "nozzle" or "tube"' in geometry
    assert '"receiver"' not in geometry
    assert "new BoxMesh {Size=Vector3.One}" in geometry
    assert "dims[axis]>=Math.Max(crossA,crossB)*1.6f" in geometry
    assert "crossA=dims[(axis+1)%3]" in geometry
    assert "crossB=dims[(axis+2)%3]" in geometry
    assert "CircularSides*12" in geometry
    assert "Max()" in geometry
    assert "ArrayMesh()" in geometry
    assert "Texture2D" not in geometry
    assert "SourceSize(part)" in original
    assert "Pass57SourceGeometryProxy.ForMissingOriginal(" in original
    assert "Pass57SourceGeometryProxy.Tube(0)" in original
    assert 'originalShape=="2"' in original
    assert "new SphereMesh { Radius=.5f, Height=1f }" in original
    assert "Transform = new Transform3D(alignment * SourceRotation(part)," in original
    assert "alignment * SourcePosition(part)).ScaledLocal(size)" in original
    assert 'if (!float.IsFinite(alpha) || alpha <= .001f) continue;' in original
    assert "LastVisibleSourceParts" in original
    assert "LastResolvedMeshParts" in original
    assert "LastCircularProxyParts" in original
    assert "LastConservativeProxyParts" in original
    assert "LastNativePrimitiveParts" in original
    assert 'or "--smoke-pass57"' in original


def test_visual_model_and_armory_report_exact_local_mesh_vs_shape_proxy_status():
    visual=source("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    armory=source("src/Twr.Godot/Scripts/Pass51OriginalLoadoutDisplayRuntime.cs")
    for metric in ("SourceLocallyResolvedMeshes","SourceCircularProxyParts",
                   "SourceConservativeProxyParts","SourceNativePrimitiveParts"):
        assert metric in visual
    for label in ("PreviewLocallyResolvedMeshes", "PreviewCircularProxies",
                  "PreviewConservativeProxies","LOCAL TRIANGLE MESHES",
                  "CYLINDER PROXIES","BOUNDS PROXIES",
                  "SOURCE MESH IDS UNAVAILABLE"):
        assert label in armory
    assert "SourceMissingMeshProxies = 0" in visual
    assert "SourceLocallyResolvedMeshes = 0" in visual
    assert "SourceCircularProxyParts = 0" in visual
    assert "SourceConservativeProxyParts = 0" in visual
    assert "SourceNativePrimitiveParts = 0" in visual
    assert "PreviewMissingSourceMeshes = model.SourceMissingMeshProxies" in armory
    assert "PreviewLocallyResolvedMeshes = model.SourceLocallyResolvedMeshes" in armory


def test_fixture_is_deterministic_and_owner_source_safe(tmp_path):
    script=ROOT/"tools/maps/make_pass57_weapon_geometry_fixture.py"
    pack=tmp_path/"Content/Weapons/SourceWeaponModels.json.gz"
    mesh_dir=tmp_path/"Content/Assets/Meshes"
    obj=mesh_dir/"555570001.obj"
    args=[sys.executable,str(script),"--output",str(pack),
          "--mesh-dir",str(mesh_dir)]
    first=subprocess.run(args+["--create"],capture_output=True,text=True)
    assert first.returncode==0,first.stderr
    data=json.loads(gzip.decompress(pack.read_bytes()))
    assert data["synthetic"] is True
    assert set(data["models"])=={"Glock 17"}
    parts=data["models"]["Glock 17"]["parts"]
    assert len(parts)==9
    assert sum(p["opacity"]>.001 for p in parts)==8
    assert sum(bool(p.get("meshId")) for p in parts)==3
    assert {p["name"]:p["class"] for p in parts}["StockWedge"]=="WedgePart"
    assert any(p["shape"]=="2" and p["class"]=="Part" for p in parts)
    assert parts[0]["meshId"]=="rbxassetid://555570001"
    assert all(len(p["t"])==3 and len(p["r"])==9 and len(p["s"])==3 for p in parts)
    assert obj.read_text().startswith("# SYNTHETIC TEST MESH")
    original_sha=hashlib.sha256(pack.read_bytes()).hexdigest()
    assert subprocess.run(args+["--create"],capture_output=True).returncode!=0
    assert hashlib.sha256(pack.read_bytes()).hexdigest()==original_sha
    cleaned=subprocess.run(args+["--clean"],capture_output=True,text=True)
    assert cleaned.returncode==0,cleaned.stderr
    assert not pack.exists() and not obj.exists()
    pack.parent.mkdir(parents=True,exist_ok=True)
    pack.write_bytes(b"owner-source-do-not-touch")
    obj.parent.mkdir(parents=True,exist_ok=True)
    obj.write_text("# original user geometry")
    assert subprocess.run(args+["--clean"],capture_output=True).returncode!=0
    assert pack.read_bytes()==b"owner-source-do-not-touch"
    assert obj.read_text()=="# original user geometry"


def test_native_windows_geometry_smoke_and_private_pack_exclusion():
    boot=source("src/Twr.Godot/Scripts/Bootstrap.cs")
    action=source(".github/workflows/windows-build.yml")
    assert "RunPass57Smoke()" in boot
    assert "TWR_SMOKE_PASS57_GEOMETRY_OK" in boot
    assert "TWR_SMOKE_PASS57_GEOMETRY_OK" in action
    assert 'canonical_rbxassetid_url=true' in boot
    assert "path_traversal_blocked=true" in boot
    assert "owner_mesh_triangles_not_claimed=true" in boot
    assert '"Glock 17"' in boot and 'SourceLocallyResolvedMeshes!=1' in boot
    assert "SourceCircularProxyParts!=1" in boot
    assert "SourceConservativeProxyParts!=2" in boot
    assert "SourceNativePrimitiveParts!=4" in boot
    assert "RemoveChild(scene);" in boot
    assert "StartGame(\"Manor\")" in boot
    assert "--smoke-pass57" in action
    assert "twr-pass57-offline-source-weapon-geometry" in action
    assert "make_pass57_weapon_geometry_fixture.py --output $sourceTools --mesh-dir $meshDir --create" in action
    assert "make_pass57_weapon_geometry_fixture.py --output $sourceTools --mesh-dir $meshDir --clean" in action
    assert "555570001.obj" in action
    assert "SourceWeaponModels.json.gz" in action
    assert action.count("if: false && success()")>=14
    assert action.count("uses: actions/upload-artifact@v4")==action.count(
        "if: false && success()")
    assert not (ROOT/"content/Weapons/SourceWeaponModels.json.gz").exists()
    assert not (ROOT/"content/Assets/Meshes/555570001.obj").exists()
