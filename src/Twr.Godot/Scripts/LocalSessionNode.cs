using Godot;
using System.Text.Json;
using Twr.Domain.Contracts;
using Twr.Domain.Contracts.Commands;
using Twr.Domain.Model;
using Twr.Domain.Persistence;
using Twr.Domain.Runtime;
using Twr.Domain.Services;

namespace Twr.Godot;

public partial class LocalSessionNode : Node
{
    public LocalSession? Session { get; private set; }

    public override void _Ready()
    {
        var smoke54=OS.GetCmdlineUserArgs().Contains("--smoke-pass54",
            StringComparer.Ordinal);
        var smoke55=OS.GetCmdlineUserArgs().Contains("--smoke-pass55",
            StringComparer.Ordinal);
        var smoke56=OS.GetCmdlineUserArgs().Contains("--smoke-pass56",
            StringComparer.Ordinal);
        var smoke58=OS.GetCmdlineUserArgs().Contains("--smoke-pass58",
            StringComparer.Ordinal);
        var smoke70=OS.GetCmdlineUserArgs().Contains("--smoke-pass70",
            StringComparer.Ordinal);
        var smoke71=OS.GetCmdlineUserArgs().Contains("--smoke-pass71",
            StringComparer.Ordinal);
        var smoke72=OS.GetCmdlineUserArgs().Contains("--smoke-pass72",
            StringComparer.Ordinal);
        var smoke73=OS.GetCmdlineUserArgs().Contains("--smoke-pass73",
            StringComparer.Ordinal);
        var smoke74=OS.GetCmdlineUserArgs().Contains("--smoke-pass74",
            StringComparer.Ordinal);
        var smoke75=OS.GetCmdlineUserArgs().Contains("--smoke-pass75",
            StringComparer.Ordinal);
        var smoke76=OS.GetCmdlineUserArgs().Contains("--smoke-pass76",
            StringComparer.Ordinal);
        var smoke77=OS.GetCmdlineUserArgs().Contains("--smoke-pass77",
            StringComparer.Ordinal);
        var smoke78=OS.GetCmdlineUserArgs().Contains("--smoke-pass78",
            StringComparer.Ordinal);
        var smoke83=OS.GetCmdlineUserArgs().Contains("--smoke-pass83",
            StringComparer.Ordinal);
        var smoke88=OS.GetCmdlineUserArgs().Contains("--smoke-pass88",
            StringComparer.Ordinal);
        var smoke89=OS.GetCmdlineUserArgs().Contains("--smoke-pass89",
            StringComparer.Ordinal);
        var smoke90=OS.GetCmdlineUserArgs().Contains("--smoke-pass90",
            StringComparer.Ordinal);
        // Pass54-56 all use isolated test-only profile files.
        var save = ProjectSettings.GlobalizePath(smoke54
            ? "user://pass54-smoke-profile.json" :
            smoke55 ? "user://pass55-smoke-profile.json" :
            smoke56 ? "user://pass56-smoke-profile.json" :
            smoke58 ? "user://pass58-smoke-profile.json" :
            smoke70 ? "user://pass70-live-combat-smoke-profile.json" :
            smoke71 ? "user://pass71-multimap-damage-smoke-profile.json" :
            smoke72 ? "user://pass72-ten-map-entry-smoke-profile.json" :
            smoke73 ? "user://pass73-ground-navigation-smoke-profile.json" :
            smoke74 ? "user://pass74-special-infected-smoke-profile.json" :
            smoke75 ? "user://pass75-blast-smoke-profile.json" :
            smoke76 ? "user://pass76-objective-fort-smoke-profile.json" :
            smoke77 ? "user://pass77-manor-stair-smoke-profile.json" :
            smoke78 ? "user://pass78-manor-bidirectional-smoke-profile.json" :
            smoke83 ? "user://pass83-manor-upper-gameplay-smoke-profile.json" :
            smoke88 ? "user://pass88-physical-waves-smoke-profile.json" :
            smoke89 ? "user://pass89-escort-nav-smoke-profile.json" :
            smoke90 ? "user://pass90-escort-completion-smoke-profile.json" :
            "user://profile.json");
        var store = new JsonProfileStore(save);
        if(smoke70 || smoke71)
            store.Save(new Profile());
        else if(smoke72)
            store.Save(new Profile());
        else if(smoke73)
            store.Save(new Profile());
        else if(smoke74)
            store.Save(new Profile());
        else if(smoke75)
            store.Save(new Profile());
        else if(smoke76)
            store.Save(new Profile());
        else if(smoke77)
            store.Save(new Profile());
        else if(smoke78)
            store.Save(new Profile());
        else if(smoke83)
            store.Save(new Profile());
        else if(smoke88)
            store.Save(new Profile());
        else if(smoke89)
            store.Save(new Profile());
        else if(smoke90)
            store.Save(new Profile());
        if(smoke54 || smoke55 || smoke56 || smoke58)
        {
            var fixture=JsonSerializer.Deserialize<Profile>(
                "{\"Credits\":12000,\"OwnedSkins\":[\"Low/Cocoa\",\"High/Gold\",\"Ethereal/Spirit\"],"+
                "\"EquippedWeaponSkins\":{\"Glock 17\":\"Low/Cocoa\"}}") ?? new Profile();
            store.Save(fixture);
        }
        // Test-only profile fixture: loaded by the DOMAIN just as any
        // ordinary JSON profile. The presentation layer never changes
        // authoritative credits, wave, XP or health directly.
        if (OS.GetCmdlineUserArgs().Contains("--smoke-pass53",
            StringComparer.Ordinal))
        {
            var fixture=JsonSerializer.Deserialize<Profile>(
                "{\"SchemaVersion\":1,\"Credits\":12000}") ?? new Profile();
            store.Save(fixture);
        }
        Session = new LocalSession(store);
    }

    public override void _Process(double delta)
    {
        if(Session is null)return;
        Session.Enqueue(new AdvanceStatusEffectsCommand(delta));
        Pump();
    }

    private void Pump()
    {
        if (Session is null) return;
        foreach (var gameEvent in Session.Tick(DateTimeOffset.UtcNow))
            EmitSignal(SignalName.PresentationEvent, gameEvent.GetType().Name);
    }

    private void Submit(IGameCommand command)
    {
        Session?.Enqueue(command);
        Pump();
    }

    public void StartMap(string map) => Submit(new StartMatchCommand(map));
    public void AdvanceWave() => Submit(new AdvanceWaveCommand());
    public void DamagePlayer(float amount,string source,bool bypassArmor=false) => Submit(new DamagePlayerCommand(amount,source,bypassArmor));
    public void HealPlayer(float amount,bool full=false) => Submit(new HealPlayerCommand(amount,full));
    public bool ActivateEnergyDrink()
    {
        if(Session is null)return false;
        var before=Session.State.Player.EnergyDrinkSeconds;
        Submit(new ActivateEnergyDrinkCommand());
        return Session.State.Player.EnergyDrinkSeconds>before;
    }
    public bool ActivateGasMask()
    {
        if(Session is null)return false;
        var before=Session.State.Player.GasMaskActive;
        Submit(new ActivateGasMaskCommand());
        return !before && Session.State.Player.GasMaskActive;
    }
    public void EquipBodyArmor() => Submit(new EquipBodyArmorCommand());
    public void GrantAmmo(string weapon,int amount,int maxReserve) => Submit(new GrantAmmoCommand(weapon,amount,maxReserve));
    public void ConfigureWeaponAmmo(string weapon,int magazine,int reserve) =>
        Submit(new ConfigureWeaponAmmoCommand(weapon,magazine,reserve));
    public bool GrantItem(string item,int amount=1,int maxCount=int.MaxValue)
    {
        if(Session is null)return false;
        // Pass29: authoritative 16-slot admission; rejected pickups stay in the world.
        if(!Pass24LootCapacityPolicy.CanGrant(Session.State.Player.Inventory,
                item,amount,maxCount))return false;
        var before=Session.State.Player.Inventory.GetValueOrDefault(item);
        Submit(new GrantItemCommand(item,amount,maxCount));
        return Session.State.Player.Inventory.GetValueOrDefault(item)>before;
    }
    public bool ConsumeItem(string item,int amount=1)
    {
        if(Session is null)return false;
        var before=Session.State.Player.Inventory.GetValueOrDefault(item);
        Submit(new ConsumeItemCommand(item,amount));
        return Session.State.Player.Inventory.GetValueOrDefault(item)<before;
    }
    public void AwardKill(string infectedType, int credits, int xp) => Submit(new AwardKillCommand(infectedType, credits, xp));
    public void AwardWaveSurvival(int wave, int completedObjectives, int playerCount) =>
        Submit(new AwardWaveSurvivalCommand(wave, completedObjectives, playerCount));
    public void EndWaveCleanup() => Submit(new EndWaveCleanupCommand());
    public void CompleteObjective(string objectiveId, string family) =>
        Submit(new CompleteObjectiveCommand(objectiveId, family));
    public SkinCaseService.Result OpenSkinCase(string caseName)
    {
        if(Session is null) return SkinCaseService.Result.Fail("No offline profile.");
        Submit(new OpenSkinCaseCommand(caseName));
        return Session.LastSkinAction;
    }

    public SkinCaseService.Result SellOwnedSkin(string skinId)
    {
        if(Session is null) return SkinCaseService.Result.Fail("No offline profile.");
        Submit(new SellOwnedSkinCommand(skinId));
        return Session.LastSkinAction;
    }

    public SkinCaseService.Result ApplyWeaponSkin(string weaponName,string? skinId)
    {
        if(Session is null) return SkinCaseService.Result.Fail("No offline profile.");
        Submit(new ApplyWeaponSkinCommand(weaponName,skinId));
        return Session.LastSkinAction;
    }

    public bool PurchaseWeapon(string weapon,int requiredLevel,int price)
    {
        if(Session is null)return false;
        var before=Session.Profile.Unlocks.Contains(weapon);
        Submit(new PurchaseWeaponCommand(weapon,requiredLevel,price));
        return !before && Session.Profile.Unlocks.Contains(weapon);
    }
    public bool SetLoadout(string slot,string weapon)
    {
        if(Session is null)return false;
        Submit(new SetLoadoutCommand(slot,weapon));
        return Session.Profile.Loadout.GetValueOrDefault(slot)==weapon;
    }
    public bool SetPerk(string perk,bool enabled)
    {
        if(Session is null)return false;
        var before=Session.Profile.EquippedPerks.Contains(perk);
        Submit(new SetPerkCommand(perk,enabled));
        return before!=Session.Profile.EquippedPerks.Contains(perk);
    }
    public bool HasPerk(string perk) => Session?.Profile.EquippedPerks.Contains(perk)==true;
    public void FailMatch(string reason) => Submit(new FailMatchCommand(reason));

    public bool SpendAmmo(string weapon, int amount)
    {
        if (Session is null) return false;
        var before = Session.State.Player.Ammo.GetValueOrDefault(weapon);
        Submit(new SpendAmmoCommand(weapon, amount));
        return Session.State.Player.Ammo.GetValueOrDefault(weapon) < before;
    }

    public bool ReloadWeapon(string weapon, int capacity)
    {
        if (Session is null) return false;
        var before = Session.State.Player.Ammo.GetValueOrDefault(weapon);
        Submit(new ReloadWeaponCommand(weapon, capacity));
        return Session.State.Player.Ammo.GetValueOrDefault(weapon) > before;
    }

    public void ReturnToLobby()
    {
        Session?.ReturnToLobby(DateTimeOffset.UtcNow);
        Pump();
    }

    public PlayerState? Player => Session?.State.Player;
    public MatchState? Match => Session?.State.Match;
    public Profile? Profile => Session?.Profile;

    [Signal]
    public delegate void PresentationEventEventHandler(string eventName);
}
