# Prison reconstruction dossier

![Prison reconstruction outline](../assets/maps/prison-outline.png)

![Prison supplied reference board](../assets/reference-boards/prison-reference-board.jpg)

## Release identity

- **Release mode:** Regular, 15 waves
- **Environment / skybox:** Sunrise
- **World location:** Iowa, USA — Iowa State Penitentiary
- **Supported objective families:** Load, Repair, Radio, Unpack, Damage, Escort
- **Collectibles / easter-egg references:** Spiffo plush; 4 documents

## What the reconstruction must preserve

*Source: Prison.wiki*

- **Layout character:** a penitentiary surrounded by trees and wired fences, with a front gate,
  a central cell-block area and an outer service area.
- **Landmarks:** front office (access into the prison through several rooms and a metal detector);
  **Cell Block A** (no furniture or barricades inside) and **Cell Block B** (two makeshift walls in
  front of the two entrances, tables on the bottom floor, plus couches and tables) — both have
  multiple sets of stairs to second and third floors, with several cells opened and holding item
  spawns; a courtyard with a basketball court and exercise equipment; a large sealed building called
  **"Isolation"** whose doors read "Keep Out" and "Do Not Open"; a mess hall with connected kitchen
  (also reachable from the parking lot), laundry room, infirmary, showers and a chapel; two guard
  towers near the mess hall with stairs to a **three-way bridge, part of which is broken**, preventing
  access to the other side; a parking lot with police cruisers and a warehouse with large generators.
  Gallery adds: reception room, offices, warden's office, office room, locker room, break room,
  visitation room, armory, entrance security room, generator room and storage room.
- **Infected spawns:** all directions beyond the prison's fences, **and on top of the roof of the
  isolation cell block inside the prison**.
- **Map-specific mechanics / details:**
  - Turtling: showers, infirmary and laundry rooms around the mess hall (single chokepoint each; ammo
    and barbed wire can spawn inside); the yard bridges can be fortified, and the broken end doubles as
    a jump-down escape route — but **the bridge has no item spawns**, and Bloaters and Juggernaut
    Infected can still target players at ground level.
  - Prison carries **four readable documents** — the most of any map. See §7.3.

## Engine scaffold

Each map is reconstructed as five independently verifiable layers:

1. **Static art** — terrain, structures, props, decals, signs, vegetation and background dressing.
2. **Collision + navigation** — player collision, infected barriers, pathfinding modifiers, traversal blockers and drop-offs.
3. **Gameplay anchors** — player starts, infected spawns, item boxes, objective anchors, fortification positions and helicopter/supply-drop support points where applicable.
4. **Environment** — sky, fog/atmosphere, color grading, lights, local/environment sounds and weather presentation.
5. **Map metadata** — map name, voting card/icon, objective support, mode restrictions and reconstruction validation data.

## Named locations visible in supplied reference material

- AEntrance
- AInterior
- Armory
- BEntrance
- BInterior
- BreakRoom
- BrokenBridge
- Chapel
- GenRoom
- GenStorageFacility
- Infirmary
- Isolation
- Isolation2
- Kitchen
- LaundryRoom
- LockerRoom
- MessHall
- MetalDetectors
- OfficeRoom
- Offices
- Overview2
- ReceptionRoom
- Showers
- StorageRoom
- VisitationRoom
- WardenOffice
- Yard
- 1
- 3

This list is a **reference-asset inventory**, not a claim that every map instance has already been extracted. The source-map conversion pass remains the authority for the final instance-by-instance inventory.

## Reference image files indexed (40)

- `Card-prison.png`
- `Prison-AEntrance.png`
- `Prison-AInterior.png`
- `Prison-Armory.png`
- `Prison-BEntrance.png`
- `Prison-BInterior.png`
- `Prison-BreakRoom.png`
- `Prison-BrokenBridge.png`
- `Prison-Chapel.png`
- `Prison-GenRoom.png`
- `Prison-GenStorageFacility.png`
- `Prison-Infirmary.png`
- `Prison-Isolation.png`
- `Prison-Isolation2.png`
- `Prison-Kitchen.png`
- `Prison-LaundryRoom.png`
- `Prison-LockerRoom.png`
- `Prison-MessHall.png`
- `Prison-MetalDetectors.png`
- `Prison-OfficeRoom.png`
- `Prison-Offices.png`
- `Prison-Overview2.png`
- `Prison-ReceptionRoom.png`
- `Prison-Showers.png`
- `Prison-StorageRoom.png`
- `Prison-VisitationRoom.png`
- `Prison-WardenOffice.png`
- `Prison-Yard.png`
- `Prison1.png`
- `Prison3.png`
- `PrisonDev6.png`
- `PrisonIcon.png`
- `PrisonPreview1.png`
- `Prison_Note_1.png`
- `Prison_Note_2.png`
- `Prison_Note_3.PNG`
- `Prison_Note_4.PNG`
- `Prison_update.png`
- `Spiffo_Prisonv2.png`
- `Vote-prison.png`

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
