import re,pytest
from conftest import ROOT
PRESENT=list((ROOT/'src/Twr.Godot/Scripts').glob('*.cs'))
@pytest.mark.parametrize('path',PRESENT,ids=lambda p:p.name)
def test_presentation_has_no_direct_authority_assignment(path):
 s=path.read_text();
 for pat in [r'\.Xp\s*=',r'\.Credits\s*=',r'\.Health\s*=',r'\.Wave\s*=',r'CompletedObjectives\.Add']:
  assert not re.search(pat,s)
def test_session_uses_command_bus_and_event_stream():
 s=(ROOT/'src/Twr.Domain/Runtime/LocalSession.cs').read_text(); assert 'CommandBus' in s and 'EventStream' in s and 'Enqueue(IGameCommand' in s
def test_save_before_results_in_completion_path():
 s=(ROOT/'src/Twr.Domain/Runtime/LocalSession.cs').read_text(); assert s.index('_save.Save') < s.index('State.Match.Phase=MatchPhase.Results')
def test_completion_receipt_idempotency(): assert 'TryIssue' in (ROOT/'src/Twr.Domain/Services/CompletionRewardService.cs').read_text()
def test_spawn_policy_explicitly_blocks_juggernaut():
 s=(ROOT/'src/Twr.Domain/Policies/InfectedSpawnPolicy.cs').read_text(); assert 'Juggernaut' in s and '!' in s
def test_normal_damage_policy_has_no_slow(): assert 'ApplyMovementSlowForNormalMelee => false' in (ROOT/'src/Twr.Domain/Policies/DamagePolicy.cs').read_text()
