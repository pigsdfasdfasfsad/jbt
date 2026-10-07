# Ranch reconstruction dossier

![Ranch reconstruction outline](../assets/maps/ranch-outline.png)

![Ranch supplied reference board](../assets/reference-boards/ranch-reference-board.jpg)

## Release identity

- **Release mode:** Regular, 15 waves
- **Environment / skybox:** Sunrise
- **World location:** Oakwood County, Texas — Fortson Ranch
- **Supported objective families:** Load, Repair, Radio, Unpack, Escort
- **Collectibles / easter-egg references:** Spiffo plush; sheriff-cruiser police chatter

## What the reconstruction must preserve

*Source: Ranch.wiki*

- **Layout character:** a large, expansive field with yellowish grass and a few scattered trees;
  wooden fences around the edges and perimeter; an entrance gate at the front next to a road.
- **Landmarks:** house, barn, stable, storehouse; a large house with several bedrooms, a kitchen,
  an office and a separate garage where supplies can be found; a **gazebo** and a **greenhouse**
  behind the house; a sweeping field of hay bales beyond the southern border; multiple vehicles
  including police cruisers blocking the entrance. Gallery also documents porch, dining table,
  living room, hallway (laundry room, bathroom, other bedrooms), storage closet, master bedroom,
  and the *Fortson Ranch* emblem (`Lgoo.png`).
- **Infected spawns:** all directions of the map — by the ranch entrance, hopping over fences and
  trampling over the hay bales.
- **Map-specific mechanics / details:**
  - **Police chattering can be heard from a radio in a sheriff cruiser at the ranch entrance.**
  - Turtling: second floor of the barn (single entry point; barbed wire on the stairs; a 50 Cal on
    the front top of the stairs). Not recommended on Classic/Hardcore — Juggernaut Infected can
    overrun it with no escape.
  - Ranch's design was inspired by an old ranch that belonged to the development team's grandparents
    (image `GrandparentsRanch.png`).

## Engine scaffold

Each map is reconstructed as five independently verifiable layers:

1. **Static art** — terrain, structures, props, decals, signs, vegetation and background dressing.
2. **Collision + navigation** — player collision, infected barriers, pathfinding modifiers, traversal blockers and drop-offs.
3. **Gameplay anchors** — player starts, infected spawns, item boxes, objective anchors, fortification positions and helicopter/supply-drop support points where applicable.
4. **Environment** — sky, fog/atmosphere, color grading, lights, local/environment sounds and weather presentation.
5. **Map metadata** — map name, voting card/icon, objective support, mode restrictions and reconstruction validation data.

## Named locations visible in supplied reference material

- Carnival Branchys
- Carnival Branchys2
- Carnival BranchysInterior
- Carnival BranchysInterior2
- Carnival BranchysKitchen
- GrandparentsRanch
- BarnInterior
- DiningTable
- Garage
- Hallway
- LivingRoom
- MasterBedroom
- StablesInterior
- StorageCloset
- StorehouseInterior
- 1
- 2
- 3
- 4
- 6
- Barn
- Gazebo
- Office
- Porch
- Stable
- View1

This list is a **reference-asset inventory**, not a claim that every map instance has already been extracted. The source-map conversion pass remains the authority for the final instance-by-instance inventory.

## Reference image files indexed (38)

- `Card-ranch.png`
- `Carnival-Branchys.png`
- `Carnival-Branchys2.png`
- `Carnival-BranchysInterior.png`
- `Carnival-BranchysInterior2.png`
- `Carnival-BranchysKitchen.png`
- `EarlyRanch.png`
- `GrandparentsRanch.png`
- `OldRanchGameplay.png`
- `OldRanchHeli.png`
- `Ranch-BarnInterior.png`
- `Ranch-DiningTable.png`
- `Ranch-Garage.png`
- `Ranch-Hallway.png`
- `Ranch-LivingRoom.png`
- `Ranch-MasterBedroom.png`
- `Ranch-StablesInterior.png`
- `Ranch-StorageCloset.png`
- `Ranch-StorehouseInterior.png`
- `Ranch1.png`
- `Ranch2.png`
- `Ranch3.png`
- `Ranch4.png`
- `Ranch6.png`
- `RanchBarn.png`
- `RanchEmblem.png`
- `RanchGazebo.png`
- `RanchIcon.png`
- `RanchLivingRoom.png`
- `RanchOffice.png`
- `RanchPorch.png`
- `RanchPreview.jpg`
- `RanchStable.png`
- `RanchThumb2.png`
- `RanchThumbnail.png`
- `RanchView1.png`
- `Spiffo_Ranchv2.png`
- `Vote-ranch.png`

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
