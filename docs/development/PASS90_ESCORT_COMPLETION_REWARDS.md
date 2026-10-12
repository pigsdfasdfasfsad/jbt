# Pass90 - Physically grounded Escort objective completion

Base: verified Pass89 (`17fd9fb8`, 666 Python tests, native Windows
Pass78, 83, 88, 89 all passed).

## Problem

Fallback objective markers have a conventional Y=1.0m placement, but
the Escort survivor is a `CharacterBody3D` whose origin is **feet-level**.
The Escort destination mesh is another 0.1m above its marker. Before
Pass90, the new Pass89 collision-sampled route could reject a destination
that was merely 1.1m above otherwise walkable ground; this could block
the normal Wave5 Ranch Escort objective from finishing.

## Implementation

- Only the Pass89 fallback Escort pathway waits for the survivor's real
  capsule to settle on the Godot physics floor.
- Ground each candidate destination with five local physical probes and
  verify a 0.42m-radius survivor capsule fits without static collision.
  Vertical marker adjustment is bounded by 1.40m. The original XZ
  destination remains unchanged wherever safely reachable.
- Route the survivor to **feet-level** waypoints, using ordinary
  `MoveAndSlide`. Reject a goal rather than placing an NPC in a wall.
- Completing the Escort requires: actual grounded NPC, player within
  the existing 45m Escort follow distance, horizontal arrival <=1.5m,
  floor-height gap <=0.48m and unobstructed collision-supported final
  segment. Never call the reward command for a disconnected floor.
- The existing domain `CompleteObjectiveCommand` remains the sole
  credits/XP authority, and `ObjectiveService` continues its
  `CompletedObjectives` idempotence guard.

Native acceptance: accelerate prior Wave1-4 clocks only in an isolated
Windows test profile, spawn the real Wave5 Ranch objective via
`GameplayRoot.SpawnWaveObjectives`, wait for the actual Escort survivor
to walk to its destination, then validate exactly 4,000 credits / 3,500
XP and a single completed objective ID. A test-only duplicate command
checks reward idempotence.

This is a measured improvement to an **approximate** original Roblox
objective location, not a reconstruction of source retail animation
rigs, multi-floor escorts or all 15 human-played waves.
