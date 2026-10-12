# Pass92 — Grounded Expressway Escort without replacing infected AI

**Parent:** verified Pass91 (Windows CI run 38295592996; 688 Python
contracts and actual eight-map real Escort objective completions).

Expressway's original interim approximation uses a **hard-coded**
7x31 road graph for infected at a nominal Y=1.0m, not the actual
walkable Y=0m bridge deck. Pass89/90's Escort physics support auditors
would reject that artificial height, leaving Expressway on an old
straight-line completion pathway that could phase through checkpoint
vehicles or road barriers.

Pass92 adds an independent Pass73 *live Godot-collision-sampled* graph
only on the public reconstructed Expressway scene. It never swaps
the existing `ExpresswayNavigationRuntime` infected graph, never
enables recovered/incomplete original source geometry, and never
affects the eight previously verified Escort map implementations.

Expressway Escort Wave6 starts use the same 17-candidate capsule floor
audit from Pass91, then move through the same true physics movement and
exactly-once domain reward from Pass90. Road blockers and traffic
remain real static colliders with swept .42m survivor-capsule tests
on the current path.

CI acceptance (before claiming verification): run old Pass78/83/88/89/
90/91 native suites again, then run a real exported-Windows Expressway
Wave6 Escort, require actual survivor movement, final supported
destination, single authoritative 4,000-credit + 3,500-XP grant,
normal level-up credit effects, idempotent duplicate command and the
private-asset exclusion audit.

**Not original retail Expressway collision:** road, vehicles and
destination points remain author-reconstructed approximations. Manor
multi-story Escort navigation and human-played 15-wave survival remain
separate outstanding reconstruction tasks.

**Status: VERIFIED on exported Windows in run 38296791093.**

## Native Windows verification — 2026-10-12

**Successful GitHub Actions run:** [38296791093](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38296791093), source commit `6e6e06929cf50460ff8a4792e45edbf1c51388de`.

- **692 Python tests passed** (one inherited warning); Godot/.NET exported Windows successfully with **0 compiler errors** (one inherited compiler warning).
- Prior native Pass78/83/88/89/90/91 regression suites passed, including all eight previously certified ground-floor Escort environments.
- Real `Expressway` Wave6 Escort survivor traveled **22.52m in 193 physics-controlled movement steps**, reached a collision-supported destination and completed via the existing domain command.
- Reward **4,000 credits and 3,500 XP**, normal level-up accounting and an idempotent replay of the same objective ID passed.
- The original interim `ExpresswayNavigationRuntime` infected graph retained **204 nodes**. The independent new collision-sampled Escort graph contained **648 floor-valid nodes**.
- Native `TWR_PASS92_EXPRESSWAY_ESCORT_REWARD_OK` and `TWR_SMOKE_PASS92_EXPRESSWAY_ESCORT_OK` markers passed; original/private asset exclusion check also passed.

**Scope:** Nine of ten public reconstructed map Escort scenarios have physical completion checks as of Pass92. Manor's reconstructed upper-floor Escort and the original retail 1:1 map/animation/audio reconstruction remain outstanding. Test clocks are accelerated; no 15-wave real-time human playthrough was proven.
