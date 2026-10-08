#!/usr/bin/env python3
"""Audit a supplied Roblox XML place without executing Luau or exporting asset bytes.

The report contains aggregate source-tree counts only. Original meshes, IDs,
script source, place files, and media must remain outside the public repository.
"""
from __future__ import annotations
import argparse
import collections
import json
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

MAPS = ('Ranch', 'Mill', 'Bypass', 'Cabin', 'Cargo', 'District',
        'Expressway', 'Prison', 'Laboratory', 'Manor')
RENDERABLE = frozenset(('Part', 'MeshPart', 'UnionOperation', 'WedgePart',
                        'CornerWedgePart', 'TrussPart'))

def iter_instances(stream):
    elements = []
    for event, elem in ET.iterparse(stream, events=('start', 'end')):
        if elem.tag != 'Item':
            continue
        if event == 'start':
            elements.append(elem)
            continue
        props = elem.find('Properties')
        names = [] if props is None else props.findall('./string[@name="Name"]')
        name = (names[0].text if names else None) or elem.get('class', 'Unknown')
        mesh = bool(props is not None and any(
            p.get('name') in ('MeshId', 'MeshID') and
            ''.join(p.itertext()).strip() for p in props
        ))
        yield {
            'ref': elem.get('referent'),
            'parent': elements[-2].get('referent') if len(elements) > 1 else None,
            'name': name,
            'class': elem.get('class', 'Unknown'),
            'references_mesh': mesh,
        }
        elements.pop()
        if elements:
            elements[-1].remove(elem)
        elem.clear()

def summarize(records):
    nodes = {r['ref']: r for r in records}
    children = collections.defaultdict(list)
    for r in records:
        children[r['parent']].append(r['ref'])

    def child(parent, name):
        found = [nodes[r] for r in children[parent] if nodes[r]['name'] == name]
        if len(found) != 1:
            raise ValueError(f'Missing or ambiguous source child {name!r}: {len(found)}')
        return found[0]['ref']

    def maybe_child(parent, name):
        if parent is None:
            return None
        found = [nodes[r] for r in children[parent] if nodes[r]['name'] == name]
        if len(found) > 1:
            raise ValueError(f'Ambiguous source child {name!r}: {len(found)}')
        return found[0]['ref'] if found else None

    def descendants(ref):
        if ref is None:
            return []
        todo = [ref]
        out = []
        while todo:
            key = todo.pop()
            out.append(nodes[key])
            todo.extend(children[key])
        return out

    replicated = child(None, 'ReplicatedStorage')
    cmaps = maybe_child(replicated, 'CMaps')
    ws = child(None, 'Workspace')
    loaded = descendants(maybe_child(ws, 'Map'))
    result = {
        'format': 'twr-source-coverage-v1',
        'authority': 'static Roblox place hierarchy',
        'contains_original_asset_bytes': False,
        'workspace_map_contains_microscope': any(
            r['name'] == 'Microscope' for r in loaded),
        'workspace_map_renderables': sum(r['class'] in RENDERABLE for r in loaded),
        'maps': {},
    }
    for name in MAPS:
        ref = maybe_child(cmaps, name)
        map_ref = maybe_child(ref, 'Map')
        walls_ref = maybe_child(maybe_child(ref, 'Walls'), 'Server Walls')
        source = descendants(map_ref)
        walls = descendants(walls_ref)
        types = collections.Counter(r['class'] for r in source if r['class'] in RENDERABLE)
        result['maps'][name] = {
            'stored_source_present': ref is not None,
            'stored_map_renderables': sum(types.values()),
            'stored_map_types': dict(sorted(types.items())),
            'stored_map_mesh_references': sum(r['references_mesh'] for r in source),
            'server_wall_shapes': sum(r['class'] in RENDERABLE for r in walls),
            'has_lighting_source_module': any(
                nodes[x]['name'] == 'Lighting' and nodes[x]['class'] == 'ModuleScript'
                for x in children[ref] if ref is not None),
            'note': 'Stored scene fragments are not proven to be a complete rendered map.',
        }
    return result

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--archive', type=Path, required=True)
    parser.add_argument('--member', default='TestPlace/TestPlace.rbxlx')
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if zipfile.is_zipfile(args.archive):
        with zipfile.ZipFile(args.archive) as archive:
            with archive.open(args.member) as stream:
                report = summarize(list(iter_instances(stream)))
    else:
        with args.archive.open('rb') as stream:
            report = summarize(list(iter_instances(stream)))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2, sort_keys=True) + '\n',
                           encoding='utf-8')
    present = sum(x['stored_source_present'] for x in report['maps'].values())
    print(f'TWR_SOURCE_COVERAGE_OK maps={present} '
          f'workspace_renderables={report["workspace_map_renderables"]}')

if __name__ == '__main__':
    main()
