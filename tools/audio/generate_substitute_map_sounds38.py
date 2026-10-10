#!/usr/bin/env python3
"""Optional synthetic WAV loop cues. NOT source/game audio; owner may replace."""
from __future__ import annotations
import argparse
from array import array
import hashlib
import json
import math
from pathlib import Path
import random
import sys
import wave

RATE=16000
SECONDS=3

def kind(cue):
    if 'birdtweets' in cue:return 'birds'
    if 'crickets' in cue:return 'crickets'
    if 'engineidle' in cue:return 'engine'
    if 'ambientwaves' in cue:return 'waves'
    if 'wind' in cue:return 'wind'
    if 'rainthunder' in cue:return 'rain'
    if 'airintake' in cue or 'airouttake' in cue:return 'ventilation'
    if 'fireplace' in cue:return 'fire'
    if 'radiochatter' in cue:return 'radio'
    if 'waterpipe' in cue:return 'water'
    if 'gramophone' in cue:return 'gramophone_crackle'
    return 'machinery'

def make_samples(cue):
    seed=int.from_bytes(hashlib.sha256(cue.encode('ascii')).digest()[:8],'little')
    rng=random.Random(seed)
    category=kind(cue);p=seed%37;values=array('h');low=0.
    for i in range(RATE*SECONDS):
        t=i/RATE
        phase=t*math.tau
        noise=rng.uniform(-1,1)
        low=low*.985+noise*.015
        slowly=math.sin(t*math.tau/3+.1*p)
        if category=='wind':s=.22*low*(.65+.35*slowly)+.025*math.sin(phase*49)
        elif category=='waves':s=.19*low*(.75+.2*slowly)+.05*math.sin(phase*57)
        elif category=='engine':s=.095*math.sin(phase*(56+p*.5)+.18*math.sin(phase*6))+.035*math.sin(phase*(112+p))+.03*low
        elif category=='ventilation':s=.085*math.sin(phase*(97+p))+.13*low
        elif category=='machinery':s=.09*math.sin(phase*(89+p))+.042*math.sin(phase*(178+p))+.028*low
        elif category=='crickets':
            chirp=max(0,math.sin(phase*2.6))**18
            s=.12*math.sin(phase*(3100+p*7))*chirp+.013*low
        elif category=='birds':
            chirp=max(0,math.sin(phase*(1.5+(p%4))+.5))**24
            s=.12*math.sin(phase*(1400+p*13+170*math.sin(phase*9)))*chirp+.01*low
        elif category=='rain':s=.08*noise+.045*low+.015*math.sin(phase*37)
        elif category=='fire':s=.045*noise*(rng.random()>.96)+.04*low
        elif category=='radio':s=.055*math.sin(phase*(210+p*4))*(.2+.8*abs(math.sin(phase*3)))+.01*noise
        elif category=='water':s=.08*math.sin(phase*(130+70*math.sin(phase*2)))+.02*low
        else:s=.024*noise*(rng.random()>.92)+.025*low
        envelope=min(1.,i/(RATE*.025),(RATE*SECONDS-1-i)/(RATE*.025))
        values.append(int(max(-.8,min(.8,s*max(0,envelope)))*32767))
    if sys.byteorder!='little':values.byteswap()
    return values.tobytes()

def generate(sound_dir:Path,manifest_file:Path):
    manifest=json.loads(manifest_file.read_text(encoding='utf8'))
    names=sorted({cue for row in manifest['maps'].values() for cue in row['unique_cues']})
    sound_dir.mkdir(parents=True,exist_ok=True)
    result={'format':'twr-pass38-SYNTHETIC-SUBSTITUTE-WAV-manifest-v1',
            'not_original_audio':True,'rate_hz':RATE,'seconds':SECONDS,'cues':{}}
    for cue in names:
        if len(cue)>80 or not cue or any(c not in 'abcdefghijklmnopqrstuvwxyz0123456789_-' for c in cue):
            raise ValueError('Unsafe source cue name '+repr(cue))
        path=sound_dir/(cue+'.wav')
        with wave.open(str(path),'wb') as out:
            out.setnchannels(1);out.setsampwidth(2);out.setframerate(RATE)
            out.writeframes(make_samples(cue))
        result['cues'][cue]={'kind':kind(cue),'bytes_sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
    (sound_dir/'SYNTHETIC_AUDIO_NOT_ORIGINAL.json').write_text(
        json.dumps(result,indent=2,sort_keys=True)+'\n')
    return result

if __name__=='__main__':
    arg=argparse.ArgumentParser()
    arg.add_argument('--source-manifest',required=True,type=Path)
    arg.add_argument('--output',required=True,type=Path)
    a=arg.parse_args()
    report=generate(a.output,a.source_manifest)
    print('TWR_PASS38_SUBSTITUTE_WAV count=',len(report['cues']),'original_audio=false')
