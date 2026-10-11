using Godot;
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
        var save = ProjectSettings.GlobalizePath("user://profile.json");
        Session = new LocalSession(new JsonProfileStore(save));
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
