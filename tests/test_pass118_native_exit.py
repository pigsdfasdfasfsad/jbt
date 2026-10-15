"""Pass118: Expressway must release its native scene and exit cleanly.

Static contracts protect the new teardown ordering; the actual Windows
process-exit requirement is enforced by the workflow, not mocked by pytest.
"""
from conftest import ROOT


def read(path):
    return (ROOT / path).read_text(encoding="utf8")


def test_native_smoke_does_not_quit_from_physics_callback():
    source = read("src/Twr.Godot/Scripts/Pass92ExpresswayEscortAcceptance.cs")
    assert "ScheduleQuitOnIdle(0);" in source
    assert "ScheduleQuitOnIdle(1);" in source
    assert 'GetTree().CallDeferred("quit", exitCode);' in source
    assert "GetTree().Quit(0)" not in source
    assert "GetTree().Quit(1)" not in source
    assert "SceneTree.SignalName.ProcessFrame" in source


def test_native_scene_free_is_verified_before_lobby_restart():
    source = read("src/Twr.Godot/Scripts/Pass92ExpresswayEscortAcceptance.cs")
    freed = source.index("game.QueueFree();")
    checked = source.index("if(GodotObject.IsInstanceValid(game))", freed)
    lobby = source.index("Runtime.ReturnToLobby();", checked)
    marker = source.index("TWR_PASS118_EXPRESSWAY_TEARDOWN_OK", lobby)
    assert freed < checked < lobby < marker
    assert "Runtime.FailMatch(" in source
    assert "Runtime.CompleteObjective(id,\"Escort\")" in source
    assert "TWR_PASS92_EXPRESSWAY_ESCORT_REWARD_OK" in source


def test_pass118_preserves_full_windows_matrix_and_repeats_clean_exit():
    workflow = read(".github/workflows/windows-build.yml")
    assert "twr-pass118-stable-native-shutdown" in workflow
    assert "Smoke Pass 91 real" in workflow
    assert "Smoke Pass 92 real" in workflow
    assert "TWR_PASS118_EXPRESSWAY_TEARDOWN_OK" in workflow
    assert "Pass92 attempt $attempt failed during gameplay or native shutdown" in workflow
    assert "$attempt -le $attempts" in workflow
    assert "System.AccessViolationException" in workflow
    assert workflow.count("uses: actions/upload-artifact@v4") == workflow.count("if: false && success()")


def test_private_source_gates_remain_enabled_on_pass118():
    workflow = read(".github/workflows/windows-build.yml")
    assert "Preflight Pass 117 authenticated private Laboratory installer refusal" in workflow
    assert "python -m pytest -q tests/test_pass117_private_source_installer.py" in workflow
    assert "Verify no private map scenes entered the CI export" in workflow
