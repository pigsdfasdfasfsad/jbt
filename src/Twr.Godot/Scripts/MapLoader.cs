using Godot; using Twr.Domain.Model;
namespace Twr.Godot; public partial class MapLoader : Node3D { public bool IsReleaseMap(string map)=>ReleaseRules.Maps.Contains(map,StringComparer.Ordinal); }
