# Reconstruction Build Process

## 1. Evidence intake

Original project sources remain immutable. The working repository stores derived manifests, hashes, normalized records, conversion outputs, tests, documentation, and appropriately sized assets. Source priority is defined in `evidence/README.md`.

## 2. Separate original platform structure from game behavior

The reconstruction does not attempt to reproduce Roblox networking internally. Original RemoteEvents/RemoteFunctions, replicated folders, client/server scripts and platform services are treated as evidence of **game rules and data flow**. Their behavior is re-expressed through a local authoritative simulation suitable for a standalone Windows game.

## 3. Map reconstruction

For each map, the converter should extract hierarchy, class/type, transform, size, material, transparency, mesh/texture IDs, collision flags, tags/attributes and meaningful names. Derived outputs should include an instance inventory, asset dependency list, top-down bounds diagram, collision/nav representation and gameplay-anchor list.

The supplied TestPlace forensic work proves why this separation matters: the reconstructed reference place contains complete Laboratory render art while `ReplicatedStorage/CMaps` carries collision/navigation/lighting structures for the broader map roster. Those are separate evidence layers and should remain separate in the converter.

## 4. Asset scaffold

Every source dependency receives a stable local identity. The scaffold records the source identifier, target local file, category, map/system usage, conversion status, license/provenance notes when known, and whether a substitute is temporary.

## 5. Engine scene construction

Godot scenes are assembled from normalized data rather than hand-copying a platform hierarchy. Static geometry, collision/navigation, environment/audio and gameplay anchors are separate layers so each can be rebuilt and tested independently.

## 6. Gameplay reconstruction

The locked product profile is an older-style Regular-only 15-wave game. Systems are implemented against recovered rules and captures, then tested independently before being integrated: waves, infected, weapons, items, objectives, perks/progression, UI, audio and map completion.

## 7. Fidelity verification

Fidelity is measured, not assumed. Verification should compare geometry/bounds, camera position/FOV, landmark placement, lighting, item/objective/spawn anchors, weapon timing, infected behavior, wave timing and UI state. Deviations are logged explicitly.

## 8. Windows export

The repository already contains a pinned Windows CI path. Validation runs before export, then the Godot/.NET build produces `ThoseWhoRemainOffline.exe` and packages the runtime so it can launch with networking unavailable.

## 9. Development-history capture

For future video production, keep milestone captures at the end of each phase: source archive/index, raw map extraction, collision-only view, graybox, first materials, lighting pass, spawn/objective overlay, first playable wave, fidelity comparison, packaged executable. The phase PNGs in this directory provide a stable storyboard for those milestones.
