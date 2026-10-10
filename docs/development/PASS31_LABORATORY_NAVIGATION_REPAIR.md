# Pass 31 - Laboratory navigation reachability and accessibility

This branch builds on the fully compiled Pass 30 Windows export, not a new game engine.

## Verified original-source evidence
- Owner-held Laboratory.scene.jsonl.gz: SHA-256 `4f88a2764af76af99ddb29f64ff18b19b705878972ebeee7093d4f02091766b4`.
- Existing TWRNAV25: 19,355 source-sampled nodes, 32,079 edges, 130 disconnected components.
- None of 15 original infected spawns and 8 original player spawns shares a source-graph route with the current five-metre anchor rule: **0/120 native spawn pairs**. Old local steering may still move infected but cannot guarantee a connected path.
- Pass 31 TWRNAV31: 86 NEW collision-checked edges, 32,165 edges total, 44 disconnected components. No original edge or waypoint was removed.
- Collision checks used 217,204 source-derived triangles, including 4,501 identified approximated MeshPart/UnionOperation boxes. All accepted links required an unobstructed body and head path and floor support.
- Source player component grew from 223 to approximately 2,318 connected nodes. It still does **not** connect the original exterior infected spawn markers to player starts.

## In-game behavior
- The source graph loader prefers private `Content/Navigation/Laboratory.nav31.gz` and falls back to `Laboratory.nav25.gz`. Both verify SHA-256 against the **exact** private compressed source scene.
- Existing zombie waypoint following now compares vertical elevation, so a same-XZ waypoint on another floor is not skipped as though reached.
- Original infected spawn markers remain unchanged BY DEFAULT.
- Press **F9** to toggle optional **assisted infected spawns**. If a source spawn is disconnected from the player's graph component, the system selects a source-derived reachable graph node at least 24 metres away instead. This is an acknowledged **gameplay accessibility approximation**, not original TWR fence-hop or exterior spawn behavior.
- F10 diagnostics show whether assist is enabled and how many spawns were redirected. F9 only affects newly spawned infected.

## Build and evidence
Windows GitHub Actions should compile the C# game, run existing headless source/150-wave domain simulations, and use a disposable synthetic nav31 graph to test real exported-game route lookup and assisted insertion. The original Laboratory graph is distributed PRIVATELY with the portable executable; it is not uploaded to public GitHub. The private pack's Python generator repeats byte-for-byte and its installation tool rejects scene SHA mismatches.

## Still missing
Full map-level zombie routes from original exterior source spawns, multi-floor clearance and fence-hop animations, original missing meshes and all ten accurate maps, actual full player-controlled 15-wave gameplay testing and measured FPS with the entire owner-held scene.
