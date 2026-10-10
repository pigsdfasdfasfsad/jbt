# Pass 34 — Original Roblox invisible Client Walls, source recovery and Windows collision verification

## Recovered evidence (owner-held original TestPlace.rbxlx)

This pass is based on the original CMaps sections, not screenshots or fabricated wall placement.
In the supplied `scripts/ModuleScript.Map.Source.txt`, `UpdateClientMap` clones
`ReplicatedStorage.CMaps[MapName].Walls["Client Walls"]` into the client world,
makes the BaseParts invisible, and assigns collision **group ID 8**.

The original group-8 **collision matrix was not recovered**. The reconstructed
game therefore places these bodies on a **dedicated Godot client-wall collision
layer (bit 4), disabled by default**. F8 is an experimental opt-in for player
collision only. It is NOT proof of the retail player-vs-zombie collision matrix.

Source SHA: `272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2`
and exact compressed Laboratory map SHA:
`4f88a2764af76af99ddb29f64ff18b19b705878972ebeee7093d4f02091766b4`.

### Original client wall part inventory

| Map | Original Client Wall parts | Infected Wall parts (separate source) |
|---|---:|---:|
| Ranch | 111 | 80 |
| Mill | 156 | 37 |
| Bypass | 131 | 55 |
| Cabin | 17 | 0 |
| Cargo | 3 | 0 |
| District | 60 | 82 |
| Expressway | 4 | 0 |
| Prison | 85 | 5 |
| Laboratory | 21 | 0 |
| Manor | 18 | 26 |
| **Total** | **606** | **285** |

The infected-wall totals are evidence only; they have not been installed in
the standalone game. Laboratory has **zero** source CMaps "Infected Walls".
The ten Client Wall packs are owner-side source fragments and MUST NOT be
mistaken for ten complete source maps. Carnival/Stadium additional fragments
are excluded from the ten release maps.

### Implemented game changes

- Recover original 3D part size, CFrame and visibility-independent collision
  from the Roblox place file using deterministic offline XML conversion.
- A SHA-verified `Content/Walls/Laboratory.clientwalls34.jsonl.gz` file is
  loaded only alongside its matching private Laboratory scene. No private map,
  animation, Lua, audio or mesh is uploaded to GitHub.
- `Pass34ClientWallsRuntime` creates 21 colliders on a unique layer. Toggle F8
  to opt in/out. The source walls remain invisible.
- `FirstPersonPlayer` collision mask includes layer bit 4, while
  `InfectedAgent` collision mask remains layer 1; existing world collision
  is unchanged.
- F10/F11 expose wall count and enablement; previous Pass33 diagnostic report
  filename remains stable for compatibility, with a new Pass34 JSON format.
- Fail-closed on missing, corrupt, oversized, or stale source-wall files;
  existing world collision then remains active unchanged.

### Acceptance tests and what they mean

- Private offline converter deterministically builds 10 source-side wall files.
- Python tests use a fabricated RBXLX with 10 source sections to verify
  transforms, separated Infected Walls, source SHA and repeatability.
- Windows native `--smoke-pass34` loads a **synthetic** one-wall file,
  verifies that F8 starts OFF, detects collision in layer 4 after opt-in, tests
  that layer 1 does not report the client-only wall, and verifies toggling OFF.
- CI cleans the synthetic pack before uploading the executable and explicitly
  refuses private `*.clientwalls34.jsonl.gz` files.
- Full real Laboratory 15-wave gameplay, actual F8 wall impacts, full map
  fidelity and exact retail collision-group behavior remain NOT verified.

### Remaining blockers

1. Retrieve/reconstruct **original exterior infected entrances and traversal**
   (doors, hop points, terrain) from sufficient authorized source evidence.
2. Recover complete original terrain and MeshPart/UnionOperation geometry, which
   is missing from the supplied RBXLX.
3. Confirm native player and infected collision interactions on the original
   complete scene using an interactive Windows playtest.
4. Finish and verify the other nine full original maps.
