# Pass 51 - Source-Anchored 3D Loadout Preview

## Baseline

Pass 51 extends the successfully exported and tested Pass 50 original 3D lobby branch. It does not replace the 91-weapon armory, modify progression, or change enemy/wave simulation.

## Implemented

- Adds a separate `Pass51OriginalLoadoutDisplayRuntime` beneath the Pass 49 source-authenticated lobby.
- Reads the **five recovered original loadout CFrames**: Primary, Secondary, Melee, Utility, and View. The CFrames already exist in the owner-verified `SourceLobby49.json.gz` file; no new original XML package is introduced.
- Equips 3D display models at the authored locations for the current saved loadout. The independent View anchor displays a selected weapon on **armory-row hover**. The active 91-weapon catalog and purchase/equip command path are unchanged.
- Renders a prepared locally installed model if available, otherwise an owner-verified private source tool assembly (with visibly labeled missing-mesh proxy parts), otherwise an explicitly **approximate** mesh.
- Labels the model's provenance in the armory: source assembly, synthetic fixture (testing only), locally prepared model, or approximate fallback. No unknown polygon data is described as authentic.
- Hides the display whenever the player returns to the map menu, opens perks, or opens the Pass50 bulletin board. All 3D lobby nodes are freed on entering gameplay.
- Adds a native Windows smoke gate exercising CFrame placement, 91 armory rows, 3D hover previews, fallback labeling, unchanged credits/ownership/saved loadout, Start/Leaderboards navigation and removal on starting a Manor match.

## Safe evidence policy

No original owner Roblox XML, 3D lobby geometry pack, bulletin snapshot, original weapon model bytes, screenshot, Windows executable or rebuilt asset binaries are committed in this pass. CI uses synthetic reference fixtures, validates them inside the real exported Godot Windows application, and cleans them before finishing. The previous public executable upload steps are explicitly disabled; any Windows distribution with owner source packs belongs in a separate private channel.

## Source and authenticity limitations

The five source marker **positions and orientation** are authenticated from the original saved place. The 3D loadout presentation and preview behavior are reconstructed: scale, showcase offset, pose, lighting, weapon pose/animation and user interface layout have **not** been compared against original TWR screenshots. External MeshPart and UnionOperation triangle data remain unavailable in the published project. Some catalog weapons have no original model part pack and use approximate display shapes.

The smoke gate establishes automated interface behavior, not pixel-perfect visual fidelity or completion of fifteen player-controlled waves.

## Validation

```text
python tools/validation/run_all.py
```

Run Windows Actions on `twr-pass51-original-loadout-preview`, and require the native marker `TWR_SMOKE_PASS51_LOADOUT_OK`. A green marker is required for a verified Phase 51 claim. 