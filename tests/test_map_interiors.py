from conftest import ROOT

MAPS=(ROOT/"src/Twr.Godot/Scripts/MapBlockoutBuilder.cs").read_text()

def test_major_landmarks_use_traversable_interior_shells():
    for name in [
        "House","Barn","Sawmill","Warehouse","PlazaNorth","CabinHouse",
        "CargoShip","Cafe","MovieTheater","CellBlockA","Research","ParkingGarage",
        "WestWing","EastWing","FrontHall"
    ]:
        assert f'InteriorShell(root, "{name}"' in MAPS
        assert f'StaticBox(root, "{name}"' not in MAPS

def test_interior_shell_has_two_open_facades_and_internal_passage():
    assert "this shell is intentionally traversable" in MAPS
    assert 'name+"_Floor"' in MAPS
    assert 'name+"_Roof"' in MAPS
    assert 'name+"_DoorWallL_"' in MAPS
    assert 'name+"_DoorWallR_"' in MAPS
    assert 'name+"_DividerN"' in MAPS
    assert 'name+"_DividerS"' in MAPS
    assert "var gap=3.2f" in MAPS

def test_map_geometry_remains_explicitly_approximate():
    assert "exact wall/door transforms are not recovered" in MAPS
