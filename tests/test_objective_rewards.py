from conftest import ROOT

def read(path):
    return (ROOT/path).read_text()

def test_objective_rewards_match_documented_values():
    p=read("src/Twr.Domain/Policies/ObjectiveRewardPolicy.cs")
    assert '"Damage" => (7000, 5000)' in p
    assert '"Load" or "Repair" or "Radio" or "Unpack" or "Escort" => (4000, 3500)' in p

def test_objective_reward_is_domain_authoritative_and_idempotent():
    s=read("src/Twr.Domain/Services/ObjectiveService.cs")
    assert "match.CompletedObjectives.Add(id)" in s
    assert "player.Credits += credits" in s
    assert "player.Xp += xp" in s
    runtime=read("src/Twr.Domain/Runtime/LocalSession.cs")
    assert "_objectives.Complete(State.Match, State.Player" in runtime
    assert 'PersistProfile($"Objective:' in runtime
