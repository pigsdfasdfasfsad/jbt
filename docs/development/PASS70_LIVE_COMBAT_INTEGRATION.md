# Pass 70 — Exported Windows Live Player/Weapon/Infected/Wave Integration

**Branch:** `twr-pass70-live-player-combat-loop`  
**Parent checkpoint:** verified Pass69 optics:
https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38196014637

## Purpose

The established `TWR_SMOKE_COMPLETE_OK maps=10 waves=150`
is a useful **domain-only** progression test: it calls
`AwardWaveSurvival` and `AdvanceWave` directly without
constructing a gameplay map, simulated player, infected physics
actor, or hit registration. It must never be misrepresented
as ten user-completed games.

Pass70 adds a separate, stronger **native exported Windows**
integration acceptance test through the *same production
GameplayRoot/FirstPersonPlayer/InfectedAgent/LocalSessionNode
code paths* used for regular single-player operation.

## Implemented acceptance sequence

- `--smoke-pass70` launches an isolated `LocalSessionNode`
  on **user://pass70-live-combat-smoke-profile.json** with an
  empty synthetic test profile. Real/previous player saves,
  original owner map packs and private guns are untouched.
- Starts the actual **Laboratory GameplayRoot**, real
  starter Glock inventory, first-person camera, HUD and
  authoritative offline match domain.
- Adds a plain synthetic Godot collision platform high above
  the map so the physics capsule, player, and shot direction
  have a reproducible open-air firing lane. Real map geometry
  stays intact. Waits for the normal weapon-equip cooldown;
  it does not override internal player timers.
- Advances the game's **real Wave 1 countdown** with one
  accelerated **game-stage clock**, allowing the normal
  wave spawner to create an actual `InfectedAgent` with
  its death-reward callback to `GameplayRoot`. For the
  smoke only, places one infected at the physics firing
  lane, assigns minimal health and zero damage/speed.
- Queries actual `Godot.PhysicsDirectSpaceState3D` first
  to verify the camera ray contacts that infected's native
  collision capsule. Godot's **headless** Windows DisplayServer
  cannot capture a real mouse cursor (`Input.MouseMode` stays
  `Visible`). An **exact `--smoke-pass70` CLI-only** guard
  bypasses *only* the cursor-capture prerequisite; ordinary
  playable controls still require capture. This lets the smoke
  send the *actual*
  `FirstPersonPlayer._UnhandledInput` mouse-left event:
  the ordinary `TryUseWeapon` -> `SpendAmmo` ->
  `FireHitscan` -> `InfectedAgent.ApplyDamage` ->
  `OnInfectedDied` -> `AwardKill` pipeline must
  consume one cartridge, kill the actor, and increase
  authoritative credits and XP.
- Sends the normal **R key** to trigger the actual
  reload timer. Advances Godot physics frames and
  verifies ammunition increased, reserve decreased,
  and combined cartridge conservation.
- Fast-forwards **only wave staging** after the firefight:
  the real gameplay root should award Wave 1 survival,
  clear infected, enter the Wave 2 intermission, begin
  its countdown, and spawn a live infected in Wave 2.
  Unlike the domain-only test, the live engine must
  instantiate and clean up actors.

This is **automated interaction acceptance**: it does not
prove movement and full combat balance, long-duration
survival performance, actual human control, map accuracy,
fifteen-wave completion, original source meshes, or
realistic zombie navigation across all ten maps.

## Native CI and privacy

- `tests/test_pass70_live_combat.py` verifies that
  production player/map/domain methods are called without
  direct test-only ammo/reward mutation, that the profile
  is isolated, and that existing privacy checks survive.
- Existing Pass59–69 exported Windows weapon/arm/aim/optic
  native smoke tests and the previous 10-map `150-wave`
  domain progression tests remain enabled.
- The Pass70 Windows smoke installs a **wholly fabricated**
  Pass68-style source Glock/other weapon assembly and
  hierarchy temporarily, then verifies their removal.
  These files are **NOT** the owner-original TestPlace
  archives. No private images, scripts, map meshes or
  model binaries are published or bundled in public GitHub CI.
- The original `--smoke-play` game startup and ordinary
  single-player menu are unchanged. The only new gameplay
  data storage is an isolated test-mode profile that
  cannot overwrite `user://profile.json`.

## Release and fidelity gaps

A real downloadable private Windows distribution with the
owner-provided source archives is not yet packaged.
Source geometry still lacks many external MeshPart triangles
and texture maps. Most of the ten retail maps are approximated
rather than complete original interiors and physically exact
structures. Real-world player-controlled 15-wave playthrough
testing, performance, accessibility, objective variants,
error recovery, reload interruption, AI combat timings
and screenshot parity remain to be completed.

## Verification status

**Implementation committed; native Windows CI acceptance pending.**
Do not mark Pass70 as verified until the specific complete
Windows workflow reports success on the tested revision.
