from conftest import ROOT

def read(path):
    return (ROOT/path).read_text()

def test_fill_objectives_use_documented_item_counts_and_carry_limit():
    s=read("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    assert all(x in s for x in ["Generator", "Tool Box", "Propane Tank A", "Water Jug C", "Can Package C"])
    assert all(x in s for x in ["Spark Plug D", "Wheel B", "Jerry Can D"])
    assert "if (_carrying)" in s
    assert "_carrying = true" in s

def test_secure_and_escort_mechanics_are_present():
    s=read("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    assert 'case "Radio":' in s and 'case "Unpack":' in s
    assert "HOLD E IN RING" in s
    assert "EscortSurvivor" in s
    assert "APPROXIMATED interaction pacing" in s

def test_wave_objectives_feed_survival_bonus_count():
    s=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "SpawnWaveObjectives()" in s
    assert "_completedObjectivesThisWave++" in s
    assert "AwardWaveSurvival(survivedWave, _completedObjectivesThisWave, 1)" in s
    assert "maximum of one or two objectives per wave" in s
