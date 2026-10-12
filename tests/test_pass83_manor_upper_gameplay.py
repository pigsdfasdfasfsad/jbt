"""Pass83 cumulative real exported Windows fallback Manor acceptance."""
from conftest import ROOT

def read(path):
    return (ROOT/path).read_text(encoding="utf8")

def test_native_upper_floors_have_collision_and_real_domain_interactions():
    code=read("src/Twr.Godot/Scripts/Pass83ManorUpperGameplayAcceptance.cs")
    for marker in [
        "TWR_PASS83_BLOCKED_VOID_OK",
        "TWR_PASS83_CROSSING_ROUTE_OK",
        "TWR_PASS83_REAL_INFECTED_CROSSING_OK",
        "TWR_PASS83_UPPER_MARKERS_OK",
        "TWR_PASS83_PICKUP_FLOOR_GUARD_OK",
        "TWR_SMOKE_PASS83_MANOR_UPPER_GAMEPLAY_OK",
    ]:
        assert marker in code
    for token in [
        'Runtime.StartMap("Manor")',
        "new GameplayRoot",
        "new InfectedAgent",
        "new PickupActor",
        "new CollisionShape3D",
        "nav.GetRoute(",
        "nav.UpperCorridorSupported79(",
        "enemy.IsOnFloor()",
        "KeyInput(Key.E,true)",
        "Pass76InteractionSafety.CanReach",
        "Runtime.DamagePlayer(",
        "Runtime.FailMatch(",
        "Runtime.ReturnToLobby()",
    ]:
        assert token in code
    assert "enemy.GlobalPosition =" not in code
    assert "Runtime.Player.Health =" not in code
    assert "Runtime.Player.Credits =" not in code
    assert "Runtime.Match.Wave =" not in code

def test_windows_pipeline_compiles_runs_native_and_privacy_audits_pass83():
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    ci=read(".github/workflows/windows-build.yml")
    assert 'new Pass83ManorUpperGameplayAcceptance' in boot
    assert '"--smoke-pass83"' in boot and '"--smoke-pass83"' in session
    assert 'user://pass83-manor-upper-gameplay-smoke-profile.json' in session
    assert "else if(smoke83)" in session
    assert "Smoke Pass 83 native supported Manor upper crossings" in ci
    assert "TWR_SMOKE_PASS83_MANOR_UPPER_GAMEPLAY_OK" in ci
    assert "twr-pass83-manor-upper-gameplay" in ci
    assert ci.index("Smoke Pass 78") < ci.index("Smoke Pass 83")
    assert ci.index("Smoke Pass 83") < ci.index("Verify no private map scenes")
    assert "github.ref_name == 'twr-pass83-manor-upper-gameplay'" in ci
    assert "github.ref_name == 'twr-pass88-physical-wave-hardening'" in ci
