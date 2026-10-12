# Pass88 — Windows native ten-map physical wave and objective acceptance

**Parent:** Pass87, built on verified Pass83 Windows CI run 38222801173.

Pass88 creates ten real production GameplayRoot scenes in an exported
Godot Windows executable, not merely calling an isolated domain model.
It checks that each actual player body grounds at map spawn, every
initial natural pickup is genuinely positioned above solid static
collision, at least one infected spawns a safe 11m away and then follows
real physics on that map, and that Wave 1 -> Wave 2 executes the
ordinary game cleanup/reward/entry through a real LocalSessionNode.

For the nine non-Manor approximate maps, it checks that the Wave 1 and
Wave 2 objectives appear at the rotated gameplay markers. Manor then
advances via production stage progression to an actual Wave 3 upstairs
Radio objective. A downstairs player is rejected despite being inside
the old broad 8m secure-height band; an upstairs player completes it.
Only isolated TEST-ONLY stage timers and initial player positions are
advanced. No enemy is teleported, no state field manually assigned.

This pass DOES NOT constitute 15 real-time minutes of player-controlled
survival, original map triangle/UV fidelity, original weapon animation
timing, original sounds, or manual retail match parity. All source/
owner-private assets remain excluded from public Git and CI output.

Workflow reruns both Pass78 genuine stair traversal and Pass83
bidirectional upper-wing infected/pickup smoke, then checks all
Pass88 native markers and private asset exclusion.

## Verified Windows execution

**Successful GitHub Actions run:** [38229534100](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38229534100),
gameplay commit `63471153601a8e75457f051ffc4188fddcddf202`.

- **660 Python tests passed**, with one inherited source-parser FutureWarning.
- Godot 4.7.2 / .NET 8 Windows build/export succeeded with **0 compiler errors** and one inherited Pass36 CS0108 warning.
- Pass78 native Windows 12/12 three-stair up/down player and infected physics markers passed again.
- Pass83 native three-wing blocked void, crossing, enemy movement and item floor checks all passed again.
- **Pass88 all ten actual runtime scenes passed:** Ranch, Mill, Bypass, Cabin, Cargo, District, Expressway, Prison, Laboratory, Manor.
- Observed six physically accepted natural loot pickups on each public fallback map; Ranch–Laboratory recorded three approved infected spawns, Manor recorded five; **zero rejected infected spawns** in this controlled run.
- Wave 1 and Wave 2 actually constructed production infected and objective actors in each scene, while test-only wave clocks were accelerated (not human survival).
- Manor Wave 3 Radio produced `TWR_PASS88_MANOR_FLOOR_OBJECTIVE_OK family=Radio wave=3 denied_downstairs=true completed_upstairs=true`.
- Final `TWR_SMOKE_PASS88_TEN_MAP_PHYSICAL_WAVES_OK maps=10` marker and owner-private asset exclusion audit passed.

### Follow-up limitations

The success certifies these explicitly defined native scenarios, not retail 1:1 map geometry, original mesh/texture/animation/audio accuracy, all objectives across 15 manually played waves, or a distributable owner-private Windows release. All ten CI scenes used public reconstructed/fallback content without original owner-held source-map files. The branch can be continued without changing the saved owner-private content.
