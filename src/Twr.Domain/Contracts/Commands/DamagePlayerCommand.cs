namespace Twr.Domain.Contracts.Commands;
public sealed record DamagePlayerCommand(float Amount,string Source,bool BypassArmor=false) : Twr.Domain.Contracts.IGameCommand;
