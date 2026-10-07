using Godot; namespace Twr.Godot; public partial class PresentationRouter : Node { public void Present(string eventName){ GD.Print($"Presentation event: {eventName}"); } }
