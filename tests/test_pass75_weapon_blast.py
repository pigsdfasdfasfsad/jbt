"""Pass75 explosive collision, falloff authority and pre-recoil ray regression."""
from conftest import ROOT


def read(path):
    return (ROOT / path).read_text(encoding="utf8")


def test_rocket_sweep_hits_live_infected_and_occlusion_blocks_splash():
    rocket=read("src/Twr.Godot/Scripts/ExplosiveProjectileRuntime.cs")
    policy=read("src/Twr.Godot/Scripts/Pass75BlastOcclusion.cs")
    assert "public override void _PhysicsProcess(double delta)" in rocket
    assert "1u | 2u | Pass36SourceFragmentsRuntime.SourceServerCollisionLayer" in rocket
    assert 'hit["position"].AsVector3() +' in rocket
    assert 'hit["normal"].AsVector3() * .08f' in rocket
    assert rocket.count("Pass75BlastOcclusion.CanReachTarget(") == 2
    assert "Damage * factor" in rocket
    assert "tanker.ApplyDamage(Damage, \"Explosive\")" in rocket
    assert "world.DirectSpaceState.IntersectRay(query).Count == 0" in policy
    assert "SolidWorldMask" in policy
    assert "target is DamageObjectiveTarget" in policy
    assert "excluded.Add(sourcePlayer.GetRid())" in policy
    assert "radiusMetres <= 0f" in policy
    assert "delta.Length() > radiusMetres" in policy


def test_frag_fuse_and_world_occlusion_preserve_source_damage():
    frag=read("src/Twr.Godot/Scripts/ThrowableProjectileRuntime.cs")
    assert "private const float FragDamage=300f;" in frag
    assert "private const float FragRadius=30f;" in frag
    assert "private const double FragFuse=3.0;" in frag
    assert "RobloxUnits.Distance(FragRadius)" in frag
    assert "Pass36SourceFragmentsRuntime.SourceServerCollisionLayer" in frag
    assert frag.count("Pass75BlastOcclusion.CanReachTarget(") == 2
    assert 'infected.ApplyDamage(FragDamage*factor,false,"Explosion")' in frag
    assert 'tanker.ApplyDamage(FragDamage,"Explosive")' in frag


def test_first_fire_uses_original_crosshair_then_applies_recoil():
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    body=player[player.index("private void TryUseWeapon()"):player.index("private double FireInterval(")]
    assert "Runtime.SpendAmmo(spec.Name, 1)" in body
    assert "FireLauncher(spec);" in body
    assert "SpreadDirection(spec.Spread)" in body
    assert body.count("ApplyRecoil(spec)") == 4
    assert body.index("FireLauncher(spec);") < body.index("ApplyRecoil(spec);")
    assert body.index('FireHitscan(spec, -_camera.GlobalTransform.Basis.Z, spec.Range, spec.Damage, "Bullet", true);') < body.rindex("ApplyRecoil(spec);")


def test_exported_native_two_map_damage_and_isolated_profile_gate():
    test=read("src/Twr.Godot/Scripts/Pass75WeaponBlastAcceptance.cs")
    session=read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    ci=read(".github/workflows/windows-build.yml")
    assert 'await ValidateLauncher()' in test
    assert 'await ValidateFrag()' in test
    assert '"Laboratory"' in test and '"Manor"' in test
    assert "new GameplayRoot" in test
    assert "GetChildren().OfType<InfectedAgent>()" in test
    assert "new ExplosiveProjectileRuntime" in test
    assert "new ThrowableProjectileRuntime" in test
    assert "new DamageObjectiveTarget" in test
    assert "Runtime.Player.Credits <= credits" in test
    assert "Runtime.Player.Health =" not in test
    assert "Runtime.Player.Credits =" not in test
    assert "Runtime.Match.Wave =" not in test
    assert "Runtime.ReturnToLobby()" in test
    assert "new Pass75WeaponBlastAcceptance" in boot
    assert '"--smoke-pass75"' in session
    assert '"user://pass75-blast-smoke-profile.json"' in session
    assert "else if(smoke75)" in session
    assert "TWR_SMOKE_PASS75_WEAPON_BLAST_OK" in ci
    assert "twr-pass75-blast-line-of-sight" in ci
    assert ci.index("Smoke Pass 74") < ci.index("Smoke Pass 75")
    assert ci.index("Smoke Pass 75") < ci.index("Verify no private map scenes")
    assert "github.ref_name == 'twr-pass75-blast-line-of-sight')" in ci
