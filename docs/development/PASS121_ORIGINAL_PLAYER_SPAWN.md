# Pass121 — Source-authored player spawn physics clearance

**Baseline:** Pass120 \`twr-pass120-original-loot-grounding\`, the established
Godot 4 + .NET offline Windows remake. This is NOT the unrelated Pass48
Laboratory sandbox.

## Why this pass exists

The original Roblox Laboratory has **eight recovered named PlayerSpawn
markers**. The existing loader chooses the first marker by name and sets
the live \`FirstPersonPlayer\` origin at that CFrame +0.5m, whether or not
the owner-private offline conversion recovered floor triangles, support
collision and room standing clearance beneath it. The infected and pickup
marker placements gained physics tests in Pass119–120, but the human player
could still load inside a wall or fall through absent MeshPart terrain.

## Production implementation

- \`RuntimeMapLayout.SourcePlayerFootMarkers121\` carries ONLY the actual
  recovered original player markers, sorted by their source names;
  old \`PlayerSpawn\` and infected/item/fortification APIs are retained.
- The imported-source player is created normally for HUD, loadout and
  camera setup, but its physical movement is temporarily disabled.
- After two Godot physics frames, Pass121 tests the preferred original
  player marker then remaining original markers in deterministic cyclic
  order. No fabricated X/Z, random jitter, unsafe distant lower-deck floor
  or unofficial terrain patches are allowed.
- Five real layer-1 floor ray probes check walkable and continuous support
  beneath a standing player. The exact production 0.29m-radius, 1.8m-high
  capsule must fit without hitting a solid wall. The player's own RID is
  excluded from rays and shape queries.
- The player is placed at that **source marker's exact X/Z**, with the
  minimum 0.08m foot clearance above the confirmed original-world floor.
  Normal walking/jumping/weapon handling then resumes.
- If ALL recovered original player spawn markers lack viable collision,
  the game rejects that map start, shows an explicit source-support failure,
  and keeps the player from free-falling through missing geometry. A
  rejected map is NOT silently remapped to an approximate blockout.
- The older nine-map approximate scene pathway is not changed, nor are
  the original infected, loot, weapon, inventory and wave rule modules.

F10 now displays whether the actual original player spawn was validated,
how many original candidates were rejected, and whether a different ORIGINAL
marker was selected. F11's user-local JSON includes the same counters.
These are diagnostics, not evidence that an owner-private pack passed CI.

## Validation

A new \`--smoke-pass121\` runs in the **exported Windows executable**, with
only a synthetic box floor, a blocking wall, a missing-floor marker and a
real-sized \`CharacterBody3D\` capsule. It proves that the policy rejects
an unsupported selected marker and wall obstruction, settles on an alternate
already-original marker without changing X/Z, preserves a safe preferred
marker, rejects fabricated origins, and excludes the player's own RID from
future support queries.

The CI retains the Pass118 repeated ExitCode=0 Windows shutdown gate, the
Pass119 infected source-spawn test, Pass120 original pickup support test,
all earlier gameplay objective/weapon/map regression tests and the Pass117
owner-private source installer refusal checks. No private meshes, archive,
Roblox username, screenshot or map XML may enter a public commit or artifact.

## Honesty and release limitations

The native fixture is synthetic and cannot substitute for an actual
owner-private original Laboratory playtest. Incomplete source MeshPart,
CSG and SmoothGrid terrain geometry can cause a fail-closed rejected map
start. It does not create missing floor polygons. Real source-map collision
fidelity, gameplay pacing, offline visual assets, stairs/AI navigation,
hand-tested completion and Windows release packaging remain unverified.
