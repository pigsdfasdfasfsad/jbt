using Twr.Domain.Contracts;
using Twr.Domain.Contracts.Commands;
using Twr.Domain.Contracts.Events;
using Twr.Domain.Core;
using Twr.Domain.Model;
using Twr.Domain.Persistence;
using Twr.Domain.Policies;
using Twr.Domain.Services;

namespace Twr.Domain.Runtime;

public sealed class LocalSession
{
    private readonly EventStream _events = new();
    private readonly CommandBus _commands = new();
    private readonly MatchDirector _match;
    private readonly DamageService _damage;
    private readonly AmmoService _ammo;
    private readonly HealingService _healing;
    private readonly InventoryService _inventory;
    private readonly ObjectiveService _objectives;
    private readonly EconomyService _economy;
    private readonly SaveCoordinator _save;
    private readonly StarterLoadoutService _starterLoadout = new();
    private readonly KillRewardService _killRewards;
    private readonly WaveRewardService _waveRewards;
    private readonly ProgressionService _progression;
    private readonly ArmoryService _armory;
    private readonly PerkService _perks;
    private readonly ConsumableService _consumables;

    public GameState State { get; } = new();
    public Profile Profile { get; private set; }

    public LocalSession(IProfileStore profiles)
    {
        Profile = profiles.Load();
        Profile.Unlocks.Add(StarterLoadoutService.SawnOff);
        Profile.Unlocks.Add(StarterLoadoutService.Glock17);
        Profile.Unlocks.Add(StarterLoadoutService.TwoByFour);
        Profile.Loadout.TryAdd("Primary", StarterLoadoutService.SawnOff);
        Profile.Loadout.TryAdd("Secondary", StarterLoadoutService.Glock17);
        Profile.Loadout.TryAdd("Melee", StarterLoadoutService.TwoByFour);
        State.Player.Level = Profile.Level;
        State.Player.Xp = Profile.Xp;
        State.Player.Credits = Profile.Credits;
        _match = new(_events);
        _damage = new(new DamagePolicy(), _events);
        _ammo = new(_events);
        _healing = new(_events);
        _inventory = new(_events);
        _objectives = new(new ObjectiveRewardPolicy(), _events);
        _economy = new(_events);
        _save = new(profiles, _events);
        _killRewards = new(_events);
        _waveRewards = new(new ReceiptLedger(), _events);
        _progression = new(_events);
        _armory = new(_events);
        _perks = new(_events);
        _consumables = new(_events);
    }

    public void Enqueue(IGameCommand command) => _commands.Enqueue(command);

    public IReadOnlyList<IGameEvent> Tick(DateTimeOffset now)
    {
        while (_commands.TryDequeue(out var command) && command is not null)
            Handle(command, now);
        return _events.Drain();
    }

    private void PersistProfile(string reason, DateTimeOffset now)
    {
        Profile.Xp = State.Player.Xp;
        Profile.Credits = State.Player.Credits;
        Profile.Level = State.Player.Level;
        _save.Save(Profile, reason, now);
    }

    private void Handle(IGameCommand command, DateTimeOffset now)
    {
        switch (command)
        {
            case StartMatchCommand x:
                _match.Start(State.Match, x.MapName, now);
                _starterLoadout.ResetForMatch(State.Player);
                if(Profile.EquippedPerks.Contains("Juggernaut"))
                {
                    State.Player.ArmorDurability=80;
                    State.Player.ArmorKind="Juggernaut";
                    _events.Publish(new ArmorChangedEvent(80,now));
                }
                break;
            case AdvanceWaveCommand:
                _match.Advance(State.Match, now);
                break;
            case DamagePlayerCommand x:
                _damage.Apply(State.Player, x.Amount, x.Source, now, x.BypassArmor);
                break;
            case SpendAmmoCommand x:
                _ammo.Spend(State.Player, x.WeaponId, x.Amount, now);
                break;
            case ReloadWeaponCommand x:
                _ammo.Reload(State.Player, x.WeaponId, x.MagazineCapacity, now);
                break;
            case ConfigureWeaponAmmoCommand x:
                _ammo.Configure(State.Player, x.WeaponId, x.Magazine, x.Reserve, now);
                break;
            case GrantAmmoCommand x:
                _ammo.GrantReserve(State.Player, x.WeaponId, x.Amount, x.MaxReserve, now);
                break;
            case HealPlayerCommand x:
                _healing.Apply(State.Player, x.Amount, x.Full, now);
                break;
            case EquipBodyArmorCommand:
                _healing.EquipBodyArmor(State.Player, now);
                break;
            case GrantItemCommand x:
                _inventory.Grant(State.Player, x.ItemId, x.Amount, x.MaxCount, now);
                break;
            case ConsumeItemCommand x:
                _inventory.Consume(State.Player, x.ItemId, x.Amount, now);
                break;
            case AwardKillCommand x:
                _killRewards.Award(State.Player, x.InfectedType, x.Credits, x.Xp, now);
                break;
            case AwardWaveSurvivalCommand x:
                if (_waveRewards.Award(State.Player, State.Match.MapName, x.Wave, x.CompletedObjectives, x.PlayerCount, now))
                    PersistProfile($"Wave{x.Wave}Survival", now);
                break;
            case EndWaveCleanupCommand:
                if(State.Player.ArmorKind=="Body" && State.Player.ArmorDurability>0)
                {
                    State.Player.ArmorDurability=0;
                    State.Player.ArmorKind="";
                    _events.Publish(new ArmorChangedEvent(0,now));
                }
                State.Player.GasMaskActive=false;
                State.Player.EnergyDrinkSeconds=0;
                break;
            case CompleteObjectiveCommand x:
                if (_objectives.Complete(State.Match, State.Player, x.ObjectiveId, x.Family, now))
                    PersistProfile($"Objective:{x.ObjectiveId}", now);
                break;
            case PurchaseWeaponCommand x:
                if (_armory.Purchase(Profile,State.Player,x.WeaponId,x.RequiredLevel,x.Price,now))
                    PersistProfile("WeaponPurchase:" + x.WeaponId,now);
                break;
            case SetLoadoutCommand x:
                if (_armory.Equip(Profile,x.Slot,x.WeaponId,now))
                    PersistProfile("Loadout:" + x.Slot,now);
                break;
            case SetPerkCommand x:
                if(_perks.Set(Profile,State.Player.Level,x.PerkName,x.Enabled,now))
                    PersistProfile("Perk:" + x.PerkName,now);
                break;
            case PurchaseCommand x:
                _economy.Purchase(State.Player, x.ItemId, x.Price, now);
                break;
            case FailMatchCommand x:
                State.Player.Inventory.Clear();
                State.Player.ArmorDurability=0;
                State.Player.ArmorKind="";
                State.Player.GasMaskActive=false;
                State.Player.EnergyDrinkSeconds=0;
                PersistProfile("MatchFailure", now);
                State.Match.SaveCommitted = true;
                _match.Fail(State.Match);
                _events.Publish(new MatchFailedEvent(State.Match.MapName, State.Match.Wave, x.Reason, now));
                break;
            default:
                throw new NotSupportedException(command.GetType().Name);
        }

        if (_progression.Apply(State.Player,now)>0)
            PersistProfile("LevelUp",now);

        if (_match.IsMapComplete(State.Match) && !State.Match.CompletionAwarded)
        {
            State.Match.CompletionAwarded = true;
            PersistProfile("MapCompletion", now);
            State.Match.SaveCommitted = true;
            State.Match.Phase=MatchPhase.Results;
            _events.Publish(new MatchCompletedEvent(State.Match.MapName, State.Match.Wave, 0, now));
        }
    }

    public void ReturnToLobby(DateTimeOffset now)
    {
        if (State.Match.Phase != MatchPhase.Results || !State.Match.SaveCommitted)
            throw new InvalidOperationException("Results require committed completion save");
        State.Match.Phase = MatchPhase.Lobby;
        _events.Publish(new ReturnToLobbyEvent(now));
    }
}
