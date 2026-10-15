using Godot;

namespace Twr.Godot;

/// <summary>Normal window close follows the same tested shutdown path.</summary>
public partial class RuntimeWindowCloseHandler : Node
{
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        GetTree().AutoAcceptQuit = false;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && IsInsideTree())
            RuntimeShutdownCoordinator.Request(GetTree(), 0);
    }
}
