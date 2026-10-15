# Pass 120 — Original-source item and fortification grounding

**Branch baseline:** Pass119 \`twr-pass119-source-spawn-physics-safety\`.
This pass continues the actual Godot/.NET Windows remake, not the separate
Laboratory sandbox.

## New gameplay work

The source loader recovers **127 Laboratory item markers** and
**47 Laboratory fortification markers** from the owner-held original Roblox
place. Until Pass120, the game instantiated the first four and first two
randomly selected source markers even when their original floor/mesh collision
was not recoverable in offline Godot. The fallback blockout maps already
had a physics-gated loot placement policy, but original maps did not.

Pass120 introduces source-only \`Pass120OriginalLootPlacement.TryGround\`.
It uses real Godot floor raycasts, a bounded height discrepancy, an upward
surface normal and a 0.58 × 0.55 × 0.58m static-world overlap test.
**Exact original X/Z source placement coordinates stay unchanged.**

At match start, original-source loot initialization occurs after Godot has
synchronized the installed source physics scene (two physics frames), instead
of performing collision queries while the scene is still in \`_Ready\`.
For each type, the original pool is shuffled without repeated candidates and
scanned until four supported items and two supported fortifications are placed
or the entire original pool is exhausted. Unsupported source locations do not
receive invented floor meshes or new authored map positions.

Normal pickup handling, E-key interaction, domain inventory authority, loot
type selection and respawn delay remain in place. When an original marker's
pickup is collected, resupply uses the **original authored marker**, not the
ground-offset render centre; physics support is checked again when it returns.
Original-source respawn queries run in \`_PhysicsProcess\`, while existing
approximate-map resupply stays in \`_Process\`.

The native original-spawn filters introduced in Pass119 remain enabled.
F10 shows source spawn/loot validity counters; F11 includes the same counters
in the user's local, offline diagnostic JSON. The counters are per placement
attempt, not an estimate of unique reconstructed map objects.

## Verification

\`--smoke-pass120\` uses a synthetic 8m square floor, a solid wall, an
unrecoverable empty-floor marker, a wrong-elevation marker and a dynamically
blocked marker. It tests genuine Godot physics clearance and creates the
existing production \`PickupActor\` at the accepted exact source X/Z position.
The test writes no private XML, scene files, model IDs or original PNGs.

The full inherited Windows build and regression suite, the Pass119 source
infected-spawn test and the Pass120 native fixture must all pass before this
pass is considered verified. The public workflow intentionally does not upload
an original-map executable to avoid releasing owner-held source assets.

## Important limitations

- Source markers do not prove the original Roblox base mesh or SmoothGrid
  terrain has been fully recovered; missing physical geometry still needs
  reconstructing and locally playtesting with the owner-private pack.
- Scanning past unsupported source markers can change original source
  pickup selection probabilities, and is explicitly a safety adaptation.
- The separate 9-map reconstruction, first-person weapon animations,
  some imported mesh/texture fidelity, source navigation connectivity,
  objective parity and full 15-wave human playthrough remain incomplete.
