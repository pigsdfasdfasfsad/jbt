# Pass 30 - Runtime geometry culling and gameplay diagnostics

This branch builds on the native-verified Pass 29 Godot Windows executable.

- Fixed source primitive streaming to test distance from the **full oriented
  world-space geometry bounds**, not the centroid of a 96-stud map tile.
  Private Laboratory source audit: at player spawn 1, the old code could
  hide 155 batches containing 4,126 genuine source primitive instances
  even though their geometry falls inside the 145-meter render range.
- Changed the lingering spore-gas wall raycast to run on Godot's physics
  tick, not the variable-rate process tick.
- Added an in-game F10 diagnostics overlay showing FPS, map startup time,
  active infected/pickups, source navigation nodes, collision tile count,
  source primitive instances and current visible/total geometry batches.
- F11 exports a local JSON diagnostics snapshot to Godot's per-user game
  data folder. It does not submit telemetry or modify gameplay.
- Added native Windows executable smoke for Laboratory gameplay startup
  and source-geometry AABB visibility.

This is still not a source-perfect TWR map or a human-completed 15-wave run.
MeshPart/UnionOperation binaries are still absent. Private original Roblox
asset content is never committed to public CI.
