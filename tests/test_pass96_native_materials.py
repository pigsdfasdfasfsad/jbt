"""Pass96 exported Windows E-key objective and all ten map safety checks."""
from conftest import ROOT


def src(name):
    return (ROOT / name).read_text(encoding="utf8")


def test_native_acceptance_requires_real_world_physics_and_player_interaction():
    test = src("src/Twr.Godot/Scripts/Pass96GroundedMaterialsAcceptance.cs")
    for token in (
        "Runtime.StartMap(map)",
        "new GameplayRoot",
        "Runtime.Player?.IsAlive!=true",
        "UseFallbackMaterialGrounding96=true",
        "Pass96GroundedMaterialLayout",
        "Pass96MaterialFeet.Count",
        "Pass89EscortSurfaceSafety.CanTravel(",
        "Pass82UpperObjectiveMarkers",
        'Family=family',
        "Input.ParseInputEvent(new InputEventKey",
        'status.Contains("CARRYING ITEM"',
        'status.Contains("1/10 INSERTED"',
        "Runtime.FailMatch(",
        "Runtime.ReturnToLobby()",
        "TWR_SMOKE_PASS96_GROUNDED_MATERIALS_OK",
    ):
        assert token in test
    assert "actor.GlobalPosition=" not in test
    assert "Runtime.Player.Credits=" not in test
    assert "Runtime.Player.Xp=" not in test
    for place in (
        "Ranch","Mill","Bypass","Cabin","Cargo","District",
        "Prison","Laboratory","Expressway","Manor"
    ):
        assert f'"{place}"' in test


def test_windows_ci_and_isolated_save_replay_previous_native_regressions():
    boot = src("src/Twr.Godot/Scripts/Bootstrap.cs")
    session = src("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    ci = src(".github/workflows/windows-build.yml")
    assert '"--smoke-pass96"' in boot and '"--smoke-pass96"' in session
    assert "new Pass96GroundedMaterialsAcceptance" in boot
    assert "pass96-material-objectives-smoke-profile.json" in session
    assert "else if(smoke96)" in session
    assert "TWR_SMOKE_PASS96_GROUNDED_MATERIALS_OK" in ci
    assert ci.index("Preflight Pass 96") < ci.index("Smoke Pass 78")
    assert ci.index("Preflight Pass 95") < ci.index("Verify no private map scenes")
    assert "twr-pass96-grounded-material-objectives" in ci
    assert ci.count("uses: actions/upload-artifact@v4") == ci.count(
        "if: false && success()")
