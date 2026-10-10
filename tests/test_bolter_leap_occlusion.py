"""Bolter leaps must obey collision and floor visibility constraints."""
from conftest import ROOT

AGENT = (ROOT/"src/Twr.Godot/Scripts/InfectedAgent.cs").read_text(encoding="utf-8")


def test_bolter_leap_contact_requires_floor_proximity_and_sight():
    assert "if (!_leapHit && distance <= 1.7f &&" in AGENT
    block = AGENT.split("if (!_leapHit && distance <= 1.7f &&", 1)[1]
    assert "Math.Abs(Target.GlobalPosition.Y - GlobalPosition.Y) <= 1.6f" in block[:300]
    assert "HasClearAttackPath())" in block[:300]


def test_bolter_cannot_start_leap_through_solid_wall():
    block = AGENT.split("if (_specialCooldown <= 0 && distance >= 5f", 1)[1]
    assert "Math.Abs(Target.GlobalPosition.Y - GlobalPosition.Y) <= 2.1f" in block[:350]
    assert "HasClearAttackPath())" in block[:350]


def test_bolter_leap_recovery_and_damage_still_present():
    assert "_leapRecovery = 1.0;" in AGENT
    assert 'Runtime?.DamagePlayer(Math.Max(6f, Damage * 1.5f), "Bolter Leap")' in AGENT
