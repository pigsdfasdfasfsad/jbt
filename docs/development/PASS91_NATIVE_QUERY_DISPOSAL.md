# Pass91 — Native Godot collision-query lifecycle stability

Run 38291332738 compiled with zero C# errors, but the exported Windows
player initialization smoke crashed inside GodotObject.Finalize with a
native AccessViolationException and large counts of leaked Shape3D RIDs.
That run never reached the eight-map Escort test.

Pass73 creates thousands of temporary ray and standing-capsule queries
during its sampled grid build; Pass89 and Pass90 also create many short-lived
collision probes per NPC motion. These GodotObject-backed wrappers were
left for eventual managed finalization during engine shutdown.

This fix explicitly disposes **all per-call** PhysicsRayQueryParameters3D
and PhysicsShapeQueryParameters3D objects in Pass73, Pass89 and Pass90
with scoped using declarations. Pass73 also disposes its persistent
CapsuleShape3D and both AStar3D instances during _ExitTree, before Godot
native physics services shut down. Collision masks, geometry and gameplay
acceptance gates are unchanged. Owner-private source content remains excluded.

The pass must still pass compiled Windows native smoke tests. This patch
addresses a plausible native-object finalization cause; do not claim
a complete engine-level crash fix until repeated validation.
