"""Pass38 source CMaps sound metadata, generated-only WAV and native F2 contracts."""
from __future__ import annotations
import hashlib
import importlib.util
import json
from pathlib import Path
import sys
import zipfile
from lxml import etree
from conftest import ROOT

MAPS=('Ranch','Mill','Bypass','Cabin','Cargo','District',
      'Expressway','Prison','Laboratory','Manor')

def module():
    path=ROOT/'tools/audio/recover_source_soundscape38.py'
    spec=importlib.util.spec_from_file_location('twr_pass38_sound',path)
    result=importlib.util.module_from_spec(spec)
    sys.modules[spec.name]=result
    spec.loader.exec_module(result)
    return result

def original(parent,kind,name):
    item=etree.SubElement(parent,'Item',attrib={'class':kind})
    props=etree.SubElement(item,'Properties')
    etree.SubElement(props,'string',name='Name').text=name
    return item,props

def attach_sound(parent,name):
    item,props=original(parent,'Sound',name)
    for key,value in (('Volume','0'),('RollOffMinDistance','10'),
                      ('RollOffMaxDistance','70'),('PlaybackSpeed','1')):
        etree.SubElement(props,'float',name=key).text=value
    etree.SubElement(props,'token',name='RollOffMode').text='2'
    etree.SubElement(props,'Content',name='SoundId').text=''
    etree.SubElement(props,'bool',name='Playing').text='true'
    etree.SubElement(props,'bool',name='Looped').text='true'
    volume,vprops=original(item,'NumberValue','SetVolume')
    etree.SubElement(vprops,'double',name='Value').text='0.75'

def synthetic_rbxlx(path):
    root=etree.Element('roblox')
    rep,_=original(root,'ReplicatedStorage','ReplicatedStorage')
    maps,_=original(rep,'Folder','CMaps')
    for n,name in enumerate(MAPS):
        m,_=original(maps,'Folder',name)
        local,_=original(m,'Folder','Local Sounds')
        env,_=original(m,'Folder','Environment Sounds')
        if name=='Laboratory':continue
        anchor,props=original(local,'Part','EngineIdleSoundTruck')
        frame=etree.SubElement(props,'CoordinateFrame',name='CFrame')
        for key,value in (('X',str(n*10)),('Y','5'),('Z','2')):
            etree.SubElement(frame,key).text=value
        attach_sound(anchor,'Sound')
        attach_sound(env,'AmbientWind')
    with zipfile.ZipFile(path,'w',compression=zipfile.ZIP_DEFLATED) as z:
        z.writestr('TestPlace/TestPlace.rbxlx',etree.tostring(root,encoding='utf8'))

def test_synthetic_original_cmaps_emitter_extraction_is_deterministic(tmp_path):
    archive=tmp_path/'TestPlace.zip'
    synthetic_rbxlx(archive)
    recover=module().recover
    a=recover(archive,tmp_path/'first')
    b=recover(archive,tmp_path/'second')
    assert a==b
    assert a['total_emitters']==18
    assert a['total_positioned']==9
    assert a['total_environment']==9
    assert a['source_sound_ids_are_empty'] is True
    for name,detail in a['maps'].items():
        data=(tmp_path/'first'/detail['file']).read_bytes()
        assert data==(tmp_path/'second'/detail['file']).read_bytes()
        assert hashlib.sha256(data).hexdigest()==detail['sha256']
        rows=json.loads(data)['emitters']
        if name=='Laboratory':
            assert rows==[]
        else:
            assert {row['scope'] for row in rows}=={'local','environment'}
            assert all(row['script_target_volume']==.75 for row in rows)
            assert all(row['source_id_available'] is False for row in rows)
            assert next(row for row in rows if row['scope']=='local')['position_studs'] is not None
    assert len(list((tmp_path/'first').iterdir()))==11

def test_native_runtime_only_plays_authorized_offline_wavs_with_f2():
    runtime=(ROOT/'src/Twr.Godot/Scripts/Pass38SourceSoundscapeRuntime.cs').read_text()
    game=(ROOT/'src/Twr.Godot/Scripts/GameplayRoot.cs').read_text()
    audio=(ROOT/'src/Twr.Godot/Scripts/OfflineAudioRuntime.cs').read_text()
    bootstrap=(ROOT/'src/Twr.Godot/Scripts/Bootstrap.cs').read_text()
    assert 'Original soundscape SHA-256 mismatch' in runtime
    assert 'HasExactMapGeometry' in runtime
    assert 'MaxLocalVoices = 12' in runtime
    assert 'AudioStreamPlayer3D' in runtime
    assert 'source_id_available' in runtime
    assert 'Content","Audio","MapSounds",cue+".wav"' in runtime
    assert 'OfflineAudioRuntime.TryLoadPrivatePcm16Wav' in runtime
    assert 'HttpClient' not in runtime
    assert 'public static AudioStreamWav? TryLoadPrivatePcm16Wav' in audio
    assert 'Pass38SourceSoundscapeRuntime.TryBuild(this, MapName,' in game
    assert 'key.Keycode == Key.F2' in game
    assert 'TWR_SMOKE_PASS38_SOUND_OK' in bootstrap
    assert '--smoke-pass38' in bootstrap

def test_ci_source_sound_fixture_is_temporary_and_public_build_is_private_data_free(tmp_path):
    import subprocess
    import os
    script=ROOT/'tools/validation/make_pass38_synthetic.py'
    workflow=(ROOT/'.github/workflows/windows-build.yml').read_text()
    assert 'twr-pass38-source-soundscapes' in workflow
    assert 'TWR_SMOKE_PASS38_SOUND_OK' in workflow
    assert 'make_pass38_synthetic.py' in workflow
    assert 'TWR-Pass38-Windows-x64-NoPrivateAssets' in workflow
    subprocess.run([sys.executable,str(script),'--output',str(tmp_path),'--create'],check=True)
    rows=json.loads((tmp_path/'Content'/'SourceSoundscapes'/'Manor.soundscape38.json').read_text())
    assert rows['owner_rbxlx_sha256']=='0'*64 and len(rows['emitters'])==2
    for cue in ('ambientwind','gramophoneaudio'):
        path=tmp_path/'Content'/'Audio'/'MapSounds'/(cue+'.wav')
        assert path.read_bytes()[:4]==b'RIFF'
    subprocess.run([sys.executable,str(script),'--output',str(tmp_path),'--clean'],check=True)
    assert not (tmp_path/'Content'/'SourceSoundscapes'/'Manor.soundscape38.json').exists()
    assert not (tmp_path/'Content'/'Audio'/'MapSounds'/'ambientwind.wav').exists()
