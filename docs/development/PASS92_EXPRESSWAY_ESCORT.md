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

**Status initially pending native Windows verification.**
