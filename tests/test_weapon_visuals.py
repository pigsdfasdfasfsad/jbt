"""First-person offline weapon category viewmodels and exported Windows smoke."""
from conftest import ROOT


def test_first_person_weapon_mesh_fallbacks_are_not_box_only():
    player=(ROOT/'src/Twr.Godot/Scripts/FirstPersonPlayer.cs').read_text()
    visual=(ROOT/'src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs').read_text()
    assert 'private WeaponViewModelRuntime _viewModel' in player
    assert '_viewModel.SetWeapon(spec)' in player
    assert '_viewModel.SetThrowable(type)' in player
    assert '_viewModel.Fire()' in player
    assert '_viewModel.Reload(_reloadTimer)' in player
    assert 'var box = (BoxMesh)_viewModel.Mesh;' not in player
    assert 'var box=(BoxMesh)_viewModel.Mesh;' not in player
    for name in ('PistolSlide','PistolGrip','Barrel','Stock','Magazine',
                 'MeleeHandle','LaunchTube','Bottle','Pump'):
        assert name in visual
    assert 'UsingPreparedScene' in visual
    assert 'res://Content/Assets/Weapons/' in visual
    assert 'ResourceLoader.Exists(source)' in visual
    assert 'HttpClient' not in visual


def test_windows_export_spawns_six_weapon_categories_and_throwable():
    build=(ROOT/'build/Windows/build.ps1').read_text()
    bootstrap=(ROOT/'src/Twr.Godot/Scripts/Bootstrap.cs').read_text()
    assert '--smoke-weapon-models' in build
    assert '--smoke-weapon-models' in bootstrap
    assert 'TWR_SMOKE_WEAPON_VISUALS_OK categories=6 throwables=1' in build
    assert 'TWR_SMOKE_WEAPON_VISUALS_OK categories=6 throwables=1' in bootstrap
    for name in ('Glock 17','Sawn Off Shotgun','AK-47','RPG-7','Flamethrower','2x4'):
        assert f'"{name}"' in bootstrap
