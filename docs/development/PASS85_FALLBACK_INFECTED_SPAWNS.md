# Pass85 — Physical spawn safety on reconstructed maps

The fallback map spawner historically applied up to ±2.5 metres of
unverified XY jitter to a random map spawn. This could place infected
inside a static wall, beyond a floor edge, or next to the player.
Pass85 tries at most 24 existing candidate positions, requiring:

- at least 11 metres of planar separation from the living player;
- a local static floor face with upward normal >=0.74;
- floor height <=0.85m away from the expected infected feet level;
- room for a 0.30m-radius, 1.60m-high actual enemy capsule;
- no collision with player/world for the selected placement.

The accepted actor is spawned once at a real floor-supported position,
through the ordinary InfectedAgent and wave engine. If none of 24
candidates can pass, that spawn is dropped (no phantom teleport,
no wall clipping). Counters track verified and rejected cases.

The filter is bypassed for owner-exact original infected spawn markers;
their original X/Z coordinates are preserved. This code does not
alter existing optional F9 assisted Laboratory source navigation.
Original enemy spawn statistics/locations remain a separate fidelity gap.

Verification: Python contracts and Pass88 native mixed-map stress smoke.
