# Pass 42 — Source-Grounded Laboratory Jump Navigation

**Status:** New implementation extending the verified Pass 41 Windows reconstruction.

## Source evidence and scope

The owner's original `scripts/ModuleScript.Pathfind.Source.txt` explicitly creates a Roblox PathfindingService path with these settings:

- `AgentRadius = 1` source stud
- `AgentHeight = 4` source studs
- `AgentCanJump = true`
- `AgentCanClimb = false`
- `WaypointSpacing = 4` source studs
- `SupportPartialPath = true`

Pass 40/41 approximated all navigation edges as *walk* connections, so a short genuine gap was treated as permanently disconnected even when the source's navigation configuration allows jumping. Pass 42 adds a **separate typed jump-edge action**, not an unvalidated walk-through link.

The available original place XML provides original-positioned native Part transforms, CanCollide state and bounding collision proxies, but **does not provide original Roblox navmesh vertices, SmoothGrid terrain, custom CSG/MeshPart triangles, or actual retail jump trajectory timings**.

## Reconstructed Laboratory jump links

The input is the owner-only full loaded source Laboratory scene and the exact previous Pass 41 navigation file:

| Data | SHA-256 |
| --- | --- |
| Original-positioned Laboratory compressed scene | `35ba9ce77ef220448cdc087679afcc5ef727fa41f54f61dba25e886bc8c8ef74` |
| Pass 41 TWRNAV41 native-bridge graph | `12ac9bae602cbdea3ae6a6789289d1068f1c8efd5d603de34d6186698c375dd0` |
| **Pass 42 TWRNAV42 v4 with typed jump actions** | `c9f03bfe0c5da13972d5c149f88c4084da61b5804501419aebb6cddac51be4fd` |

The deterministic converter `tools/validation/recover_source_jumps42.py` samples pairs from *different* graph components, requires endpoints to rest on authored nonproxy native Part floor surfaces, and checks nine points along each approximate jump arc against original-positioned collision bounds. It never teleports an actor, uses invented stairs, invents a mesh, or enables a jump between arbitrary far-away components.

- Up to 8 source studs (2.24 metres) per jump-edge chord.
- At most 3.5 studs (0.98 metres) of elevation difference.
- Candidate pairs with supported native Part floor endpoints: **2,067**.
- Candidate pairs passing sampled collision arc: **230**.
- Distinct source graph component pairs: **9**.
- Additional jump edges added: **9**, preserving all 14,726 existing nodes and 43,937 existing walk links.

| Graph metric | Pass 41 | Pass 42 |
| --- | ---: | ---: |
| Source-grounded waypoint nodes | 14,726 | 14,726 |
| Walk connections | 43,937 | 43,937 |
| Typed jump connections | 0 | **9** |
| Total graph connections | 43,937 | **43,946** |
| Disconnected regions | 117 | **108** |
| Original player-start connected area | 291 | **1,681** waypoints |

This is a substantial topological expansion, **not proof that a real infected can traverse every link**. The link-selection clearance uses source approximations and actual Godot collision will still govern agent motion during gameplay.

## Runtime

`Pass25SourceNavigationRuntime` now prefers a matching `Content/Navigation/Laboratory.nav42.gz`, then Pass41 nav41, then Pass40 nav31, then older nav25/local steering. It validates the exact scene SHA, graph SHA, counts, actions, radius/height budgets, uniqueness, and graph point coordinates.

- Walk edges remain unchanged and follow the previous AStar movement system.
- Jump edges encode action=1 and are exposed through `IsJumpLinkBetween`.
- `InfectedAgent` follows typed jump segments using a bounded ballistic leap while retaining Godot `CharacterBody3D.MoveAndSlide` collisions. The previous waypoint-skip threshold will not silently jump over short action segments.
- Failed or unsupported source packs revert to older navigation without crashing the game.
- The original source infected spawn markers remain unchanged by default. The prior **F9 optional** grounded entrance assistance is available when disconnected original entrances are unplayable.

## Three revised Laboratory map diagrams

Owner-only image files `Content/MapPlans/Laboratory.navgraph42-0.png` through `-2.png` use the same original source coordinate transform as Pass 39. **Cyan** shows sampled jump actions; **gold** shows Pass 41 native walk repairs, **green** shows the now-expanded area connected to source player spawn, and **purple** remains disconnected.

In-game **F4**, then **N**, switches to these source-navigation diagnostics; **Left/Right** switches floors. The game accepts the updated diagram set only with matching graph and PNG SHA values. If it is missing, the Pass41/40 diagrams or Pass39 original-source outline remain available.

F10/F11 now expose the active verified jump graph, source jump edge count, infected jump attempts and observed landings.

## Windows validation and 15-wave acceptance

The Python tests validate deterministic generation, source SHA attachment, physical jump-arc rejection against test walls, proxy-floor rejection, safe format framing, and private asset exclusion.

The native **exported Windows** `--smoke-pass42` test installs only an entirely fabricated 19-node/18-edge map (one marked jump action), exercises AStar route action decoding, actual physics-bound infected jump attempt, F4/N navigation image switching, and **accelerated 15-wave gameplay stage transitions using actual Godot gameplay actors and wave cleanup**. All synthetic sources are deleted before creating the public-safe executable artifact.

This **does not constitute a player-controlled 15-wave match**, animation QA, framerate benchmark with all original owner assets, full enemy physical-route coverage, or 1:1 gameplay validation. Those remain explicit acceptance gates.

## Owner-only installation

Use the Pass 42 all-in-one Windows ZIP to retain old maps and audio. Its private `Content` tree adds:

```text
Content/Navigation/Laboratory.nav42.gz
Content/Navigation/LABORATORY_JUMP_LINKS_MANIFEST42.json
Content/MapPlans/Laboratory.navgraph42-0.png
Content/MapPlans/Laboratory.navgraph42-1.png
Content/MapPlans/Laboratory.navgraph42-2.png
Content/MapPlans/LABORATORY_NAVGRAPH_MANIFEST42.json
```

Public Git history contains **only code/tests/docs**, not the owner-held original scene, derived private graph files, or private source-coordinate images. The owner-pack ZIP also includes recovery scripts and checksums.

## Remaining major fidelity gaps

The genuine missing SmoothGrid terrain and MeshPart/UnionOperation triangles, the other nine full loaded-world map scenes, original zombie animations and jump timing, original sound bytes, retail dynamic door states and input/audio/visual parity remain incomplete. A full player-controlled 15-wave Laboratory playthrough on Windows must be measured before claiming genuine completion.
