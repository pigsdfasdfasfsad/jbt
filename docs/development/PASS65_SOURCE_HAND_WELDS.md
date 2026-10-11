# Pass 65 — Source-Authored First-Person Hand Grips and Reload/Equip Welds

**Base:** verified Pass64 `twr-pass64-source-shell-and-bolt`.
**Branch:** `twr-pass65-source-hand-grips`.

## Original source evidence

The normalized, inert original weapon records in
`content/weapons/entries/*.json` preserve per-weapon
`stats.LeftCF` / `stats.RightCF` and
`animations.<clip>.Sequence[*].AV.LeftWeld/RightWeld` with
`OffsetEnd`, optional `OffsetStart`, `TargetHierarchy`, `TargetChar`,
`Lerp`, `Ease`, and authored cue timings.

Source examples:

| Weapon | Source evidence |
|---|---|
| Glock 17 | Rest left/right grips; 3 equip left-weld keys; 6 original reload left-weld keys, of which 5 are anchored to valid weapon groups and 1 targets the avatar character Base (unsupported here); 2 reload right-weld keys |
| Mossberg 500 | Source left hand grip reattaches to Pump Handle for cycling and reload via exact `PumpRel:inverse() * LeftCF` reference |
| Mosin Nagant | 4 right-weld cues during bolt cycling and 7 during reload; right-hand targets include the moving original Bolt Handle |
| AK-47 / M4A1 / P90 | Original left-hand magazine/reload and equip-weld CFrames, with right-hand rest grips |

## Implemented

- New `Pass65SourceHands.cs` strictly decodes numeric original
  `CFrame.new` matrices, finite bounded angles in
  `CFrame.Angles`, and the exact original numeric symbolic constant
  `(math.pi/2)` (plus its negation). It does not execute source Luau.
- `module.Stats.LeftCF` and `module.Stats.RightCF` resolve only for
  the corresponding side. The exact original
  `module.Stats.Rels.PumpRel:inverse() * module.Stats.LeftCF` (and
  equivalent whitelisted Slide/Bolt/Charge group grips) resolve through
  the **already-verified, owner-only** source hierarchy as:
  `currentGroupHandle * inverse(originalGroupHandle) * restHand`.
  Arbitrary inverse operations and unknown Lua names are rejected.
- Each source `OffsetEnd` is interpreted relative to an original
  `TargetHierarchy` named group, if that group is installed. The
  rest-pose transform is used when original group data are missing;
  `TargetChar` references to the original avatar are deliberately
  skipped because authentic Roblox character rigs are not restored.
- Pass62 hierarchy binding now exposes the live group Handle CFrame
  computed from current source-indexed part transforms. Hand targets
  can therefore follow a moving magazine, pump, bolt, or nested Bolt
  Handle instead of remaining motionless during reloading.
- A visibly **approximate** dark glove rig with one palm, wrist,
  thumb and four fingers per hand is procedurally created for
  supported first-person weapon models. These are 14 simple local
  meshes total; they are not original Roblox avatar hand/arm models
  or first-person IK. Their local positions and orientations are
  controlled by source-corrected grip CFrames.
- Hand timeline playback is normalized to the existing **authoritative**
  equip/reload durations and uses bounded keyframe interpolation
  without changing combat or reload mechanics. The hands return to
  source rest poses after clips; the static armory showroom hides
  these invented glove proxies.
- No original owner pack was modified or committed. Without verified
  group data, static source-grip poses still work, while unsupported
  group targets skip to safe rest positions. Existing gun visuals,
  ammunition, sounds, enemy logic and progression remain unchanged.

## Automated acceptance

- `tests/test_pass65_source_hands.py` checks source data, the restricted
  CFrame and whitelisted inverse grammar, per-gun wrist motions,
  proxy separation from original assets and regression continuity.
- A new exported Windows `--smoke-pass65` uses only the fabricated
  Pass64 Glock/Mossberg/Mosin source pack and synthetic motor sidecar.
  It verifies paired hand proxies, Glock equip/reload including
  source magazine hand movement, left hand following Mossberg pump,
  right hand following Mosin bolt, end-of-clip reset, and malformed
  Lua expressions rejected.
- The Windows workflow retains Pass59–64 native smoke tests,
  the prior domain-wide scripted wave progression check,
  and private-asset exclusion after cleaning every synthetic pack.
  Public GitHub EXE upload stays disabled.

## Remaining gaps

True original left/right Roblox avatar arm, hand, wrist, finger MeshParts
and texture assets; exact Motor6D C0/C1 joints and character Base
transforms; source animation easing matching, user-configured avatar
skin and glove styling; dynamic source AimCF; proper collision and
hand IK; complete ten-map restoration, real end-to-end 15-wave
single-player acceptance, and owner-private release packaging.

**Status:** native Windows acceptance pending until the Pass65 workflow
runs successfully. Do not treat procedural gloves as proof of original
TWR first-person visual fidelity.
