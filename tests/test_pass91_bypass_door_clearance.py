"""Pass91 Bypass fallback shell entrances support larger Escort capsules."""
from conftest import ROOT

def read(path):
    return (ROOT / path).read_text(encoding="utf-8")

def test_bypass_plazas_recess_wall_behind_exterior_doors_only():
    source=read("src/Twr.Godot/Scripts/MapBlockoutBuilder.cs")
    assert 'InteriorShell(root, "PlazaNorth"' in source
    assert 'InteriorShell(root, "PlazaSouth"' in source
    assert source.count("frontDoorRelief91:true") == 4
    for name in ("CellBlockA","CellBlockB"):
        p=source.index(f'InteriorShell(root, "{name}"')
        assert "frontDoorRelief91:true" in source[p:p+240]
    assert "bool frontDoorRelief91=false" in source
    assert "const float clearance91=2.70f" in source
    assert "AddRecessedDivider91" in source
    assert 'name+"_DividerN"' in source
    assert 'name+"_DividerS"' in source
    assert "new Vector3(width,height,end-begin)" in source
    assert 'StaticBox(root,name,' in source

def test_npc_route_debugging_preserves_real_physics_movement():
    code=read("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    acceptance=read("src/Twr.Godot/Scripts/Pass91MultimapEscortAcceptance.cs")
    assert "EscortNextWaypoint91" in code
    assert "EscortRemainingWaypoints91" in code
    assert "npcNextWaypoint=" in acceptance
    assert "physicsVelocity=" in acceptance
    assert "escortGraphEdges=" in acceptance
    assert "survivor.GlobalPosition=" not in acceptance


def test_bypass_entrances_are_flush_not_impossible_vertical_steps():
    source=read("src/Twr.Godot/Scripts/MapBlockoutBuilder.cs")
    assert "var floorCenterY91=frontDoorRelief91" in source
    assert "? bottom-.10f" in source
    assert ": bottom+.10f" in source
    assert 'new Vector3(center.X,floorCenterY91,center.Z)' in source
    # The reconstructed arena Ground collider surface is Y=0.
    assert 'new Vector3(0,-0.5f,0), new Vector3(width,1,depth)' in source
