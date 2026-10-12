"""Pass89 requires real exported Windows Escort navigation acceptance."""
from conftest import ROOT


def read(path):
    return (ROOT/path).read_text(encoding="utf8")


def test_native_escort_checks_real_wall_and_npc_collision():
    code=read("src/Twr.Godot/Scripts/Pass89EscortNavigationAcceptance.cs")
    for marker in (
        "TWR_PASS89_SOLID_WALL_REJECT_OK",
        "TWR_PASS89_LIVE_ROUTE_INVALIDATED_OK",
        "TWR_PASS89_REAL_ESCORT_DETOUR_OK",
        "TWR_SMOKE_PASS89_ESCORT_PHYSICAL_NAVIGATION_OK",
    ):
        assert marker in code
    for check in (
        'Runtime.StartMap("Ranch")',
        "new GameplayRoot",
        "new ObjectiveRuntime",
        "GetVerifiedEscortRoute89",
        "CanTravel(",
        "new StaticBody3D",
        "new BoxShape3D",
        "survivor.IsOnFloor()",
        "EscortSafeSteps89",
        "Runtime.FailMatch(",
        "Runtime.ReturnToLobby()",
    ):
        assert check in code
    assert "survivor.GlobalPosition =" not in code
    assert "Runtime.Player.Health =" not in code
    assert "Runtime.Player.Credits =" not in code
    assert "Runtime.Match.Wave =" not in code


def test_pass89_ci_runs_previous_native_tests_and_private_exclusion_before_download():
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    ci=read(".github/workflows/windows-build.yml")
    assert 'new Pass89EscortNavigationAcceptance' in boot
    assert '"--smoke-pass89"' in boot and '"--smoke-pass89"' in session
    assert '"user://pass89-escort-nav-smoke-profile.json"' in session
    assert "else if(smoke89)" in session
    assert "twr-pass89-escort-ground-navigation" in ci
    assert "TWR_SMOKE_PASS89_ESCORT_PHYSICAL_NAVIGATION_OK" in ci
    assert ci.index("Smoke Pass 78")<ci.index("Smoke Pass 83")
    assert ci.index("Smoke Pass 83")<ci.index("Smoke Pass 88")
    assert ci.index("Smoke Pass 88")<ci.index("Smoke Pass 89")
    assert ci.index("Smoke Pass 89")<ci.index("Verify no private map scenes")
    assert ci.index("Verify no private map scenes")<ci.index("Upload verified Pass89 public-fallback")
    assert "ThoseWhoRemainOffline-Windows-x64" in ci
