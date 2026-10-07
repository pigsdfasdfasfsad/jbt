using Twr.Domain.Contracts.Events;
using Twr.Domain.Core;
using Twr.Domain.Model;
namespace Twr.Domain.Services;
public sealed class MatchDirector(EventStream events)
{
    public void Start(MatchState m,string map,DateTimeOffset now)
    {
        if(!ReleaseRules.Maps.Contains(map,StringComparer.Ordinal)) throw new ArgumentException("Map outside locked release set",nameof(map));
        m.MapName=map;m.Wave=1;m.Phase=MatchPhase.Wave;m.CompletionAwarded=false;m.SaveCommitted=false;m.CompletedObjectives.Clear();
        events.Publish(new WaveStartedEvent(1,now));
    }
    public bool Advance(MatchState m,DateTimeOffset now)
    {
        if(m.Phase is MatchPhase.Results or MatchPhase.CompletionPending)return false;
        if(m.Wave>=ReleaseRules.MaxWaves){m.Phase = MatchPhase.CompletionPending;return false;}
        m.Wave++;m.Phase=MatchPhase.Wave;events.Publish(new WaveStartedEvent(m.Wave,now));return true;
    }
    public void Fail(MatchState m){if(m.Phase!=MatchPhase.Results)m.Phase=MatchPhase.Results;}
    public bool IsMapComplete(MatchState m)=>m.Wave == ReleaseRules.MaxWaves && m.Phase == MatchPhase.CompletionPending;
}
