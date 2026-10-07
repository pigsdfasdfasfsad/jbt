#!/usr/bin/env python3
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
scan=[]
for base in [ROOT/'src/Twr.Domain',ROOT/'src/Twr.Godot']:
 for p in base.rglob('*'):
  if p.is_file() and p.suffix in {'.cs','.gd','.tscn','.godot','.cfg','.csproj'}: scan.append(p)
for p in scan:
 s=p.read_text(encoding='utf-8',errors='replace').lower()
 for token in ['system.net.http','httpclient','webrequest','websocket','roblox.com','rbxapi','fireserver','invokeserver']:
  assert token not in s,f'offline runtime violation {token} in {p.relative_to(ROOT)}'
print('offline runtime audit ok')
