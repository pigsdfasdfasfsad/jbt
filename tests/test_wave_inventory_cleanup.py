from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_body_armor_expires_at_wave_boundary():
    s=read("src/Twr.Domain/Runtime/LocalSession.cs")
    assert "case EndWaveCleanupCommand:" in s
    assert "State.Player.ArmorDurability = 0" in s
    g=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "Runtime.EndWaveCleanup();" in g

def test_death_loses_carried_items():
    s=read("src/Twr.Domain/Runtime/LocalSession.cs")
    failure=s.index("case FailMatchCommand x:")
    clear=s.index("State.Player.Inventory.Clear();", failure)
    persist=s.index('PersistProfile("MatchFailure"', failure)
    assert clear < persist
