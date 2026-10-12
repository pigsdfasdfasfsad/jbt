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

**Initial status:** pending GitHub Windows CI verification.
