# Pass 49 — Original 3D Lobby Geometry and Authored Menu Cameras

**Source:** User-provided `TestPlace/TestPlace.rbxlx`, under `Workspace/Lobby`. This is a real serialized 3D lobby environment, not a guess based on a wiki picture.

## Source recovery

Exact owner source XML SHA-256:
`272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2`.

The source's original `Workspace/Lobby` contains **831 BaseParts**, including 8 invisible camera anchors and 5 invisible loadout anchors. The renderer imports the **818 remaining source geometry records**, preserving each record's original relative CFrame, size, color, material, alpha and model hierarchy name. It uses the original **Start** camera location as a local-space origin, avoiding the game's deeply negative Roblox lobby Y coordinates (~-2945 studs) without changing the relative layout.

| Genuine original serialized data | Count |
| --- | ---: |
| Lobby geometry records (not camera/loadout markers) | **818** |
| Geometry records actually visible | **774** |
| Native Part + WedgePart records | 300 |
| External MeshPart and UnionOperation records | 518 |
| Visible external-mesh records requiring size-preserving proxies | **505** |
| Camera positions/orientations | **8** |
| Source loadout position/orientation markers | **5** |
| Original PointLight and SpotLight emitters | **18** |

The original CFrame camera names are: Start, Shop, Loadout, Perks, Options, Gifts, Leaderboards and WeaponNode. Original loadout anchor names are Primary, Secondary, Melee, Utility and View. These are recovered from source data, not inferred from the reconstructed menu.

There are also original Roblox `SurfaceGui`, `TextLabel`, `ImageLabel`, `Decal` and other visual UI objects. **Those original 3D UI panels have not been translated into working Godot widgets**; instead the existing functional offline menu, armory, perks and map selection remain in place.

## Owner-private artifacts

`Content/Lobby/SourceLobby49.json.gz` (29,214 bytes), SHA-256:

`98a8bf71c0561be814b2de01831b48e413e349ed4925fab3c0b8a403d7845b49`.

`Content/Lobby/SOURCE_LOBBY_MANIFEST49.json` records exact geometry class counts, visible/missing-mesh counts, original camera and loadout names, source anchor and source identity.

Neither the original TestPlace RBXLX nor either derived private lobby file enters public GitHub history or a public Windows CI artifact.

## In-game behavior

Pass49 adds `Pass49OriginalLobbyRuntime` to the existing Godot .NET Windows game:

- On boot, if the private pack is present and its original XML **and exact packed-data SHA-256** match, the reconstructed source-geometry lobby renders behind the existing 2D menu.
- The original authored **Start** camera is used for map selection. The original **Loadout** camera activates in the 91-weapon armory, and the original **Perks** camera activates when opening the existing perk UI.
- Existing controls remain actionable: ten maps, armory purchases, credit/progression/loadout handling, perks and local 15-wave start all stay functional.
- The flat black UI background becomes a dark semitransparent tint **only when the verified source lobby is loaded**, allowing 3D to be visible without making menu text unreadable. If the private file is missing, invalid or stale, the previous fully opaque menu remains usable.
- All original lobby geometry and cameras are freed when entering a game map. No source lobby geometry, lighting or collision is introduced into zombie matches.

All 774 visible parts are instanced as grouped Godot `MultiMeshInstance3D` draw groups. Native bricks, balls, cylinders, source wedges and primitive SpecialMesh shapes are preserved when practical. Original remote MeshPart/UnionOperation polygon triangles are **not embedded** in the RBXLX and remain explicitly sized/rotated bounding geometry. These are not claimed to be original surface meshes. The original light fixture positions/colors/intensities are used; a small approximated directional ambient fill compensates for missing original skybox and postprocessing.

## Native Windows validation

The exported Windows acceptance test uses a completely fabricated source-styled 8-part lobby, with a native sphere, wedge, missing-MeshPart and CSG proxy, one invisible helper, eight camera CFrames, five loadout anchors and two lights. It checks that the actual compiled Godot runtime:

- Loads original-style geometry only under an explicit synthetic smoke switch
- Rejects inappropriate original source-data SHA claims or oversize data
- Places the Start source camera at the rebased local origin
- Switches to Loadout during the actual functional **91-item armory**
- Returns to Start camera on map selection
- Removes the 3D backdrop on entering a 15-wave game without affecting the original game flow
- Deletes every synthetic private sidecar before uploading the public compiled executable

A native synthetic test **does not** establish exact original screenshot, illumination or 3D mesh parity on the owner's private scene; actual graphical Windows testing is needed.

## Local reproduction

```powershell
python tools/maps/extract_original_lobby49.py --archive TestPlace.zip --out Content/Lobby/SourceLobby49.json.gz
```

This reads inert XML only and does not use Roblox Studio or an online asset server. It emits a manifest with exact SHA-256 provenance. The generator enforces the original file SHA and original 818/8/5/18 shape.

## Remaining 1:1 gaps

The original UI `SurfaceGui` widgets, external MeshPart/UnionOperation geometry and UV/textures, original skybox/postprocessing, 9 remaining full 3D world maps, original animations/audio and a **human-played full 15-wave acceptance run** remain incomplete. This pass makes one real previously missing part of the original world available offline, not the whole game.
