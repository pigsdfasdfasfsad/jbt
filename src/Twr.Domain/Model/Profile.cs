namespace Twr.Domain.Model;
public sealed class Profile
{
    public int SchemaVersion {get;set;}=1;
    public int Level {get;set;}=1;
    public int Xp {get;set;}=0;
    public int Credits {get;set;}=0;
    public HashSet<string> Unlocks {get;set;}=[];
    public Dictionary<string,string> Loadout {get;set;}=new();
    public HashSet<string> EquippedPerks {get;set;}=[];
    public HashSet<string> RewardReceipts {get;set;}=[];
    // Additive profile fields: older offline saves deserialize with empty
    // skin collections. Keys are canonical Case/Skin source identifiers.
    public HashSet<string> OwnedSkins {get;set;}=new(StringComparer.Ordinal);
    public Dictionary<string,string> EquippedWeaponSkins {get;set;}=new(StringComparer.Ordinal);
}
