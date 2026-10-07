using Godot; namespace Twr.Godot; public partial class PauseController : CanvasLayer { public void SetPaused(bool paused)=>GetTree().Paused=paused; }
