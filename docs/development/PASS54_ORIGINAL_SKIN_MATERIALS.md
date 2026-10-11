# Pass 54 - Original Skin Material and Per-Part Tag Reconstruction

## Baseline and intended result

Continues native-verified Pass53. Uses supplied owner files to recover source-authored cosmetic colors and material classes, then applies them to the exact original source weapon part slots when both optional owner packs are installed. This does not claim exact Roblox shaders, full texture images, original MeshPart triangles, or animated skin effects.

## Owner evidence and extracted facts

- scripts.zip / scripts/ModuleScript.Skins.Source.txt: SHA256 aeff32781cc9eafdbbd5ae7bc12ff157cc32934e7dae6773c87d7bfba355a015
- TestPlace.zip / TestPlace/TestPlace.rbxlx: SHA256 272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2
- **118** original skin definitions, exactly five Value1–Value5 channels = **590** RGB/material slots
- Original texture/surface-appearance **references** are present, but not equivalent to recovered texture/PBR bytes; unavailable texture references are explicitly flagged
- Original Tools hierarchy: **98 models / 1,484 parts**; **728** original parts have a Value1–Value5 marker across **73** weapon models; duplicate identical tags on two M110 SASS parts collapse safely
- Authored values: Low/Cocoa Value1 RGB (40,17,17), Value2 (61,26,26); High/Gold Value1 (188,155,93) Roblox Marble material; Ethereal/Spirit white ForceField; Godot material shading remains approximate

## Changes implemented

1. The SHA-bound extractor at tools/skins/recover_private_source_skins54.py parses inert bytes only (never executes Luau), verifies both uploaded source SHA256s, validates 118/590 and 98/1484/728 counts, and emits two bounded owner-private JSON gzip packs without overwriting existing packs.
2. Pass54SourceSkinMaterials reads optional Content/Skins/SourceSkinMaterials54.json.gz and SourceWeaponSkinSlots54.json.gz, validates pinned expected compressed SHA256s for owner packs, guards decompression and all RGB, material, skin-name, weapon-index and Value slot fields. Only --smoke-pass54 permits synthetic fixture packs.
3. OriginalWeaponSourceRuntime adds source-skin metadata to each exactly tagged tool part by the original source part index. Untagged source parts retain their underlying original materials.
4. WeaponViewModelRuntime applies five source RGB/material channels and approximates neon, metal, glass, ForceField and reflectance with render-only Godot material shaders. Missing source textures/variants are reported. Weapon damage, ammo, physics and player saves are unaffected.
5. With no palette pack installed, Pass53 hashed-color fallbacks remain explicitly approximate. A palette with no source tagged geometry uses recovered colors with approximate slot placement. The showroom status discloses authenticity and texture gaps.
6. A native exported Windows smoke uses fake source palettes, fake weapon slot tags and synthetic lobby/models; checks Value1/Value2 RGB accuracy, material changes on legitimate cosmetic selection, clearing on reset, economy invariants and match lifecycle.

## Owner-only local installation

Generate the sidecars locally, with lxml installed and both source ZIP archives available:

    python tools/skins/recover_private_source_skins54.py --skins-archive scripts.zip --place-archive TestPlace.zip --out build/output/Content/Skins

Install the compressed packs beside the offline EXE under Content/Skins; do not commit raw or extracted owner files into public GitHub.

| Owner-only sidecar | Expected SHA256 |
| --- | --- |
| SourceSkinMaterials54.json.gz | e9881057cd8bbe9c16c9c3e567de9ccba1fe94250f4db0d55ef5c61b6871b023 |
| SourceWeaponSkinSlots54.json.gz | f38d7e6275db2689f8244af7abf0e06ac8690fd6da5a1d13edc887b05cb24afe |

## Verification requirements

The branch twr-pass54-original-skin-materials builds/exports the native Windows game after the Python regression suite and executes --smoke-pass54 with fabricated source packs and an isolated test-only profile. Native success marker: TWR_SMOKE_PASS54_SOURCE_SKINS_OK. All synthetic packs must be cleaned before the source-asset exclusion audit. A passing native Windows job is required before calling Pass54 verified.

## Outstanding fidelity

Original 3D MeshPart triangles, accurately authored Roblox diffuse/normal/alpha textures, animated Ethereal finishes, exact shader parity, original skin drop rates, full map interior detail, manually played 15-wave matches, and a privately distributed installed Windows EXE are not yet complete.
