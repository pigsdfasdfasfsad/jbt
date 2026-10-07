from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_fortification_catalog_is_content_driven():
    s=read("src/Twr.Godot/Scripts/FortificationCatalogRuntime.cs")
    assert 'Content/fortifications/fortifications.json' in s
    for field in ["count", "swings", "damage", "radius"]:
        assert f'"{field}"' in s

def test_hammer_controls_and_wave_cleanup_exist():
    s=read("src/Twr.Godot/Scripts/FortificationController.cs")
    assert "Key.F" in s
    assert "MouseButton.WheelUp" in s and "MouseButton.WheelDown" in s
    assert "Input.IsMouseButtonPressed(MouseButton.Left)" in s
    assert "definition.Swings" in s
    assert "Runtime.ConsumeItem(definition.Name)" in s
    assert "ClearDeployed()" in s

def test_unrecovered_fortification_rates_are_labeled_approximate():
    controller=read("src/Twr.Godot/Scripts/FortificationController.cs")
    actor=read("src/Twr.Godot/Scripts/FortificationActor.cs")
    assert "APPROXIMATED: exact base hammer swing cadence" in controller
    assert 'ApplyDamage(15f, false, "BarbedWire")' in actor
    assert "APPROXIMATED: source confirms slowing but not magnitude/duration" in actor
