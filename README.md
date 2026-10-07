# TWR Offline

Durable development repository for the standalone offline reconstruction of **Those Who Remain**.

## Locked target

- Windows standalone game using Godot 4 + C#/.NET.
- No Roblox, Roblox Studio, Roblox account, Roblox server, or Internet connection at runtime.
- Local-authoritative single-player simulation with command/event boundaries suitable for later LAN/co-op work.
- Older-style **Regular-only, 15-wave** release profile. Wave 15 completes the map; there is no wave 16.
- Hardcore, Classic, Endless, enemy Juggernauts, Juggernaut waves, and ordinary-hit movement stun are disabled.
- Completion reward is one-time and is persisted before results/lobby transition.
- Release maps: Ranch, Mill, Bypass, Cabin, Cargo, District, Expressway, Prison, Laboratory, Manor.

## Recovery status

This branch contains the latest recoverable development state assembled from surviving engineering artifacts and immutable source evidence. The previous full working tree did not survive byte-for-byte. Files restored from surviving artifacts, files deterministically regenerated from recovered normalized evidence, and newly reconstructed implementation files are distinguished in `recovery/PROVENANCE.json` and `docs/RECOVERY_STATUS.md`.

The historical engineering report records **82/82 tests**, **78 C# files**, **21 acceptance gates**, ten converted source maps, and **14,363 normalized RemoteSpy calls**. Those are historical checkpoint facts, not automatically current claims. Run `python tools/validation/run_all.py` for current repository truth.

## Evidence policy

Original uploaded evidence remains immutable and outside ordinary public Git history. This repository stores hashes, manifests, normalized data, generated reconstruction outputs, tests, source code, and documentation. Raw `.rbxlx`/`.rbxl`, raw evidence ZIPs, raw recordings, and release binaries are intentionally excluded.

## Validation

```text
python -m pip install -e .[test]
python tools/validation/run_all.py
```

## Windows output

The pinned workflow targets:

`build/output/ThoseWhoRemainOffline.exe`

A successful executable build is only claimed after the Windows workflow actually produces that file.
