# Roblox / Godot linear unit parity

The current original-scene Laboratory importer uses 0.28 Godot metres per
Roblox stud. Recovered infected WalkSpeed and weapon Distance/Range were
previously used as unscaled metres, which made distances and velocities
3.57x too large relative to authored source geometry.

Fixes applied:
- Player walk/sprint authored values 17/24 studs per second become
  4.76 / 6.72 metres per second in Godot. The sprint base of 24 is still
  approximate and should be calibrated against real original gameplay.
- Original infected WalkSpeed values are multiplied by 0.28 at spawn.
- Weapon maximum ranges authored in studs are multiplied by 0.28 when
  constructing Godot raycasts.
- Laboratory source geometry, collisions and light distances use the same
  source-to-world factor via RobloxUnits.MetersPerStud.

Unverified values that must NOT be treated as source-derived include
Godot jump acceleration, gravity, special-infected leap launch velocity,
and melee/interaction radius. Real-time 15-wave gameplay remains untested.
