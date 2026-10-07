from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_enhanced_electronics_applies_exact_40_percent_to_clap_bomb_radius():
    actor=read("src/Twr.Godot/Scripts/FortificationActor.cs")
    controller=read("src/Twr.Godot/Scripts/FortificationController.cs")
    assert "RangeMultiplier" in actor
    assert "Definition.Radius!.Value * RangeMultiplier" in actor
    assert 'HasPerk("Enhanced Electronics")' in controller
    assert "return 1.4f" in controller
    assert "STRONGLY INFERRED" in actor

def test_hardened_sight_and_bruiser_reduce_reconstructed_screen_effects():
    hud=read("src/Twr.Godot/Scripts/GameplayHud.cs")
    assert 'HasPerk("Hardened Sight")' in hud
    assert "baseLowAlpha*=0.5f" in hud
    assert 'HasPerk("Bruiser")' in hud
    assert "alpha*=0.35f" in hud
    assert "APPROXIMATED base presentation" in hud
    assert "Bruiser's 65% reduction is VERIFIED" in hud

def test_damage_event_drives_hud_flash_without_presentation_authority():
    root=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "Runtime.PresentationEvent += OnPresentationEvent" in root
    assert 'eventName=="PlayerDamagedEvent"' in root
    assert "_hud.FlashDamage()" in root
    assert "Runtime.PresentationEvent -= OnPresentationEvent" in root
