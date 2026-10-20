"""Recover reviewed camera-relative model fitting without reverting newer ballistics.

A source-only patch is applied atomically after exact digest/path validation.
The marker makes later builds idempotent; stale/partial edits fail closed.
"""
from pathlib import Path
import gzip
import hashlib
import io
import json
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
DIGEST = '6f018d436d11c9444dd7715f30604b6f2db454dc3c6494bb238b2b4ccb1a154b'
COMPRESSED_DIGEST = '6b21920c9962cf379d2e812b700bc249798ded2d3263337f5f4c5d2b618025eb'
PATCH_BYTES = 33735
COMPRESSED_BYTES = 10132
PAYLOAD = 'tools/dev/held_clearance/update.patch.gz'
MARKER = 'tools/dev/held_clearance/integrated.json'
ALLOWED = {
    'src/Twr.Godot/Scripts/Bootstrap.cs',
    'src/Twr.Godot/Scripts/FirstPersonPlayer.cs',
    'src/Twr.Godot/Scripts/HeldModelClearance.cs',
    'src/Twr.Godot/Scripts/HeldModelClearanceAcceptance.cs',
    'src/Twr.Godot/Scripts/LocalSessionNode.cs',
    'src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs',
    'tests/test_held_clearance_release.py',
    'tests/test_pass67_source_sights.py',
    'tests/test_tanker_audio_completion.py',
    'tools/build/package_source_safe.py',
    'tools/dev/write_release_handoff.py',
    'tools/validation/run_graphics_validation.py',
    'tools/validation/run_source_fidelity.py',
}


def apply(root: Path = ROOT) -> None:
    for name in ALLOWED | {PAYLOAD, MARKER}:
        target = root / name
        for part in (target, *target.parents):
            if part == root:
                break
            if part.is_symlink():
                raise ValueError('Linked clearance integration path')
    data = (root / PAYLOAD).read_bytes()
    if len(data) != COMPRESSED_BYTES or hashlib.sha256(data).hexdigest() != COMPRESSED_DIGEST:
        raise ValueError('Compressed clearance patch integrity failure')
    with gzip.GzipFile(fileobj=io.BytesIO(data)) as source:
        patch = source.read(PATCH_BYTES + 1)
    if len(patch) != PATCH_BYTES or hashlib.sha256(patch).hexdigest() != DIGEST:
        raise ValueError('Clearance patch integrity failure')
    entries = re.findall(rb'^diff --git a/(.+) b/(.+)$', patch, re.M)
    if len(entries) != len(ALLOWED) or any(a != b for a,b in entries):
        raise ValueError('Unexpected clearance patch entries')
    if {a.decode('utf8') for a,b in entries} != ALLOWED:
        raise ValueError('Unreviewed clearance patch path')
    marker = root / MARKER
    if marker.exists():
        if json.loads(marker.read_text(encoding='utf8')).get('patch_sha256') != DIGEST:
            raise ValueError('Invalid clearance integration marker')
        print('HELD_CLEARANCE_ALREADY_INTEGRATED source_only=true')
        return
    reverse = subprocess.run(['git','apply','--reverse','--check','-'],
        cwd=root,input=patch,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if reverse.returncode != 0:
        subprocess.run(['git','apply','--check','-'],cwd=root,input=patch,check=True)
        subprocess.run(['git','apply','-'],cwd=root,input=patch,check=True)
    marker.parent.mkdir(parents=True,exist_ok=True)
    marker.write_text(json.dumps({'patch_sha256':DIGEST,
        'baseline':'e84e0ec1c8c83881b53f3dfc76e257af6f0be710',
        'private_assets_included':False},indent=2)+'\n',encoding='utf8')
    print('HELD_CLEARANCE_INTEGRATED source_only=true')


if __name__ == '__main__':
    apply()
