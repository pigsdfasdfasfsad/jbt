using Twr.Domain.Contracts.Events; using Twr.Domain.Core; using Twr.Domain.Model;
namespace Twr.Domain.Services;
public sealed class AmmoService(EventStream events) { public bool Spend(PlayerState p,string weapon,int amount,DateTimeOffset now){if(amount<=0)return false;var cur=p.Ammo.GetValueOrDefault(weapon);if(cur<amount)return false;p.Ammo[weapon]=cur-amount;events.Publish(new AmmoChangedEvent(weapon,p.Ammo[weapon],now));return true;} }
