import json
from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_runtime_weapon_catalog_loads_source_handling_fields():
    s=read("src/Twr.Godot/Scripts/RuntimeWeaponCatalog.cs")
    for token in ['"FOV"','"HorizontalRecoil"','"VerticleRecoil"','"RecoilShake"']:
        assert token in s
    assert 'Duration(root, "Aim", 0.5)' in s
    assert 'Duration(root, "UnAim", 0.25)' in s
    assert 'Duration(root, "Equip"' in s

def test_source_weapon_examples_keep_distinct_fov_and_recoil():
    m110=json.loads((ROOT/"content/weapons/entries/m110-sass.json").read_text())
    glock=json.loads((ROOT/"content/weapons/entries/glock-17.json").read_text())
    assert m110["stats"]["FOV"] == 10
    assert glock["stats"]["FOV"] == 60
    assert m110["stats"]["VerticleRecoil"] > glock["stats"]["VerticleRecoil"]

def test_player_ads_recoil_and_equip_timing_use_catalog_values():
    s=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert "UpdateAim(delta,spec)" in s
    assert "spec.AimFov" in s
    assert "spec.AimSeconds" in s and "spec.UnAimSeconds" in s
    assert "ApplyRecoil(spec)" in s
    assert "spec.VerticalRecoil" in s and "spec.HorizontalRecoil" in s
    assert "spec.EquipSeconds/equipSpeed" in s
    assert "APPROXIMATED Godot viewmodel alignment" in s

def test_marksman_speed_perks_scale_speed_not_duration_subtraction():
    s=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert 'HasPerk("Dexterous")==true ? 1.3 : 1.0' in s
    assert 'HasPerk("Eagle Eyes")==true ? 1.4 : 1.0' in s
    assert 'HasPerk("Brisk") == true ? 1.2 : 1.0' in s
    assert "spec.ReloadSeconds / reloadSpeed" in s
    assert 'HasPerk("Steady Hand")==true ? 0.5f : 1f' in s

def test_carpenter_build_speed_uses_one_point_four_speed_multiplier():
    s=read("src/Twr.Godot/Scripts/FortificationController.cs")
    assert 'HasPerk("Carpenter") ? SwingSeconds/1.4' in s
