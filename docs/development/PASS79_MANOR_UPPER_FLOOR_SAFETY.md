# Pass 79 - Collision-verified Manor upper-floor safety

Parent: Pass78 (632 Python tests and twelve native player/infected stair traversals).

This pass adds a fallback-Manor-only, real-physics support audit to infected
same-level upper-floor chase. Three laterally separated down-facing rays are
cast every 0.60 metres along a candidate corridor; floor surface must be
within 0.26 m of expected feet height, have upward-facing normal >=0.74 and
no ordinary collision-ray obstruction. Segments are capped at 45 metres.

A first-pass AI guard prevents direct movement across the 4 m unsupported
split-deck void, and prevents the ground-floor navigation graph being
misapplied to upstairs feet. No actor is teleported. This pass does not yet
provide an alternate route across the gap; Pass80 will add an explicit,
physically supported approximation and collision-audited gate-to-gate route.

This is not a recovered original Manor interior or original Roblox navmesh.
Owner original scene packs, retail stair triangles and original audio remain
outside Git history.

Acceptance: Python structural tests on support sampling, layer isolation,
and AI movement guard; final cumulative native scenario in Pass83.
