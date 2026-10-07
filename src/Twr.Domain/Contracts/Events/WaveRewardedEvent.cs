namespace Twr.Domain.Contracts.Events;
public sealed record WaveRewardedEvent(int Wave,int Credits,int Xp,int ObjectiveCredits,int ObjectiveXp,double DifficultyScale,DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;
