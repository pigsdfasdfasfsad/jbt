#!/usr/bin/env python3
"""Append verified item/fortification markers to an owner-held Laboratory pack.

The Roblox XML is parsed inertly. No Luau is run, no network is used, and no
private source geometry or original asset bytes are written to Git.
"""
from __future__ import annotations
import argparse
import gzip
import hashlib
import json
import math
import os
from pathlib import Path
import xml.etree.ElementTree as ET
import zipfile

ROOTS = {
    'ItemPickupBox': ('Item', ('Workspace', 'Ignore', 'Spawn Boxes', 'Item Spawn Boxes')),
    'FortificationPickupBox': ('Fortification', ('Workspace', 'Ignore', 'Spawn Boxes', 'Fortification Spawn Boxes')),
}


def _position(element):
    props = element.find('Properties')
    if props is None:
        return None
    for prop in props:
        if prop.get('name') != 'CFrame':
            continue
        coords = {child.tag: child.text for child in prop}
        try:
            value = [float(coords[key]) for key in ('X', 'Y', 'Z')]
            if all(math.isfinite(v) for v in value):
                return value
        except (KeyError, TypeError, ValueError):
            return None
    return None


def _markers(source, member):
    hierarchy = {}
    candidates = []
    with zipfile.ZipFile(source) as archive, archive.open(member) as stream:
        stack = []
        for event, elem in ET.iterparse(stream, events=('start', 'end')):
            if elem.tag != 'Item':
                continue
            if event == 'start':
                stack.append(elem)
                continue
            ref = elem.get('referent')
            props = elem.find('Properties')
            name_node = None if props is None else props.find('./string[@name="Name"]')
            name = name_node.text if name_node is not None else elem.get('class', '')
            parent = stack[-2].get('referent') if len(stack) > 1 else None
            hierarchy[ref] = (name, parent)
            if name in ROOTS and elem.get('class') == 'Part':
                xyz = _position(elem)
                if xyz is None:
                    raise ValueError('Pickup marker missing CFrame: ' + str(ref))
                candidates.append((ref, name, parent, xyz))
            stack.pop()
            if stack:
                stack[-1].remove(elem)
            elem.clear()

    def parents(ref):
        result = []
        seen = set()
        while ref is not None:
            if ref in seen:
                raise ValueError('cyclic Roblox hierarchy')
            seen.add(ref)
            name, ref = hierarchy[ref]
            result.append(name)
        return tuple(reversed(result))

    records = []
    for ref, name, parent, pos in candidates:
        group, expected_path = ROOTS[name]
        if parents(parent) != expected_path:
            raise ValueError('Unexpected pickup marker path: ' + str(parents(parent)))
        records.append({'kind': 'pickup_spawn', 'group': group,
                        'ref': ref, 't': pos})
    records.sort(key=lambda r: (r['group'], r['ref']))
    actual = {g: sum(r['group'] == g for r in records) for g in ('Item', 'Fortification')}
    if actual != {'Item': 127, 'Fortification': 47}:
        raise ValueError('Unexpected original marker counts: ' + str(actual))
    return records


def augment(source_zip: Path, original_pack: Path, dest: Path,
            member: str = 'TestPlace/TestPlace.rbxlx') -> dict:
    markers = _markers(source_zip, member)
    with zipfile.ZipFile(source_zip) as archive, archive.open(member) as stream:
        sha = hashlib.sha256()
        for chunk in iter(lambda: stream.read(1 << 20), b''):
            sha.update(chunk)
    with gzip.open(original_pack, 'rt', encoding='utf-8') as original:
        header = json.loads(next(original))
        if header.get('format') != 'twr-laboratory-scene-v1' or header.get('map') != 'Laboratory':
            raise ValueError('Wrong source pack')
        if sha.hexdigest() != header.get('source_sha256'):
            raise ValueError('Wrong Roblox place snapshot: pack source_sha256 mismatch')
        header['counts']['item_markers'] = 127
        header['counts']['fortification_markers'] = 47
        dest.parent.mkdir(parents=True, exist_ok=True)
        temp = dest.with_suffix(dest.suffix + '.tmp')
        try:
            with temp.open('wb') as output, gzip.GzipFile(filename='', mode='wb',
                                        fileobj=output, compresslevel=6, mtime=0) as packed:
                packed.write((json.dumps(header, separators=(',', ':')) + '\n').encode())
                for line in original:
                    entry = json.loads(line)
                    if entry.get('kind') == 'pickup_spawn':
                        continue
                    packed.write((json.dumps(entry, separators=(',', ':')) + '\n').encode())
                for record in markers:
                    packed.write((json.dumps(record, separators=(',', ':')) + '\n').encode())
            os.replace(temp, dest)
        finally:
            temp.unlink(missing_ok=True)
    return {'source_sha256': sha.hexdigest(), 'item_markers': 127,
            'fortification_markers': 47, 'bytes': dest.stat().st_size}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--archive', required=True, type=Path)
    parser.add_argument('--source-pack', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--member', default='TestPlace/TestPlace.rbxlx')
    args = parser.parse_args()
    result = augment(args.archive, args.source_pack, args.output, args.member)
    print('TWR_LAB_PICKUP_MARKERS_OK ' + json.dumps(result, sort_keys=True))


if __name__ == '__main__':
    main()
