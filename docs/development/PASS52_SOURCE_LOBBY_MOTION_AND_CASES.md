# Pass 52 - Original Loadout Offsets, Lobby Camera Motion and Offline Shop

## Baseline and scope

Continues **twr-pass51-original-loadout-preview**, the verified 477-test Windows branch, without changing combat, wave authority, player saves, purchases, infected AI or original external mesh assets.

Two archived scripts from the owner's uploads are directly relevant:

| Original evidence | SHA-256 | Confirmed facts |
| --- | --- | --- |
| `scripts/ModuleScript.Lobby.Source.txt` in `scripts.zip` | `6f31efccd2624db497383be8259f45690c4093409cf7e3682688fac9b0e6c7df` | FOV = 50, 3-second camera travel, `Ease.SlowDown2`, four equipped loadout slots and one View anchor, original per-weapon `LoadoutOffset`, +/-25 degree vertical preview tilt |
| `scripts/ModuleScript.Shop.Source.txt` in `scripts.zip` | `4df17a84418b830e3a26f9246057a38b08234597344f62d2335a6a661e98e9db` | Seven case credit prices, separate from monetized DevProducts |

Both archives remain owner-held; their raw bytes are excluded from this code change. No decompiled Luau executes at runtime.

## Implementation

- **Source CFrame offsets.** `Pass52SourceLoadoutOffsets` loads inert `LoadoutOffset.expression` fields from the 91 normalized release-weapon JSON files already bundled with the Godot game. It recognizes bounded numeric `CFrame.new` literals with twelve finite values, plus the exact ` * CFrame.Angles(0, math.pi, 0)` suffix used by **RPG-7** and **Festive RPG-7**. It maps Roblox +Z to Godot -Z without executing Luau. Non-numeric expressions and malformed frames fail closed. These original offsets are applied to equipped-slot 3D weapon displays relative to the Pass49 original authored Primary/Secondary/Melee/Utility anchor frames; the selected View anchor remains independent.
- **Camera.** `Pass49OriginalLobbyRuntime` keeps the first original Start camera immediate and transitions menu-to-menu over exactly **3 seconds** using source `1.001 * (1 - 2 ^ (-10*t))` easing, interpolating transform position/orientation. Original FOV = **50 degrees**. Interrupted transitions rebase from the currently rendered frame. The source Options-specific quadratic midpoint route is still missing and is not claimed as implemented.
- **Preview controls.** Holding the right mouse button in Armory rotates the selected weapon model; vertical pitch clamps to **+/-25 degrees**, as in the source. The mouse sensitivity and exact model handle alignment are reconstructed approximations. The preview does not fire or purchase weapons.
- **Offline shop.** Adds a `SHOP / CASES` main-menu button that switches to the recovered original **Shop** camera. Seven credit-price case cards are visible and selectable for **inspection only**. Case credits, skin ownership, drop odds, roulette, codes, packs, powerups, gamepasses, and Robux purchasing are not simulated or falsely shown as functioning.

## Verified source case prices (credits)

| Source case | Credits |
| --- | ---: |
| Low | 6,000 |
| Mid | 20,000 |
| High | 50,000 |
| Neon | 120,000 |
| Tactical | 12,000 |
| Ethereal | 1,000,000 |
| Hallows | 200,000 |

## Automated acceptance

New Python test `tests/test_pass52_original_lobby_and_shop.py` checks source literal bounds, representative Glock 17 CFrame, correct seven prices, camera easing, preview pitch clamp, menu wiring and private-data handling.

On the `twr-pass52-source-loadout-and-shop` branch, the Windows job runs native `--smoke-pass52` with **fabricated** Pass49 lobby, Pass50 bulletin and source weapon model packs. The smoke checks the exact five original-style lobby markers, 50 degree FOV, start/shop/loadout camera travel and deterministic mid/final frames, recovered weapon transforms, seven case cards, preview tilt, no credits or unlocks changed, and the absence of lobby objects after entering a Manor match. The Windows workflow must emit `TWR_SMOKE_PASS52_LOBBY_SHOP_OK` to earn a verified status. Synthetic packs are cleaned before the privacy audit.

## Outstanding limitations

The source positions and script constants are evidence-backed; exact render parity, original mesh/CSG triangles, original skins and images, dynamic physical gun pose, UI layout, all map interiors, human-driven 15-wave playthroughs, full offline case/skin inventory and final private Windows release are **not** complete. Windows compilation alone is not a true human gameplay certification.
