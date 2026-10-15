# Pass122 - provisional objective support validation

**Mainline parent:** twr-pass121-original-player-spawn-clearance.

The recovered source scene contract has no authenticated objective-spawn coordinates.
Its two previous objective positions were rough offsets around the **first** player
marker, which became stale when Pass121 selected another valid original player marker.

Pass122 flags only those positions as provisional, rebuilds them around the newly
validated player origin and uses real Godot layer-1 world floor and standing-clearance
queries before instantiating objectives. A provisional objective without a safe anchor
is skipped rather than spawned inside a wall or over missing floor terrain.

Original source player, infected and item markers are not changed. Existing
fallback-map objective placement remains unchanged. The objective family, reward
receipt, and 1-2 objective per-wave scheduling are preserved; fewer objectives
may appear when reconstructed map collision is incomplete. Provisional objectives
must never be represented as a verified 1:1 location.

This pass is not proof of completed original map geometry. Human playtesting,
objective routing and missing source meshes remain open work.
