using Twr.Domain.Contracts.Events; using Twr.Domain.Core; using Twr.Domain.Model; using Twr.Domain.Policies;
namespace Twr.Domain.Services;
public sealed class DamageService(DamagePolicy policy, EventStream events) { public void Apply(PlayerState p,float amount,string source,DateTimeOffset now){var d=policy.ClampDamage(amount);p.Health=Math.Max(0,p.Health-d);events.Publish(new PlayerDamagedEvent(d,p.Health,source,now));} }
