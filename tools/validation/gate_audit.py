#!/usr/bin/env python3
from pathlib import Path
import json,re
ROOT=Path(__file__).resolve().parents[2]
gates=json.loads((ROOT/'tests/acceptance_gates.json').read_text()); assert len(gates)==21 and len({x['id'] for x in gates})==21
rules=(ROOT/'src/Twr.Domain/Model/ReleaseRules.cs').read_text()
session=(ROOT/'src/Twr.Domain/Runtime/LocalSession.cs').read_text()
parser=(ROOT/'tools/maps/rbxlx_static_parser.py').read_text()
weapons=json.loads((ROOT/'content/weapons/catalog.json').read_text()); perks=json.loads((ROOT/'content/perks/perks.json').read_text()); maps=json.loads((ROOT/'content/maps/maps.json').read_text())
hashes=json.loads((ROOT/'evidence/recovery/source_archive_verification.json').read_text()); rem=json.loads((ROOT/'content/remotes/recovery_status.json').read_text())
workflow=(ROOT/'.github/workflows/windows-build.yml').read_text()
checks=[
 'HttpClient' not in ''.join(p.read_text(errors='replace') for p in (ROOT/'src').rglob('*.cs')),
 'Mode = "Regular"' in rules and all(x in rules for x in ['Hardcore','Classic','Endless']),
 'MaxWaves = 15' in rules,
 'm.Wave>=ReleaseRules.MaxWaves' in (ROOT/'src/Twr.Domain/Services/MatchDirector.cs').read_text() and 'Wave=16' not in session,
 'EnemyJuggernautAllowed = false' in rules and 'Juggernaut' in (ROOT/'src/Twr.Domain/Policies/InfectedSpawnPolicy.cs').read_text(),
 'NormalMeleeMovementStun = false' in rules,
 'TryIssue' in (ROOT/'src/Twr.Domain/Services/CompletionRewardService.cs').read_text(),
 session.index('_save.Save') < session.index('MatchPhase.Results'),
 'ReturnToLobby' in session,
 maps['count']==10,
 maps['excluded']==['Stadium','Carnival'],
 'CommandBus' in session and 'EventStream' in session,
 all(tok not in ''.join(p.read_text() for p in (ROOT/'src/Twr.Godot/Scripts').glob('*.cs')) for tok in ['.Xp=','.Credits=','.Health=','.Wave=']),
 'script_source_inert' in parser and 'ET.fromstring' in parser and 'eval(' not in parser and 'exec(' not in parser and 'subprocess' not in parser,
 '*.rbxlx' in (ROOT/'.gitignore').read_text() and '*.zip' in (ROOT/'.gitignore').read_text(),
 weapons['active_weapon_count']==91,
 perks['count']==21,
 hashes['all_match'] and len(hashes['archives'])==6,
 all((ROOT/f'docs/maps/previews/{m.lower()}.png').is_file() for m in ['Ranch','Mill','Bypass','Cabin','Cargo','District','Expressway','Prison','Laboratory','Manor']),
 all(x in workflow for x in ['3.13','8.0.425','4.7.2']),
 rem['transport_policy'].startswith('Standalone uses typed in-process') and rem['historical_runtime_normalization']['database_bytes_recovered'] is False,
]
assert len(checks)==len(gates)
failed=[g['id'] for g,ok in zip(gates,checks) if not ok]
assert not failed,'failed gates: '+', '.join(failed)
print(f'acceptance gate audit ok: {len(gates)} records')
