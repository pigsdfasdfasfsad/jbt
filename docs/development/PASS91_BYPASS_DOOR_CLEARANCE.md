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

## Pass91 entrance slab level

The same Bypass setback also has a second unverified step: the
reconstructed InteriorShell floor was centered at Y=+0.10m with a 0.20m
thickness, meaning its top was **0.20m above the outdoor Ground plane**.
Godot `CharacterBody3D.MoveAndSlide` does not automatically step UP
a vertical 20cm slab; a non-jumping Escort could reach the doorway and
remain physically blocked even if its route was clear of walls.

For the two Bypass plaza blockouts only, the slab top now aligns with
the existing arena Ground at Y=0. Other interiors, original owner map
packs and the source-recorded topology are unchanged. This does not
teleport NPCs or allow wall clipping; it removes an invented vertical
threshold from an invented doorway.
