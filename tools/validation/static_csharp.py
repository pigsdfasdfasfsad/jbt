#!/usr/bin/env python3
from __future__ import annotations
import re,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
files=sorted((ROOT/'src').rglob('*.cs'))
assert files,'no C# files'
text='\n'.join(p.read_text(encoding='utf-8') for p in files)
assert 'ReleaseRules' in text and 'MaxWaves = 15' in text
assert 'EnemyJuggernautAllowed = false' in text
assert 'NormalMeleeMovementStun = false' in text
assert 'MapCompletionXpBonus' in text
# Godot/presentation layer may issue commands and consume events, but must not mutate authority fields directly.
for p in (ROOT/'src/Twr.Godot/Scripts').glob('*.cs'):
    s=p.read_text(encoding='utf-8')
    forbidden=[r'\.Xp\s*=',r'\.Credits\s*=',r'\.Health\s*=',r'\.Wave\s*=',r'\.Ammo\s*\[.*\]\s*=',r'CompletedObjectives\.Add']
    for pat in forbidden: assert not re.search(pat,s),f'presentation authority mutation in {p}: {pat}'
print(f'static C# audit ok: {len(files)} files')
