using Godot; using Twr.Domain.Contracts.Commands;
namespace Twr.Godot;
public partial class InteractionController : Node { [Export] public LocalSessionNode? Runtime {get;set;} public void CompleteObjective(string id)=>Runtime?.Session?.Enqueue(new CompleteObjectiveCommand(id)); }
