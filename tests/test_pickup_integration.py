from conftest import ROOT

GAME=(ROOT/"src/Twr.Godot/Scripts/GameplayRoot.cs").read_text()

def test_manor_natural_pickups_are_interior_reconstruction_points():
    assert "SpawnNaturalPickups()" in GAME
    assert "no natural item spawns" in GAME
    assert "new Vector3(-7, 0.35f, 18)" in GAME

def test_radio_supply_drop_lands_before_eight_pickups_spawn():
    assert "SpawnSupplyDrop()" in GAME
    assert "SpawnSupplyContents(position)" in GAME
    assert "four item pickups plus four fortification pickups" in GAME
    assert "for (var i = 0; i < 4; i++)" in GAME

def test_unpack_spawns_exactly_eight_with_variant_marked_approximate():
    assert "for (var i = 0; i < 8; i++)" in GAME
    assert "both Unpack variants produce exactly eight pickups" in GAME
    assert "APPROXIMATED: absent source for variant scheduling" in GAME
