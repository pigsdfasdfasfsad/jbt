namespace Twr.Domain.Policies;
public sealed class DamagePolicy { public bool ApplyMovementSlowForNormalMelee => false; public float ClampDamage(float amount)=>Math.Max(0,amount); }
