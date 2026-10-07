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
    private readonly ObjectiveService _objectives;
    private readonly EconomyService _economy;
    private readonly CompletionRewardService _completion;
    private readonly SaveCoordinator _save;
    private readonly StarterLoadoutService _starterLoadout = new();
    private readonly KillRewardService _killRewards;
    private readonly IProfileStore _profiles;

    public GameState State { get; } = new();
    public Profile Profile { get; private set; }

    public LocalSession(IProfileStore profiles)
    {
        _profiles = profiles;
        Profile = profiles.Load();
        State.Player.Level = Profile.Level;
        State.Player.Xp = Profile.Xp;
        State.Player.Credits = Profile.Credits;
        _match = new(_events);
        _damage = new(new DamagePolicy(), _events);
        _ammo = new(_events);
        _objectives = new(_events);
        _economy = new(_events);
        _completion = new(new CompletionRewardPolicy(), new ReceiptLedger());
        _save = new(profiles, _events);
        _killRewards = new(_events);
    }

    public void Enqueue(IGameCommand command) => _commands.Enqueue(command);

    public IReadOnlyList<IGameEvent> Tick(DateTimeOffset now)
    {
        while (_commands.TryDequeue(out var command) && command is not null)
            Handle(command, now);

        return _events.Drain();
    }

    private void Handle(IGameCommand command, DateTimeOffset now)
    {
        switch (command)
        {
            case StartMatchCommand x:
                _match.Start(State.Match, x.MapName, now);
                _starterLoadout.ResetForMatch(State.Player);
                break;
            case AdvanceWaveCommand:
                _match.Advance(State.Match, now);
                break;
            case DamagePlayerCommand x:
                _damage.Apply(State.Player, x.Amount, x.Source, now);
                break;
            case SpendAmmoCommand x:
                _ammo.Spend(State.Player, x.WeaponId, x.Amount, now);
                break;
            case ReloadWeaponCommand x:
                _ammo.Reload(State.Player, x.WeaponId, x.MagazineCapacity, now);
                break;
            case AwardKillCommand x:
                _killRewards.Award(State.Player, x.InfectedType, x.Credits, x.Xp, now);
                break;
            case CompleteObjectiveCommand x:
                _objectives.Complete(State.Match, x.ObjectiveId, now);
                break;
            case PurchaseCommand x:
                _economy.Purchase(State.Player, x.ItemId, x.Price, now);
                break;
            default:
                throw new NotSupportedException(command.GetType().Name);
        }

        if (_match.IsMapComplete(State.Match) && !State.Match.CompletionAwarded)
        {
            var xp = _completion.Award(State.Player, State.Match.MapName);
            State.Match.CompletionAwarded = true;
            Profile.Xp = State.Player.Xp;
            Profile.Credits = State.Player.Credits;
            Profile.Level = State.Player.Level;
            _save.Save(Profile, "MapCompletion", now);
            State.Match.SaveCommitted = true;
            State.Match.Phase = MatchPhase.Results;
            _events.Publish(new MatchCompletedEvent(State.Match.MapName, State.Match.Wave, xp, now));
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
