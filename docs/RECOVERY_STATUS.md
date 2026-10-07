# Recovery Status

## What survived directly

The original later working tree did not survive byte-for-byte. Direct surviving artifacts used here are the engineering/development documentation bundle, normalized evidence catalog, the 27-page current-build engineering report, and six immutable evidence archives that are mounted outside Git. Clean top-down map reconstruction images were recovered as embedded PNGs from the report, not captured from a live Godot build.

## What was reconstructed

The current branch reconstructs the engine-independent C# local-authority layer, Godot integration shell, deterministic content compiler, static RBXLX XML parser, RemoteSpy normalization utility, validation pipeline, acceptance gates, Windows build script, tests, and public-safe generated content. Recovered source tables are normalized without executing Luau.

## Historical checkpoint versus current recovery

The surviving engineering report records 82/82 tests, 78 C# files, 21 acceptance gates, ten source maps converted to static IR, and 14,363 normalized RemoteSpy calls. Those counts remain historical evidence. They are not promoted to current facts unless reproduced.

Current validation is produced by `python tools/validation/run_all.py` and is intentionally allowed to differ from the historical counts.

## Unrecovered bytes

Two high-value later evidence packages are still present in Project storage but their raw bytes could not be materialized into this recovery runtime: `twr places.zip` and the two new RemoteSpy session ZIPs. Because the original `.rbxlx` source has higher authority than TestPlace, the recovery does **not** fabricate static geometry IR from lower-authority substitutes. Map recovery records therefore explicitly distinguish historical IR presence from currently recovered IR bytes.

Likewise, the historical 14,363-call RemoteSpy database is not recreated from guesses. The repository retains the historical count with a qualification and includes a normalizer ready for the raw captures when they become byte-accessible.

## Completion XP

The prior engineering report used player level x 300 XP for the reconstructed completion reward. Recovered original-script evidence does not prove that as the exact historical retail map-completion payout. The current `MapCompletionXpBonus` value is therefore **Proposed** and configurable.

## Generated Godot content mirror

Canonical generated runtime data is tracked once under `content/`. `tools/content/sync_godot_content.py` deterministically creates `src/Twr.Godot/Content/` before validation/build/export, and that mirror is intentionally Git-ignored to avoid storing byte-identical duplicates.
