namespace Twr.Domain.Services; public sealed class LootService { public bool CanPickup(bool playerAlive,bool alreadyTaken)=>playerAlive&&!alreadyTaken; }
