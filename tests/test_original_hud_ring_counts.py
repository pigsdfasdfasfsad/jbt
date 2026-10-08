"""Source original HUD numeric ring counts must be reflected in Godot layout."""
from conftest import ROOT

def test_rings_use_original_roblox_gui_increment_counts():
    dial=(ROOT/"src/Twr.Godot/Scripts/WeaponDialHud.cs").read_text()
    hud=(ROOT/"src/Twr.Godot/Scripts/GameplayHud.cs").read_text()
    for count in (208,125,92):
        assert f"{count}, _" in dial
    assert "private void DrawSourceRing(" in dial
    assert "hpFraction, armorRatio, xpRatio" in hud
    assert 'player.ArmorKind == "Juggernaut" ? 80f : 40f' in hud
    assert "ProgressionRules.RequiredForNextLevel(player.Level)" in hud
