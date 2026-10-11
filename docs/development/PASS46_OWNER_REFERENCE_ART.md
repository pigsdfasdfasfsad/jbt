# Pass 46 — Owner-Supplied Offline Reference Art

**Scope:** 2D owner-supplied image integration in the menu, armory and HUD, not the original Roblox UI texture binaries or 3D geometry.

## What was actually added

The Pass 45 ZIP had an offline art loader but no Content/Art folder, leaving the weapon dial and map cards dependent on placeholder visuals. Pass 46 uses the user's supplied **images.zip** archive and the already-recovered 91-weapon catalog to add:

- **10 map screenshot cards** from RanchIcon, MillIcon, BypassIcon, CabinIcon, CargoIcon, DistrictIcon, ExpresswayIcon, PrisonIcon, LaboratoryIcon and ManorIcon images, resized/cropped to 512x288
- **88 matched weapon reference silhouettes**, resized to 256x144, brightened for legibility while preserving the original image shape/alpha
- **3 deliberate fallbacks:** Ruger 10-22, Sawn Off Shotgun and Spiked Baseball Bat have no suitable separate source silhouettes; do not substitute another weapon's image
- A functional armory row decoration that does NOT replace purchase buttons or affect unlock/credit mechanics
- Source 2D weapon reference imagery in the existing live-ammunition weapon HUD, retaining actual ammo/XP/health counts

Static -UI.png screenshots with baked ammunition counts were excluded to avoid contradictory display values.

The owner image archive SHA-256 is **b07b058367d115b9710feb25accf0ac5cc8a1de74b4b1769a890ff0c457115f0**. The source pack is under Content/Art/MapCards, Content/Art/WeaponIcons and Content/Art/visuals.json. Each PNG is SHA-256 verified at runtime. The owner-private SOURCE_IMAGE_PROVENANCE46.json identifies each input picture and its derived output.

## Offline reproduction

Run from the repository root with the user-owned source images.zip and the checked-in weapon catalog:

    python tools/validation/build_pass46_source_art.py --images images.zip --catalog content/weapons/catalog.json --output Content/Art

Use an empty output folder. The generator refuses to overwrite and never downloads remote content. Output is an owner-only resource pack, not for committing to public GitHub history.

## Native Windows verification

Public CI generates artificial **2x2 PNG fixtures** (never any user image), exports the actual Godot/.NET Windows executable and exercises:

1. Manifest/hash validation and all ten map-button images
2. All 91 functional armory rows, with three fabricated source image references
3. Absence of art on unavailable references without corrupting button status
4. Transparent mouse input on art so it cannot intercept purchase clicks
5. Source weapon image loading in the existing live-ammunition HUD
6. Cleanup and explicit exclusion of all art from the public Windows artifact

This smoke is not a retail TWR appearance comparison or a human-played fifteen-wave match.

## How to install

Extract the complete owner-only Pass46 ZIP on Windows and run ThoseWhoRemainOffline.exe. The ten map selection cards and weapon armory reference silhouettes are available without internet or Roblox. The separate Pass46 Source Reference Art Pack can be overlaid into an already extracted Pass46 folder.

The images are resized source references; they do NOT reconstruct missing original Roblox custom mesh triangles, animations, first-person 3D gun models, original audio, or the nine remaining complete 3D map scenes. These are still explicit incomplete requirements.
