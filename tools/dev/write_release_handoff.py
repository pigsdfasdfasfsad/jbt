"""Update the existing continuation record from exact successful CI receipts.

The status-only commit is not presented as a separately compiled executable.
Historical development records are preserved below the current checkpoint.
"""
from __future__ import annotations
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
# CURRENT CHECKPOINT — SOURCE FIDELITY AND RETAINED WINDOWS RELEASE

**Development branch:** `twr-source-fidelity-release`, continued from
`twr-runtime-stabilization` / `18bec9515d2d44020e8c402f9eec0800af34feba`.

**Exact compiled and tested source commit:** `{expected}`.
This status update is a subsequent documentation-only commit; it does not change
which source revision was executed and packaged.

**Verification and retained artifacts:**
https://github.com/pigsdfasdfasfsad/jbt/actions/runs/{run_id}

**Windows package:** `{receipt['archive']}`
- Archive bytes: {receipt['size_bytes']}.
- SHA-256: `{receipt['sha256']}`.
- Complete companion files and bundled .NET runtime included.
- Clean-directory native launch passed with system SDK paths removed.
- Original private maps, mesh binaries, texture pixels, and test profiles excluded.
- This is the existing reconstructed-map game, NOT a completed original-asset 1:1 release.

## Implemented player-visible changes

1. Original map importer now treats built-in SpecialMesh Scale as relative to
   parent Part.Size, distinguishes Brick/Sphere/Wedge/Cylinder render batches,
   and places cylinder axes correctly in source local X. Parent-part collision
   sizes remain unchanged. FileMesh vertices and upload-space bounds are still
   unresolved where no original binary is installed.
2. Deployed Barbed Wire, Clap Bomb, Jack, and 50 Cal no longer use generic boxes.
   Wire uses an instanced coil/barb batch; mounted guns show aiming, ammunition,
   recoil and brief muzzle feedback, with existing damage authority retained.
3. Jack uses the reference-described approximately ten-second fuse instead of
   proximity triggering. Peak damage/radius remain script-catalog values.
   Linear distance falloff is explicitly reconstructed, not a recovered curve.
4. A viewmodel-only light makes hands/weapons readable without changing the
   map's ordinary lighting layer, aim rays, ammunition, or world collision.
5. A strict packaging path checks exact-commit regression receipts, rejects
   private/test content, launches the bundled package from a clean directory,
   hashes all distributed files, and retains a downloadable Windows archive.

## Verification scope

The workflow contains complete Python/content checks, native Windows export,
production startup/input/equipment tests, repeated Ranch/Expressway/Manor exits,
and the inherited 34-check native matrix. Real-renderer Linux Mesa/OpenGL tests
also inspect actual imported mesh transforms and capture genuine gameplay and
deployed-model screenshots. Their exact receipts accompany the run.

A separate local-only inspection used the user's actual TestPlace XML with the
compiled c82a6d8187bf697b37e382ebfd30fc0c690ccb5f QA harness: 34,268 geometry records,
18,130 collision shapes, valid original player grounding, six supported pickups,
and approximately 6.9 metres of ordinary keyboard-driven grounded movement.
That original-scene test was Linux-only, did not validate a complete match, and
used a locally serialized v2 pack rather than replacing the strict Pass117
private-installer/cache hash pins. No source coordinates or assets are included
in this public status record.

## Remaining gaps and next concrete task

- Actual Laboratory still lacks 489 referenced mesh IDs and 59 referenced
  texture IDs in the inspected original snapshot. Visible placeholders remain.
- The local original pack has no matching installed navigation/floorplan cache;
  existing SHA checks correctly refuse mismatched serialized scene versions.
- Other original maps need source-by-source geometry and traversal verification.
- The supply pallet still permits its historical configured-height fallback
  without a verified floor; next implementation should fix the complete
  call/drop/physical-landing/eight-pickup lifecycle and its failure cleanup.
- Mounted visuals, source lighting, weapon wall clipping, and missing animation
  data need further comparison with the references.
- Some inherited tests report non-fatal audio/resource cleanup warnings; retain
  these as open issues rather than claiming zero leaks.
- No complete human-operated real-time fifteen-wave original-map survival run,
  original pixel fidelity, or full 1:1 completion is established.

Private source files remain outside public Git history and release artifacts.
{END}

---

'''
    canonical.write_text(section + old, encoding='utf8', newline='\n')
    print('CONTINUATION_RECORD_UPDATED tested_source=' + expected)


if __name__ == '__main__':
    main()
