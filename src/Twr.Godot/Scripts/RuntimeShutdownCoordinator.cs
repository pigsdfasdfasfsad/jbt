using System;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Retires managed scene wrappers while the native engine is still alive.
/// Does not bypass process exit or turn a failing acceptance into success.
/// </summary>
public partial class RuntimeShutdownCoordinator : Node
{
    private const string CoordinatorName = "RuntimeShutdownCoordinator";
    private int _exitCode;
    private int _phase;
    private int _physicsTicks;
    private int _drainUntil;
    private ulong _started;

    public static void Request(SceneTree tree, int exitCode)
    {
        ArgumentNullException.ThrowIfNull(tree);
        var root = tree.Root;
        // A request can originate inside main-scene _Ready, while root
        // rejects synchronous AddChild. Reserve one pending coordinator
        // before deferring the attachment, so repeated close events cannot
        // create duplicates or lose a requested failure exit code.
        var existing = root.HasMeta(CoordinatorName)
            ? root.GetMeta(CoordinatorName).AsGodotObject() as RuntimeShutdownCoordinator
            : root.GetNodeOrNull<RuntimeShutdownCoordinator>(CoordinatorName);
        if (existing is not null && GodotObject.IsInstanceValid(existing))
        {
            if (exitCode != 0) existing._exitCode = exitCode;
            return;
        }
        tree.AutoAcceptQuit = false;
        tree.Paused = false;
        var coordinator = new RuntimeShutdownCoordinator
        {
            Name = CoordinatorName,
            ProcessMode = ProcessModeEnum.Always,
            _exitCode = exitCode,
            _started = Time.GetTicksMsec()
        };
        root.SetMeta(CoordinatorName, coordinator);
        root.CallDeferred(Node.MethodName.AddChild, coordinator);
    }

    public override void _Ready()
    {
        // The attached node name now provides normal request deduplication.
        var root = GetTree().Root;
        if (root.HasMeta(CoordinatorName) &&
            ReferenceEquals(root.GetMeta(CoordinatorName).AsGodotObject(), this))
            root.RemoveMeta(CoordinatorName);
    }

    public override void _PhysicsProcess(double delta) => _physicsTicks++;

    public override void _Process(double delta)
    {
        var tree = GetTree();
        try
        {
            if (Time.GetTicksMsec() - _started > 10000)
                throw new TimeoutException("Native scene shutdown did not drain");
            if (_phase == 0)
            {
                foreach (var child in tree.Root.GetChildren())
                {
                    if (ReferenceEquals(child, this)) continue;
                    SaveOpenSessions(child);
                    child.ProcessMode = ProcessModeEnum.Disabled;
                    child.QueueFree();
                }
                _phase = 1;
                return;
            }
            if (_phase == 1)
            {
                if (tree.Root.GetChildCount() > 1) return;
                // Wait for physics deletion work, not merely two fast
                // render frames on an uncapped headless process.
                _drainUntil = _physicsTicks + 2;
                _phase = 2;
                return;
            }
            if (_phase == 2)
            {
                if (_physicsTicks < _drainUntil) return;
                // Shutdown only: finalize unused wrappers before the
                // native SceneTree/PhysicsServer begin final teardown.
                GC.Collect();
                GC.WaitForPendingFinalizers();
                _phase = 3;
                return;
            }
            if (_phase == 3)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                _phase = 4;
                return;
            }
            GD.Print("TWR_RUNTIME_PREQUIT_OK exit_code=" + _exitCode +
                " scene_nodes_retired=true physics_drained=true finalizers_drained=true");
            SetProcess(false);
            SetPhysicsProcess(false);
            tree.CallDeferred("quit", _exitCode);
        }
        catch (Exception error)
        {
            // Native memory corruption is not caught or accepted as success.
            GD.PushError("TWR_RUNTIME_PREQUIT_FAILED " + error);
            _exitCode = _exitCode == 0 ? 1 : _exitCode;
            SetProcess(false);
            SetPhysicsProcess(false);
            tree.CallDeferred("quit", _exitCode);
        }
    }

    private static void SaveOpenSessions(Node node)
    {
        if (node is LocalSessionNode runtime && runtime.Match is not null &&
            runtime.Match.Phase != MatchPhase.Lobby &&
            runtime.Match.Phase != MatchPhase.Results)
            runtime.FailMatch("Player closed application");
        foreach (var child in node.GetChildren()) SaveOpenSessions(child);
    }
}
