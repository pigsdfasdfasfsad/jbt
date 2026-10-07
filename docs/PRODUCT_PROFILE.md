# Product Profile

The release target is an offline standalone reconstruction of the older-style Those Who Remain experience.

## Locked release rules

- Windows standalone executable: `ThoseWhoRemainOffline.exe`
- Offline single-player first; no network dependency at runtime
- Local-authoritative simulation
- One playable game mode: **Regular**
- Exactly **15 waves** per map
- Wave 15 completion ends the map and grants the one-time map-completion XP bonus
- Save immediately after the completion reward and before results/lobby return
- No Hardcore, Classic, or Endless mode
- No enemy Juggernaut spawning and no Juggernaut waves
- Ordinary infected melee damage does **not** apply movement stun/slowdown
- Old-style results flow, then lobby/map selection
- Stadium and Carnival are excluded

## Completion reward evidence status

The surviving engineering report shows a prior reconstructed implementation using a proposed completion formula of player level multiplied by 300 XP, plus 3,500 credits. The recovered original-script evidence does **not** prove that as the historical retail map-completion reward. Therefore the current implementation keeps `MapCompletionXpBonus` configurable and marks the default multiplier as **Proposed**, not source-confirmed.

## Release map scope

Ranch, Mill, Bypass, Cabin, Cargo, District, Expressway, Prison, Laboratory, and Manor.

## Evidence policy

Original evidence is immutable. Raw archives and large `.rbxlx` files are not committed to ordinary Git history; the repository stores hashes, manifests, normalized content, reconstruction outputs, tests, and source code.
