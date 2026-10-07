namespace Twr.Domain.Contracts.Events; public sealed record AmmoChangedEvent(string WeaponId, int AmmoAfter, DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;
