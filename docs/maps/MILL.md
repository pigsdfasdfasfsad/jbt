# Mill reconstruction dossier

![Mill reconstruction outline](../assets/maps/mill-outline.png)

![Mill supplied reference board](../assets/reference-boards/mill-reference-board.jpg)

## Release identity

- **Release mode:** Regular, 15 waves
- **Environment / skybox:** Snowy
- **World location:** Canada — Milton Moose lumberyard
- **Supported objective families:** Load, Repair, Radio, Unpack, Damage, Escort
- **Collectibles / easter-egg references:** Spiffo plush; unusable Winchester Model 70 reference

## What the reconstruction must preserve

*Source: Mill.wiki*

- **Layout character:** a large lumberyard beside a **frozen lake**; freezing temperatures and snow
  have frozen the lake, **permitting infected to walk across uninhibited**.
- **Landmarks:** a warehouse facility (office cubicles, container boxes); a sawmill facility (stacked
  logs, locker room, lounge, supply closet, and a **catwalk** overlooking the area, reachable by two
  stairways); *Milton Moose* logging vehicles; a security cabin, a *FOXOIL* gas station and an office
  full of cubicles with an outside parking lot; a **tunnel** next to the office parking lot; an arch
  bridge leading to a mountain pass (the gallery also names a **beam bridge**); an abandoned
  18-wheeler next to the security cabin.
- **Infected spawns:** the frozen lake; the tunnel next to the offices on one side; the mountain pass
  next to the beam bridge on the opposite side.
- **Map-specific mechanics / details:**
  - On the bridge, an abandoned *Milton Moose* patrol truck carries an **unusable Winchester Model 70**.
  - The **Mill tunnel is directly connected to the Bypass tunnel.**
  - The frozen lake is wide and open but **has no item spawns**.
  - The sawmill facility is likely based on a *McCoy Logging Co.* processing plant from *Project Zomboid*
    (identical format).

## Engine scaffold

Each map is reconstructed as five independently verifiable layers:

1. **Static art** — terrain, structures, props, decals, signs, vegetation and background dressing.
2. **Collision + navigation** — player collision, infected barriers, pathfinding modifiers, traversal blockers and drop-offs.
3. **Gameplay anchors** — player starts, infected spawns, item boxes, objective anchors, fortification positions and helicopter/supply-drop support points where applicable.
4. **Environment** — sky, fog/atmosphere, color grading, lights, local/environment sounds and weather presentation.
5. **Map metadata** — map name, voting card/icon, objective support, mode restrictions and reconstruction validation data.

## Named locations visible in supplied reference material

- Camping Spot Mill breakroom
- BeamBridge
- GasStation
- Locker
- MountainPass
- Office
- SawmillFac
- SecurityCabin
- WarehouseFac
- WarehouseOffice
- UnusedMillTrees

This list is a **reference-asset inventory**, not a claim that every map instance has already been extracted. The source-map conversion pass remains the authority for the final instance-by-instance inventory.

## Reference image files indexed (17)

- `Camping_Spot_Mill_breakroom.png`
- `Card-mill.png`
- `Mill-BeamBridge.png`
- `Mill-GasStation.png`
- `Mill-Locker.png`
- `Mill-MountainPass.png`
- `Mill-Office.png`
- `Mill-SawmillFac.png`
- `Mill-SecurityCabin.png`
- `Mill-WarehouseFac.png`
- `Mill-WarehouseOffice.png`
- `MillIcon.png`
- `MillThumb.png`
- `MillThumb2.png`
- `Spiffo_Millv2.png`
- `UnusedMillTrees.png`
- `Vote-mill.png`

## Build sequence

- [ ] Freeze/hash source map and associated evidence.
- [ ] Extract hierarchy, transforms, dimensions, materials, collision and named anchors.
- [ ] Generate a machine-readable instance inventory.
- [ ] Generate top-down bounds/outlines from extracted geometry.
- [ ] Build engine-neutral asset mapping and resolve external dependencies.
- [ ] Reconstruct static scene in Godot.
- [ ] Add collision/navigation and validate traversal.
- [ ] Add spawn, item and objective anchors.
- [ ] Recreate lighting, atmosphere and audio zones.
- [ ] Run visual/fidelity comparisons against supplied captures.
- [ ] Run gameplay acceptance tests and record known deviations.

## Completion evidence expected

A map is not considered reconstructed merely because it renders. Completion requires a source manifest, normalized scene data, top-down diagram, instance/asset inventory, collision/nav validation, spawn/objective validation, comparison captures, automated tests where possible, and a written deviation log.
