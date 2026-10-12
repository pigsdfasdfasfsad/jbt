# Pass94 — Reconstructed Manor upper-floor Escort descent

**Base:** Pass93 (verified Windows CI #38298143607), and Pass92 (nine real, collision-checked, idempotently rewarded Escort scenarios).

## Purpose

Manor Wave5 spawns its reconstructed Escort at the front hall upper-floor marker. Previously this survivor used legacy direct straight-line steering: it had neither a supported upper-catwalk route, a stair-descent route, nor a floor-grounded destination. Pass93 prevented unjustified reward, but it did not give the survivor a way to complete.

Pass94 introduces a **Manor-only, fallback-only** physical route planner. It uses the existing three Pass77 stair endpoints, Pass80 crosswalks, Pass82 upper objective markers, Pass73 independent Escort collision graph, Pass89 full capsule sweeps, and Pass90 grounded destination/reward authorization.

- Validate the real 0.42m Escort capsule at the upper spawn rather than applying the ground-floor-only Pass91 filter.
- Resolve the original approximate 24m north goal against the *real* lower collider when no supported upper deck exists there.
- Route from the side balcony across the real 1.2m crosswalk to the stair head, descend 12 measured sloped-collider intervals, and reach the goal using the independently collision-audited ground Escort graph.
- Recheck every physical movement probe along the actual slope; a blocked/missing ramp or disconnected downstream graph returns no route.
- Keep existing infected navigation, normal nine-map Escort behavior, authoritative credit/XP accounting, and source-owner-private geometry completely unchanged.
- Never teleport the survivor or bypass the objective completion guard.

## Acceptance

The new exported Windows native test instantiates a real production Manor scene, advances only test-profile wave timers to Wave5, obtains the actual Escort survivor, observes a genuine mid-ramp visit and lower-ground arrival, verifies the complete domain-authoritative 4,000-credit/3,500-XP payout once, and repeats the completion command to test idempotence. Earlier Windows gameplay and privacy gates remain in CI.

**Status:** pending native Windows verification. Source mesh/animation exactness, later objective rotations, original retail multi-floor AI navigation, and human-controlled 15-wave playthrough are not claimed.

**If the physics acceptance fails**, preserve its actor-position, waypoint, blocked-step and graph telemetry, repair the real obstructing geometry or route rather than disable collision, shorten the test, or grant test-only currency.
