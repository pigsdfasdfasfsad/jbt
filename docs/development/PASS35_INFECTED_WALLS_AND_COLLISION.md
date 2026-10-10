# Pass 35 — Original Infected Wall Recovery and Optional Physics

## Source evidence

Owner-provided `TestPlace/TestPlace.rbxlx` has the exact SHA-256:
`272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2`.

The `ReplicatedStorage/CMaps/{map}/Walls/Infected Walls` section contains
**285 collidable shapes across six of the ten release maps**:

| Map | Parts | Wedges | MeshParts | Total |
| --- | ---: | ---: | ---: | ---: |
| Ranch | 35 | 45 | 0 | 80 |
| Mill | 37 | 0 | 0 | 37 |
| Bypass | 49 | 6 | 0 | 55 |
| District | 13 | 66 | 3 | 82 |
| Prison | 3 | 2 | 0 | 5 |
| Manor | 13 | 13 | 0 | 26 |
| Cabin, Cargo, Expressway, Laboratory | 0 | 0 | 0 | 0 |
| **Total** | **150** | **132** | **3** | **285** |

The native Part and WedgePart dimensions/transforms can be carried into Godot
exactly at the source-part level. The three District MeshParts have missing
external mesh binaries: their size-accurate oriented-box colliders are **not**
original triangle geometry and are explicitly marked as approximations.

The supplied Luau `ModuleScript.Map.Source.txt` shows client-side `Client Walls`
loaded into a separate collision group, but the retail infected-wall collision
**matrix has not been recovered**. The source `Infected Walls` are not proof
that any specific infected-wall group masks or server interaction is known.

These `CMaps` are **collision fragments, not nine complete reconstructed maps**.
The current game's non-Laboratory levels use blockout/fallback geometry, so
turning source walls on can create misalignment and must be user-controlled.

## Changes to the standalone game

- `tools/validation/recover_original_infected_walls35.py` produces six small,
  deterministic gzip JSONL collision packs from the owner archive, with hashes,
  counts, original source CFrames, and explicit approximation flags.
- `Pass35InfectedWallsRuntime` validates the exact owner RBXLX SHA, exact known
  wall-pack SHA, counts, finite coordinates and orthonormal transforms. It
  rejects corrupt files and never connects to Roblox or the internet.
- **F7** toggles an experimental, invisible **infected-only** wall layer (bit 8).
  It is **OFF by default**. The player never collides with this layer.
- All infected `CharacterBody3D`s include bit 8 in their collision masks. Their
  local obstacle-steering rays also detect it when enabled. World geometry
  collision and original F8 player-only collision remain unchanged.
- F10 overlay/F11 JSON report show wall counts, approximation counts, and F7
  state. No private source bytes are uploaded to GitHub Actions.
- Laboratory has **zero Infected Walls** in the source, and this pass does not
  repair its absent terrain, missing exterior paths, or original spawn routing.

## Acceptance and limits

- Synthetic XML regression tests verify native Part/WedgePart and marked
  MeshPart handling, the exact ten-map inventory, and deterministic output.
- Windows native smoke uses a generated one-wall **Manor** fixture far from
  the blockout. It tests collision OFF -> F7 ON -> F7 OFF, verifies the wall
  is on layer 8, and proves it is absent from the player's collision mask.
- The public CI artifact must contain no `.infectedwalls35.jsonl.gz` owner
  source pack. Owner-side portable distribution includes those six files.
- Full 15-wave interactive tests, all nine full original map meshes, source
  terrain and precise infected wall collision-group matrix remain outstanding.