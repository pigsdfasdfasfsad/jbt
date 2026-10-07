namespace Twr.Domain.Policies;
public sealed class CompletionRewardPolicy {
    // Proposed recovery default from the prior engineering implementation; not proven historical retail data.
    public int MapCompletionXpBonus { get; init; } = 300;
    public int CalculateXp(int playerLevel) => Math.Max(1, playerLevel) * MapCompletionXpBonus;
}
