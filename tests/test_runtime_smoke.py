from conftest import ROOT

def test_windows_build_launches_exported_playable_scene_headlessly():
    build=(ROOT/"build/Windows/build.ps1").read_text()
    bootstrap=(ROOT/"src/Twr.Godot/Scripts/Bootstrap.cs").read_text()
    assert "Start-Process -FilePath $exportExe" in build
    assert "--smoke-play" in build
    assert "--quit-after','120" in build
    assert "Exported playable smoke pass" in build
    assert 'StartGame("Manor")' in bootstrap
    assert "GetCmdlineUserArgs" in bootstrap
