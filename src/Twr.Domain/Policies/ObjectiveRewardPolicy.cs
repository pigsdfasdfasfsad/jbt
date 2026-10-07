namespace Twr.Domain.Policies;

public sealed class ObjectiveRewardPolicy
{
    public (int Credits, int Xp) Reward(string family) => family switch
    {
        "Damage" => (7000, 5000),
        "Load" or "Repair" or "Radio" or "Unpack" or "Escort" => (4000, 3500),
        _ => (0, 0)
    };
}
