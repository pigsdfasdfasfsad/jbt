using Godot;
namespace Twr.Godot;
// Presentation-only: reacts to domain events/read models; never mutates XP, ammo, health, wave, rewards or objectives.
public partial class HudController : Control { public void OnPresentationEvent(string eventName){ QueueRedraw(); } }
