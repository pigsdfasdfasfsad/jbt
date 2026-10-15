# Pass119 - Authored original infected marker physics clearance

## Purpose
The owner's original Roblox place contains authored infected spawn positions.
Some original MeshPart and SmoothGrid terrain triangle meshes are not yet
recoverable offline. Prior source-mode gameplay instantiated an infected
capsule at any exact marker without checking whether **the current Godot
physics scene** had a floor and standing space there.

## Runtime change
- When **original source mode** is active, keep the same source-authored
  marker chosen by the existing RandomSpawnPoint function.
- Test floor height (within 0.70m of original feet marker), slope
  (surface normal Y >= 0.74), and complete 0.30m-radius / 1.60m-high
  enemy standing capsule against layer-1 world collision.
- Reject immediate player overlap within 2.5m horizontally. No new large
  "spawn safe radius" is imposed on the original retail placement rules.
- If the preferred original point fails, try only the other original
  infected markers in bounded cyclic order; preserve exact authored X/Z.
  No invented floor, random jitter, or teleport across decks.
- If all original markers are unsupported, reject that spawn rather than
  create a wall-embedded or falling zombie. Diagnostic counters record
  accepted, rejected and alternative-original selections.
- Keep Pass85 physics-gated **approximate** map spawns unchanged.
  F9 explicitly opted-in assisted navigation retains its existing
  separate placement behavior.

A source marker fallback may change exact spawn probabilities compared
with Roblox; it is a physical safety guard, **not** proof of 1:1 pacing.
Missing custom mesh triangles still require reconstruction.

## Native Windows verification
The new --smoke-pass119 Godot acceptance constructs ONLY an artificial
8x8 collision-supported floor, a blocking wall, and markers with
no-floor / embedded / supported positions. It uses real DirectSpaceState
ray and capsule queries, then demands that:

1. Unsupported original markers are skipped.
2. A blocked marker does not spawn inside a solid object.
3. A valid existing original marker is accepted without changing its X/Z.
4. A near-player candidate, a marker not in the original list, and
   a list with only unsupported markers are rejected.
5. The exported Windows test process exits with code zero.

The public CI deliberately contains no original Roblox map ZIP, scene,
collision pack or user screenshots. The **private original Laboratory
installation has not been validated by this synthetic fixture**.

## Outstanding completion work
Human first-person traversal of the owner source Laboratory, exact
custom mesh/terrain source recovery for the other nine maps, infected
route continuity, original visual textures, UI/firing fidelity,
performance, full 15-wave playthrough and a private Windows release
remain unverified.
