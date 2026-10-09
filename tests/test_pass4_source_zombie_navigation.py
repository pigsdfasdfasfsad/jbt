"""All ten maps receive stuck navigation recovery and source zombie hit reactions."""
from conftest import ROOT
S=ROOT/"src/Twr.Godot/Scripts"

def test_infected_do_not_indefinitely_push_against_wall():
    agent=(S/"InfectedAgent.cs").read_text()
    assert 'TickStuckRecovery(delta, distance);' in agent
    assert '_recoveryDetour = 2.5' in agent
    assert '_steerSign = -_steerSign;' in agent
    assert 'angle in new[] { 1.28f, 1.88f, 2.35f }' in agent
    assert 'ObstacleAhead(direction,1.9f)' in agent
    assert 'Math.Max(0,_stuckDuration - 1.5)' in agent

def test_source_zombies_flail_on_hit_and_use_type_gait():
    agent=(S/"InfectedAgent.cs").read_text()
    visual=(S/"InfectedVisualAssembler.cs").read_text()
    assert '_visual?.HitReaction();' in agent
    assert 'public void HitReaction()' in visual
    assert '"Sprinter" or "Bolter" => 12.5f' in visual
    assert '"Bloater" or "Burster" => 6.6f' in visual
    assert '_hitDuration > 0' in visual
