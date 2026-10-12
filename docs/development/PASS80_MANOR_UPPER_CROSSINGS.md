# Pass 80 - Fallback Manor balcony crossings

From Pass79, add a physical crosswalk next to each of the three verified
reconstructed stair exits (West, East, Front). The walkable static box spans
9.0 m across the original fallback split deck, 1.20 m along the bridge, and
has its upper face 0.035 m below adjacent platforms, preventing a raised lip.
This is a *new approximate* collision element, NOT recovered retail content.

Previous fallback wing interior divider colliders visibly overlapped the
stair passage. Their long north/south segments are conservatively split
around the reconstructed stair run while preserving remaining walls and
interior doors. No non-Manor map or recovered source scene is modified.

Same-level infected navigation now tries a four-section, Godot floor/LOS
audited crossing after the direct support test fails. Each route stage
checks the actual static floor at <=60cm samples. Waypoint acceptance is
tightened on elevated detours to prevent cutting corners over the gap.

Risk: this is approximate topology and may not match a retail Manor corridor.
The native CI acceptance in Pass83 will exercise physical passage. Do not
label this exact original geometry without owner comparison.
