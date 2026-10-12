"""Pass71 native exported Windows multi-map combat and fatal damage regression."""
from conftest import ROOT


def read(path):
    return (ROOT / path).read_text(encoding="utf8")


def test_native_multimap_real_pursuit_and_melee_occlusion():
    source = read("src/Twr.Godot/Scripts/Pass71NativeMultiMapDamageAcceptance.cs")
    infected = read("src/Twr.Godot/Scripts/InfectedAgent.cs")
    assert 'new[] { "Laboratory", "Manor" }' in source
    assert "new GameplayRoot" in source
    assert "new FirstPersonPlayer" not in source
    assert "game.GetChildren().OfType<InfectedAgent>()" in source
    assert "infected.MoveSpeed = 3.2f" in source
    assert "pursuedDistance >= startDistance - .65f" in source
    assert "infected.IsOnFloor()" in source
    assert "new StaticBody3D" in source
    assert "wall.QueueFree()" in source
    assert "Runtime.Player.Health" in source
    assert "HasClearAttackPath()" in infected
    assert "Runtime.DamagePlayer(Damage, InfectedType)" in infected
    assert "game.WaveStage != \"RESULTS\"" in source
    assert "Runtime.Match.SaveCommitted" in source
    assert "Runtime.ReturnToLobby()" in source


def test_dead_players_cannot_finish_reload_or_resume_firing():
    player = read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    source = read("src/Twr.Godot/Scripts/Pass71NativeMultiMapDamageAcceptance.cs")
    physics = player[player.index("public override void _PhysicsProcess"):player.index("public override void _UnhandledInput")]
    assert physics.index("!Runtime.Player.IsAlive") < physics.index("if (_reloadTimer > 0)")
    assert "_reloadTimer = 0;" in physics
    assert "_reloadWeapon = null;" in physics
    handler = player[player.index("public override void _UnhandledInput"):player.index("public void SetMountedMode")]
    assert handler.index("!Runtime.Player.IsAlive") < handler.index("InputEventMouseMotion motion")
    assert "player.ReloadRemainingSeconds70 <= 0" in source
    assert "Runtime.SpendAmmo(weapon.Name, 2)" in source
    assert "player.ReloadRemainingSeconds70 > 0" in source
    assert "Runtime.AwardKill(" not in source
    assert "Runtime.DamagePlayer(" not in source
    assert "Runtime.Player.Health =" not in source


def test_isolated_profile_export_ci_and_private_asset_gate():
    session = read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    bootstrap = read("src/Twr.Godot/Scripts/Bootstrap.cs")
    workflow = read(".github/workflows/windows-build.yml")
    assert '"--smoke-pass71"' in session
    assert 'user://pass71-multimap-damage-smoke-profile.json' in session
    assert "if(smoke70 || smoke71)" in session
    assert "new Pass71NativeMultiMapDamageAcceptance" in bootstrap
    assert "twr-pass71-native-multimap-damage" in workflow
    assert "TWR_PASS71_MAP_OK map=Laboratory" in workflow
    assert "TWR_PASS71_MAP_OK map=Manor" in workflow
    assert "TWR_SMOKE_PASS71_MULTIMAP_DAMAGE_OK" in workflow
    assert workflow.index("Smoke Pass 70") < workflow.index("Smoke Pass 71")
    assert workflow.index("Smoke Pass 71") < workflow.index("Verify no private map scenes")
    assert "github.ref_name == 'twr-pass71-native-multimap-damage')" in workflow
