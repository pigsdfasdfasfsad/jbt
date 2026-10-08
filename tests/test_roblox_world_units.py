"""Roblox-authoring units must be converted consistently at simulation boundaries."""
from conftest import ROOT


def source(name):
    return (ROOT/'src/Twr.Godot/Scripts'/name).read_text()


def test_roblox_stud_to_metre_factor_is_shared():
    units=source('RobloxUnits.cs')
    assert 'public const float MetersPerStud = 0.28f;' in units
    lab=source('LaboratorySourceLoader.cs')
    assert 'private const float Stud = RobloxUnits.MetersPerStud;' in lab


def test_player_and_infected_speed_values_are_scaled_into_metres():
    player=source('FirstPersonPlayer.cs')
    infected=source('InfectedAgent.cs')
    game=source('GameplayRoot.cs')
    assert '17f * RobloxUnits.MetersPerStud' in player
    assert '24f * RobloxUnits.MetersPerStud' in player
    assert '15f * RobloxUnits.MetersPerStud' in infected
    assert 'definition.WalkSpeed * speedScale * RobloxUnits.MetersPerStud' in game


def test_recovered_weapon_distances_are_converted_before_raycast():
    player=source('FirstPersonPlayer.cs')
    assert 'range * RobloxUnits.MetersPerStud' in player
    assert 'var remaining = Math.Max(0.1f, range * RobloxUnits.MetersPerStud);' in player
