"""Update the existing continuation record from exact successful CI receipts.

The status-only commit is not presented as a separately compiled executable.
Historical development records are preserved below the current checkpoint.
"""
from pathlib import Path
import json
import os
import re

ROOT = Path(__file__).resolve().parents[2]
START = '<!-- CURRENT_SOURCE_SAFE_RELEASE -->'
END = '<!-- END_CURRENT_SOURCE_SAFE_RELEASE -->'


def main() -> None:
    expected = os.environ['TESTED_COMMIT']
    if not re.fullmatch('[0-9a-f]{40}', expected):
        raise ValueError('Expected exact tested source SHA')
    source = Path(os.environ.get('RELEASE_RECEIPT_DIR', str(ROOT / 'build/deliverables')))
    receipt = json.loads((source / 'release-receipt.json').read_text(encoding='utf8'))
    if receipt.get('tested_commit') != expected or receipt.get('clean_launch_passed') is not True:
        raise ValueError('Package receipt does not prove this revision')
    run_id = os.environ['GITHUB_RUN_ID']
    if not run_id.isdigit(): raise ValueError('Invalid workflow run ID')
    canonical = ROOT / 'docs/development/CURRENT_IMPLEMENTATION_STATUS.md'
    old = canonical.read_text(encoding='utf8')
    if START in old:
        prefix, tail = old.split(START, 1)
        old = prefix + tail.split(END, 1)[1].lstrip('\n')
    section = f'''{START}
# CURRENT CHECKPOINT - SOURCE FIDELITY AND RETAINED WINDOWS RELEASE

**Development branch:** `twr-source-fidelity-release`, continued from
`twr-runtime-stabilization` / `18bec9515d2d44020e8c402f9eec0800af34feba`.

**Exact compiled and tested source commit:** `{expected}`.
The subsequent continuation commit changes documentation only. It does not
change which source revision was executed, visually captured, and packaged.

**Verification and retained artifacts:**
https://github.com/pigsdfasdfasfsad/jbt/actions/runs/{run_id}

**Windows package:** `{receipt['archive']}`
- Archive bytes: {receipt['size_bytes']}.
- SHA-256: `{receipt['sha256']}`.
- Complete companion files and bundled .NET runtime included.
- Clean-directory native launch passed with system SDK paths removed.
- Original private maps, mesh binaries, textures, and test profiles excluded.
- This is the existing reconstructed-map game, NOT a finished 1:1 recreation.

Previous verified source checkpoint: `19bcba9e5b05bc7b502bd062d8d83d3a72fd3dd4`,
run 39205332855. The current release builds on that checkpoint.

Earlier retained binary: `91dd5b92485e466b50b042aa20680ece2f55f942`,
run 38709423959. Its Windows, graphics and packaging jobs all passed; only its
post-build documentation job failed because a newer build-tool-only commit
had advanced the branch. Do not conflate that documentation failure with a
failed executable or claim the later source was tested by the earlier run.

## Implemented player-visible changes

1. Original map importer treats built-in SpecialMesh Scale as relative to
   parent Part.Size, distinguishes Brick/Sphere/Wedge/Cylinder render batches,
   and puts cylinder axes on source local X. Parent-part collision is retained.
   FileMesh topology/upload-space dimensions remain unresolved when absent.
2. Deployed Barbed Wire, Clap Bomb, Jack and 50 Cal replace generic boxes.
   Wire uses instanced coils/barbs. Mounted guns show aim, ammunition, recoil
   and muzzle feedback without changing damage or collision authority.
3. Jack uses the reference-described ten-second fuse rather than proximity.
   Script-catalog peak damage and radius remain authoritative. Linear distance
   falloff is explicitly reconstructed, not a recovered server curve.
4. Viewmodel-only illumination makes hands/weapons readable without changing
   map lighting, shot rays, ammunition, or world collision.
5. Supply requests now plan a supported drop near the player, including all
   eight reachable payload locations and a clear descent. Only real collision
   contact authorizes delivery; a guessed Y coordinate no longer invents a
   floor. Changed obstruction/missing floor cancels once. Deferred callbacks
   preserve four regular plus four fortification slots, and match teardown
   retires pending pallets. Descent and delivery waypoints are approximated.
6. Strict packaging checks exact-commit receipts, rejects private/test files,
   launches from a clean directory, hashes all files and retains a Windows ZIP.

7. Source-neutral Radio/Unpack/Load/Repair objectives can use complete,
   collision-rechecked graph detours instead of requiring one straight line.
   Candidate pools now include all directions around the player. Load and
   both Repair variants get complete, unique, verified round-trip material
   layouts before actors appear. Source Escort now has its own complete-route
   planner as described below; Damage retains its separate existing policy.
8. Native traversal acceptance walks a real player around a blocking wall,
   rejects partial paths and a newly blocked route, then walks all 32 material
   interactions across Load, small Repair and large Repair with real E input
   and exactly-once rewards. The post-build offline audit now distinguishes
   authored runtime code from MSBuild-generated namespace imports.

9. Provisional source Escort now verifies both the player approach and the
   complete survivor-to-destination route. A real CharacterBody3D follows
   floor-checked waypoints, stops at a new obstruction and replans only a
   complete viable route. Per-wave plans are cleared and destinations reserved.
   Native tests cover a blocked/reopened corner and three-metre up/down ramps,
   grounded arrivals and one-time rewards. These are not original coordinates.
10. H toggles an optional, off-by-default walking-route guide. F4 remains the
    existing floorplan overlay. Guidance follows the real remaining materials,
    switches to the correct depot while carrying, and retires at completion or
    disabled controls. It never walks, collects, spends inventory or pays rewards
    for the player. A real-rendered ten-item E-key Load circuit verifies it.
11. Load uses a low-profile noncolliding staging deck instead of the opaque
    green box. Bolter recovery retains gravity and waits for a grounded start
    before another leap; the regression reproduces suspension before the fix.
12. Generated metadata synchronization tracks owned-file hashes, preserves
    unknown/private installed assets, and backs up edited metadata outside the
    game project before replacement. Unchanged stale generated files alone may
    be retired. Public packaging still refuses private assets; preservation does
    not permit their publication.

## Verification scope

The distribution requires all SIX targeted native checks (the original input,
fortification and material circuits plus Bolter grounding, complete Escort and
optional guidance), all nine repeated shutdown groups, the inherited 34-check
matrix, and THREE real-renderer checks including guided loading. Exact-commit
receipts and a clean-directory packaged launch are mandatory. Earlier counts
alone cannot authorize this newer release.

Complete Python/content checks, native Windows export, production input and
pickup/fortification/supply lifecycles, repeated Ranch/Expressway/Manor exits,
and the inherited 34-check native matrix are in the workflow. Mesa/OpenGL
checks inspect actual imported mesh transforms and produce real screenshots.
Exact receipts accompany the run; screenshots are not proof of retail parity.

## Private original Laboratory inspection (separate from public release)

Local Linux tests used the user's actual TestPlace XML, with 34,268 source
geometry records and 18,130 collision shapes. The original player grounded,
six natural pickups appeared, and about 6.9 metres of actual keyboard-driven
walking remained supported. The first wave spawned actual infected.

A local engine-physics-derived navigation graph (41,098 nodes, 144,048 edges,
167 regions) was encoded in the existing nav31 format, bound to the exact
locally serialized scene SHA. Existing loader validation accepted it normally;
no strict nav41/nav42 or private-installer SHA pins were relaxed. All original
infected markers remain disconnected from the player's floor region. The
existing explicit F9 assistance mode did enable connected spawns, route
search/cache use, and enemies approaching and killing the stationary player.
That is a verified assisted gameplay path, NOT original spawn/pathfinding parity.

The earlier source-map inspection ran on 91dd5b9. A subsequent local Linux
traversal test of the source-traversal contribution completed Load plus large
Repair in that same private scene: 22 actual E-key deposits, two mission
receipts, about 98.5m of keyboard-driven walking, no position teleports, and
idempotent rewards. This was isolated mission verification, not a survival
match or a Windows test of private assets. The existing weapon extractor also
recovered the authenticated source assembly pack (98 original tools, 91 catalog
weapons covered); original mesh triangle bytes remain absent. No private map,
weapon assembly pack, source coordinates or navigation bytes entered GitHub.

An additional local Linux original-Laboratory test during objective-playability
work completed a provisional Escort with a real player approach, grounded
survivor movement, exactly one mission receipt and no mid-motion position
assignment. This was NOT a Windows private-asset test and did not connect the
original infected entries. Private data and diagnostic files remain outside
public Git history. See local validation receipts for its exact assembly hash;
the Windows package contains only the source-safe reconstructed game.

## Remaining gaps / next concrete task

- Recover original custom mesh and texture binaries: the inspected Laboratory
  references 489 unresolved mesh IDs and 59 unresolved texture IDs.
- Resolve the source traversal breaks connecting original exterior infected
  entries to the player's region; F9 assistance is not the default solution.
- Continue original-scene Escort coverage across more locations and recovered
  stairways, and improve Damage-objective layout. The new Escort controller
  passes synthetic corners/ramps and an isolated original-Laboratory mission,
  not every original route or a full survival match. Provisional coordinates
  and reconstruction gaps must remain explicitly labeled.
- Visually compare remaining maps, source lighting, first-person wall clipping,
  animation timing and original equipment dimensions against references.
- Address non-fatal audio/resource cleanup warnings still emitted by some
  inherited native tests rather than claiming zero leaks.
- Perform full human-operated real-time fifteen-wave original-map survival.

The retained source-safe Windows build excludes the privately inspected map.
Do not report full 1:1 completion or extrapolate percentages from pass numbers.
{END}

---

'''
    canonical.write_text(section + old, encoding='utf8', newline='\n')
    print('CONTINUATION_RECORD_UPDATED tested_source=' + expected)


if __name__ == '__main__':
    main()
