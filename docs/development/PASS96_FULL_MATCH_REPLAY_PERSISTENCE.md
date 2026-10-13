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

**Status:** verified on exported Windows build (native CI green). This is an automated stage-accelerated physics/economy test, not a claim of real-time 15-wave human survival, original retail combat timing, source-authentic map meshes or a finished 1:1 remake. The uncertain map-completion XP bonus (`CompletionRewardPolicy`) remains unchanged; the pass does not invent a retail payout without verified source evidence.

## Verified exported-Windows results

[GitHub Actions run 38321946176](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38321946176) succeeded with **713 Python tests passing** (one existing source-parser warning), Windows Godot/.NET export, production Pass96 preflight, the cumulative Pass78/83/88/89/90/91/92/94/95 native physics/mission regressions, and the private-asset exclusion audit.

The genuine stage-accelerated Ranch `GameplayRoot` traversed all 15 production waves with real infected physics spawns. The match committed exactly one completion event and a saved 184,050-credit account balance, then returned to the lobby. A **second Ranch match in the same LocalSession** paid Wave 1's 1,500 credits again (balance 185,550), and ignored the duplicate claim. Source-verified wave XP, level-up progress, and JSON persistence passed exact arithmetic assertions. An attempted premature Wave15 reward and attempted mid-match restart were both denied.

Key native markers:
- `TWR_PASS96_FIFTEEN_WAVES_OK map=Ranch waves=15 completion_events=1 committed=true persisted_credits=184050`
- `TWR_PASS96_REPLAY_WAVE1_OK same_map=Ranch earned_again=true duplicate_paid=false saved=true credits=185550`
- `TWR_SMOKE_PASS96_FULL_MATCH_REPLAY_OK`

This is a 15-wave **automated accelerated** scene/economy replay, **not** an actual 75-minute interactive survival session, full 1:1 map/audio/weapon/animation parity, or a distributable Windows game with the owner-private recovered sidecars. Original source-unknown map-completion XP is still not invented.
