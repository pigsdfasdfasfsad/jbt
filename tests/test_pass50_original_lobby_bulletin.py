"""Pass50 source-positioned offline lobby typography and functional camera/page UX."""
from __future__ import annotations
import gzip,hashlib,json,subprocess,sys
from pathlib import Path
from zipfile import ZipFile
from lxml import etree
from conftest import ROOT

EXTRACT=ROOT/'tools/maps/extract_original_lobby_signs50.py'
FAKE=ROOT/'tools/maps/make_pass50_bulletin_smoke_fixture.py'

def node(parent,cls,name,ref=None):
    props=etree.Element('Properties')
    etree.SubElement(props,'string',name='Name').text=name
    item=etree.SubElement(parent,'Item',
        attrib={'class':cls,**({'referent':ref} if ref else {})})
    item.append(props)
    return item,props

def part(parent,name,pos,ref):
    element,props=node(parent,'Part',name,ref=ref)
    cf=etree.SubElement(props,'CoordinateFrame',name='CFrame')
    values={'X':str(pos[0]),'Y':str(pos[1]),'Z':str(pos[2])}
    values.update({'R'+str(i)+str(j):str(int(i==j))
                   for i in range(3) for j in range(3)})
    for k,v in values.items():
        etree.SubElement(cf,k).text=v
    size=etree.SubElement(props,'Vector3',name='size')
    for k,v in zip('XYZ',(2,1,.2)):
        etree.SubElement(size,k).text=str(v)
    return element

def fake_xml(tmp_path):
    root=etree.Element('roblox')
    ws,_=node(root,'Workspace','Workspace')
    lobby,_=node(ws,'Folder','Lobby')
    cams,_=node(lobby,'Folder','CamPoints')
    part(cams,'Start',(100,-200,20),'START')
    board,_=node(lobby,'Folder','BulletinBoard')
    pieces,_=node(board,'Folder','Pieces')
    part(pieces,'Paper',(101,-201,15),'REAL_REF')
    guis,_=node(board,'Folder','SurfaceGuis')
    weekly,_=node(guis,'Folder','Weekly')
    gui,props=node(weekly,'SurfaceGui','Main','SURFACE_REF')
    etree.SubElement(props,'Ref',name='Adornee').text='REAL_REF'
    etree.SubElement(props,'token',name='Face').text='5'
    canvas=etree.SubElement(props,'Vector2',name='CanvasSize')
    etree.SubElement(canvas,'X').text='800'
    etree.SubElement(canvas,'Y').text='500'
    def label(name,text,y):
        el,props=node(gui,'TextLabel',name)
        etree.SubElement(props,'string',name='Text').text=text
        etree.SubElement(props,'bool',name='Visible').text='true'
        etree.SubElement(props,'float',name='TextSize').text='40'
        color=etree.SubElement(props,'Color3',name='TextColor3')
        for k,v in {'R':'.1','G':'.2','B':'.3'}.items():
            etree.SubElement(color,k).text=v
        position=etree.SubElement(props,'UDim2',name='Position')
        for k,v in {'XS':'0','XO':'5','YS':'0','YO':str(y)}.items():
            etree.SubElement(position,k).text=v
        size=etree.SubElement(props,'UDim2',name='Size')
        for k,v in {'XS':'0','XO':'400','YS':'0','YO':'45'}.items():
            etree.SubElement(size,k).text=v
        return el
    label('Title','WEEKLY LEADERBOARD:',15)
    label('User','FAKE_STALE_PLAYER',100)
    label('Value','FAKE_OLD_SCORE',145)
    archive=tmp_path/'synthetic_ui.xml.zip'
    with ZipFile(archive,'w') as z:
        z.writestr('TestPlace/TestPlace.rbxlx',etree.tostring(root))
    return archive

def test_original_heading_extraction_uses_source_adornee_and_excludes_rank_names(tmp_path):
    archive=fake_xml(tmp_path)
    out=tmp_path/'Content/Lobby/LobbySigns50.json.gz'
    script=[sys.executable,str(EXTRACT),'--archive',str(archive),
            '--out',str(out),'--synthetic']
    r=subprocess.run(script,capture_output=True,text=True)
    assert r.returncode==0,r.stderr
    original=out.read_bytes()
    d=json.loads(gzip.decompress(original))
    assert d['synthetic'] is True
    assert d['source_surfacegui_count']==1
    assert d['source_textlabel_count']==3
    assert d['historic_snapshots_intentionally_excluded'] is True
    assert len(d['panels'])==1
    p=d['panels'][0]
    assert p['source_part_frame']['t']==[1,-1,-5]
    assert p['source_part_frame']['s']==[2,1,.2]
    assert p['page']=='always'
    assert [h['text'] for h in p['headings']]==['WEEKLY LEADERBOARD:']
    assert p['headings'][0]['font_px']==40
    assert p['headings'][0]['rgb']==[26,51,76]
    assert 'FAKE_STALE_PLAYER' not in json.dumps(d)
    assert 'FAKE_OLD_SCORE' not in json.dumps(d)
    assert subprocess.run(script,capture_output=True).returncode==0
    assert out.read_bytes()==original
    report=json.loads((out.parent/'SOURCE_LOBBY_SIGNS_MANIFEST50.json').read_text())
    assert report['original_textlabels_excluded']==2
    assert report['compressed_pack_sha256']==hashlib.sha256(original).hexdigest()
    denied=subprocess.run([sys.executable,str(EXTRACT),
        '--archive',str(archive),'--out',str(tmp_path/'impostor.gz')],
        capture_output=True,text=True)
    assert denied.returncode!=0
    assert 'SHA mismatch' in denied.stderr

def test_ci_synthetic_pack_has_34_headings_with_safe_deletion(tmp_path):
    pack=tmp_path/'Content/Lobby/LobbySigns50.json.gz'
    def call(flag):
        return subprocess.run([sys.executable,str(FAKE),'--output',
            str(pack),flag],capture_output=True,text=True)
    assert call('--create').returncode==0
    d=json.loads(gzip.decompress(pack.read_bytes()))
    assert len(d['panels'])==9
    assert sum(len(p['headings']) for p in d['panels'])==34
    assert sum(len(p['headings']) for p in d['panels']
               if p['page']=='always')==30
    assert d['source_textlabel_count']==872
    assert d['historic_snapshots_intentionally_excluded'] is True
    assert set(p['page'] for p in d['panels']) == {
        'always','level','kills','donatedRobux','wavesSurvived'}
    assert call('--create').returncode!=0
    assert call('--clean').returncode==0
    assert not pack.exists()
    pack.write_bytes(gzip.compress(json.dumps({
        'synthetic':False,'format':'twr-pass50-source-lobby-signs-v1',
        'original_rbxlx_sha256':'owner'}
    ).encode()))
    assert call('--clean').returncode!=0
    assert pack.exists()

def test_godot_runtime_rejects_historical_user_data_and_checks_owner_sha():
    loader=(ROOT/'src/Twr.Godot/Scripts/Pass50SourceLobbySignsRuntime.cs').read_text()
    lobby=(ROOT/'src/Twr.Godot/Scripts/Pass49OriginalLobbyRuntime.cs').read_text()
    menu=(ROOT/'src/Twr.Godot/Scripts/Bootstrap.cs').read_text()
    assert '6858501862b3b3344e13bdb91b002979592225722073e52e1848a2abb9ace732' in loader
    assert 'SourceTextLabelCount' in loader
    assert 'OriginalHistoricalValuesExcluded' in loader
    assert 'Historical scoreboard values must not be presented as current' in loader
    assert 'Synthetic bulletin used in normal gameplay' in loader
    assert 'Original source bulletin SHA mismatch' in loader
    assert 'source_part_frame' in loader
    assert 'new Label3D {' in loader
    assert 'PixelSize=fontWorldScale' in loader
    assert '(.5f*size[2]+.025f)*Stud' in loader
    assert 'Pass50SourceLobbySignsRuntime.TryBuild(stage)' in lobby
    assert 'EnsureOriginalLobby("Leaderboards")' in menu
    assert 'Text="BULLETIN BOARD"' in menu
    assert 'next.Pressed +=' in menu
    assert 'Pass50SourceLobbySigns' in menu
    assert 'The saved online ranking snapshot' in menu
    assert 'AddBackground(_bulletin, .17f)' in menu
    assert 'StartGame("Manor")' in menu

def test_native_windows_smoke_checks_menu_camera_and_private_pack_deletion():
    boot=(ROOT/'src/Twr.Godot/Scripts/Bootstrap.cs').read_text()
    action=(ROOT/'.github/workflows/windows-build.yml').read_text()
    assert '--smoke-pass50' in boot and '--smoke-pass50' in action
    assert 'TWR_SMOKE_PASS50_BULLETIN_OK' in boot
    assert 'TWR_SMOKE_PASS50_BULLETIN_OK' in action
    assert 'boardButton.EmitSignal(Button.SignalName.Pressed)' in boot
    assert 'next.EmitSignal(Button.SignalName.Pressed)' in boot
    assert 'armoryButtons!=91' in boot
    assert 'restored_static_headings=34' in boot
    assert 'make_pass49_lobby_smoke_fixture.py' in action
    assert 'make_pass50_bulletin_smoke_fixture.py' in action
    assert 'SOURCE_LOBBY_SIGNS_MANIFEST50.json' in action
    assert 'LobbySigns50.json.gz' in action
    assert 'TWR-Pass50-Windows-x64-NoPrivateAssets' in action
    assert 'twr-pass50-original-bulletin-board' in action
