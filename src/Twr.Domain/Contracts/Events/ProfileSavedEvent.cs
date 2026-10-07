namespace Twr.Domain.Contracts.Events; public sealed record ProfileSavedEvent(string Reason, DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;
