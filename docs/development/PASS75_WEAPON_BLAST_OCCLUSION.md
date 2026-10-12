# Pass 75 — Projectile World Collision and Occlusion-Safe Splash Damage

**Source branch:** `twr-pass75-blast-line-of-sight`  
**Parent:** [Pass74 verified native special-infected build](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38205313363).

## Implemented

1. **Live infected launcher collision:** `ExplosiveProjectileRuntime` moves in Godot `_PhysicsProcess`, using a swept collision ray with layer 2 for real infected bodies, layer 1 world geometry and any optional activated source-server wall layer. The projectile previously used Godot's default layer-1-only ray and could pass through infected. Surface impact is offset 8cm toward the incoming face so explosion rays do not start inside the hit wall or enemy.
2. **Solid-world splash shielding:** `Pass75BlastOcclusion.CanReachTarget` protects infected and damage objectives against both rocket and Frag radius damage through solid walls. It verifies finite distance, maximum radius, a visible collision-free ray to enemy central mass/tanker middle, and excludes the struck actor and firing player from the wall mask. Original optional source walls remain included.
3. **Frag original source data preserved:** 300 recovered damage, 30 Roblox-stud radius, and 3-second recovered fuse remain unchanged. The existing approximate distance-falloff curve is retained but must now pass wall visibility. There is no direct bypass of the authoritative `InfectedAgent.ApplyDamage`, tanker `ApplyDamage` or original kill/reward callback.
4. **First-shot recoil direction:** Fired rays, shotgun spread and launcher direction are resolved against the crosshair's current camera orientation before recoil moves the camera. Recoil continues to affect subsequent shots/aim as intended, while the first shot no longer fires away from its pre-trigger crosshair due solely to same-frame recoil.
5. **Native exported Windows acceptance:** On actual Laboratory and Manor gameplay roots, the isolated automated smoke uses synthetic raised collision lanes, production wave-spawned infected, rocket swept physics, stationary test Frag physics with its genuine gravity and fuse, temporary solid walls, live damage objectives, and normal local-session credit rewards. Launcher and Frag must fail to harm occluded infected and objectives, succeed after wall removal, and leave a far/out-of-radius objective intact. The test contains no direct presentation mutation of player health/XP/credits or domain wave fields.

## Limits / release status

This addresses genuine game mechanics in the current reconstructed engine. It **does not** prove the recovered original Roblox damage curves, first-person arms, grenade bounce characteristics, per-weapon pellet counts or complete ten-map architecture. Source-original mesh triangles/UVs, exact collision interiors and full 15-wave player-driven sessions remain outstanding. The owner asset packs and private saves are excluded from public GitHub CI; no public downloadable EXE is produced.

**Status:** implementation committed; compiled Windows acceptance not yet verified.
