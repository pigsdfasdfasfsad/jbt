"""Pass118: special-infected spawns must not assign detached global transforms.

The original Pass117 Windows log showed Node3D.SetGlobalPosition while
a Bloater spawned a projectile, followed by an AccessViolationException.
Native Pass74 and Pass90 Windows smoke tests exercise the real behavior.
"""
from conftest import ROOT


def source(path):
    return (ROOT / path).read_text(encoding="utf8")


def test_bloater_spawn_is_parent_local_before_ready():
    game = source("src/Twr.Godot/Scripts/GameplayRoot.cs")
    bloater = game.split("private void OnInfectedSpecialAttack(", 1)[1].split(
        "private void SpawnBursterHazard(", 1
    )[0]
    assert "if (infected.InfectedType != \"Bloater\" || _finished" in bloater
    assert "IsQueuedForDeletion()" in bloater
    assert "new SporeProjectileRuntime" in bloater
    assert "var spawnGlobalPosition = infected.GlobalPosition + Vector3.Up * 1.2f;" in bloater
    assert "Position = ToLocal(spawnGlobalPosition)" in bloater
    assert not any(line.strip().startswith("GlobalPosition =") for line in bloater.splitlines())
    assert "Damage = 26.4f" in bloater and "Radius = 15f" in bloater


def test_burster_cloud_preserves_source_world_contact():
    game = source("src/Twr.Godot/Scripts/GameplayRoot.cs")
    cloud = game.split("private void SpawnBursterHazard(", 1)[1].split(
        "private void Finish(", 1
    )[0]
    assert "new SporeCloudRuntime" in cloud
    assert "Position = ToLocal(position)" in cloud
    assert "GlobalPosition = position" not in cloud
    assert "blastRadius = 20f" in cloud
    assert "TickDamage = 5f" in cloud


def test_pass118_native_specials_and_escort_require_clean_process():
    ci = source(".github/workflows/windows-build.yml")
    assert "Pass 118 native Bloater projectile and Burster cloud placement" in ci
    assert "TWR_PASS74_HAZARDS_OK" in ci
    assert "Pass118 Pass90 emitted native memory corruption" in ci
    assert "System.AccessViolationException" in ci
    assert ci.index("Pass 118 native Bloater projectile") < ci.index(
        "Smoke Pass 90 real"
    )
