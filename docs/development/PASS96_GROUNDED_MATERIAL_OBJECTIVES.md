# Pass96 — Physics-grounded Load/Repair materials

**Base:** [Pass95 verified exported Windows build](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38306155437).

## Previously failing gameplay assumption

The Load objective's ten loose boxes and Repair objective's ten/twelve components were distributed on a naive 6–8.5 metre ring around their objective anchor. No floor query or world-wall check was performed at material spawn time. On reconstructed maps (particularly multi-storey Manor and Lab interiors), boxes could float, land in walls, or appear behind an inaccessible room even though the E-key interaction itself already enforced a world-ray occlusion check.

## Implementation

Pass96 adds `Pass96GroundedMaterials.TryPlan` as a **source-absent fallback-only** spawn plan. For each Load/Repair mission:

- Seek a real supported floor under the approximate depot, within a bounded 6m horizontal offset. Keep the same floor; never invent a cross-floor teleport or rewrite owner-original markers.
- Validate each proposed item using Pass90's five-ray walkable-floor and full-body standing-clearance check, then Pass89's real 0.42m survivor capsule sweep, supported-floor samples and occluding-wall rays back to the depot.
- Use multiple bounded radial candidate rings to keep materials distributed and visually separate. If a cramped reachable space cannot fit every unique prop, stack remaining **non-colliding** props only at an already-verified location; the original discrete E press still collects one item at a time.
- Material meshes render 35cm above the actual colliding floor. The root target and item points use the same grounded floor anchor. No original owner-source scene or other objective family is altered.
- Expose real telemetry: `Pass96GroundedMaterialLayout`, `Pass96RejectedMaterialCandidates`, `Pass96OverlappingMaterialFallbacks` and `Pass96MaterialFeet`. An unsupported plan falls back to the old approximate layout but is never falsely reported as physics-verified.

## Native verification

The Windows `--smoke-pass96` smoke enters ten actual reconstructed maps, creates real Load and Repair objective actors (200+ live material locations), and verifies every material is physically on the deposit's story and has a direct, fully collision-audited approach to its deposit. On Ranch it additionally uses actual key events for one E pickup and one E deposit. It uses an isolated profile, preserves the previous Pass78–95 native smoke tests, and keeps the public CI privacy exclusion checks.

**Status:** candidate pending native Windows verification. This is practical gameplay reconstruction, **not** recovered retail item coordinates, original prop meshes, NPC AI or evidence of a fully manual fifteen-wave playthrough.

## Distribution

Public CI intentionally does not upload owner-private source packs or Windows binaries. The owner-only Windows packaging path remains separate; a green CI workflow is not itself a public download link.

## First native regression and provenance repair

The first Pass96 Windows run [38321854869](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38321854869) **passed the actual ten-map material accessibility preflight**, then failed Pass88's existing strict marker-rotation assertion on Ranch Wave1. Pass88 had previously assumed every objective's world transform must exactly equal its chosen Pass87 marker. Pass96 deliberately moves only material-family fallback objectives to a verified nearby floor, which invalidates that transform-equality assumption without changing the selected marker.

The fix records `Pass96AuthoredMaterialAnchor` **before** physically grounding the depot. Pass88 now requires exact equality between that original scheduled marker and the computed Pass87 rotation, while independently requiring the relocated material objective's complete supported layout and a bounded 7.5m physical relocation. This strengthens the original rotation contract rather than disabling the test or allowing arbitrary objective teleports.

**Status:** regression correction committed; Pass96 needs a fresh all-green exported Windows run before it is verified.
