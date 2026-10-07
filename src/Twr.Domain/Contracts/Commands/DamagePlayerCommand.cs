namespace Twr.Domain.Contracts.Commands; public sealed record DamagePlayerCommand(float Amount, string Source) : Twr.Domain.Contracts.IGameCommand;
