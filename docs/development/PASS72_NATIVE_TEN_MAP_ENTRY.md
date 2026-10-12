# Pass 72 — Native Windows entry into all ten Godot gameplay maps

**Branch:** `twr-pass72-native-ten-map-entry`
**Parent:** [Pass71 completed native Windows validation](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38201351136).

## New acceptance coverage

- Instantiates a new **production GameplayRoot**, first-person **FirstPersonPlayer** capsule and camera, **GameplayHud**, live pickup actors and actual fortification controller on **each of the ten release maps** from `ReleaseRules.Maps`.
- Unlike previous synthetic Pass70/71 raised combat floors, **Pass72 leaves the player at the actual map-defined starting point** and checks that the real CharacterBody3D is finite, alive and physically grounded after physics settles.
- Drives the legitimate Wave 1 countdown, actual Wave 1 Godot infected spawn, authoritative Wave 1 survival reward and actor cleanup, Wave 2 intermission/countdown and another genuine Wave 2 infected spawn **on every map**. Only wall-clock *stage durations* are accelerated to avoid ten five-minute sessions; actor life cycles and domain commands remain untouched.
- Ensures each map's runtime state goes through a test-only authoritative `FailMatch`, committed Results save, and `ReturnToLobby`, with the previous Godot gameplay scene freed before the next map starts.
- The dedicated `--smoke-pass72` profile file never touches `user://profile.json`, original source archives, owner-only mesh packs or any external service. All native CI checks run using publicly committed approximate maps and/or safely generated fixtures.
- GitHub Actions Windows CI requires a per-map marker for **all ten** real Godot scenes and one final `TWR_SMOKE_PASS72_TEN_MAP_ENTRY_OK maps=10` marker, and retains prior source-private data exclusions.

## Verification status

**Verified native Windows release checkpoint:** [successful Pass72 CI run 38201737219](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38201737219), compiled from source SHA `80d09e498d6737e9324fc4d2313e2798dd02304a`.

- **596 Python tests passed**, with one inherited XML FutureWarning.
- The Godot/.NET Windows build and actual exported-executable smoke completed successfully.
- All ten `TWR_PASS72_MAP_OK` markers were emitted with `grounded=true`, `native_pickups=true`, and `wave2_infected=true` on Ranch, Mill, Bypass, Cabin, Cargo, District, Expressway, Prison, Laboratory and Manor.
- The exported Windows game emitted `TWR_SMOKE_PASS72_TEN_MAP_ENTRY_OK maps=10`, proving per-map game nodes, grounded player physics, real wave-spawned infected, authoritative Wave 1 survival and Wave 2 actor creation.
- The privacy gate passed and no owner-original source archives or original meshes were published to GitHub artifacts. CI deliberately leaves Windows artifact publication disabled.

These test scenes use the available **fallback reconstruction map content**, not the user's full retail Roblox maps. No manual 15-wave human session is claimed.

## Important fidelity boundary

This is an automated two-wave staging acceptance, **not** ten complete original maps, source mesh parity, a human-played 15-wave run, enemy long-distance path verification or a private player-ready distribution. Existing map fallbacks remain approximate. Source-private original Roblox geometry and other owner media are never uploaded to public CI.
