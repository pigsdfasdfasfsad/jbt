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

## Initial native failure and collision reconstruction

GitHub Actions run [38299114181](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38299114181) passed the original Windows gameplay regressions, then the production Manor Wave5 Escort remained stationary at (-4.25, 2.96, 28.75), rejected all nine destination routes on every physics frame and correctly did not award credits. It proved source-contract passing alone did not establish real traversability.

A follow-up diagnostic run [38299839014](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38299839014) isolated the break in the physical route:
- Actual upper side-deck to crosswalk entry: **true**
- Crosswalk entry to centre: **true**
- Centre to upper stair landing: **false**
- Stair head to first sloped ramp section: **true**
- Ground-level physical Escort graph from stair exit to selected goal: **18 supported points**
- Actual route to goal: **zero**

The fallback crosswalk had 1.20m Z collision length, and the existing sloped ramp landing had only 1.40m Z length. Their nearest surfaces stopped **10cm apart**, leaving the real .42m Escort capsule with no supported crosswalk-to-stair route. An initial additional connector collider (`*_UpperStairConnector94`) was **rejected**: native run [38300447954](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38300447954) showed it blocked the formerly verified real player uphill traversal in Pass78 at Y=2.89m (a raised collision face just before the landing). The extra collider was removed. An interim attempt to extend the existing `*_UpperCrossing80` horizontal deck from 1.20m to 1.60m passed the old stair regressions, but **did not resolve the Escort capsule obstruction**, so the original 1.20m collision dimensions have now been restored. No owner-source geometry, survivor collision radius or domain reward authority is changed. The route must still pass native regression verification.

The new route also bounds failed goal searches to once per .75s instead of making 9 expensive candidate plans on every physics frame. It keeps survivors on their sloped stair route until their real feet reach the ground, rather than switching to a ground-only graph at 2.2m elevation.

**Follow-up status:** native success remains unverified until a new Windows export passes the full preceding Escort regressions, completes the physical Manor stair descent, pays the domain reward once and passes the private-content exclusion check.

## Additional capsule collision investigation

Windows run [38300907996](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38300907996) passed the earlier Pass78/83/88/89/90/91/92 native regressions using the longer **existing** Manor crosswalk (1.60m), but the Manor survivor still rejected `crosswalk centre -> stair upper`. The 18-point grounded lower route remained valid. Therefore the previous 10cm floor gap was a real geometry omission but **not enough to explain the failure**; do not claim that an added plank or a wider catwalk verifies NPC navigation.

The next instrumentation independently reports the full .42m capsule CastMotion safe fraction, the first torso wall-ray hit, and 50mm-resolution physical support probes along the failing segment. A CI-only preflight runs this evidence immediately after the Windows export; only a real fully completed production Manor Escort proceeds to the earlier ten-map/native regressions and eventual privacy audit. No reward, navmesh, physics layer or collision mask is disabled for that test.

## Pass94 actual collision root cause and bounded detour

Fast native Windows run [38301535595](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38301535595) measured the central crossing-to-stair CastMotion safe fraction at **0.4297**, with no torso wall-ray hits. Its 50mm ground audit at approximately Z=28.10m instead hit a nearly vertical pitched-landing end face, collision normal `(0, 0.4267, -0.9044)` (too steep to walk). The actual failure was not only the visible 10cm gap. No collision should be disabled.

The **new proposed route** from upper Manor side balcony to its same stair head first moves along the *already solid side deck* at X offset 3.35m, then steps horizontally onto the existing 5.6m-wide ramp head. This avoids the problematic forward-facing collision nose completely. Each side-lane leg is checked with the identical full-size survivor capsule, physical floor samples, and line-of-sight; if a leg is not safely supported, the planner still attempts only the older verified crossing path before rejecting the route. All direct NPC teleports and unearned completion rewards remain forbidden. Original 1.20m upper-crosswalk geometry is restored.

**Native end-to-end success is pending**, including actual survivor descent, exactly-once domain payout, Pass78/83 stair regressions and remaining eight-map/Expressway tests.
