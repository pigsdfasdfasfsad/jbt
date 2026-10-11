# Pass 48 — Offline infected source accessory kit

This pass restores the supplied original Roblox infected-asset components to the standalone Godot game. It does **not** recover the retail zombie model files, complete original R6 outfits, or animation tracks.

## Source evidence

The owner-held TestPlace.zip contains `TestPlace/TestPlace.rbxlx` with the original `ReplicatedStorage/Assets/AI/Infected` asset library. Original-place XML SHA-256: `272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2`.

The source includes Generic head/eye/hair models, Military/Riot helmet pieces, Hazmat respirator and hood variations, and the original Bloater/Burster head, torso and arm attachments. Other original asset groups, such as Juggernaut, exist but **are not** claimed as implemented playable enemy types.

The complete active-world zombie rig and its original clothing combinations do not exist as recoverable serialized rigs in this place. Pass48 explicitly reconstructs a six-part standard R6 body as a proxy and mounts real authored accessory part transforms relative to original source Handle/Head anchors.

## Exact owner-pack inventory

| Metric | Count |
| --- | ---: |
| Playable infected types receiving the original source kit | **8/8** |
| Reconstructed source-and-proxy cosmetic combinations | **15** |
| Distinct original AI accessory/part groups used | **22** |
| Distinct visible source-authored accessory components | **35** |
| Distinct external mesh references not present as embedded triangles | **24** |
| Reconstructed standard R6 body proxies in each combination | **6** |
| Total records across combinations, including repeated base proxies | **141** |

Variants per type: Civilian 4, Sprinter 2, Bolter 1, Military 1, Riot 1, Hazmat 4, Bloater 1, Burster 1.

These 15 combinations are **reconstructed from source-available pieces**, not verified retail random-variant selection logic. The source accessory geometry preserves the original local rotations, sizes, colors, opacity and remote asset IDs, but missing MeshPart/CSG triangle bytes still use bounded Godot proxies. The original cloud textures/UVs and skeletal animation curves remain missing.

Owner-only derived asset: `Content/Enemies/InfectedSourceVariants.json.gz`, SHA-256 `9ef29ce243992722dbab0ea5f5f78872ea4a6b5da4718c925662a34cfb743d64`.

## Runtime

`InfectedSourceModelRuntime` now validates the exact source XML SHA and compressed asset-pack SHA, refuses untrusted or oversized decompressed assets, and accepts entirely synthetic packs only under explicit smoke-test flags. If the pack is absent or fails validation, the previous standalone procedural infected characters remain available.

It builds the six reconstructed R6 parts with articulated arm and leg pivots, then adds original source accessory components to the matching limbs, head or torso. The existing attack, gait and damage logic continues unchanged. Camera, infected collisions, match stages, gameplay, weapons and progression do not depend on the asset kit.

F10 and F11 report how many living infected use the asset-kit pipeline, how many have a verified owner pack, the number of authentic source accessory parts versus reconstructed R6 body parts, and the number of external mesh references falling back to proxies.

## Reproduce privately

From repository root, with the original user-provided place archive:

```powershell
python tools/maps/extract_original_infected_assets48.py --archive TestPlace.zip --out Content/Enemies/InfectedSourceVariants.json.gz
```

The extractor streams the original XML as inert data. It generates the owner-only pack and `SOURCE_INFECTED_ASSET_KIT_MANIFEST48.json`. No Roblox installation, Roblox servers, internet, or execution of original scripts is needed.

Neither the original XML nor the derived kit is committed to public source control. The public GitHub Actions build uses a completely fabricated 15-variant eight-type pack, then removes it before publishing the Windows executable.

## Native acceptance and remaining work

The Windows Godot integration smoke requires all eight synthetic infected types, six explicit R6 proxies, two simulated source attachments, one deliberately unavailable external-mesh proxy per actor, and one real `InfectedAgent` containing the visual assembly. The prior weapon/map collision/navigation/audio smoke tests continue to run. The original private kit is separately checked against the known source and pack checksums.

**Not completed:** full original infected rigs and clothing, external mesh triangles, original animations, genuine 15-wave player-driven playtest, most remaining map geometry and terrain, and retail screenshot/audio parity.
