#!/usr/bin/env python3
"""Tiny FABRICATED source soundscape and PCM16 WAV fixtures for exported Windows CI."""
from __future__ import annotations
import argparse
import json
import math
from pathlib import Path
import struct
import wave

FILE='Manor.soundscape38.json'
CUES=('ambientwind','gramophoneaudio')

def rows():
    common={'source_id_available':False,'original_volume':0.,'script_target_volume':.5,
            'initial_playing':False,'looped':True,
            'rolloff_min_studs':10.,'rolloff_max_studs':100.,
            'rolloff_mode':2,'playback_speed':1.}
    return [
        {**common,'scope':'environment','cue':'ambientwind',
         'source_name':'AmbientWind','source_path':'Environment Sounds/AmbientWind',
         'position_studs':None},
        {**common,'scope':'local','cue':'gramophoneaudio',
         'source_name':'GramophoneAudio',
         'source_path':'Local Sounds/GramophoneAudio/Sound',
         'position_studs':[8.,2.,1.]}
    ]

def write(root:Path):
    pack=root/'Content'/'SourceSoundscapes';pack.mkdir(parents=True,exist_ok=True)
    sounds=root/'Content'/'Audio'/'MapSounds';sounds.mkdir(parents=True,exist_ok=True)
    items=rows()
    document={'format':'twr-source-soundscape38-v1','map':'Manor',
        'owner_rbxlx_sha256':'0'*64,'source_path':'ReplicatedStorage/CMaps/Manor',
        'source_ids_present':False,'audio_binary_bytes_present':False,
        'playback_is_approximate_with_external_wav':True,
        'emitter_count':len(items),'local_count':1,'environment_count':1,
        'emitters':items}
    (pack/FILE).write_text(json.dumps(document,sort_keys=True,separators=(',',':'))+'\n')
    for n,cue in enumerate(CUES):
        with wave.open(str(sounds/(cue+'.wav')),'wb') as wav:
            wav.setnchannels(1);wav.setsampwidth(2);wav.setframerate(16000)
            wav.writeframes(b''.join(struct.pack('<h',int(2500*math.sin(
                i*2*math.pi*(200+70*n)/16000))) for i in range(8000)))
    print('TWR_PASS38_FIXTURES_CREATED synthetic_only=true')

def clean(root:Path):
    files=[root/'Content'/'SourceSoundscapes'/FILE]
    files += [root/'Content'/'Audio'/'MapSounds'/(cue+'.wav') for cue in CUES]
    for path in files:path.unlink(missing_ok=True)
    for folder in (root/'Content'/'SourceSoundscapes',root/'Content'/'Audio'/'MapSounds'):
        if folder.is_dir() and not any(folder.iterdir()):folder.rmdir()
    print('TWR_PASS38_FIXTURES_CLEANED')

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--output',required=True,type=Path)
    group=parser.add_mutually_exclusive_group(required=True)
    group.add_argument('--create',action='store_true')
    group.add_argument('--clean',action='store_true')
    a=parser.parse_args()
    (write if a.create else clean)(a.output)
