"""Apply inspected, idempotent runtime fixes; refuse mismatched source contexts.

The preparation job commits only this script's explicit C#/test edits before
compilation. No source archives, generated content, or private assets are read.
All edits are staged in memory before any file is replaced.
"""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[2]
PREFIX = "src/Twr.Godot/Scripts/"


def transform(root=ROOT):
    pending = {}

    def edit(path, before, after):
        full = root / path
        source = pending.get(path, full.read_text(encoding="utf-8"))
        if after in source:
            return
        if source.count(before) != 1:
            raise ValueError(f"Expected unique source context in {path}: {before[:90]!r}")
        pending[path] = source.replace(before, after, 1)

    player = PREFIX + "FirstPersonPlayer.cs"
    edit(player, "    public LocalSessionNode? Runtime { get; set; }\n",
         "    public LocalSessionNode? Runtime { get; set; }\n"
         "    public bool GameplayControlsEnabled { get; private set; } = true;\n"
         "\n"
         "    public void SetGameplayControlsEnabled(bool enabled)\n"
         "    {\n"
         "        GameplayControlsEnabled = enabled;\n"
         "        SetProcessUnhandledInput(enabled);\n"
         "        if (enabled) return;\n"
         "        Velocity = Vector3.Zero;\n"
         "        _reloadTimer = 0;\n"
         "        _reloadWeapon = null;\n"
         "        _jumpRequested = false;\n"
         "        _jumpRequestRemaining73 = 0;\n"
         "        _aiming = false;\n"
         "        if (_camera is not null && GodotObject.IsInstanceValid(_camera))\n"
         "        {\n"
         "            ResetSourceAimVisual67();\n"
         "            ResetSourceScope69();\n"
         "        }\n"
         "    }\n")
    for signature in (
        "    public override void _PhysicsProcess(double delta)",
        "    public override void _UnhandledInput(InputEvent @event)",
        "    private void BeginReload()",
        "    private void TryUseWeapon()",
        "    private void TryThrowThrowable()",
    ):
        before = signature + "\n    {\n"
        edit(player, before, before + "        if (!GameplayControlsEnabled) return;\n")

    game = PREFIX + "GameplayRoot.cs"
    edit(game, "            _player.SetPhysicsProcess(false);\n",
         "            _player.SetGameplayControlsEnabled(false);\n"
         "            _player.SetPhysicsProcess(false);\n")
    edit(game, "            _player.SetPhysicsProcess(true);\n",
         "            _player.SetGameplayControlsEnabled(true);\n"
         "            _player.SetPhysicsProcess(true);\n")
    edit(game,
         "        if (Runtime?.Player is null || Runtime.Match is null) return;\n"
         "        // Do not advance source gameplay wave timers",
         "        if (Runtime?.Player is null || Runtime.Match is null) return;\n"
         "        // Death must win over pending source/physics readiness.\n"
         "        // Otherwise _PhysicsProcess stops planning for a dead player\n"
         "        // while _Process waits forever for that plan to finish.\n"
         "        if (!_finished && !Runtime.Player.IsAlive)\n"
         "        {\n"
         "            Finish(false, \"YOU DIED\");\n"
         "            return;\n"
         "        }\n"
         "        // Do not advance source gameplay wave timers")
    edit(game,
         "        _finished = true;\n        _sourceObjectivePlanPending123 = false;\n",
         "        _finished = true;\n"
         "        _sourcePlayerPlacementPending121 = false;\n"
         "        _pendingOriginalLoot120 = false;\n"
         "        _naturalRespawns.Clear();\n"
         "        _player.SetGameplayControlsEnabled(false);\n"
         "        StopTransientActors();\n"
         "        _sourceObjectivePlanPending123 = false;\n")
    edit(game,
         "    private void OnPresentationEvent(string eventName)\n    {\n",
         "    private void OnPresentationEvent(string eventName)\n    {\n"
         "        if (_finished || !IsInsideTree() || IsQueuedForDeletion()) return;\n")
    edit(game,
         "    private void ClearInfected()\n",
         "    private void StopTransientActors()\n"
         "    {\n"
         "        foreach (var child in GetChildren())\n"
         "        {\n"
         "            if (child is not (SporeCloudRuntime or SporeProjectileRuntime or\n"
         "                ThrowableProjectileRuntime or SupplyDropRuntime)) continue;\n"
         "            if (child is SupplyDropRuntime drop) drop.Landed = null;\n"
         "            child.SetProcess(false);\n"
         "            child.SetPhysicsProcess(false);\n"
         "            child.QueueFree();\n"
         "        }\n"
         "        _supplyDrops.Clear();\n"
         "        _queuedSupplySeconds = -1;\n"
         "        _supplyQueuedForNextWave = false;\n"
         "    }\n\n"
         "    private void ClearInfected()\n")
    edit(game,
         "            RetirePass43Traversal(infected);\n            infected.QueueFree();",
         "            RetirePass43Traversal(infected);\n"
         "            infected.Died = null;\n"
         "            infected.SpecialAttackRequested = null;\n"
         "            infected.QueueFree();")

    for file in ("Pass90EscortCompletionAcceptance.cs", "Pass94ManorEscortAcceptance.cs"):
        path = PREFIX + file
        for code in (0, 1):
            edit(path, f"            GetTree().Quit({code});",
                 f"            RuntimeShutdownCoordinator.Request(GetTree(), {code});")
    edit(PREFIX + "Pass92ExpresswayEscortAcceptance.cs",
         '        GetTree().CallDeferred("quit", exitCode);',
         '        RuntimeShutdownCoordinator.Request(GetTree(), exitCode);')
    edit("tests/test_pass118_native_exit.py",
         "    assert 'GetTree().CallDeferred(\"quit\", exitCode);' in source",
         "    assert 'RuntimeShutdownCoordinator.Request(GetTree(), exitCode);' in source\n"
         "    shutdown = read(\"src/Twr.Godot/Scripts/RuntimeShutdownCoordinator.cs\")\n"
         "    assert 'tree.CallDeferred(\"quit\", _exitCode);' in shutdown\n"
         "    assert 'GC.WaitForPendingFinalizers();' in shutdown\n"
         "    assert 'child.QueueFree();' in shutdown")

    bootstrap = PREFIX + "Bootstrap.cs"
    edit(bootstrap,
         "        var args=OS.GetCmdlineUserArgs();\n",
         "        AddChild(new RuntimeWindowCloseHandler { Name = \"RuntimeWindowClose\" });\n"
         "        var args=OS.GetCmdlineUserArgs();\n"
         "        if(args.Contains(\"--smoke-runtime-readiness\",StringComparer.Ordinal))\n"
         "        {\n"
         "            AddChild(new RuntimeReadinessAcceptance { Runtime = _runtime });\n"
         "            return;\n"
         "        }\n")
    session = PREFIX + "LocalSessionNode.cs"
    edit(session,
         "        var store = new JsonProfileStore(save);\n",
         "        var runtimeReadinessSmoke = OS.GetCmdlineUserArgs().Contains(\n"
         "            \"--smoke-runtime-readiness\", StringComparer.Ordinal);\n"
         "        if (runtimeReadinessSmoke)\n"
         "            save = ProjectSettings.GlobalizePath(\n"
         "                \"user://runtime-readiness-smoke-profile.json\");\n"
         "        var store = new JsonProfileStore(save);\n"
         "        if (runtimeReadinessSmoke) store.Save(new Profile());\n")
    return pending


def main():
    pending = transform()
    for relative, content in pending.items():
        (ROOT / relative).write_text(content, encoding="utf-8", newline="\n")
    print(json.dumps({"runtime_source_edits": sorted(pending)}, indent=2))


if __name__ == "__main__":
    main()
