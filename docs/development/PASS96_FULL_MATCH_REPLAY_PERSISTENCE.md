# Pass96 — Replayable Regular-mode wave rewards and full-match Windows check

**Base:** Pass95 green Windows run [38306155437](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38306155437). The existing reconstructed game has ten maps, source-verified Regular wave payouts, and native completed front/west/east Manor Escort scenarios.

## Gameplay defect repaired

The per-session `WaveRewardService` previously owned a single private `ReceiptLedger` keyed by `WaveSurvival:{map}:{wave}`. Since `LocalSession` remains active when returning to the lobby, replaying Ranch (or any previously played map) in **that same application session** silently rejected survival credits and XP for waves already recorded on the first run. This made later matches underpay despite normal gameplay. A source-level or first-run-only smoke could not detect it.

Pass96 clears **only that in-memory wave duplicate ledger** after a successful transition from Lobby into a fresh Regular match. It does not clear account credits, XP, level, unlocks, cosmetics, or persisted profile receipts. Each run therefore pays a legitimately survived wave once; repeating the same wave within that run does not pay twice.

It also rejects a new match requested while already in an active match and refuses invalid wave-survival claims (outside an active wave, from a dead player, or using an earlier/later wave number). The existing source-verified Bonuses/Levels formulas are unchanged, as are the release's 15-wave cap, map list, spawn cadence and combat.

## Native acceptance

`--smoke-pass96` creates a real Ranch `GameplayRoot`, grounded FPS player, real HUD and actual infected characters for each of 15 production waves. In this **test only**, stage clock durations are advanced directly so CI doesn't wait 75 minutes: real `StartWave/EndWave/AdvanceAfterWave`, enemy spawn/cleanup, domain wave reward grants, progression, completion event and atomic JSON saves run unchanged. It demands exactly 15 successful wave markers, one committed map-completion event and no duplicate payout on calling `AdvanceWave` again.

Then it destroys the first scene, returns to the real Lobby, **reopens Ranch in a second match using the same LocalSession**, and requires Wave 1 to grant source-calculated credits and XP again. It also verifies duplicate Wave 1 commands do not reward twice, checks saved profile credits/XP/level after every wave and following replay, and rejects attempted future-wave claims and in-progress restarts. Reused player inventory resets normally; persistent progression does not.

CI first runs this targeted native Windows acceptance and then all historical Pass78–95 gameplay/physics regressions and the public-build private-asset audit. No original meshes or paid/source-private sidecars are published, and no CI build is advertised as a completed distributable private .exe.

**Status:** awaiting successful native Windows acceptance. This is an automated stage-accelerated physics/economy test, not a claim of real-time 15-wave human survival, original retail combat timing, source-authentic map meshes or a finished 1:1 remake. The uncertain map-completion XP bonus (`CompletionRewardPolicy`) remains unchanged; the pass does not invent a retail payout without verified source evidence.
