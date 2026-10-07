namespace Twr.Domain.Model;
public sealed class Profile { public int SchemaVersion {get;set;}=1; public int Level {get;set;}=1; public int Xp {get;set;}=0; public int Credits {get;set;}=0; public HashSet<string> Unlocks {get;set;}=[]; public Dictionary<string,string> Loadout {get;set;}=new(); public HashSet<string> RewardReceipts {get;set;}=[]; }
