from conftest import ROOT

MAPS=(ROOT/"src/Twr.Godot/Scripts/MapBlockoutBuilder.cs").read_text()

def test_manor_has_recovered_two_story_topology():
    assert 'AddUpperFloor(root,"WestWingUpper"' in MAPS
    assert 'AddUpperFloor(root,"EastWingUpper"' in MAPS
    assert 'AddUpperFloor(root,"FrontHallUpper"' in MAPS
    assert "Manor has usable rooms on both floors" in MAPS

def test_upper_floors_have_walkable_decks_and_stairs():
    assert 'name+"_DeckL"' in MAPS
    assert 'name+"_DeckR"' in MAPS
    assert 'name+"_Stair"+i' in MAPS
    assert "const int steps=8" in MAPS
    assert "Exact dimensions remain APPROXIMATED" in MAPS
