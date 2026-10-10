# Pass 32 - Physics-grounded source spawn accessibility and stuck recovery

This builds on the verified Pass 31 Windows Godot release. It does not claim
to recover missing Roblox SmoothGrid data, original zombie entrances or
original AI behavior.

## Original Laboratory source evidence and remaining gaps

A local owner-held source-collision audit of 217,204 triangles from 18,130
source collider records, including 4,501 bounding-proxy shapes, found:
- 8/8 player source spawn markers have a nearby floor within 1.5 metres.
- 8/15 original infected source markers have nearby support in reconstructed
  colliders; four are around 6.10 metres above the closest recovered solid
  surface, and three have no matching solid surface directly below them.
- The supplied TestPlace.rbxlx snapshots do not contain a SmoothGrid terrain
  payload. Absence of recovered terrain does not prove missing terrain in
  the real Roblox retail level. Never bridge unsupported voids with invented
  scene geometry in default mode.
- Pass 31's 19,355-node graph has 44 disconnected regions and has no direct
  original infected-spawn -> player-start path among 15x8 spawn pairs.
- The 32 graph options nearest each original infected source marker in the
  player's connected region provide floor-supported candidates in the
  collision-proxy audit across all 120 original marker combinations.
  These are **not** yet verified with full Godot collision, clearance or
  player-controlled 15-wave gameplay. Candidate counts are evidence, not
  proof of full physical navigation.

## Gameplay changes (F9 ON ONLY)

A source-derived assisted candidate must:
1. Be within the same connected source navigation component as the player.
2. Be >=24 metres from the player (not pop in next to them).
3. Have a collision-world raycast hit near the source waypoint's intended
   foot elevation, with sufficiently walkable upward normal.
4. Have no world-body overlap in the 1.6 m capsule standing volume.

If a candidate fails, try another (up to 32). If all fail, leave the original
spawn coordinate and count the failure for diagnostics. No new walls,
platforms, or fabricated entrances are introduced in the original scene.

While F9 ON, a previously spawned enemy that is motionless for >=7.5 seconds
or falls more than nine metres below the player can be relocated to a
physically safe distant source graph waypoint. Each enemy can be recovered
at most twice with >=18 seconds between relocations. Process at most two
enemies in a 1.5-second scan. Restoring the default F9 OFF state stops all
automatic relocations and preserves original source positions for new spawns.

F10/F11 report accepted grounded assisted spawns, stranded-enemy rescues and
failed placement attempts for real QA comparisons.

## CI and security

CI loads *generated synthetic-only* Laboratory geometry, collision, NAV31
and UI images inside the Windows-exported Godot executable. The native
--smoke-pass32 test checks walkable support, rejects a void, validates F9
default and opt-in, checks per-infected rescue limits and lets a real
InfectedAgent CharacterBody3D move over multiple physics frames.

The synthetic asset fixtures are deleted before the CI artifact is uploaded.
Original Roblox archives and source-derived private Laboratory sidecars
are distributed only in the owner-held portable release ZIP.

## Not accepted as fully complete

The 15-wave user-controlled playtest, accurate exterior entry paths,
Roblox mesh/CSG recovery, terrain fidelity and the remaining nine full
source maps are still outstanding.
