#!/usr/bin/env python3
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parents[2]
required=[ROOT/'src/Twr.Godot/project.godot',ROOT/'src/Twr.Godot/Twr.Godot.csproj',ROOT/'src/Twr.Godot/Scenes/Main.tscn',ROOT/'src/Twr.Godot/Scenes/SystemsHarness.tscn',ROOT/'src/Twr.Godot/export_presets.cfg']
assert all(p.is_file() for p in required)
maps=json.loads((ROOT/'content/maps/maps.json').read_text())
assert maps['count']==10 and maps['excluded']==['Stadium','Carnival']
for m in maps['maps']:
 p=ROOT/m['preview']; assert p.is_file() and p.stat().st_size>10000,p
# synced content must be byte-identical to canonical content
canon=sorted(p for p in (ROOT/'content').rglob('*') if p.is_file())
mirror=ROOT/'src/Twr.Godot/Content'
for p in canon:
 q=mirror/p.relative_to(ROOT/'content'); assert q.is_file(),q; assert p.read_bytes()==q.read_bytes(),q
print(f'Godot project audit ok: {len(canon)} content files mirrored')
