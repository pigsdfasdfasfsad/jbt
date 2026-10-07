namespace Twr.Domain.Model;
public static class ReleaseRules {
    public const string Mode = "Regular";
    public const int MaxWaves = 15;
    public static readonly string[] Maps = ["Ranch","Mill","Bypass","Cabin","Cargo","District","Expressway","Prison","Laboratory","Manor"];
    public static readonly string[] ExcludedModes = ["Hardcore","Classic","Endless"];
    public static readonly string[] ExcludedMaps = ["Stadium","Carnival"];
    public const bool EnemyJuggernautAllowed = false;
    public const bool NormalMeleeMovementStun = false;
}
