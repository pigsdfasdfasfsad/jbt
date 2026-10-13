"""Pass105 source contracts: a *real* wave-spawned infected remains active while
the normal player carries and E-deposits the final production Repair material.

This test does NOT claim human-operated combat survival or live zombie attacks.
"""
from conftest import ROOT


def src(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_live_wave2_enemy_pursues_while_repair_material_is_carried():
    s=src("src/Twr.Godot/Scripts/Pass103PhysicalLoadLifecycleAcceptance.cs")
    for expected in (
        "public bool VerifyPursuit105 { get; set; }",
        "private async Task<(InfectedAgent enemy,Vector3 start)> SpawnRealPursuer105(",
        "game._Process(.05);",
        "game.SetProcess(false);",
        "actor.Target!=player",
        "game.VerifiedFallbackSpawns85<1",
        "await Frames(90);",
        "!actor.IsOnFloor()",
        "Runtime.Player?.IsAlive!=true",
        "horizontal.Length()<.30f",
        "item==count-1",
        "await SpawnRealPursuer105(game,player,map);",
        "await WalkSafely(",
        "await PressE();",
        "objective.Pass98CarryingMaterial",
        "livePursuer105.Health<=0f",
        "final_material_deposited_with_enemy_alive=true",
        "TWR_PASS105_MAP_REPAIR_PURSUIT_OK map=",
        "TWR_SMOKE_PASS105_LIVE_REPAIR_PURSUIT_OK",
        "player_combat_kill_not_tested=true",
        "full_combat_survival_unverified=true",
    ):
        assert expected in s, expected
    assert "player.GlobalPosition=" not in s
    assert "player.Velocity=" not in s
    assert "actor.GlobalPosition=" not in s
    assert "actor.MoveSpeed=" not in s
    assert "actor.Damage=" not in s
    assert "actor.Health=" not in s
    assert "Runtime.HealPlayer(" not in s
    assert "Runtime.DamagePlayer(" not in s
    assert "MoveAndSlide();" in src("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    infected=src("src/Twr.Godot/Scripts/InfectedAgent.cs")
    assert "public override void _PhysicsProcess(double delta)" in infected
    assert "Runtime.DamagePlayer(Damage, InfectedType)" in infected


def test_native_pass105_gated_with_pass103_104_and_all_earlier_physics():
    boot=src("src/Twr.Godot/Scripts/Bootstrap.cs")
    local=src("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    ci=src(".github/workflows/windows-build.yml")
    assert 'args.Contains("--smoke-pass105"' in boot
    assert "VerifyPursuit105=true" in boot
    assert "ExtendToRepair104=true" in boot
    assert 'var smoke105=OS.GetCmdlineUserArgs().Contains("--smoke-pass105"' in local
    assert "pass105-live-repair-pursuit-smoke-profile.json" in local
    assert "else if(smoke105)" in local
    assert "twr-pass105-live-repair-pursuit" in ci
    for step in (
        "Preflight Pass 103 real full production Load physical walking",
        "Preflight Pass 104 full production Wave2 Repair by physical walking",
        "Preflight Pass 105 real Wave2 infected pursuit during Repair material deposit",
        "Preflight Pass 96 ten-map real Load/Repair material access",
        "Preflight Pass 96 full native fifteen-wave match and same-map payout replay",
        "Smoke Pass 78 native three Manor stair flights",
        "Verify no private map scenes entered the CI export",
    ):
        assert step in ci
    for marker in (
        "TWR_PASS105_MAP_REPAIR_PURSUIT_OK map=$map production_wave=2",
        "TWR_SMOKE_PASS105_LIVE_REPAIR_PURSUIT_OK maps=Ranch,Manor full_wave2_repairs=2",
        "final_material_deposited_with_enemy_alive=true",
        "physics_enemy_alive_at_deposit=true",
        "TWR_SMOKE_PASS102_ALL_SPAWN_OBJECTIVE_INGRESS_OK",
        "TWR_PASS100_MAP_WALK_OK map=$map family=$family",
    ):
        assert marker in ci
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()"
    )
