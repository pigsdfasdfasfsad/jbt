# Pass 64 — Source-guided Shotgun Shell and Nested Bolt-Action Motion

Branch: `twr-pass64-source-shell-and-bolt`
Base: successful Pass63 Windows checkpoint.

## Source evidence, not approximated claims

The checked-in owner-normalized `Content/weapons/entries/*.json`
contains the following inert original Luau animation data:

| Weapon | Source sequence | Motion evidence |
|---|---|---|
| Mosin Nagant | `Sliding` | Two `BoltRel` translation cues (0.45 studs, return); two nested `Bolt/Bolt Handle` rotation cues (90 degrees, return); a `7.62x54R Shell` ejection marker |
| Winchester Model 70 | `Sliding` | Two source bolt travel cues (0.45 studs, return); two bolt-handle rotation cues (60 degrees, return); a `.308 Winchester Shell` ejection marker |
| Mossberg 500 | `Sliding` | Original `12 Gauge Shell` marker at the pump stroke, direction `Vector3.new(0.86602,0.5,0)`, source speed 15 studs/s |
| SPAS-12 | `Sliding` | Original `12 Gauge Shell` marker at the cycling stroke with the same source direction and speed |
| Benelli M4 | `Sliding` | Original `12 Gauge Shell` marker and separately authored `Offset` and direction |
| Serbu Super Shorty | `Sliding` | Original `12 Gauge Shell` marker, but compound inverse offset cannot be parsed as a safe literal (fallback ejection port used) |

These source records describe **animation marker timing**, not exact
original shell physics, missing casing meshes or game-world collisions.
No Lua/Luau execution is involved.

## Implemented

1. `Pass64SourceAction.cs` reads original `Sliding` and `Reload`
   motor keyframes for exact hierarchy
   `["Bolt","Handle","BoltMotor"]` and
   `["Bolt","Bolt Handle","Handle","Bolt HandleMotor"]`.
   The only symbolic motion accepted for bolt travel is
   `module.Stats.Rels.BoltRel` followed by bounded numeric
   `CFrame.new(...)`; `CFrame.Angles` on the handle accepts bounded
   numeric Z rotation and the **exact original Mosin** literal
   `(math.pi/2)` without evaluating a runtime expression.
2. Pass62 owner-sidecar binding now exposes the real source nested
   `Bolt/Bolt Handle` group and its authored Model Handle pivot.
   `ApplyBoltAction` composes the moving whole bolt carrier **after**
   the local bolt handle rotation, so all nested handle meshes follow
   the carrier and return to original Part transforms. The original
   Motor6D C0/C1 joint offsets are **still not recovered**.
3. `Pass64SourceAction` also decodes original `Shell` marker names,
   velocities and bounded numeric `Vector3.new(x,y,z)` headings. The
   source speed is converted using the engine's historical stud scale,
   with optional numeric-only positional offset.
4. `WeaponViewModelRuntime` now creates **explicit proxy** casing
   cylinders at an approximate ejection-port location at the original
   source event times. Ejected cases use the source heading/speed but
   deliberately simplified gravity, angular motion, material, dimensions
   and short lifetime. A strict 8-proxy cap and deterministic removal
   prevent accumulating shells.
5. Existing Pass63 magazine `MagRel` poses are retained for
   `ReloadType=="Increment"` shotguns, representing a shell feeding
   into the weapon using owner-original magazine/round group transforms
   where verified source assemblies exist. Without the owner packs,
   the existing visual fallback remains.
6. No ammunition counts, reloading authority, firerate, progression,
   economy, damage, zombie behavior, map selection or campaign systems
   were modified.

## Native acceptance and synthetic owner safety

- `tests/test_pass64_source_action.py` checks original data records,
  parser fences, two-level pivot binding, source-only fixture generation
  and strict no-overwrite/no-unknown-file-delete cleanup.
- `tools/maps/make_pass64_bolt_shell_fixture.py` creates purely fake
  original-shaped source records for Glock, Mossberg and Mosin. The
  Mosin has a Bolt group and a genuinely nested Bolt Handle group,
  including two separately visible child meshes.
- `--smoke-pass64` in the exported Windows Godot executable verifies
  source cue counts, exact Mosin math.pi/2 and Winchester numeric angles,
  rotation of the **nested** handle before the carrier cycles, carrier
  travel and return-to-rest, timed 7.62x54R and 12-gauge casing markers,
  bounded shell-proxy deletion, and source-guided incremental Mossberg
  feeding pose. Synthetic owner packs are deleted before private asset
  exclusion checks.
- Pass59–63 native weapon regressions and the ten-map scripted wave
  progression smoke continue to run on the new branch. The latter is
  not a human-controlled 15-wave full-match test.

## Known fidelity limits

The original exact original shell polygon meshes, ejector port offsets,
per-shell collision, original casing audio, reliable cartridge inventory
per insert, Motor6D C0/C1 source joint offsets, hand/arm animation,
source dynamic AimCF, original unavailable external weapon mesh
triangles/UVs, full map visual reconstruction and real 10-map survival
acceptance are not delivered by Pass64.

Owner-private weapon packs are not committed to GitHub and public
Windows executable upload remains disabled.

## Verification record

Pending successful actual Pass64 exported Windows CI. Implementation
commits and regression assertions alone are not native verification.
