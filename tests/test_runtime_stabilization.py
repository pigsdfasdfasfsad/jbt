"""Source contracts supplement (not replace) native readiness/exit acceptance."""
from conftest import ROOT


def read(name):
    return (ROOT / 'src/Twr.Godot/Scripts' / name).read_text(encoding='utf8')


def test_control_gate_covers_input_physics_and_independent_action_entries():
    player = read('FirstPersonPlayer.cs')
    assert 'public bool GameplayControlsEnabled' in player
    for signature in ('public override void _PhysicsProcess(double delta)',
                      'public override void _UnhandledInput(InputEvent @event)',
                      'private void BeginReload()', 'private void TryUseWeapon()',
                      'private void TryThrowThrowable()'):
        body = player.split(signature, 1)[1].split('{', 1)[1]
        assert body.lstrip().startswith('if (!GameplayControlsEnabled) return;')
    assert 'SetProcessUnhandledInput(enabled)' in player


def test_death_is_processed_before_waiting_for_source_planning():
    game = read('GameplayRoot.cs')
    process = game.split('public override void _Process(double delta)', 1)[1]
    assert process.index('if (!_finished && !Runtime.Player.IsAlive)') < process.index(
        'if (_sourcePlayerPlacementPending121 ||')
    assert '_player.SetGameplayControlsEnabled(false);' in game
    assert '_player.SetGameplayControlsEnabled(true);' in game
    assert '_naturalRespawns.Clear();' in game
    assert 'StopTransientActors();' in game
    assert 'infected.SpecialAttackRequested = null;' in game


def test_shutdown_drains_frames_without_swallowing_a_failing_exit():
    shutdown = read('RuntimeShutdownCoordinator.cs')
    assert '_drainUntil = _physicsTicks + 2;' in shutdown
    assert 'if (tree.Root.GetChildCount() > 1) return;' in shutdown
    assert 'GC.WaitForPendingFinalizers();' in shutdown
    assert 'if (exitCode != 0) existing._exitCode = exitCode;' in shutdown
    assert 'tree.CallDeferred("quit", _exitCode);' in shutdown
    assert 'Environment.Exit' not in shutdown
    for source in ('Pass90EscortCompletionAcceptance.cs', 'Pass94ManorEscortAcceptance.cs'):
        code = read(source)
        assert 'RuntimeShutdownCoordinator.Request(GetTree(), 0);' in code
        assert 'RuntimeShutdownCoordinator.Request(GetTree(), 1);' in code


def test_readiness_regression_uses_actual_game_and_authoritative_commands():
    code = read('RuntimeReadinessAcceptance.cs')
    assert 'new GameplayRoot' in code
    assert 'player._UnhandledInput(reload);' in code
    assert 'Runtime.DamagePlayer(10000' in code
    assert 'Runtime.Match.SaveCommitted != true' in code
    assert 'Runtime.Player.Health =' not in code
    assert 'TWR_SMOKE_RUNTIME_READINESS_OK' in code
