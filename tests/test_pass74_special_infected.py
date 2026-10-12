"""Pass74 native special infected launch/detonation/spore safety checks."""
from conftest import ROOT


def read(path):
    return (ROOT / path).read_text(encoding="utf8")


def test_bolter_leap_support_collision_and_close_range_timing():
    actor=read("src/Twr.Godot/Scripts/InfectedAgent.cs")
    policy=read("src/Twr.Godot/Scripts/Pass74SpecialInfectedSafety.cs")
    assert "Pass74SpecialInfectedSafety.HasSupportedLanding" in actor
    assert "_leapVelocity = direction * 28f" in actor
    assert "_leapTime = Math.Clamp(distance / 28.0, .24, .55)" in actor
    assert "_leapRecovery = 1.0" in actor
    assert "BolterLeapStarts74++" in actor
    assert "BolterLeapHits74++" in actor
    assert "BolterUnsupportedLandingRejects74++" in actor
    assert "if (Health <= 0 || Target is null" in actor
    assert "PhysicsRayQueryParameters3D.Create(start, finish)" in policy
    assert "Math.Abs(floor.Y - targetFeet.Y) <= MaximumFloorOffset" in policy
    assert "attacker, target" in policy
    assert "Pass74SpecialInfectedSafety.CanHitPlayerWithBlast" in read(
        "src/Twr.Godot/Scripts/GameplayRoot.cs")


def test_burster_blast_world_occlusion_respects_original_source_radius():
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    policy=read("src/Twr.Godot/Scripts/Pass74SpecialInfectedSafety.cs")
    assert "blastRadius = 20f" in game
    assert 'Runtime.DamagePlayer(27.5f, "Burster detonation")' in game
    assert "RobloxUnits.Distance(blastRadius)" in game
    assert "player.GetRid()" in policy
    assert "ray.CollisionMask = SolidWorldMask" in policy
    assert "IntersectRay(ray).Count == 0" in policy
    assert "radiusMetres <= 0f" in policy
    assert "player.GlobalPosition.DistanceTo(origin) > radiusMetres" in policy
    assert "SpawnBursterHazard(infected.GlobalPosition)" in game
    assert "TickDamage = 5f" in game


def test_all_eight_variant_live_physics_and_domain_counters():
    harness=read("src/Twr.Godot/Scripts/Pass74SpecialInfectedAcceptance.cs")
    assert 'InfectedCatalog.Active.Length != 8' in harness
    assert 'await ValidateRanchBolterAndResistance()' in harness
    assert 'await ValidateManorHazardsAndBloater()' in harness
    assert "new GameplayRoot" in harness
    assert "game.GetChildren().OfType<InfectedAgent>()" in harness
    assert "OcclusionWall(game" in harness
    assert "BolterLeapHits74 != 1" in harness
    assert "EffectiveSlowFactor74" in harness
    assert '"Hazmat" or "Burster" => 1f' in harness
    assert '"Bolter" => .6f' in harness
    assert 'dummy.ApplyDamage(20f, false, "Melee")' in harness
    assert 'dummy.ApplyDamage(10f, false, "Fire")' in harness
    assert 'Runtime.ActivateGasMask()' in harness
    assert "new SporeProjectileRuntime" in harness
    assert "bloater.BloaterVolleyCount74 != 1" in harness
    assert "Runtime.DamagePlayer(1000" not in harness
    assert "Runtime.Player.Health =" not in harness
    assert "Runtime.Player.Credits =" not in harness
    assert "Runtime.ReturnToLobby()" in harness


def test_ci_native_flag_isolated_save_and_excluded_private_assets():
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    ci=read(".github/workflows/windows-build.yml")
    assert '"--smoke-pass74"' in boot
    assert '"--smoke-pass74"' in session
    assert '"user://pass74-special-infected-smoke-profile.json"' in session
    assert "else if(smoke74)" in session
    assert "new Pass74SpecialInfectedAcceptance" in boot
    assert "twr-pass74-special-infected-safety" in ci
    assert "TWR_SMOKE_PASS74_SPECIAL_INFECTED_OK" in ci
    assert ci.index("Smoke Pass 73") < ci.index("Smoke Pass 74")
    assert ci.index("Smoke Pass 74") < ci.index(
        "Verify no private map scenes entered the CI export")
    assert "github.ref_name == 'twr-pass74-special-infected-safety')" in ci
