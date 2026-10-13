"""Pass98: one E press across overlapping pickups/materials and full objective life."""
from conftest import ROOT


def src(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_materials_share_press_budget_with_world_pickups():
    safety = src("src/Twr.Godot/Scripts/Pass76InteractionSafety.cs")
    objective = src("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    player = src("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    pickup = src("src/Twr.Godot/Scripts/PickupActor.cs")
    assert "public static bool IsInteractionPressClaimed(" in safety
    assert "public static void ClaimInteractionPress(" in safety
    assert "ClaimedPickupPresses.Contains(player.GetInstanceId())" in safety
    assert "ClaimedPickupPresses.Remove(player.GetInstanceId())" in safety
    assert "Pass76InteractionSafety.ObservePickupKey(" in player
    assert "Pass76InteractionSafety.ClaimPickupPress(Player)" in pickup
    assert "Pass76InteractionSafety.IsInteractionPressClaimed(Player)" in objective
    assert "Pass76InteractionSafety.ClaimInteractionPress(Player)" in objective
    assert "interactPressed &&" in objective
    assert "private bool TickFillInteract()" in objective
    assert "if (_carrying)" in objective


def test_material_pickup_uses_nearest_real_visible_item():
    objective = src("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    assert "item.IsQueuedForDeletion()" in objective
    assert "distance<=2.5f && distance<nearestDistance" in objective
    assert "Pass76InteractionSafety.CanReach(" in objective
    assert "Pass98CarryingMaterial => _carrying;" in objective
    assert "Pass98DepositedMaterials => _deposited;" in objective
    assert "Pass98RemainingMaterials => _items.Count;" in objective
    assert "Pass98RequiredMaterials => _required;" in objective
    assert "return false;" in objective


def test_exported_real_production_load_repair_completion_on_two_maps():
    acceptance = src("src/Twr.Godot/Scripts/Pass98MaterialLifecycleAcceptance.cs")
    for marker in (
        'await VerifyMap("Ranch")',
        'await VerifyMap("Manor")',
        'family=wave==1?"Load":"Repair"',
        "game._Process(RegularWaveRules.CountdownSeconds+.25)",
        "Runtime.Match?.Phase!=MatchPhase.Wave",
        "Input.ParseInputEvent(new InputEventKey",
        "Pass98RemainingMaterials!=count-item-1",
        "Pass98CarryingMaterial",
        "Pass98DepositedMaterials!=item+1",
        "Runtime.Match?.CompletedObjectives.Contains(id)!=true",
        "Runtime.CompleteObjective(id,family)",
        "ProgressionRules.RequiredForNextLevel",
        "TWR_PASS98_OBJECTIVE_E_COMPLETE_OK",
        "TWR_PASS98_SHARED_E_PRESS_OK",
        "await VerifyContendedPress(game,player)",
        "overlapping_objectives=2",
        "TWR_SMOKE_PASS98_MATERIAL_LIFECYCLE_OK",
        "Runtime.ReturnToLobby()",
        "Runtime.Match.SaveCommitted",
    ):
        assert marker in acceptance
    assert "objective.GlobalPosition=" not in acceptance
    assert "objective.Pass98DepositedMaterials=" not in acceptance
    assert "Runtime.Player.Credits=" not in acceptance
    assert "Runtime.Player.Xp=" not in acceptance


def test_native_ci_reruns_material_physics_reward_and_private_asset_regressions():
    ci = src(".github/workflows/windows-build.yml")
    boot = src("src/Twr.Godot/Scripts/Bootstrap.cs")
    session = src("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    assert '"--smoke-pass98"' in boot and '"--smoke-pass98"' in session
    assert "new Pass98MaterialLifecycleAcceptance" in boot
    assert "pass98-full-material-lifecycle-smoke-profile.json" in session
    assert "else if(smoke98)" in session
    assert "Preflight Pass 98 real full Load and Repair E-key completions" in ci
    assert "TWR_SMOKE_PASS98_MATERIAL_LIFECYCLE_OK" in ci
    assert "TWR_PASS98_SHARED_E_PRESS_OK" in ci
    assert "TWR_SMOKE_PASS96_GROUNDED_MATERIALS_OK" in ci
    assert "TWR_PASS97_MATERIALS_IN_FULL_MATCH_OK" in ci
    assert ci.index("Preflight Pass 98") < ci.index("Smoke Pass 78")
    assert ci.index("Smoke Pass 94") < ci.index("Verify no private map scenes")
    assert "twr-pass98-material-objective-completion" in ci
    assert ci.count("uses: actions/upload-artifact@v4") == ci.count(
        "if: false && success()"
    )
