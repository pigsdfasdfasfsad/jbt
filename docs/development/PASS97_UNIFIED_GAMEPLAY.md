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

A native Windows run on `twr-pass97-unified-gameplay` must pass the full source/contract suite, Godot .NET export, both Pass96 preflights, older required native regressions, and the private-content audit. Until that run is green, this combined pass is **not verified**, even though each unmerged Pass96 branch passed independently.

The combined pass makes no new claims about 1:1 retail map geometry, source NPC animations, human-controlled real-time fifteen-wave survival, or a downloadable public executable. The public workflow does not attach a private-asset build.
