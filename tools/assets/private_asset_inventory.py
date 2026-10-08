#!/usr/bin/env python3
"""Verify a private, owner-provided Laboratory asset installation.

Never attempts to download Roblox assets. All source asset identifiers,
local-file hashes and missing lists remain in a private local JSON report.
The public repository contains no original source asset bytes.
"""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path

PNG_SIGNATURE = b'\x89PNG\r\n\x1a\n'

def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1 << 20), b''):
            digest.update(chunk)
    return digest.hexdigest()

def inspect(manifest: Path, assets_root: Path) -> dict:
    source = json.loads(manifest.read_text(encoding='utf-8'))
    if source.get('format') != 'twr-laboratory-assets-v1':
        raise ValueError('Unexpected owner-held Laboratory asset manifest')
    refs = source['references']
    report = {
        'format': 'twr-private-asset-inventory-v1',
        'map': 'Laboratory',
        'source_sha256': source.get('source_sha256'),
        'raw_meshes_available_in_place_export': False,
        'original_union_csg_render_binaries_available': False,
        'asset_root': str(assets_root.resolve()),
        'resources': {}
    }
    for category, source_type, folder, ext in (
        ('meshes','MeshId','Meshes','.res'),
        ('textures','TextureID','Textures','.png'),
    ):
        inventory = []
        values = refs.get(source_type, {})
        for asset_id, references in sorted(values.items(), key=lambda item: int(item[0])):
            if not asset_id.isascii() or not asset_id.isdecimal():
                raise ValueError('Invalid source asset identifier in manifest')
            path = assets_root / folder / (asset_id + ext)
            installed = path.is_file() and not path.is_symlink() and path.stat().st_size > 0
            if installed and ext == '.png':
                with path.open('rb') as stream:
                    installed = stream.read(8) == PNG_SIGNATURE
            if installed and ext == '.res':
                with path.open('rb') as stream:
                    installed = stream.read(4) == b'RSRC'
            inventory.append({
                'asset_id': asset_id,
                'reference_count': int(references),
                'installed': bool(installed),
                'bytes': path.stat().st_size if installed else 0,
                'sha256': sha256(path) if installed else None,
            })
        report['resources'][category] = {
            'required': len(inventory),
            'installed': sum(row['installed'] for row in inventory),
            'missing': sum(not row['installed'] for row in inventory),
            'items': inventory,
        }
    report['fully_resolved'] = all(
        entry['missing'] == 0 for entry in report['resources'].values()
    ) and report['original_union_csg_render_binaries_available']
    return report


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--manifest', type=Path, required=True)
    parser.add_argument('--assets-root', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--require-complete', action='store_true')
    args = parser.parse_args()
    report = inspect(args.manifest, args.assets_root)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print('TWR_PRIVATE_ASSET_INVENTORY_OK ' + ' '.join(
        f"{name}={r['installed']}/{r['required']}" for name, r in report['resources'].items()
    ))
    if args.require_complete and not report['fully_resolved']:
        raise SystemExit('Original mesh/texture/union asset set remains incomplete.')


if __name__ == '__main__':
    main()
