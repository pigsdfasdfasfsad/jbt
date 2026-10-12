# Pass91 Bypass plaza doorway correction

The native eight-map Escort acceptance at GitHub Actions runs
38290072178 and 38290094725 reproduced a Bypass fallback blockage:
the real Escort stopped at (3.58, 0, -17.41) immediately outside the
PlazaNorth southern entrance after moving ~23 metres. The invented
PlazaNorth shell has a central X=4 partition whose long segment ended
at the *same Z=-18 plane* as its exterior doorway. The 0.42m Escort
capsule could not turn into the invented interior despite centerline
navigation.

Pass91 now shortens the **invented**, fallback-only north and south
plaza partition ends by 2.70m, leaving a clear entrance turning pocket
while retaining the rest of the physical wall. Both Bypass plazas are
treated symmetrically. Other interiors and all original-owner source
scenes are not modified.

Also records the live next Escort waypoint, remaining waypoints and
actual physics velocity on a future native failure. It does not
invent original Roblox wall geometry or bypass the collision-checked
CharacterBody3D movement and completion reward gate.

Verify with the Pass91 native eight-map Windows test and all earlier
native smoke suites. Results must be recorded only after CI success.
