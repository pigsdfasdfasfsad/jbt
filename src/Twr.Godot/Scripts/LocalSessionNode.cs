using Godot;
using Twr.Domain.Contracts;
using Twr.Domain.Contracts.Commands;
using Twr.Domain.Model;
using Twr.Domain.Persistence;
using Twr.Domain.Runtime;

namespace Twr.Godot;

public partial class LocalSessionNode : Node
{
    public LocalSession? Session { get; private set; }

    public override void _Ready()
    {
        var save = ProjectSettings.GlobalizePath("user://profile.json");
        Session = new LocalSession(new JsonProfileStore(save));
    }

    public override void _Process(double delta) => Pump();

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
    public void EquipBodyArmor() => Submit(new EquipBodyArmorCommand());
    public void GrantAmmo(string weapon,int amount,int maxReserve) => Submit(new GrantAmmoCommand(weapon,amount,maxReserve));
    public void GrantItem(string item,int amount=1) => Submit(new GrantItemCommand(item,amount));
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

    [Signal]
    public delegate void PresentationEventEventHandler(string eventName);
}
