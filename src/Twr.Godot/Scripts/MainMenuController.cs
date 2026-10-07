using Godot; using Twr.Domain.Model;
namespace Twr.Godot;
public partial class MainMenuController : Control { [Export] public LocalSessionNode? Runtime {get;set;} public void StartSelectedMap(string map){if(!ReleaseRules.Maps.Contains(map))throw new ArgumentException("Map not in release set");Runtime?.StartMap(map);} }
