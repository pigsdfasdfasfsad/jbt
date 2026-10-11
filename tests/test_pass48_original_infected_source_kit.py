"""Pass48 original infected AI-asset kit integration and truthful R6 proxy limits."""
from __future__ import annotations
import gzip,hashlib,json,subprocess,sys
from pathlib import Path
from zipfile import ZipFile
from lxml import etree
from conftest import ROOT

EXTRACTOR=ROOT/'tools/maps/extract_original_infected_assets48.py'
FAKE=ROOT/'tools/maps/make_pass48_infected_smoke_fixture.py'

def _item(parent, name, cls='Folder'):
    item=etree.SubElement(parent,'Item',attrib={'class':cls})
    prop=etree.SubElement(item,'Properties')
    etree.SubElement(prop,'string',name='Name').text=name
    return item

def _source_part(parent,name,offset):
    element=_item(parent,name,'MeshPart')
    props=element.find('Properties')
    cframe=etree.SubElement(props,'CoordinateFrame',name='CFrame')
    values={'X':str(offset),'Y':'2','Z':'0'}
    for a in range(3):
        for b in range(3):
            values['R'+str(a)+str(b)]=str(int(a==b))
    for key,value in values.items():etree.SubElement(cframe,key).text=value
    size=etree.SubElement(props,'Vector3',name='size')
    for key,v in (('X','1'),('Y','1'),('Z','1')):
        etree.SubElement(size,key).text=v
    etree.SubElement(props,'Color3uint8',name='Color3uint8').text='4283802418'
    etree.SubElement(props,'float',name='Transparency').text='0'
    etree.SubElement(etree.SubElement(props,'Content',name='MeshId'),
                     'url').text='rbxassetid://9999999999'
    return element

def _original_like_archive(tmp_path):
    sys.path.insert(0,str(ROOT/'tools/maps'))
    import extract_original_infected_assets48 as kit
    root=etree.Element('roblox')
    storage=_item(root,'ReplicatedStorage','ReplicatedStorage')
    assets=_item(storage,'Assets');ai=_item(assets,'AI')
    infected=_item(ai,'Infected')
    all_groups=sorted({g for specs in kit.PLANS.values()
                         for variant in specs for g,_ in variant})
    cache={'':infected}
    for group in all_groups:
        parts=group.split('/')
        for n in range(1,len(parts)+1):
            path='/'.join(parts[:n])
            if path not in cache:
                parent=cache['/'.join(parts[:n-1])]
                cache[path]=_item(parent,parts[n-1], 'Model' if n==len(parts) else 'Folder')
        leaf=cache[group]
        handle=_item(leaf,'Handle','Part')
        props=handle.find('Properties')
        cf=etree.SubElement(props,'CoordinateFrame',name='CFrame')
        for key,value in {'X':'0','Y':'2','Z':'0',
          **{'R'+str(i)+str(j):str(int(i==j)) for i in range(3) for j in range(3)}}.items():
            etree.SubElement(cf,key).text=value
        size=etree.SubElement(props,'Vector3',name='size')
        for key in ('X','Y','Z'):etree.SubElement(size,key).text='1'
        etree.SubElement(props,'float',name='Transparency').text='1'
        _source_part(leaf,'SourceEyePiece',.15)
    archive=tmp_path/'fake-place.zip'
    with ZipFile(archive,'w') as zipout:
        zipout.writestr('TestPlace/TestPlace.rbxlx',etree.tostring(root))
    return archive,len(all_groups)

def test_inert_xml_original_kit_extraction_is_deterministic_and_bounded(tmp_path):
    archive,n=_original_like_archive(tmp_path)
    first=tmp_path/'one'/'InfectedSourceVariants.json.gz'
    second=tmp_path/'two'/'InfectedSourceVariants.json.gz'
    def run(output,fixture=True):
        cmd=[sys.executable,str(EXTRACTOR),'--archive',str(archive),
             '--out',str(output)]
        if fixture:cmd.append('--synthetic')
        return subprocess.run(cmd,capture_output=True,text=True)
    assert run(first).returncode==0
    assert run(second).returncode==0
    assert first.read_bytes()==second.read_bytes()
    data=json.loads(gzip.decompress(first.read_bytes()))
    assert data['synthetic'] is True
    assert data['original_animation_tracks_restored'] is False
    assert data['original_cloud_mesh_triangles_restored'] is False
    assert len(data['types'])==8
    assert sum(len(v) for v in data['types'].values())==15
    for variants in data['types'].values():
        for variant in variants:
            assert sum(p.get('reconstructed_r6_proxy',False) for p in variant['parts'])==6
            assert any(p.get('source_authored',False) for p in variant['parts'])
            assert all(max(abs(x) for x in p['t'])<8 for p in variant['parts'])
    manifest=json.loads((first.parent/'SOURCE_INFECTED_ASSET_KIT_MANIFEST48.json').read_text())
    assert manifest['types_with_sourced_accessories']==8
    assert manifest['source_group_count']==n
    assert manifest['unique_original_visible_accessory_parts']==n
    assert manifest['source_combination_is_a_reconstruction'] is True
    assert manifest['original_source_snapshot_rigs_restored'] is False
    assert manifest['compressed_pack_sha256']==hashlib.sha256(first.read_bytes()).hexdigest()
    # An arbitrary file cannot pose as the user's original recovered place.
    assert run(tmp_path/'forged.gz',fixture=False).returncode!=0

def test_fabricated_infected_asset_pack_does_not_overwrite_real_user_data(tmp_path):
    target=tmp_path/'Content'/'Enemies'/'InfectedSourceVariants.json.gz'
    def go(mode):
        return subprocess.run([sys.executable,str(FAKE),
             '--output',str(target),mode],capture_output=True,text=True)
    assert go('--create').returncode==0
    d=json.loads(gzip.decompress(target.read_bytes()))
    assert d['synthetic'] is True and len(d['types'])==8
    assert sum(len(v) for v in d['types'].values())==15
    for variants in d['types'].values():
        for variant in variants:
            assert len(variant['parts'])==8
            assert sum(p['source_authored'] for p in variant['parts'])==2
            assert sum(bool(p.get('meshId')) for p in variant['parts'])==1
    assert go('--create').returncode!=0
    assert go('--clean').returncode==0
    assert not target.exists()
    target.write_bytes(gzip.compress(json.dumps({
        'format':'twr-source-infected-variants-v1','synthetic':False,
        'generation':'untrusted','types':{}}).encode()))
    assert go('--clean').returncode!=0
    assert target.exists()

def test_original_pack_is_sha_pinned_and_proxy_source_counts_exposed():
    runtime=(ROOT/'src/Twr.Godot/Scripts/InfectedSourceModelRuntime.cs').read_text()
    visual=(ROOT/'src/Twr.Godot/Scripts/InfectedVisualAssembler.cs').read_text()
    game=(ROOT/'src/Twr.Godot/Scripts/GameplayRoot.cs').read_text()
    hud=(ROOT/'src/Twr.Godot/Scripts/Pass30DiagnosticsHud.cs').read_text()
    assert '9ef29ce243992722dbab0ea5f5f78872ea4a6b5da4718c925662a34cfb743d64' in runtime
    assert '272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2' in runtime
    assert 'Infected source pack outside size budget' in runtime
    assert 'Infected source expansion exceeds budget' in runtime
    assert 'Synthetic infected pack outside smoke mode' in runtime
    assert 'LastSourceAssetParts' in runtime
    assert 'LastReconstructedR6Parts' in runtime
    assert 'LastUnresolvedMeshProxies' in runtime
    assert 'if (!float.IsFinite(opacity) || opacity <= .001f)' in runtime
    assert 'VerifiedOwnerSourceAccessoryKit' in visual
    assert 'Pass48RecoveredInfectedAccessoryParts' in game
    assert 'Pass48ReconstructedInfectedR6Parts' in game
    assert 'pass48_original_infected_accessory_parts' in hud
    assert 'pass48_reconstructed_infected_r6_parts' in hud

def test_native_windows_plays_all_eight_synthetic_infected_and_cleans_pack():
    bootstrap=(ROOT/'src/Twr.Godot/Scripts/Bootstrap.cs').read_text()
    ci=(ROOT/'.github/workflows/windows-build.yml').read_text()
    assert '--smoke-pass48' in bootstrap and '--smoke-pass48' in ci
    assert 'TWR_SMOKE_PASS48_INFECTED_OK' in bootstrap
    assert 'TWR_SMOKE_PASS48_INFECTED_OK' in ci
    assert 'SourceAccessoryPartCount!=2' in bootstrap
    assert 'ReconstructedR6BodyParts != 6' in bootstrap
    assert 'actual_infected_actor=true' in bootstrap
    assert 'make_pass48_infected_smoke_fixture.py' in ci
    assert 'TWR-Pass48-Windows-x64-NoPrivateAssets' in ci
    assert 'twr-pass48-infected-source-asset-kit' in ci
    assert 'InfectedSourceVariants.json.gz' in ci
