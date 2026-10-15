# Pass 118 — Native Windows shutdown reliability

**Baseline:** `twr-pass117-original-laboratory-private-installer`, head `71094a06f454c6fa433f308d3d71a84e53921a98`.

## Verified pre-pass defect

The Pass117 Windows workflow run [38506835062](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38506835062) completed compilation, the Python tests, preflights and the Expressway Wave6 Escort. The executable printed:

- `TWR_PASS92_EXPRESSWAY_ESCORT_REWARD_OK` (4,000 credits; 3,500 XP; idempotent replay).
- `TWR_SMOKE_PASS92_EXPRESSWAY_ESCORT_OK` (native actor travelled approximately 22.5m, 135 physics steps).

The process then returned Windows `-1073740940` (`0xC0000374`, heap-corruption termination) **while the engine was unloading**, so the job correctly failed. The output markers alone must never count as a Windows PASS. Earlier native Pass92 run 38296791093 succeeded; this may be shutdown-timing dependent.

## Pass118 implementation

- Route both successful and failing Pass92 exits through the idle deferred Godot call queue, avoiding synchronous `SceneTree.Quit` from a physics-frame async continuation.
- Verify the actual `GameplayRoot` node has been freed before restarting the offline lobby. The saved result and authoritative once-only reward remain unchanged.
- Allow an idle process frame for deferred callbacks before marking the teardown complete.
- On the Pass118 branch, run **two independent exported-Windows Expressway processes**; each must emit the reward, gameplay and teardown markers **and return exit code zero** without a fatal native exception.
- Carry forward the full previous Windows CI regression sequence, the strict owner-private Laboratory installer checks and the no-private-asset export guard.
- No original/source geometry or user archives are committed to this public repository.

## Verification status

These changes are code and CI hardening until the new Pass118 GitHub Actions workflow completes. A clean Windows exit has **not** been demonstrated merely by committing this pass. A native crash, even after correct gameplay markers, remains a failure to be investigated and not a reason to disable tests.

The Pass117 owner-private original Laboratory pack remains external to GitHub and unchanged.
