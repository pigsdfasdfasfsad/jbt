# Pass123 — Physics-frame source-neutral objective corridor verification

## Source baseline

Parent: Pass122 \`twr-pass122-provisional-objective-safety\`, connected main
Godot/.NET Windows game branch.

The recovered original Roblox source map lacks authenticated objective
coordinates. The previous pass marked two objectives as **provisional**
and checked that their world-positioned physics capsule had a floor.
However, it queried \`World3D.DirectSpaceState\` during render-frame
\`StartWave\` through \`SpawnWaveObjectives\`. Godot physics queries are
safest inside physics callbacks; a marker with a floor can also be behind
an impassable wall or accidentally reused by both same-wave objectives.

## Production changes

- \`StartWave\` still sets the actual authoritative match phase and waves.
  **Only** provisional original-source imports defer objective placement.
  Fallback-map, original infected, loot, weapons and domain scoring are
  unchanged.
- A physics tick selects the same one or two scheduled provisional
  objectives, checks source-neutral floor/support with Pass122, and verifies
  a short **bidirectional, full-capsule continuous walking corridor**
  from the actual Pass121 verified player spawn using Pass89. Candidates
  behind walls, on missing floors, or without an uninterrupted ground
  corridor fail closed.
- A marker already selected for a same-wave objective is rejected if
  another objective would overlap it. The planner checks the finite pool
  rather than inventing new coordinates.
- The next idle-frame \`_Process\` instantiates the selected real
  \`ObjectiveRuntime\` actors, keeping collision-object allocation away
  from the physics-query callback.
- The initial 1–2 objective attempt count, family selection, and
  authoritative completion/wave reward contracts remain unchanged.
  When no reachable provisional location exists, fewer objectives
  appear; this is logged as a reconstruction limitation rather than a
  fictitious source-accurate objective or silently paid reward.
- F10/F11 report accepted provisional routes, rejected corridors, and
  duplicate candidates, separate from original player/enemy/loot markers.

## Real Windows native regression

\`--smoke-pass123\` creates only synthetic physics objects:
a supported floor, a blocking wall, and a real-sized player capsule.
It confirms (a) a floor-supported but wall-disconnected objective is
rejected, (b) a different original **provisional** marker with a physical
walk lane is selected, (c) duplicate same-wave placement is rejected,
(d) missing floor is not invented, and (e) physically removing the wall
makes the first marker reachable again.

The CI requires the two native success markers AND process exit code
zero; success-looking logs followed by a heap-corruption crash do not
count. All Pass118–122 checks remain active. No owner's private mesh,
textures, ZIPs, Roblox XML, or map scene bytes are committed or
published.

## Incomplete fidelity

A continuous **straight** capsule corridor is a conservative test;
a real player may be able to navigate around a wall along a longer
route. It is not original retail pathfinding, a proper navmesh, or proof
that a non-Radio objective's spawned materials and survivor can traverse.
The full authenticated original maps, 15-wave playthrough, original
lighting/meshes, and release-ready offline executable still require
end-to-end owner-private validation.
