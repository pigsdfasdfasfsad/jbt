# Pass 41 — Original Workspace.Lobby recovered and integrated into Pass40

This branch starts from the **fully tested Pass40 latest game** (commit 928c4d13582e2b549e421423128f2733d48ae67a), NOT the older Pass35 fork where the original 3D Lobby was first prototyped.

## Owner-original recovery

Source: original, private `TestPlace/TestPlace.rbxlx`; SHA-256 `272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2`.

Recoverable `Workspace.Lobby` scene records:
- **817 geometry records** at source CFrame position/rotation/size/material/color/transparency (297 Parts, 2 WedgeParts, 303 MeshParts, 215 UnionOperations).
- **8 source menu camera poses**: Start, Shop, WeaponNode, Loadout, Perks, Options, Leaderboards, Gifts.
- **18 source light emitter records**, at most 12 active locally.
- **518 custom MeshPart/CSG mesh vertex payloads are absent**. Their placeholder meshes are not authentic original geometry. Original lobby textures and UI sprites are not claimed as reconstructed.
- Spatial scene is recentered to the original `Workspace.Lobby` coordinate anchor for a separate Godot menu stage without altering gameplay source maps.

Private deterministic scene file: `Content/Lobby/OriginalLobby.lobby36.jsonl.gz`, exact pack SHA-256 `484ca68c8113b7e41a3024b570f9480474f13cbb56b576ea2a6c5a827c1b36c5`.

## Functional UI integration

The app retains the same click-operable UI, store, weapon purchases, equip and progression logic:
- **Main menu**: source 3D Lobby with original Start camera, flat menu if missing sidecar.
- **Armory/Shop**: source Shop camera; a button switches to Loadout camera without changing active UI functionality.
- **Perks**: source Perks camera.
- **Gameplay**: on entry, source Lobby camera is disabled, Lobby meshes hidden and Lobby WorldEnvironment set to null so world source skybox/fog and first-person camera remain authoritative. On return, source Lobby returns.
- No private code/scripts run at runtime; the Lobby pack is compressed data with strict source SHA and size/record bounds.

This is a **Pass41 integration into Pass40**; Pass36/37 source map fragments, lighting effects, Pass38 positioned soundscape, Pass39 original Lab floor plans and Pass40 grounded Lab navigation code remain compiled into the app. Those features still require their matching owner-private sidecar files beside the EXE to activate; compiling code alone does not imply those private sidecars are installed.

## Testing and distribution

The public Windows Actions job only injects a **fabricated** 16-object/eight-camera Lobby source package, executes the real exported Windows Godot/C# binary with `--smoke-pass41-lobby` and checks `TWR_SMOKE_PASS36_LOBBY_OK`. It tests all four camera destinations and deactivating/reactivating the Lobby around gameplay; it removes the synthetic pack before publishing the executable. Original source geometry is not committed to the repository, and public executable artifacts exclude original Lobby packs.

The final owner-private portable ZIP adds the real 817-record Lobby separately, plus existing owner-original data where verified. Source loader failures fall back to the previous flat menu and do not stop launch.

## Not verified or completed

No actual graphical side-by-side screenshot test of the full Lobby on a Windows GPU; the camera & transformed meshes are verified via source geometry and synthetic native smoke. 518 original meshes/CSGs and many surface textures are still unavailable. Original map exterior zombie routes, original terrain and missing MeshParts, exact original UI look/animations, ten-map full fidelity and real 15-wave human playtesting remain outstanding.
