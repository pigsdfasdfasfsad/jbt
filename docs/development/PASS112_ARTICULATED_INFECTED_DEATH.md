# Pass 112 — Source R6 pivot-aware death animation

**Baseline:** [Pass111 corpse visuals](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass111-bounded-infected-corpse-fall), including the exported Windows genuine Glock kill-and-corpse native acceptance. At branch creation Pass111's larger CI matrix remained in progress; verify it separately.

## Why this pass is needed

Pass111 created a lightweight death silhouette from the actual rendered infected meshes, using a **single rigid body tilt**. Owner-provided footage and the recovered `ModuleScript.Ragdolls.Source.txt` metadata establish a richer R6 death presentation. The recovered source-index lists `MakeLimbBoxes`, `Launch`, and `RagdollInfected` but provides **no fully recoverable original joint constraint settings, retail animation clips or joint physics** in this public repo. Reproducing a physical source-perfect ragdoll is not yet supported.

## Actual implementation

`Pass112R6CorpseArticulation.TryFindAnchor` recognizes BOTH recovered `LeftArmPivot`/`RightArmPivot`/`LeftLegPivot`/`RightLegPivot` nodes in a locally installed infected kit and the fallback `LeftShoulder`/`RightShoulder`/`LeftHip`/`RightHip` nodes. It does not invent shoulder locations when a prepared imported 3D rig lacks these names.

When Pass111 snapshots each **actual visible** `MeshInstance3D`, Pass112 optionally creates one visual-only `Node3D` corresponding to its real source-parent limb pivot. It reconstructs the pivot's actual death-frame world transform in corpse-local coordinates; the mesh's own pose is then reparented relative to that pivot, keeping the frame of the lethal hit continuous. Every non-limb mesh stays in the original flattened static corpse group. The existing cap of **64** render meshes per corpse and **16** live corpses remains intact. Actual/material override references and private source mesh geometry are retained strictly in memory; nothing is uploaded or exported.

For the first ~0.62 seconds after the hit, the corpse body still pitches down, but the four independent arm/leg pivots now rotate relative to their **own original death-frame rotations**. Arms spread out while both knees bend at different rates with a brief damped rebound. Headshots reverse the arm's forward fall component. These angular curves are explicitly **artistic approximations**, not original retail Motor6D constraints, bone simulation, colliders or Roblox physics.

The render-only corpse still has **no** damage authority, AI, collision, navigation, inventory, XP, reward logic, owner-private pack files or lifespan changes. The real living infected's attack, headshot multipliers, health, spore hazards and award callbacks remain untouched.

## Native exported-Windows acceptance

`--smoke-pass112` reuses the verified Pass70 real first-person `Glock 17` kill, synthetic *temporary* isolated platform and synthetic source-weapon assembly fixture. The only headless cursor-capture bypass is activated under the explicitly named native smoke flag. The actual weapon interaction, physics ray, ammo spending, kill/XP reward, reload action and transition into Wave2 remain normal.

It additionally verifies:

1. A single dead infected becomes a Pass111 non-colliding corpse with 8–64 actual rendered mesh leaves, including four recognized R6 shoulder/hip pivot groups and at least six limb-associated mesh parts.
2. By the time the player finishes their ordinary R-key reload, the rooted fall progress exceeds 96% and independently applied limb rotation exceeds 0.15 rad. The original death pose remains the animation's initial orientation rather than substituting an unrelated skeleton.
3. After the actual Wave1-to-Wave2 transition, corpse objects are gone and original ammo, health, reward, XP and wave accounting remain valid.
4. Windows smoke output includes **three compatible markers** emitted by the *same* native test: `TWR_SMOKE_PASS112_ARTICULATED_CORPSE_OK`, `TWR_SMOKE_PASS111_INFECTED_CORPSE_OK`, and `TWR_SMOKE_PASS70_LIVE_COMBAT_OK`.
5. The source-model fixture is created temporarily by the existing `make_pass68_aimcf_fixture.py` and removed in `finally`; source-safe fixture parsers accept it **only** under `--smoke-pass70`, `--smoke-pass111`, or `--smoke-pass112`. Every normal Windows game launch refuses synthetic owner lookalike packs.

All earlier Pass111 and Pass103–110, map, stair, escort, infection, 15-wave domain replay, persistence, source-leak and packaging gates remain on the branch. Windows CI still cannot prove human screenshots match original Roblox animation or wall-floor contact; those comparisons require real graphics and owner reference footage.

## Native acceptance result and outstanding full-suite boundary

The dedicated exported-Windows **Pass111 real Glock kill/corpse stage** and **Pass112 articulated R6 limb motion stage** both completed successfully in [Actions run 38422509731](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38422509731). The native test used an actual shooting/reload/wave transition, recognized four shoulder/hip groups and six or more limb mesh leaves, and observed independent falling-limb movement. Python validation and the Windows compilation/export also passed.

Later minor revisions release native pose handles at corpse destruction and explicitly dispose the temporary isolated gameplay scene before exiting Godot. Their [latest Windows run](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass112-articulated-infected-death-pose) must be checked before calling the current SHA verified.

One separate Pass111 baseline [run 38420641147](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38420641147) passed its own corpse acceptance and many prior regression steps but returned a Windows heap-corruption exit from the historical **Pass92 Expressway Escort** process after its gameplay success markers. That is not proven to be caused by Pass111 death rendering; there is no Pass92 infected-kill code path shown in its acceptance. Do not claim the historical full workflow was green, or dismiss a nonzero process exit as a pass.

## Code and verification

- [Pass112 source branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass112-articulated-infected-death-pose)
- [Pass112 Windows workflow](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass112-articulated-infected-death-pose)
- `src/Twr.Godot/Scripts/Pass112R6CorpseArticulation.cs`: recovered R6 pivot aliases, independent bounded limb pose functions
- `src/Twr.Godot/Scripts/Pass111FallenInfected.cs`: source-material-preserving nested static renderer, pivot local transforms, expiry and telemetry
- `src/Twr.Godot/Scripts/Pass70LiveCombatAcceptance.cs` and `Bootstrap.cs`: real Glock physics/kill, limb pose/cleanup and stage test
- `tests/test_pass112_articulated_corpse.py`: regression assertions and source pack safety conditions

**Status boundary:** Source commits do not establish native-Windows green status until the exact commit's GitHub Actions compilation, dedicated Pass112 stage and older full workflow report success. A complete original 1:1 ragdoll would still require full local R6 joint topology, constraints, force curves, and owner visual comparison.
