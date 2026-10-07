namespace Twr.Domain.Model;

public static class ProgressionRules
{
    // VERIFIED from Levels: XP required for the next level is 25 * (level + 1)^2.
    public static int RequiredForNextLevel(int level)
    {
        var n=Math.Max(1,level)+1L;
        return (int)Math.Min(int.MaxValue,25L*n*n);
    }

    // VERIFIED from Levels: reaching a level grants level * 150 credits.
    public static int CreditRewardForLevel(int level) =>
        (int)Math.Min(int.MaxValue,Math.Max(0L,(long)level*150L));
}
