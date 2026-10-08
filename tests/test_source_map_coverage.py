"""No-network synthetic tests for source map coverage accounting."""
import importlib.util
import io
import xml.etree.ElementTree as ET
from conftest import ROOT

SPEC = importlib.util.spec_from_file_location(
    'twr_map_audit', ROOT / 'tools/maps/audit_map_sources.py')
AUDIT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(AUDIT)

def test_source_map_audit_all_ten_present():
    document = ET.Element('roblox')
    number = 0
    def item(parent, cls, name):
        nonlocal number
        number += 1
        node = ET.SubElement(parent, 'Item',
                             {'referent': f'S{number}', 'class': cls})
        props = ET.SubElement(node, 'Properties')
        ET.SubElement(props, 'string', {'name': 'Name'}).text = name
        return node
    workspace = item(document, 'Workspace', 'Workspace')
    loaded = item(workspace, 'Folder', 'Map')
    item(loaded, 'MeshPart', 'Microscope')
    replicated = item(document, 'ReplicatedStorage', 'ReplicatedStorage')
    cmaps = item(replicated, 'Folder', 'CMaps')
    for name in AUDIT.MAPS:
        root = item(cmaps, 'Folder', name)
        item(root, 'ModuleScript', 'Lighting')
        map_root = item(root, 'Folder', 'Map')
        item(map_root, 'MeshPart', 'Mesh')
        walls = item(root, 'Folder', 'Walls')
        item(item(walls, 'Folder', 'Server Walls'), 'Part', 'Wall')
    report = AUDIT.summarize(list(AUDIT.iter_instances(
        io.BytesIO(ET.tostring(document)))))
    assert report['workspace_map_renderables'] == 1
    assert report['workspace_map_contains_microscope']
    assert len(report['maps']) == 10
    assert all(x['stored_source_present'] for x in report['maps'].values())
    assert all(x['stored_map_renderables'] == 1 for x in report['maps'].values())
    assert all(x['server_wall_shapes'] == 1 for x in report['maps'].values())
    assert not report['contains_original_asset_bytes']

def test_missing_cmaps_does_not_imply_missing_loaded_scene():
    xml = (
        '<roblox><Item class="Workspace" referent="w"><Properties>'
        '<string name="Name">Workspace</string></Properties>'
        '<Item class="Folder" referent="m"><Properties>'
        '<string name="Name">Map</string></Properties></Item></Item>'
        '<Item class="ReplicatedStorage" referent="s"><Properties>'
        '<string name="Name">ReplicatedStorage</string></Properties>'
        '</Item></roblox>'
    )
    report = AUDIT.summarize(list(AUDIT.iter_instances(
        io.BytesIO(xml.encode()))))
    assert report['workspace_map_renderables'] == 0
    assert not any(x['stored_source_present'] for x in report['maps'].values())
