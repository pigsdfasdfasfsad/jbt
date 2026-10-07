# District reconstruction dossier

![District reconstruction outline](../assets/maps/district-outline.png)

![District supplied reference board](../assets/reference-boards/district-reference-board.jpg)

## Release identity

- **Release mode:** Regular, 15 waves
- **Environment / skybox:** Sunset
- **World location:** Region undocumented; occurs long after initial outbreak
- **Supported objective families:** Load, Repair, Radio, Unpack, Damage, Escort
- **Collectibles / easter-egg references:** Spiffo plush; watchtower, graffiti, burial site

## What the reconstruction must preserve

*Source: District.wiki*

- **Layout character:** a small overrun survivor settlement — a dilapidated town walled and barricaded
  to block off streets and hold a safe zone, overgrown by grass and nature, divided by a **road
  intersection at the centre**. Signs of struggle: overturned tables, broken barricades, scribbles on
  walls addressed to fellow survivors.
- **Landmarks:** a café used as a makeshift emergency building; a "Foxoil" gas station; a **movie
  theater with two theater rooms** (plus concession stand, hallway, restroom); a **book store whose
  second floor is a living quarter** (with bedroom); a convenience store with a back room and back door.
- **Infected spawns:** all directions; they enter through doorways and broken walls inside buildings,
  and jump over barricades and fences.
- **Map-specific mechanics / details:**
  - District takes place **long after the initial outbreak**.
  - The convenience store can be used as a controlled restock zone: build a trail of barbed wire and
    lead infected in circles by repeatedly entering the front entrance and exiting the back door.
  - The buildings surrounding the playable area are based on the town of **West Point** in *Project Zomboid*.
  - **Removed feature:** an unfinished plank on the side of the convenience store reading "Cindy" was
    removed permanently for unknown reasons (image `CindySign.png`). A red truck formerly stood inside
    the book store (`DistrictBookstore.png`).

## Engine scaffold

Each map is reconstructed as five independently verifiable layers:

1. **Static art** — terrain, structures, props, decals, signs, vegetation and background dressing.
2. **Collision + navigation** — player collision, infected barriers, pathfinding modifiers, traversal blockers and drop-offs.
3. **Gameplay anchors** — player starts, infected spawns, item boxes, objective anchors, fortification positions and helicopter/supply-drop support points where applicable.
4. **Environment** — sky, fog/atmosphere, color grading, lights, local/environment sounds and weather presentation.
5. **Map metadata** — map name, voting card/icon, objective support, mode restrictions and reconstruction validation data.

## Named locations visible in supplied reference material

- Barricade
- Barricade2
- Bedroom
- BookStoreInterior
- Cafe
- CafeInterior
- CafeKitchen
- ConcessionStand
- ConvenienceStore
- ConvenienceStoreBackDoor
- ConvenienceStoreBackRoom
- ConvenienceStoreInterior
- GasStation
- LivingQuarter
- TheaterHall
- TheaterRestroom
- TheaterRoom
- TheaterRoom2
- Ad
- Bookstore
- Memorial
- Messages
- Main Cafe

This list is a **reference-asset inventory**, not a claim that every map instance has already been extracted. The source-map conversion pass remains the authority for the final instance-by-instance inventory.

## Reference image files indexed (32)

- `Card-district.png`
- `District-Barricade.png`
- `District-Barricade2.png`
- `District-Bedroom.png`
- `District-BookStoreInterior.png`
- `District-Cafe.png`
- `District-CafeInterior.png`
- `District-CafeKitchen.png`
- `District-ConcessionStand.png`
- `District-ConvenienceStore.png`
- `District-ConvenienceStoreBackDoor.png`
- `District-ConvenienceStoreBackRoom.png`
- `District-ConvenienceStoreInterior.png`
- `District-GasStation.png`
- `District-LivingQuarter.png`
- `District-TheaterHall.png`
- `District-TheaterRestroom.png`
- `District-TheaterRoom.png`
- `District-TheaterRoom2.png`
- `DistrictAd.png`
- `DistrictBookstore.png`
- `DistrictDev2.png`
- `DistrictIcon.png`
- `DistrictMemorial.png`
- `DistrictMessages.png`
- `DistrictProgress.png`
- `DistrictProgress2.png`
- `DistrictProgress3.png`
- `District_Main_Cafe.png`
- `District_update.PNG`
- `Spiffo_on_District.PNG`
- `Vote-district.png`

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
