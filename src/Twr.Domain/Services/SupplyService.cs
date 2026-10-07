namespace Twr.Domain.Services; public sealed class SupplyService { public bool CanRequest(int wave,bool activeDrop)=>wave>=1&&wave<=15&&!activeDrop; }
