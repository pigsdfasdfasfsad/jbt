using Twr.Domain.Contracts.Events;
using Twr.Domain.Core;
using Twr.Domain.Model;

namespace Twr.Domain.Services;

public sealed class MatchDirector(EventStream events)
{
    public void Start(MatchState match, string map, DateTimeOffset now)
    {
        if (!ReleaseRules.Maps.Contains(map, StringComparer.Ordinal))
            throw new ArgumentException("Map outside locked release set", nameof(map));

        match.MapName = map;
        match.Wave = 1;
        match.Phase = MatchPhase.Wave;
        match.CompletionAwarded = false;
        match.SaveCommitted = false;
        match.CompletedObjectives.Clear();
        events.Publish(new WaveStartedEvent(1, now));
    }

    /// <summary>
    /// Called only after the current timed wave has ended.
    /// Waves 1-14 advance. Finishing Wave 15 enters a private completion-pending
    /// state so LocalSession can grant/persist the reward before Results is visible.
    /// </summary>
    public bool Advance(MatchState match, DateTimeOffset now)
    {
        if (match.Phase is MatchPhase.Results or MatchPhase.CompletionPending)
            return false;

        if (match.Wave >= ReleaseRules.MaxWaves)
        {
            match.Phase = MatchPhase.CompletionPending;
            return false;
        }

        match.Wave++;
        match.Phase = MatchPhase.Wave;
        events.Publish(new WaveStartedEvent(match.Wave, now));
        return true;
    }

    public bool IsMapComplete(MatchState match) =>
        match.Wave == ReleaseRules.MaxWaves &&
        match.Phase == MatchPhase.CompletionPending;
}
