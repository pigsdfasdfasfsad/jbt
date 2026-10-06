# TWR Offline

Canonical durable source repository for the standalone offline reconstruction of **Those Who Remain**.

## Target
- Windows standalone game
- Godot 4 + C#/.NET
- Offline-only runtime
- No Roblox / Roblox Studio / Roblox account / Roblox servers required
- Old-style **Regular-only, 15-wave** ruleset
- No Hardcore / Classic / Endless
- No enemy Juggernauts
- No ordinary infected-hit movement stun
- Large map-completion XP reward after wave 15
- Maps converted from supplied original `.rbxlx` files

## Development branch
Active engineering work is performed on `twr-offline-dev` and promoted to `main` after validation.

## Evidence
Original uploaded evidence archives remain immutable and are not duplicated into ordinary Git history. The repository stores manifests, hashes, normalized data, conversion outputs, tests, source code, and appropriately sized authorized assets.

## Final output
The release pipeline will ultimately produce:

`ThoseWhoRemainOffline.exe`

plus all required local runtime/data files for fully offline play.
