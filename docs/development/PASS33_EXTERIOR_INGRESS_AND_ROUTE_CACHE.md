# Pass 33 — Original entrance fidelity audit and performant zombie routing

## Grounded findings from the private Laboratory source
- Original source scene: 34,268 geometry records, 18,130 reconstructed physical collision surfaces, 15 infected entry markers, and 8 player starts.
- SHA-256 of the exact compressed owner-held scene: `4f88a2764af76af99ddb29f64ff18b19b705878972ebeee7093d4f02091766b4`.
- Existing Pass 31 nav graph: 19,355 waypoints, 32,165 edges, 44 disconnected regions.
- Current offline source-nav graph connects **zero of 120 original infected-entry/player-start pairs**.
- The 15 original infected markers lie between **17.75m and 226.39m** (3D Euclidean nearest-node distance) from the graph region reachable by the eight original player starts. These distances are NOT walks or safe paths.
- Eight of 15 infected spawn markers have nearby supporting reconstructed collision; seven do not. Some may be missing original SmoothGrid terrain/union/collision sources, not necessarily broken in retail TWR.
- The supplied TestPlace Roblox XML does not expose usable SmoothGrid voxel terrain. Producing invented ramps, geometry bridges, or exterior entrances would harm 1:1 fidelity.

## What was actually implemented

`Pass25SourceNavigationRuntime.GetRoute()` now avoids A* searches for explicitly disconnected components. All lookups still use nearest source node and a 5m three-dimensional anchor check. A bounded 512-entry LRU stores previously computed routes by (start-node, end-node); every returned array is a copy, preventing one zombie from corrupting another's cached path. The graph is static during the match, so no stale dynamic wall route is introduced by cache reuse. Existing local collision steering and the previous Pass 31 nav31 fallback are preserved.

The gameplay F10/F11 diagnostics expose:
- Path requests and actual A* searches
- Cache hits and occupied cache slots
- Disconnected graph rejects and invalid distant anchor rejects

**These changes improve CPU predictability and observability; they DO NOT reconstruct the original exterior routes.** F9 assisted spawning/grounded rescue remain explicitly optional and are OFF by default.

## Native Windows acceptance tests

The exported Godot C# executable is invoked with `--smoke-pass33` and disposable synthetic Laboratory sidecars. This verifies a cold route followed by 256 cache hits; mutation safety; 64 disconnected-source queries that make zero A* calls; and 2,550 distinct source waypoint requests that cannot expand the LRU past 512 entries. Python source regression tests remain separate from these native runtime checks. No owner-held Roblox binaries are uploaded to the public build artifact.

## Remaining priorities

1. Obtain original, legally usable SmoothGrid terrain and actual custom mesh/union colliders, and investigate the missing 7/15 source-ground supports.
2. Recover exterior door, window, fence-hop, and infected-ingress logic from original source/recorded gameplay without invented shortcuts.
3. Confirm navigation for all original marker pairs under real full-scene collision, not only source waypoint graph proximity.
4. Run real player-controlled 15-wave Laboratory games while saving F10/F11 FPS, zombie path search, memory and support reports.
5. Extend original-position complete reconstruction and interactive comparison to the other nine maps.
