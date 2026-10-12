# Pass 81 - Guarded upper-room detours

The reconstructed Manor's upper floors contain a split central deck and
existing collision-dividers. Pass79 rejects unsupported direct steering; Pass80
permits real crosswalks at stair exits. Pass81 adds a bounded selection of five
same-deck corridor lanes (inward, outer and actor-aligned) when those two
simpler routes do not work. Every segment is re-sampled against Godot real
collision at 60 cm intervals with three lateral floor probes.

A detour cannot cross from the left half to the right half without the
Pass80 verified bridge and cannot traverse from one wing to another through
unsupported air. The best supported candidate wins by physical path length.
An unreachable/occluded target yields no invented route, so infected stop
rather than clipping through floors.

This is local fallback gameplay engineering, not original Roblox navmesh
reconstruction. Pass83 is the target exported Windows physical acceptance.
