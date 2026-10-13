# Pass 100 — Ten-map physical Load/Repair walking acceptance

**Baseline:** Pass99 exclusive player-wide material carrying was successfully Windows-validated in [run 38328457239](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38328457239).

## Production improvement

The fallback-only `Pass96GroundedMaterials.TryPlan` previously checked a full-size capsule-safe straight path *from the deposit to a candidate pickup*. Pass100 checks the return direction as well. An outward-safe placement that is not physically reversible is rejected before an object is created. The original, owner-authored map source geometry/markers remain untouched.

## Native gameplay acceptance upgrade

The existing exported `--smoke-pass96` runs a stronger 10-map assessment. In addition to the previous 200+ ground-supported material placements, each map instantiates real Load and Repair objective actors, then:

1. Positions the physics-controlled player on the verified depot **once on entering the map**. Removes unrelated randomized natural loot actors only inside the isolated test scene.
2. Rotates the player's facing direction, presses the actual W key, and advances real physics ticks through `FirstPersonPlayer._PhysicsProcess` and `MoveAndSlide`, with travel/time-out checks.
3. Stops at a grounded material, presses/releases E, and asserts **one** real inventory carry and exactly one consumed world material.
4. Physically walks the return path using W, presses/releases E and asserts **one** deposit and an empty carried-material slot. Reuses the same walking player for Repair, with no second entrance teleport.
5. Repeats for ten fallback maps and both material families (20 material round trips). Does not move the item, objective, physics floor, reward ledger or world colliders.

Each path emits `TWR_PASS100_MAP_WALK_OK map=... family=...`. The full headless exported Windows application must emit `TWR_SMOKE_PASS100_MATERIAL_WALK_OK maps=10 objective_families=Load,Repair material_roundtrips=20`. CI checks all 20 cases rather than accepting only a generic success status. The earlier Pass96, Pass97 replay, Pass98 material completion, Pass99 exclusive carry, Escort, multiplayer-independent combat, and private-data exclusion checks remain configured.

## Verification scope and limits

The acceptance exercises only **the first item** of each Load/Repair objective per fallback map. It verifies the return route under *actual player movement*, not merely a static line-of-sight or route planner. Complete production Wave1/Wave2 material loops with E presses remain independently covered by Pass98 in Ranch and Manor; stage clocks in those tests are accelerated.

The initial entry placement is controlled by the test to isolate the depot-to-material corridor. This **does not** certify a player can walk from the real map spawn, negotiate Manor stairs with a carried object, or survive a 15-wave human-controlled game. The native acceptance is a headless collision test, not a graphical source-matched screenshot or original retail coordinate verification. Owner-original art, meshes, audio, animations, map interiors and offline package release still require separate fidelity work.

**Status:** Code and a Windows CI gate have been committed. The branch should only be called verified if the *latest source-code SHA* succeeds in an exported Windows build and emits all 20 per-map markers. Windows CI intentionally does not upload executable artifacts or restricted owner source files.
