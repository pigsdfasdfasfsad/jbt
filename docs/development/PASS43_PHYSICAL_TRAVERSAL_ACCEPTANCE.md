# Pass 43 — Physics-Correct Jump Completion and Playtest Telemetry

**Target:** improve the offline Laboratory reconstruction's verifiability without claiming that the original Roblox navigation mesh or missing triangle geometry has been restored.

## Defect identified in Pass 42

The original source `Pathfind` module permits jumping. Pass 42 recovered nine bounded jump-action edges and introduced ballistic movement in `InfectedAgent`. However, its landing counter incremented after **any** post-launch `IsOnFloor()` contact. That included falling back to the starting side after a wall impact or reaching the time limit. The native Pass 42 smoke only asserted `SourceJumpAttempts > 0` on a single **continuous floor**. It did not prove that any enemy traversed an actual gap.

## Runtime correction

`Pass43JumpLandingPolicy` requires three conditions before recording a successful landing:

1. Godot `CharacterBody3D` reports genuine floor contact **after** at least 0.15 seconds of flight.
2. The physics body lands within **0.70 metres horizontal** of the intended destination waypoint.
3. The body is within **0.95 metres vertical** of the intended destination.

An ordinary source takeoff-side collision, premature landing, obstructed flight or timed-out jump is now a separate **failed attempt**, not a successful traversal. Collision testing and motion remain in native Godot `MoveAndSlide`; the system never teleports to its selected landing waypoint.

`GameplayRoot` accumulates successes, launches and failures when an infected dies or is cleared between waves, so the metrics do not reset when enemy nodes are deleted. A temporary live-jump counter shows airborne attempts. These totals persist within the active match until returning to the lobby.

## Performance telemetry

The observational `Pass43FrameWindow` stores no more than **2,048 frame-time samples**. It tracks:

- Median (p50), p95, and p99 frame time in milliseconds.
- Worst sampled frame time, sampled-frame count and cumulative observed sampled time.
- Maximum simultaneously living infected observed during sampling.

The record deliberately excludes `delta <= 0`, nonfinite values and values over 0.5 seconds, which would otherwise distort the accelerated CI wave-state tests. It is therefore a **bounded sampling window**, not a complete system-profiling trace or guaranteed representative FPS. F10 displays sampled percentiles alongside the per-match jump totals. F11 writes the existing latest snapshot **and** a timestamped JSON archive to Godot's local user-data directory.

## Native Windows acceptance test: physically separated platforms

`tools/validation/make_pass43_gap_synthetic.py` builds a fabricated two-platform Laboratory scene from the existing purely synthetic Pass 42 fixture. It retains the 19-node v4 graph with one typed jump edge, changes only the fabricated collision floor into **two solid 1-metre-thick platforms** and leaves a **4.2-stud (1.176-metre) open pit**. The original scene SHA inside synthetic navigation files is updated to match the new fixture. No owner-derived scene or real asset is distributed to public CI.

The exported Windows game runs `--smoke-pass43` to verify that:

- Godot physics rays detect floor on both sides **and no floor** at the middle of the gap.
- A real wave-spawned infected, following the typed AStar route, actually launches, crosses and lands on the **opposite** physical floor, without any frame-sized teleport.
- A large obstructing `StaticBody3D` installed in the same gap causes another jump to be recorded as **failed**, never as successful.
- Jump totals remain correct after wave clear, when original infected Godot nodes are deleted.
- The actual Godot wave-state runtime progresses through all **15 waves** when stage time is artificially advanced.
- Synthetic private source/scene/navigation sidecars and PNGs are deleted before the public no-private-assets Windows artifact is uploaded.

This is a *physics and state integration test*, not a full 15-wave human-controlled experience on the real 34,268-record Laboratory source scene.

## User-controlled acceptance gates

Run `ThoseWhoRemainOffline.exe` from the owner-only Windows ZIP and open Laboratory with the original `Content/Maps/Laboratory.scene.jsonl.gz`, collision pack, and `Laboratory.nav42.gz`.

During a full 15-wave real-time session, monitor F10 and press F11 every wave. Collect F12 screenshots for misaligned original floors, unexpected jumps or FPS problems. The following remain required before claiming completion:

- **Real owner-source traversal:** each of the nine approximate jump links should be inspected against the actual recovered collision and missing triangle-geometry context.
- **Game performance:** check p95/p99 frame time with multiple zombies, dynamic lights, original loaded Laboratory geometry and original asset packs, not only tiny synthetic fixtures.
- **Full match:** survive 15 actual five-minute waves with real player control; verify enemies, radio/unpack objectives, fortifications, inventory, weapons and return to lobby.
- **Other maps:** replace the nine remaining approximate full scenes when legitimate complete original-world source is available.
- **Fidelity:** recover original missing MeshPart/UnionOperation geometry and terrain, and establish original animations, audio, dynamic obstacles and screenshot comparison.

The game remains a standalone **offline reconstruction**, not yet a 1:1 remake.

## Reproducibility and privacy

All build logic, tests and fixture generators live in source control. The owner-held original Laboratory scene, private original-coordinate source data and original-derived maps remain **outside** public GitHub history and outside the public Windows Actions artifact. The owner-only playable ZIP combines the verified compiled executable with previously recovered authorized private content.

`Pass43JumpLandingPolicy` and `Pass43FrameWindow` are small independent C# units, and `tests/test_pass43_physical_gap_acceptance.py` validates the regression contract, synthetic fixture integrity and test-runner wiring.
