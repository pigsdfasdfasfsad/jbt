# Pass 55 - Original Skin Texture References and Reconstructed Offline Effects

## Baseline

Continues the Windows-native-verified Pass54 branch. This pass adds a source-grounded **skin texture reference and UV metadata layer** and a modest reconstructed shimmer for selected emissive materials. It does not claim that the original Roblox texture bytes or animation code have been recovered.

## Original source findings

The supplied owner archive at scripts.zip / scripts/ModuleScript.Skins.Source.txt has SHA256 aeff32781cc9eafdbbd5ae7bc12ff157cc32934e7dae6773c87d7bfba355a015.

- **118** original skin definitions with **118 image icon references**.
- **94** authored Texture/Texture1 layers, each with source texture numeric asset ID, optional Color3 tint, transparency, StudsPerTileU/V, OffsetStudsU/V and ZIndex values.
- **33** MeshPart TextureID assignments.
- Original image/normal-map bytes and UV projection onto all six Roblox Part faces have not been recovered in a directly usable format from the files used here. The image reference ID is NOT a texture image.
- Neither this source skin module nor the recovered SkinApply.lua module specifies a universal UV scroll speed or emission pulse period. Animated shimmer timing in Pass55 is **reconstructed**, not source-authored.

## Code implemented

1. **Deterministic offline extractor** at tools/skins/recover_source_skin_layers55.py validates the original SHA, parses inert Luau-style literals using strict bounded patterns (no eval/execution), enumerates 118 skins and 94 + 33 source texture references, then emits one deterministic gzip sidecar. It never downloads images and refuses to overwrite any file.
2. **Private pack loader** at Pass55SourceSkinLayers.cs reads the optional SourceSkinLayers55.json.gz, checks exact SHA-256 c6af38ce98042dc47f928b73720d5f7035b3f1d278936695855fe025d406bcdd and 118/94/33 owner schema, bounds compressed/expanded JSON sizes, validates material slot indices and numeric asset IDs, and rejects synthetic fixture packs outside --smoke-pass55. Owner-private bytes are intentionally not checked into Git.
3. **Texture renderer** at Pass55SkinTextureRenderer.cs loads matching source images **only when a local Content/Assets/Textures/<numeric-id>.png file exists**, via existing OfflineAssetResolver. When the original referenced image is not installed it creates a deterministic, explicitly-labeled small procedural texture proxy. It retains source tint, transparency and tiling/offset literals, with a disclaimer that exact Roblox world-face UV projection and stacking of multiple ZIndex layers are only approximated on Godot meshes.
4. **Cosmetic animation** is a bounded, deterministic emission pulse and slow UV motion for source-class Neon/ForceField materials. The original source identifies those classes but no source animation timing; UI provenance explicitly reports "RECONSTRUCTED SHIMMER (TIMING UNKNOWN)" instead of implying recovered movement. Effects are render-only and do not change recoil, ballistics, damage, ammo, credits, or inventory.
5. **3D showroom and first-person rendering** use the same skin/material path. The lobby's model processes only cosmetic animation, suppressing weapon recoil and reload motion. Any cosmetic skin change/reset clears its previous texture overlays and associated animation list.
6. **Native synthetic-only Windows acceptance** installs a fake original-style lobby, fake tool part tags, fake Pass54 palette and Pass55 layer metadata, and an intentionally synthetic local PNG (ID 5555500000001). The test validates layer ID, both available PNG and missing-image procedural fallback, authored tiling/offset, reconstructed material pulse, no economy change, and correct lobby cleanup on entering gameplay. Synthetic assets are removed before the privacy audit.

## Owner-only installation

A combined private ZIP is available in the conversation: TWR_Pass55_Private_Skin_Materials_and_Layers.zip. Merge its Content directory into the **same directory as the local offline EXE**. It provides:

- Content/Skins/SourceSkinMaterials54.json.gz: original 118 skin RGB/material definitions
- Content/Skins/SourceWeaponSkinSlots54.json.gz: original 728 part skin-slot assignments
- Content/Skins/SourceSkinLayers55.json.gz: original skin icons and 94 + 33 texture references/UV values

The package does **not** contain original texture image bytes. To install separately obtained, authorized local texture images, name each PNG with its original numeric Roblox texture asset ID in Content/Assets/Textures/. There is no internet requirement or Roblox dependency once images are present.

To generate the Pass55 metadata yourself from the supplied scripts archive:

    python tools/skins/recover_source_skin_layers55.py --scripts-archive scripts.zip --output build/output/Content/Skins/SourceSkinLayers55.json.gz

## Acceptance and outstanding gaps

Windows CI runs on twr-pass55-source-skin-textures-and-shimmer and requires 100% Python regression success, a zero-error Godot/.NET Windows export, native TWR_SMOKE_PASS55_LAYERS_OK with synthetic packs, and public/private source-asset leak checks. A green native marker is required before declaring the pass verified.

Still unresolved: authentic diffuse/normal/alpha image bytes for many ID references; fully faithful Roblox six-face overlay UV projection and multi-layer compositing; actual original animation controller/timing; MeshPart triangles, complete 10-map 3D fidelity, manual full 15-wave sessions, and an installable private final game build.
