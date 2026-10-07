using Godot; using Twr.Domain.Persistence; using Twr.Domain.Runtime; using Twr.Domain.Contracts.Commands;
namespace Twr.Godot;
public partial class LocalSessionNode : Node {
    public LocalSession? Session {get;private set;}
    public override void _Ready(){var save=ProjectSettings.GlobalizePath("user://profile.json");Session=new LocalSession(new JsonProfileStore(save));}
    public override void _Process(double delta){ if(Session is null)return; foreach(var e in Session.Tick(DateTimeOffset.UtcNow)) EmitSignal(SignalName.PresentationEvent,e.GetType().Name); }
    public void StartMap(string map)=>Session?.Enqueue(new StartMatchCommand(map));
    [Signal] public delegate void PresentationEventEventHandler(string eventName);
}
