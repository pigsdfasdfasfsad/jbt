# Pass 71 — Native two-map pursuit, melee occlusion and fatal reload interruption

**Branch:** `twr-pass71-native-multimap-damage`
**Parent:** Pass70 verified exported Windows Laboratory combat pipeline.

## Delivered
- New `--smoke-pass71` Godot exported-Windows acceptance harness launches **two real GameplayRoot scenes**, first Laboratory, then Manor, with a fresh isolated local session and actual first-person player, HUD, Wave 1 countdown and an actual spawned physics InfectedAgent.
- Each scene uses a clearly synthetic raised physics lane that isolates CharacterBody3D pursuit/contact from the unrecovered original map navigation. The infected moves and grounds itself via actual Godot physics. The test asserts real progress towards the player.
- A solid wall on Godot collision layer 1 is inserted between the player and an infected within ordinary attack range. The authoritative health value must not decrease while line of sight is blocked; wall removal allows the normal InfectedAgent-to-LocalSessionNode damage command to cause fatal damage.
- The real game must end on Results, persist the match, destroy live infected, and return to the lobby before starting the other map.
- FirstPersonPlayer now cancels pending reload timers on player death **before** processing reload completions. Dead players cannot initiate reload, shoot, throw items or queue movement actions. This fixes a genuine game behavior issue, not just a smoke-test deficiency.
- A normal R key begins a reload with previously spent cartridges via the authoritative command bus. The test kills the player before the reload completes and verifies that no rounds are transferred from reserve even after its original reload time passes.
- CI runs the new native Godot Windows test from the exported executable and checks the specific per-map and final markers. Existing Pass70 and earlier smokes remain unchanged. CI retains the source-map and owner-private content exclusion checks.

## Explicit limits
These tests **do not** establish accurate original map navigation, source collisions, zombie balance, actual 15-wave survival, human input acceptance, or full original mesh/animation/audio parity. Raised synthetic floors and controlled test enemy types are test fixtures only; normal game maps and waves remain unchanged. Owner-provided packs and private profile data stay outside public CI.

## Validation
The GitHub Actions Windows validation is the authoritative compile/runtime gate for this branch. Do not label this pass *verified* until the run completes successfully, including all Python tests and native executable smoke. This chat environment cannot build a Windows Godot/.NET game directly.

## Verified checkpoint

[Successful exported Windows CI (Pass71)](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38201351136) — Python regression suite, Godot/.NET Windows build, actual two-map pursuit/occlusion/death/reload smoke and owner-private asset exclusion all passed. The test used synthetic raised collision lanes and does not claim native retail-map navigation or complete original asset parity.
