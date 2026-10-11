# Pass 56 - Offline Wiki Weapon/Skin Visual Reference Gallery

## Baseline and finding

Continues native-verified Pass 55. The user-provided images.zip and image-docs.zip are large collections of **wiki imagery**, including weapon renders/screenshots, UI thumbnails, skin comparisons, and map illustrations. Their rendered weapon/skin screenshots are useful for visual quality assurance, but **they are not the original Roblox MeshPart TextureID diffuse/normal/UV image byte resources**. Pass56 therefore adds a separate, correctly labelled visual-reference gallery rather than stretching screenshot thumbnails across gun meshes.

Owner archives are SHA256-bound:
- images.zip: b07b058367d115b9710feb25accf0ac5cc8a1de74b4b1769a890ff0c457115f0
- image-docs.zip: e2ebf80af7b6591af0de885a9cee9e863da398407fdc741ac3f215077936984f

## Verified catalog coverage

The deterministic extractor at tools/assets/recover_wiki_reference_images56.py cross-checks filenames against the repository's 91 normalized weapon names and 118 known source skin IDs, using verified old-wiki suffix abbreviations and subject labels from image-docs.zip. The private output has:

| Metric | Grounded count |
|---|---:|
| Offline weapon catalog entries | 91 |
| Weapons with source wiki reference images | 91 |
| Default weapon images | 90 |
| Weapon/skin exact-combination screenshot references | 1,563 |
| Weapon pose/UI reference images | 270 |
| Recognized source skin reference icons | 110 |
| Unique bundled wiki image files | 2,033 |

One weapon does not have a specifically labelled Default image; not all 118 skins have wiki icons. Cases with missing or ambiguous icons/screenshots are explicitly omitted instead of inferred.

The 2,033 distinct image files are stored privately in **SourceWikiReferenceImages56.zip**, ~19.2 MB, accompanied by a JSON manifest of exact source ID associations, per-image SHA256, dimensions, content length and source wiki subject. Expected reproducible compressed archive SHA256:

c06abbaefcaa33430a5a37ddb10fe7540fb27ac39c863fd05c420cad0d2cd2c1

The archive and historical wiki PNG/JPG bytes are NOT committed to the public GitHub repository.

## Runtime implementation

- Pass56WikiVisualReferences loads an optional Content/References/SourceWikiReferenceImages56.zip adjacent to the offline Windows executable or working directory. It checks **the entire pinned owner ZIP SHA256**, both source archive SHA256s in its manifest, expected 91/2,033/110 catalog counts, bounded ZIP and JSON sizes, constrained image paths, per-image byte counts/SHA256, and original width/height. It caches at most eight decoded PNG/JPG textures.
- The 2D viewer opens from both **ARMORY / LOADOUT → WIKI IMAGE** and **SKIN INVENTORY → WIKI REFERENCE IMAGE**. A screenshot matching the selected weapon and skin is displayed if the precise documented pair exists. Otherwise it falls back to that weapon's Default image with a notice that this is **not** the exact skin comparison. The interface displays its provenance and never treats it as an original material/UV texture.
- The gallery hides and restores the originating Armory or Inventory UI, includes a working BACK button, and is destroyed when a game map is entered. It never mutates credits, saved loadout, case ownership, damage, ammo, or game flow.
- Missing owner ZIP or verification failure is nonfatal: the UI remains usable with a conspicuous missing-reference message.
- Distinct native --smoke-pass56 synthetic test fixtures are accepted only under the explicit test flag, and are cleared before the public Windows asset-leak audit.

## Private installation

The separate owner-only **TWR_Pass56_Private_Visual_Reference_Kit.zip** is an install-ready container. Extract its Content folder into the directory containing your offline Windows game EXE. It contains:

- Content/References/SourceWikiReferenceImages56.zip
- Content/Skins/SourceSkinMaterials54.json.gz
- Content/Skins/SourceWeaponSkinSlots54.json.gz
- Content/Skins/SourceSkinLayers55.json.gz
- PASS56_INSTALLATION.txt

All art remains local; there is no Roblox, server, or internet dependency once installed. This ZIP provides **historical visual references** and source-authored palette/part metadata, **not** original Roblox texture images, 3D mesh triangles, or original animation timing.

The documented extraction tool can rebuild the pack from the user-provided sources and the existing Pass54 private material file:

    python tools/assets/recover_wiki_reference_images56.py --images images.zip --image-docs image-docs.zip --weapons content/weapons/catalog.json --skins-pack Content/Skins/SourceSkinMaterials54.json.gz --output Content/References/SourceWikiReferenceImages56.zip

Pillow is required for safe image dimension and integrity inspection. Existing owner images are never overwritten by the tool.

## Native acceptance and limitations

The Windows CI branch twr-pass56-source-wiki-gallery requires the Python regression suite, a zero-error Godot/.NET Windows export, a native Pass56 image/gallery navigation test, and a public/private source asset exclusion check. The fixture contains only three fake PNG files (Glock17 default, Glock17 Neon/Ego, and Ego icon) and an isolated test-only profile; it does NOT upload the actual wiki image archive. Marker: TWR_SMOKE_PASS56_WIKI_REFERENCE_OK.

Still outstanding: actual original UV texture image bytes for source texture asset IDs, all original weapon meshes, original animated effects, side-by-side human verification against the Roblox game, many fully accurate map environments, long-form real player-controlled matches, and the privately distributed final Windows EXE. This is a visual fidelity **reference and inspection** pass, not proof of 1:1 game visuals.


## Verified Pass 56 Windows checkpoint

**Code commit validated:** `6ccce36fb5def382778838c12489e70b2d6b142f`.

**Successful Windows Actions run:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38109511432

- **512 Python tests passed**, one unrelated pre-existing Pass34 FutureWarning.
- Native Godot/.NET Windows EXE export **succeeded, zero errors**; one inherited Pass36 CS0108 compiler warning.
- Baseline domain simulation: `TWR_SMOKE_COMPLETE_OK maps=10 waves=150`. This is not a manually played full game.
- Native Pass56: `TWR_SMOKE_PASS56_WIKI_REFERENCE_OK synthetic_only=true files=3 png_decoded=true fallback_disclosed=true gallery_back_restored=true inventory_view_preserved=true credits_unchanged=true gallery_removed_on_match=true owner_images_not_uploaded=true`.
- Public/private asset leak check: passed. No public EXE, original owner source script or wiki reference ZIP uploaded by CI.
- The complete owner-only reference archive underwent an independent local integrity check: **all 2,033 files matched SHA256 and image dimensions**. Its SHA256 is `c06abbaefcaa33430a5a37ddb10fe7540fb27ac39c863fd05c420cad0d2cd2c1`. The install-ready private kit ZIP also passed ZIP integrity checks.

This native test exercises **synthetic image data** only; screenshot-by-screenshot fidelity of all 91 real weapon renders and complete human gameplay remains unverified.
