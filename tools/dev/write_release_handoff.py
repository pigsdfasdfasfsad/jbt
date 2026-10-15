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

Prior retained working binary: `91dd5b92485e466b50b042aa20680ece2f55f942`,
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

## Verification scope

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

These private tests ran on source commit 91dd5b92485e466b50b042aa20680ece2f55f942,
not the new Windows executable, and did not validate a complete real-time match.
The private map and navigation files were never uploaded to public GitHub.

## Remaining gaps / next concrete task

- Recover original custom mesh and texture binaries: the inspected Laboratory
  references 489 unresolved mesh IDs and 59 unresolved texture IDs.
- Resolve the source traversal breaks connecting original exterior infected
  entries to the player's region; F9 assistance is not the default solution.
- Recover or physically route provisional source objective locations beyond
  the current conservative straight-corridor policy, then test full material
  pickup/deposit and Escort routes in the actual original scene.
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
