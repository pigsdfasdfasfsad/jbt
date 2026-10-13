"""Pass73 native ground nav and full-map movement/interaction guards."""
from conftest import ROOT


def read(path):
    return (ROOT / path).read_text(encoding="utf8")


def test_bounded_colliders_and_no_source_map_path_replacement():
    nav=read("src/Twr.Godot/Scripts/Pass73GroundNavigationRuntime.cs")
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    enemy=read("src/Twr.Godot/Scripts/InfectedAgent.cs")
    assert "MaximumGridCells = 2_800" in nav
    assert "SampleBudgetPerPhysicsFrame = 120" in nav
    assert "LinkBudgetPerPhysicsFrame = 90" in nav
    assert "MaximumRouteCache = 192" in nav
    assert "PhysicsRayQueryParameters3D.Create" in nav
    assert "PhysicsShapeQueryParameters3D" in nav
    assert "space.IntersectShape(capsuleQuery, 1)" in nav
    assert "MaximumAnchorVertical" in nav
    assert "CorridorClear(from, to)" in nav
    assert "GetPointPath(start, end)" in nav
    assert "!originalWorldAvailable && _sourceNavigator is null" in game
    assert 'MapName != "Expressway"' in game
    assert "Pass73GroundNavigationRuntime.Attach(" in game
    assert "GroundNavigator73 = _groundNavigation73" in game
    assert "GroundNavigator73!.GetRoute" in enemy
    assert "SteerAroundObstacles(desired)" in enemy
    assert "HighwayNavigator!.GetRoute" in enemy
    assert "SourceNavigator!.GetRoute" in enemy


def test_jump_input_buffer_expires_and_dead_interactions_cannot_reward():
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    pickup=read("src/Twr.Godot/Scripts/PickupActor.cs")
    objective=read("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    assert "JumpBufferSeconds73 = .14" in player
    assert "_jumpRequestRemaining73 = Math.Max(0," in player
    assert "if (_jumpRequestRemaining73 <= 0) _jumpRequested = false" in player
    assert "_jumpRequestRemaining73 = JumpBufferSeconds73;" in player
    assert "Runtime?.Player?.IsAlive != true" in pickup
    assert objective.count("Runtime?.Player?.IsAlive != true") >= 3
    assert "CompleteObjective(ObjectiveId, Family)" in objective


def test_native_windows_real_actor_keyboard_pursuit_and_rewards():
    harness=read("src/Twr.Godot/Scripts/Pass73FullMapGameplayAcceptance.cs")
    bootstrap=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    ci=read(".github/workflows/windows-build.yml")
    assert "foreach (var map in ReleaseRules.Maps)" in harness
    assert 'new GameplayRoot' in harness
    assert "Input.ParseInputEvent(new InputEventKey" in harness
    assert "SetKey(Key.W, true)" in harness
    assert "SetKey(Key.Shift, true)" in harness
    assert "SetKey(Key.Space, true)" in harness
    assert "new StaticBody3D" in harness
    assert "navigator.GetRoute(south, north)" in harness
    assert "enemy.GroundNavigator73 != navigator" in harness
    assert "enemy.IsOnFloor()" in harness
    assert "new PickupActor" in harness
    assert "Runtime.Player.Health <= injured" in harness
    assert "new ObjectiveRuntime" in harness
    assert "Runtime.Match!.CompletedObjectives.Contains(objectiveId)" in harness
    assert "Runtime.Player.Inventory.GetValueOrDefault(\"Frag\") != fragBefore" in harness
    assert "Runtime.Player.Health =" not in harness
    assert "Runtime.Player.Credits =" not in harness
    assert "Runtime.AdvanceWave()" not in harness
    assert '"--smoke-pass73"' in bootstrap
    assert '"user://pass73-ground-navigation-smoke-profile.json"' in session
    assert "else if(smoke73)" in session
    assert "twr-pass73-fullmap-ground-navigation" in ci
    assert "TWR_SMOKE_PASS73_FULLMAP_GAMEPLAY_OK maps=10" in ci
    assert ci.index("Smoke Pass 72") < ci.index("Smoke Pass 73")
    assert ci.index("Smoke Pass 73") < ci.index("Verify no private map scenes")
    # Later passes may rerun this same native acceptance; preserve the
    # original branch selector without requiring it to be the only branch.
    assert "github.ref_name == 'twr-pass73-fullmap-ground-navigation'" in ci
    assert "if: success()" in ci
    # Shared owner-privacy exclusion also covers subsequent pass branches.
