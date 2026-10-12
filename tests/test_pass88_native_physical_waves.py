"""Pass88: physical ten-map wave and objective regression in Windows CI."""
from conftest import ROOT

def src(path):
    return (ROOT/path).read_text(encoding="utf-8")

def test_native_physics_wave_acceptance_preserves_domain_authority():
    c=src("src/Twr.Godot/Scripts/Pass88TenMapPhysicalWaveAcceptance.cs")
    for s in ("TWR_PASS88_MAP_OK","TWR_PASS88_MANOR_FLOOR_OBJECTIVE_OK",
              "TWR_SMOKE_PASS88_TEN_MAP_PHYSICAL_WAVES_OK"):
        assert s in c
    for s in ('Runtime.StartMap(map)', 'new GameplayRoot',
              'new InfectedAgent','Pass86NaturalLootPlacement.TryGround(',
              'VerifiedFallbackSpawns85', 'Pass84SecureZoneFloorSafety.CanAccumulate(',
              'Pass87FallbackObjectiveRotation.GetIndex(', 'FirstObjective(game)',
              'FinishWaveAndStartNext(game,2)', 'FinishWaveAndStartNext(game,3)',
              'Runtime.FailMatch(', 'Runtime.ReturnToLobby()', 'enemy.IsOnFloor()'):
        assert s in c if s!="new InfectedAgent" else 'OfType<InfectedAgent>()' in c
    assert "enemy.GlobalPosition =" not in c
    assert "Runtime.Player.Health =" not in c
    assert "Runtime.Match.Wave =" not in c

def test_native_ci_revalidates_pass78_pass83_and_all_new_passes():
    boot=src("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=src("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    ci=src(".github/workflows/windows-build.yml")
    assert 'new Pass88TenMapPhysicalWaveAcceptance' in boot
    assert '"--smoke-pass88"' in boot
    assert '"--smoke-pass88"' in session
    assert '"user://pass88-physical-waves-smoke-profile.json"' in session
    assert "else if(smoke88)" in session
    assert "twr-pass88-physical-wave-hardening" in ci
    assert "Smoke Pass 88 native ten-map verified spawn/loot" in ci
    assert "TWR_SMOKE_PASS88_TEN_MAP_PHYSICAL_WAVES_OK" in ci
    assert "github.ref_name == 'twr-pass83-manor-upper-gameplay' || github.ref_name == 'twr-pass88-physical-wave-hardening'" in ci
    assert ci.index("Smoke Pass 78") < ci.index("Smoke Pass 83") < ci.index("Smoke Pass 88")
    assert ci.index("Smoke Pass 88") < ci.index("Verify no private map scenes")
    assert ci.index("Verify no private map scenes") < ci.index("Private")
