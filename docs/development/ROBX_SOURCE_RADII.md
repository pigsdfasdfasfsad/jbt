# Recovered source radius parity

The authored Roblox source module reports:
- Frag blast radius: 30 studs, equivalent to 8.4 Godot metres
- Molotov/Nerve Gas lingering radius: 30 studs, equivalent to 8.4 m
- Bloater spore cluster radius: 15 studs, equivalent to 4.2 m
- Burster burst/spore-cloud radius: 20 studs, equivalent to 5.6 m

The authored source radius constants remain untouched for provenance, but
Godot collision-distance and effect-size comparisons use RobloxUnits.Distance.
This resolves an earlier inconsistency where original radii were interpreted
as raw metres, greatly inflating the effect area relative to source maps.

This is a units fix, NOT evidence that original particle visuals, damage
falloff curves, cooldowns or projectile trajectories have been reproduced.
