# Pass 36: 15-wave native gameplay lifecycle gate

Previous build tests ran a domain-only 150-wave rules simulation. This pass adds an exported Windows Godot/C# --smoke-pass36 that constructs the REAL GameplayRoot with a synthetic Laboratory map, advances the clock through 15 waves and checks each countdown, wave, infected spawn, objective lifecycle, wave-end cleanup, intermission and final results.

- Executes 59 distinct phase transitions across 15 waves.
- Instantiates live Godot infected actors and normal wave objectives.
- Checks survivor cleanup and results.
- Uses accelerated time; it is **not a 15-wave human-controlled gameplay test**, performance benchmark, zombie physics validation over 15 full wave durations, or a recreation of original TWR timing.
- CI uses source-bound synthetic test fixtures, never original private game binaries.
- Existing Windows executable and optional private Laboratory asset workflows preserved.
- Missing original MeshPart/CSG triangles, terrain, and 9 complete maps remain major fidelity gaps.
