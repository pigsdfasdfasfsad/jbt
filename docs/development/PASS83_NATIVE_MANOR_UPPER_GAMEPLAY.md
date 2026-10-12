# Pass 83 - Exported-Windows Manor upper-floor gameplay acceptance

Parent: Pass82 (new, approximate Manor upper pickup and wave objective
markers). Pass83 brings the passes 79-82 changes under a single exported
Windows Godot physics acceptance.

The test starts one authentic production GameplayRoot and runs on the
public-safe reconstructed Manor collision. It inspects all three authored
catwalk colliders, inserts and then removes a test-only solid crosswalk
barrier, rejects unsafe routes while blocked, constructs a physically
floor-audited route when clear, and moves a real production InfectedAgent
across each balcony without teleportation or scripted jump.

For each wing it also verifies that second-floor item/objective positions
have actual Godot static-body floor support, that a lower-floor player
cannot collect an upstairs item through solid floors, and that a real
E-key pickup restores domain-authoritative health from the upper deck.
The isolated profile is saved in user://pass83-manor-upper-gameplay-smoke-profile.json.

The public Windows workflow compiles Godot/.NET, runs Python regression
contracts, exports Windows, runs --smoke-pass83 with required per-stair
markers and verifies no original/private assets entered the output.

**Status at authoring:** native Windows verification pending CI. Do not
state pass83 is verified until the GitHub Actions job succeeds. No original
retail Manor meshes or private source ZIPs are shipped in this public
source code branch. Human-played all-map 15-wave parity is NOT proven.
