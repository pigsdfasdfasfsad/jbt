#!/usr/bin/env python3
"""Generate a valid, disposable PCM16 WAV to test an external audio override."""
import argparse
import math
import struct
import wave
from pathlib import Path

SAMPLES=2205
RATE=22050
def generate(folder):
    folder.mkdir(parents=True,exist_ok=True)
    path=folder/"gunshot.wav"
    with wave.open(str(path),"wb") as audio:
        audio.setnchannels(1)
        audio.setsampwidth(2)
        audio.setframerate(RATE)
        data=b"".join(struct.pack("<h",int(
            math.sin(i*2*math.pi*880/RATE)*12000
        )) for i in range(SAMPLES))
        audio.writeframes(data)
    print("TWR_SYNTHETIC_PCM16_WAV_OK samples="+str(SAMPLES))
if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--output-dir",type=Path,required=True)
    generate(parser.parse_args().output_dir)
