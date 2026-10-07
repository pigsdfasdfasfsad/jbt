using Twr.Domain.Model; using Twr.Domain.Policies;
namespace Twr.Domain.Services;
public sealed class CompletionRewardService(CompletionRewardPolicy policy,ReceiptLedger receipts) {
    public const string ReceiptId="MapCompletion";
    public int Award(PlayerState player,string mapName){var id=$"{ReceiptId}:{mapName}";if(!receipts.TryIssue(id))return 0;var xp=policy.CalculateXp(player.Level);player.Xp+=xp;return xp;}
}
