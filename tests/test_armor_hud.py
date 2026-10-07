from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_armor_hud_uses_four_documented_segments():
    s=read("src/Twr.Godot/Scripts/GameplayHud.cs")
    assert "new ColorRect[4]" in s
    assert "for(var i=0;i<4;i++)" in s
    assert "var max=juggernaut ? 80f : 40f" in s
    assert "var per=max/4f" in s

def test_body_armor_and_juggernaut_use_documented_colors():
    s=read("src/Twr.Godot/Scripts/GameplayHud.cs")
    assert "230f/255f,140f/255f,40f/255f" in s
    assert "70f/255f,140f/255f,230f/255f" in s
    assert 'player.ArmorKind=="Juggernaut"' in s
