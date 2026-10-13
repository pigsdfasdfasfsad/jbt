# Pass 107 — Viewport-aware objective carry presentation and ADS stow

**Parent:** [Pass106](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass106-material-models-and-carry-view), the native-verified 757-test material carry and real W/E gameplay baseline.

## Implemented

Pass106 camera-attached Load/Repair material visuals used a single static pose (0.42, -0.45, -0.78) for every view size, FOV and weapon. Pass107 introduces `Pass107CarryPresentation` and applies it to the very same camera-mounted model on genuine E pickups and each real objective frame. The new approximate visual anchor follows the active viewport aspect and camera FOV, is placed in the lower-left of the camera view rather than on the sight axis, and adds a small bounded walk bob. It uses a smaller model scale.

While the **real player** enters right-button ADS (or camera FOV is less than 54 degrees) the camera-only item mesh is temporarily hidden. Its real held-item status is not touched; it can still be deposited by legitimate E gameplay and reappears when hip-firing resumes. This avoids putting camera-held props directly in front of magnified source sight or scope artwork. The weapon viewmodel, recoil, FOV authority, physical map, bullet rays, collisions, one-item carry budget, pickups, reward logic and persistence are unchanged.

The position, dimensions and orientation are **reconstructed approximations**, not original source meshes or motions. Headless geometry checks cannot replace human graphical 16:9, ultrawide and 4:3 view comparisons.

## Regression checks

- `tests/test_pass107_carry_presentation.py`: code/authority boundaries and explicit visual presentation invariants.
- Pass103 and Pass104 exported native Windows real W/E loops now also assert that each successfully held 3D model occupies the designed screen-relative hip anchor (`HasSafeAnchor`), in addition to the Pass106 40+ pickup/clear camera lifecycle checks. The previous native Pass105 infected-melee carried-material scenario and weapon/stair/map/privacy checks remain selected.
- `.github/workflows/windows-build.yml` includes the new branch and reuses existing Pass106 and earlier native gates. The workflow does not publish or upload a public owner-private executable.

## Remaining release/visual blockers

Native acceptance verifies transform/mesh lifecycle and E-key physical interactions, **not** actual rendered pixel occlusion, human-camera ADS transition or authentic artwork. Before a 1:1 claim: capture real Windows screenshots for a representative sidearm, rifle, scoped rifle and melee weapon at 16:9 and ultrawide; compare hip/ADS/crosshair against the original game recordings; tune the per-item model size and camera offsets; confirm shadows, missing source meshes and HUD overlap. The original ten maps are still partly approximated, and a complete owner-packaged offline EXE is not delivered by this public-source branch.

## Verification status

The Pass106 parent has 757 Python tests and full exported Windows native acceptance confirmed in [run 38348938021](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38348938021). Pass107 has source, test and workflow commits; its own Windows CI outcome must be consulted before asserting it passes. See [Pass107 workflow runs](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass107-carry-aim-safe-presentation).
