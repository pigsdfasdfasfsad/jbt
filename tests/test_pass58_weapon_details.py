"""Pass58: source-guided weapon silhouettes and fully isolated Windows geometry test."""
from __future__ import annotations

import gzip
import hashlib
import json
import subprocess
import sys

from conftest import ROOT


def read(path):
    return (ROOT/path).read_text(encoding="utf8")


def test_source_guided_original_mesh_proxy_semantics_and_budgets():
    s=read("src/Twr.Godot/Scripts/Pass58WeaponDetailGeometry.cs")
    assert "No Roblox" in s and "RECONSTRUCTED" in s
    assert 'sourceClass!="MeshPart"' in s
    assert "FiniteBounds(authoredSize)" in s
    assert "TryLensAxis(authoredSize,out var thinAxis)" in s
    assert 'detailKind="magazine"' in s
    assert 'detailKind="stock"' in s
    assert 'detailKind="grip"' in s
    assert 'detailKind="trigger_guard"' in s
    assert 'detailKind="chassis"' in s
    assert 'detailKind="lens"' in s
    assert '"reticleglass" or "opticglass"' in s
    assert '"pistolgrip" or "gripmodel"' in s
    assert '"body" or "receiver"' in s
    assert '"magazine" or "mag"' in s
    assert "Cached.TryGetValue(detailKind,out var existing)" in s
    assert "ArrayMesh()" in s
    assert "RingGuard()" in s
    assert "Extrude(MagazineProfile,0)" in s
    assert "Extrude(ChassisProfile,2)" in s
    assert "MaxPolygonCorners = 12" in s
    assert "vertices.Count>2000" in s
    assert "float.IsFinite(size.X)" in s
    assert "GetTree()" not in s
    assert "HttpClient" not in s


def test_source_geometry_retains_author_positions_and_obj_override():
    s=read("src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs")
    assert "mesh = OfflineAssetResolver.Mesh(meshId)" in s
    assert "var recoveredFromLocalFile=mesh is not null" in s
    assert "if(mesh is null && Pass58WeaponDetailGeometry.TryCreate(" in s
    assert "mesh ??= nativeBall" in s
    assert "SourceRotation(part)" in s
    assert "SourcePosition(part)" in s
    assert "SourceSize(part)" in s
    assert "originalPart.SetMeta" in s
    assert "LastDetailProfileProxies++" in s
    assert "LastLensProxies++" in s
    assert "LastConservativeProxyParts++" in s
    assert '"--smoke-pass58"' in s
    assert "TWR_ORIGINAL_TOOL_ASSEMBLED" in s


def test_source_model_and_armory_expose_fidelity_numbers():
    v=read("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    preview=read("src/Twr.Godot/Scripts/Pass51OriginalLoadoutDisplayRuntime.cs")
    for field in ("SourceDetailProfileProxies","SourceLensProxies"):
        assert field in v
        assert field+" = 0" in v
    assert v.count("LastDetailProfileProxies")>=2
    assert v.count("LastLensProxies")>=2
    assert "PreviewDetailProfiles" in preview
    assert "PreviewLensDisks" in preview
    assert "RECONSTRUCTED SILHOUETTES" in preview
    assert "OPTIC DISKS" in preview
    assert "LOCAL TRIANGLE MESHES" in preview
    assert "SOURCE MESH IDS UNAVAILABLE" in preview
    assert "PreviewDetailProfiles = 0" in preview


def test_private_audit_pins_original_source_data_and_reports_no_triangle_recovery():
    s=read("tools/maps/audit_weapon_details58.py")
    assert "fa63bcfb3fc69c69c1066aa280372bdc8e2e093d61684c9c252c75c43d7080e7" in s
    assert "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2" in s
    assert 'len(data["models"])!=100' in s
    assert "if len(names)!=98" in s
    assert 'totals["total_parts"]!=1484 or len(mesh_ids)!=1137' in s
    assert '"original_mesh_triangle_bytes_recovered":0' in s
    assert 'with out_path.open("x",encoding="utf8")' in s
    assert 'if sha!=EXPECTED_SHA:' in s
    assert "urllib" not in s and "requests" not in s


def test_synthetic_original_tool_fixture_is_deterministic_and_non_destructive(tmp_path):
    script=ROOT/"tools/maps/make_pass58_weapon_detail_fixture.py"
    pack=tmp_path/"Content/Weapons/SourceWeaponModels.json.gz"
    mesh_dir=tmp_path/"Content/Assets/Meshes"
    obj=mesh_dir/"555580001.obj"
    args=[sys.executable,str(script),"--output",str(pack),
          "--mesh-dir",str(mesh_dir)]
    first=subprocess.run(args+["--create"],capture_output=True,text=True)
    assert first.returncode==0,first.stderr
    payload=json.loads(gzip.decompress(pack.read_bytes()))
    assert payload["synthetic"] is True
    parts=payload["models"]["Glock 17"]["parts"]
    assert len(parts)==13
    assert sum(part["opacity"]>.001 for part in parts)==12
    assert sum("meshId" in part for part in parts)==9
    assert parts[0]["meshId"]=="rbxassetid://555580001"
    assert {part["name"] for part in parts}>={
        "Barrel","Magazine","Stock","PistolGrip","TriggerGuard",
        "ReticleGlass","UnknownCasing","StockWedge","Receiver"}
    assert obj.read_text().startswith("# PASS58_SYNTHETIC_TETRAHEDRON")
    old=hashlib.sha256(pack.read_bytes()).hexdigest()
    assert subprocess.run(args+["--create"],capture_output=True).returncode!=0
    assert hashlib.sha256(pack.read_bytes()).hexdigest()==old
    subprocess.run(args+["--clean"],check=True,capture_output=True,text=True)
    assert not pack.exists() and not obj.exists()
    pack.parent.mkdir(parents=True,exist_ok=True)
    obj.parent.mkdir(parents=True,exist_ok=True)
    pack.write_bytes(b"OWNER DATA")
    obj.write_bytes(b"OWNER OBJ")
    assert subprocess.run(args+["--clean"],capture_output=True).returncode!=0
    assert pack.read_bytes()==b"OWNER DATA"
    assert obj.read_bytes()==b"OWNER OBJ"


def test_native_pass58_source_model_uses_no_player_authority_or_private_upload():
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    action=read(".github/workflows/windows-build.yml")
    assert 'if(args.Contains("--smoke-pass58",StringComparer.Ordinal))' in boot
    assert "RunPass58Smoke()" in boot
    assert "TWR_SMOKE_PASS58_DETAIL_GEOMETRY_OK" in boot
    assert "TWR_SMOKE_PASS58_DETAIL_GEOMETRY_OK" in action
    assert "SourceDetailProfileProxies!=5" in boot
    assert "SourceLensProxies!=1" in boot
    assert "SourceConservativeProxyParts!=1" in boot
    assert "SourceCircularProxyParts!=1" in boot
    assert "SourceNativePrimitiveParts!=3" in boot
    assert "SourceLocallyResolvedMeshes!=1" in boot
    assert "profile_mesh_cached=true" in boot
    assert "OriginalWeaponSourceRuntime" in boot
    assert "pass58-smoke-profile.json" in session
    assert "if(smoke54 || smoke55 || smoke56 || smoke58)" in session
    assert "twr-pass58-original-weapon-silhouettes" in action
    assert "--smoke-pass58" in action
    assert "make_pass58_weapon_detail_fixture.py --output $sourceTools --mesh-dir $meshDir --create" in action
    assert "make_pass58_weapon_detail_fixture.py --output $sourceTools --mesh-dir $meshDir --clean" in action
    assert "make_pass49_lobby_smoke_fixture.py --output $sourceLobby --clean" in action
    assert "555580001.obj" in action
    assert action.count("if: false && success()")>=14
    assert action.count("uses: actions/upload-artifact@v4")==action.count(
        "if: false && success()")
    assert not (ROOT/"content/Weapons/SourceWeaponModels.json.gz").exists()
    assert not (ROOT/"content/Assets/Meshes/555580001.obj").exists()
