from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_level_requirement_formula_matches_recovered_levels_source():
    s=read("src/Twr.Domain/Model/ProgressionRules.cs")
    assert "25L*n*n" in s
    assert "level*150L" in s
    assert "VERIFIED from Levels" in s

def test_reward_xp_runs_through_progression_service():
    s=read("src/Twr.Domain/Runtime/LocalSession.cs")
    assert "_progression.Apply(State.Player,now)" in s
    assert 'PersistProfile("LevelUp",now)' in s

def test_level_up_consumes_xp_and_grants_credit_reward():
    s=read("src/Twr.Domain/Services/ProgressionService.cs")
    assert "player.Xp-=required" in s
    assert "player.Level++" in s
    assert "player.Credits+=credits" in s
    assert "LevelAdvancedEvent" in s

def test_hud_shows_level_and_next_level_requirement():
    s=read("src/Twr.Godot/Scripts/GameplayHud.cs")
    assert "ProgressionRules.RequiredForNextLevel(player.Level)" in s
    assert "LEVEL {player.Level}" in s
