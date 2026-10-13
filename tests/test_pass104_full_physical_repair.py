"""Pass104: actual Wave1->2 progression and no-teleport full Repair mission."""
from conftest import ROOT


def src(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_repair_reuses_verified_native_player_physics_and_authority():
    impl=src("src/Twr.Godot/Scripts/Pass103PhysicalLoadLifecycleAcceptance.cs")
    for fragment in (
        "public bool ExtendToRepair104 { get; set; }",
        "private async Task WalkRepairWave104(",
        "if(ExtendToRepair104)",
        "await WalkRepairWave104(game,player,map,id);",
        "Runtime.Match.CompletedObjectives.Contains(completedLoadId)",
        "game._Process(RegularWaveRules.WaveDurationSeconds+.25);",
        "game._Process(RegularWaveRules.WaveEndSeconds+.25);",
        "game._Process(RegularWaveRules.IntermissionSeconds+.25);",
        "game._Process(RegularWaveRules.CountdownSeconds+.25);",
        'Runtime.Match.Wave!=2 || game.WaveStage!="WAVE"',
        'x.Family=="Repair"',
        "objective.Pass96MaterialFeet.ToArray()",
        "for(var item=0;item<count;item++)",
        "Pass89EscortSurfaceSafety.CanTravel(",
        "GetVerifiedEscortRoute89(",
        "await WalkSafely(",
        "InputEvent(Key.W,true)",
        "InputEvent(Key.W,false)",
        "InputEvent(Key.E,true)",
        "InputEvent(Key.E,false)",
        "objective.Pass98RemainingMaterials",
        "objective.Pass98DepositedMaterials",
        "objective.Pass98CarryingMaterial",
        "Runtime.CompleteObjective(id,\"Repair\")",
        "ValidateReward(expected,map+\"/wave2-Repair-replayed-receipt\")",
        "Runtime.FailMatch(ExtendToRepair104",
        "Runtime.Match.SaveCommitted",
        "_completedRepairs104!=2",
        "_repairDeposits104<20",
        "TWR_PASS104_MAP_FULL_REPAIR_OK map=",
        "TWR_SMOKE_PASS104_FULL_PHYSICAL_REPAIR_OK",
    ):
        assert fragment in impl, fragment
    assert "player.GlobalPosition=" not in impl
    assert "player.Velocity=" not in impl
    assert "objective.GlobalPosition=" not in impl
    assert "Runtime.Player.Credits=" not in impl
    assert "Runtime.Player.Xp=" not in impl
    assert "Runtime.HealPlayer(" not in impl
    assert "Runtime.DamagePlayer(" not in impl
    assert "MoveAndSlide();" in src("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")


def test_pass104_windows_preflight_preserves_prior_full_load_and_ten_map_gates():
    boot=src("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=src("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    ci=src(".github/workflows/windows-build.yml")
    assert 'args.Contains("--smoke-pass104"' in boot
    assert "ExtendToRepair104=true" in boot
    assert 'var smoke104=OS.GetCmdlineUserArgs().Contains("--smoke-pass104"' in session
    assert "pass104-physical-repair-lifecycle-smoke-profile.json" in session
    assert "else if(smoke104)" in session
    assert "twr-pass104-full-physical-repair" in ci
    assert "Preflight Pass 103 real full production Load physical walking" in ci
    assert "Preflight Pass 104 full production Wave2 Repair by physical walking" in ci
    assert "TWR_PASS104_MAP_FULL_REPAIR_OK map=$map production_wave=2 family=Repair material_items=10" in ci
    assert "TWR_SMOKE_PASS104_FULL_PHYSICAL_REPAIR_OK maps=Ranch,Manor completed_wave1_loads=2 completed_wave2_repairs=2 wave2_material_deposits=20" in ci
    assert "TWR_SMOKE_PASS102_ALL_SPAWN_OBJECTIVE_INGRESS_OK newly_verified_maps=5 total_verified_maps=10" in ci
    assert "TWR_PASS100_MAP_WALK_OK map=$map family=$family" in ci
    assert "TWR_PASS99_GLOBAL_MATERIAL_CARRY_OK" in ci
    assert "Smoke Pass 78 native three Manor stair flights" in ci
    assert "Verify no private map scenes entered the CI export" in ci
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()"
    )
