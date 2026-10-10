"""Pass 33: source-graph routing cache and disconnected-ingress contract tests.

The executable native smoke test runs real Godot C# AStar3D queries; these
Python checks only prevent accidental removal of the guard rails.
"""
from conftest import ROOT


def source(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_repeated_infected_queries_use_bounded_lru():
    nav = source("src/Twr.Godot/Scripts/Pass25SourceNavigationRuntime.cs")
    assert "private const int MaximumCachedRoutes = 512;" in nav
    assert "Dictionary<(long From,long To), LinkedListNode<CachedRoute>>" in nav
    assert "while (_cachedRoutes.Count > MaximumCachedRoutes)" in nav
    assert "_recentRoutes.RemoveLast();" in nav
    assert "RouteCacheHits++;" in nav
    assert "ActualPathSearches++;" in nav
    assert "return cached.Value.Path.ToArray();" in nav
    assert "return bounded.ToArray();" in nav


def test_disconnected_original_ingress_short_circuits_before_astar():
    nav = source("src/Twr.Godot/Scripts/Pass25SourceNavigationRuntime.cs")
    method = nav.split("public Vector3[] GetRoute(Vector3 from, Vector3 to)", 1)[1]
    cut = method.split("private static int FindRoot", 1)[0]
    assert "if (_componentRoot[(int)start] != _componentRoot[(int)end])" in cut
    assert "DisconnectedRouteRejects++;" in cut
    assert cut.index("DisconnectedRouteRejects++;") < cut.index("_graph.GetPointPath(start,end);")
    assert "MaximumAnchorDistance" in cut
    assert "DistantAnchorRejects++;" in cut


def test_navigation_telemetry_is_visible_and_saved_offline():
    hud = source("src/Twr.Godot/Scripts/Pass30DiagnosticsHud.cs")
    assert "Navigation?.RouteRequests" in hud
    assert "Navigation?.ActualPathSearches" in hud
    assert "Navigation?.RouteCacheHits" in hud
    assert "Navigation?.DisconnectedRouteRejects" in hud
    assert "TWR_Pass33_Diagnostics.json" in hud


def test_exported_game_exercises_caching_and_original_unreachable_spawns():
    app = source("src/Twr.Godot/Scripts/Bootstrap.cs")
    workflow = source(".github/workflows/windows-build.yml")
    assert 'args.Contains("--smoke-pass33"' in app
    assert "TWR_SMOKE_PASS33_PATHCACHE_OK" in app
    assert "nav.ActualPathSearches != 1" in app
    assert "nav.RouteCacheHits != 256" in app
    assert "nav.DisconnectedRouteRejects < 64" in app
    assert "nav.CachedRouteCount > 512" in app
    assert "--smoke-pass33" in workflow
    assert "make_pass30_synthetic_sidecars.py --output $outputDir --clean" in workflow
    assert "TWR-Pass36-Windows-x64-NoPrivateAssets" in workflow
