using Twr.Domain.Contracts.Events; using Twr.Domain.Core; using Twr.Domain.Model;
namespace Twr.Domain.Services;
public sealed class EconomyService(EventStream events) { public bool Purchase(PlayerState p,string item,int price,DateTimeOffset now){if(price<0||p.Credits<price)return false;p.Credits-=price;events.Publish(new PurchaseCompletedEvent(item,p.Credits,now));return true;} }
