"""Do not confuse disposable synthetic native smoke fixtures with owner art."""
import gzip
import hashlib
import json
import struct
import subprocess
import sys

from conftest import ROOT


def test_disposable_sidecars_are_bound_to_one_synthetic_scene(tmp_path):
    builder = ROOT / 'tools/validation/make_pass30_synthetic_sidecars.py'
    subprocess.run([sys.executable, str(builder), '--output', str(tmp_path), '--create'],
                   check=True, capture_output=True)
    scene = tmp_path / 'Content/Maps/Laboratory.scene.jsonl.gz'
    assert scene.is_file()
    digest = hashlib.sha256(scene.read_bytes()).digest()
    with gzip.open(scene, 'rt') as stream:
        header = json.loads(next(stream))
    assert header['synthetic'] is True
    assert header['format'] == 'twr-source-map-v2'
    assert header['counts']['geometry'] == 16
    nav = gzip.decompress((tmp_path / 'Content/Navigation/Laboratory.nav25.gz').read_bytes())
    assert nav.startswith(b'TWRNAV25') and nav[20:52] == digest
    col = gzip.decompress((tmp_path / 'Content/Collision/Laboratory.col26.gz').read_bytes())
    assert col.startswith(b'TWRCOL26') and col[24:56] == digest
    geo = gzip.decompress((tmp_path / 'Content/Geometry/Laboratory.native28.gz').read_bytes())
    assert geo.startswith(b'TWRINS28') and geo[20:52] == digest
    visual = json.loads((tmp_path / 'Content/Art/visuals.json').read_text())
    assert len(visual['map_cards']) == 10
    assert visual['weapon_icons']['glock17']['file'] == 'WeaponIcons/G17-UI.png'
    # A second invocation must never silently replace owner content.
    second = subprocess.run([sys.executable, str(builder), '--output', str(tmp_path), '--create'],
                            capture_output=True, text=True)
    assert second.returncode != 0
    subprocess.run([sys.executable, str(builder), '--output', str(tmp_path), '--clean'],
                   check=True, capture_output=True)
    assert not scene.exists()
    assert not (tmp_path / 'Content/Art/visuals.json').exists()
