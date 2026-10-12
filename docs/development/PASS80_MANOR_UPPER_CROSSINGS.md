# Pass 80 - Fallback Manor balcony crossings

From Pass79, add a physical crosswalk next to each of the three verified
reconstructed stair exits (West, East, Front). The walkable static box spans
9.0 m across the original fallback split deck, 1.20 m along the bridge, and
has its upper face aligned with adjacent platform surfaces, preventing the
physical capsule from catching on a lowered internal deck edge.
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

Pass83 native regression found that infected could skim the outside edge
of a crosswalk before reaching its inner entrance. The actual entrance
waypoints now sit at +/-4.25m on the supported side decks, and an agent
outside the supported straight approach must first retreat through audited
floor-support segments. Unlike a global location reset, every centimetre
is still traversed by the ordinary CharacterBody physics controller.
