# Pass87 — Nonrepeating approximate-map objective locations

The earlier reconstruction bound all normal-map Wave 1–15 objectives to
the first one or two invented fallback marker coordinates, so several
otherwise playable maps had monotonous and misleading spatial pacing.

Pass87 cycles among the available approximate objective anchors using a
bounded (markerCount-1) stride. Every marker is visited once per cycle;
two simultaneous objectives use distinct anchors when the map has two or
more. This is a deterministic local gameplay quality improvement, not a
claim that retail Roblox uses this selection algorithm.

Authentic owner/private source scene marker ordering is untouched.
Manor's established Pass82 early-wave downstairs / later-wave upstairs
behavior is intentionally preserved. Objective family eligibility still
comes from the source-backed ten-map catalog.

Pass88 checks this scheduling across all ten game maps while running
native map objects and one real infected wave per arena.
