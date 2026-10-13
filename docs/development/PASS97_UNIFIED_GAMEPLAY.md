# Pass 97 — Unified grounded objectives and full-match replay

## Problem

Two successful Pass96 implementations diverged from the verified Pass95 branch. Neither contained the other's changes:

- **Physics-grounded Load and Repair objectives:** original approximate placement was replaced on public fallback geometry with 204 collision-audited items across ten maps, with actual E-key pickup/deposit tested in exported Windows. This is not owner-original item geometry.
- **Same-map replay and progression:** replaying a fifteen-wave map in the same offline application session formerly failed to grant legitimate wave rewards because a receipt ledger lasted for the entire process. The domain now resets only *per-match wave receipts* at an authorized Lobby-to-Match transition, preserving player credits, level, XP and persisted account records. A native exported Windows run proved full fifteen-wave stage progression and a second same-map Wave 1 payout exactly once.

This pass combines both into a single Godot/.NET Windows source tree. Git history uses a **two-parent merge commit** from the two green Pass96 branches, preserving authorship/provenance and all existing features rather than rebuilding or discarding work.

## Integration

- Both production gameplay changes remain intact: Pass96 grounded materials in `ObjectiveRuntime` and `GameplayRoot`, and per-match receipt handling in `LocalSession`, `MatchDirector`, `WaveRewardService` and `ReceiptLedger`.
- The original `--smoke-pass96` flag continues to run ten-map Load/Repair physics checks, with an isolated `pass96-material-objectives-smoke-profile.json`.
- A distinct `--smoke-pass96-replay` flag now runs the fifteen-wave/second-match replay acceptance, with another isolated `pass96-full-match-replay-smoke-profile.json`. Unlike the prior separate branches, neither harness masks the other.
- Windows CI runs **both** Pass96 preflights **in the same exported executable** before the previous stair, infected, map, Escort and rewards regressions. It retains the owner-private source pack exclusion audit and disables public binary upload.
- `tests/test_pass97_unified_native_gameplay.py` asserts the combined entrypoints, isolated profiles, both real native acceptance markers, prior required physics checks and preserved reward accounting.

## Verification gate

The combined `twr-pass97-unified-gameplay` branch passed the complete native Windows pipeline in [run 38325416220](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38325416220): 724 Python tests passed (one prior source-parser warning), .NET/Godot Windows export, Manor West/East Escort, both Pass96 preflights, Pass78/83/88/89/90/91/92/94 physics regressions, and the private-asset exclusion audit. **Pass97 is verified**, including integrated match-level acceptance.

The combined pass makes no new claims about 1:1 retail map geometry, source NPC animations, human-controlled real-time fifteen-wave survival, or a downloadable public executable. The public workflow does not attach a private-asset build.

## Verified Pass97 integration evidence

The native Ranch test advanced 15 Regular waves using genuine production infected and objectives (only stage clocks accelerated). During the **same match**, eight Load/Repair objectives created 82 collision-grounded item positions. The run committed exactly one match completion and a saved 184,050-credit balance, then paid Wave1 once more on a second Ranch match in the same LocalSession (185,550 credits), denying a duplicate claim. The separate ten-map preflight checked 210 Load/Repair materials on physically supported floors with zero overlapping fallback placements. Random Repair variants can change totals.

Windows CI [38325416220](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38325416220) completed green; [38325152843](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38325152843) was also a green baseline merge run. This final documentation-only update follows the tested gameplay commit.

This is not a completed 1:1 retail remake. Human-controlled real-time 15-wave survival, exact original map geometry and item placements, full source animations/audio, and a distributable owner-approved Windows package remain outstanding. The public workflow deliberately does not publish the executable or owner-private source packs.
