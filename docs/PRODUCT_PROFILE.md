# Product Profile

The release target is an offline standalone recreation of the older-style Those Who Remain experience.

## Locked release rules

- Windows standalone executable: `ThoseWhoRemainOffline.exe`
- Offline single-player first
- Local authoritative simulation
- One playable game mode: **Regular**
- Exactly **15 waves** per map
- Wave 15 completion ends the map and grants the map-completion XP bonus
- No Hardcore mode
- No Classic mode
- No Endless mode
- No enemy Juggernaut spawning
- Ordinary infected melee damage does **not** apply movement stun
- Old-style lobby/map-voting presentation where practical
- Stadium and Carnival are excluded

## Supplied map scope

The source map archive currently contains Ranch, Mill, Bypass, Cabin, Cargo, District, Expressway, Prison, Laboratory, and Manor snapshots. These original `.rbxlx` files are the primary authority for map conversion.

## Evidence policy

Original evidence is immutable. Raw archives and large `.rbxlx` files are not committed to ordinary Git history; the repository stores hashes, manifests, normalized content, conversion outputs, tests, and source code.
