from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_pause_overlay_controls_resume_restart_quit_and_volume():
    s=read("src/Twr.Godot/Scripts/PauseOverlayRuntime.cs")
    for token in ["RESUME","RESTART MAP","QUIT TO MENU","MASTER VOLUME","AudioServer.SetBusVolumeDb"]:
        assert token in s
    assert "ProcessModeEnum.WhenPaused" in s

def test_gameplay_pause_restart_and_quit_preserve_match_lifecycle():
    s=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "(key.Keycode is Key.P or Key.Escape)" in s
    assert 'FailMatch("Player restarted map")' in s
    assert 'FailMatch("Player quit to menu")' in s
    assert "Runtime?.ReturnToLobby()" in s
    assert "RestartRequested?.Invoke(MapName)" in s

def test_results_allow_restart_or_menu():
    s=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "[R] RESTART" in s and "[ENTER] MENU" in s
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    assert "_game.RestartRequested = RestartGame" in boot
    assert "private void RestartGame(string map)" in boot
