"""Integrate deployed presentation and reference-backed Jack timing."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def replace(path,old,new):
    p=ROOT/path;s=p.read_text(encoding='utf8')
    if new in s:return
    if s.count(old)!=1:raise RuntimeError('Source mismatch: '+path+' '+old[:60])
    p.write_text(s.replace(old,new),encoding='utf8',newline='\n')
def apply():
    path='src/Twr.Godot/Scripts/FortificationActor.cs'
    replace(path,'    public int ShotsFired76 { get; private set; }',
      '    public int ShotsFired76 { get; private set; }\n'
      '    public int RemainingShots76 => _remainingShots;\n'
      '    public FortificationPresentation DeployedVisual { get; private set; } = null!;\n'
      '    private Label3D _nameplate = null!;')
    file=ROOT/path;s=file.read_text();start=s.index('    public override void _Ready()');end=s.index('    public override void _Process(',start)
    new='''    public override void _Ready()
    {
        DeployedVisual = new FortificationPresentation
        {
            Name = "DeployedVisual", FortificationType = Definition.Name
        };
        AddChild(DeployedVisual);
        _nameplate = new Label3D
        {
            Text = Definition.Name=="50 Cal" ? "50 CAL [E] | 21/21" : Definition.Name,
            Position = new Vector3(0, 1.7f, 0),
            FontSize = 30,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled
        };
        AddChild(_nameplate);
    }

'''
    if 'DeployedVisual = new FortificationPresentation' not in s:
        file.write_text(s[:start]+new+s[end:],encoding='utf8',newline='\n')
    replace(path,
      '        Player.SetMountedAim(_toggleAim || Input.IsMouseButtonPressed(MouseButton.Right));',
      '        Player.SetMountedAim(_toggleAim || Input.IsMouseButtonPressed(MouseButton.Right));\n'
      '        DeployedVisual.Aim(Player.AimDirection);')
    replace(path,'        ShotsFired76++;',
      '        ShotsFired76++;\n'
      '        DeployedVisual.Aim(Player.AimDirection);\n'
      '        DeployedVisual.PresentShot();\n'
      '        _nameplate.Text = $"50 CAL [E] | {_remainingShots}/21";')
    replace('src/Twr.Godot/Scripts/RuntimeReadinessAcceptance.cs',
      '        await RuntimePickupVisualAcceptance.Verify(game);',
      '        await RuntimePickupVisualAcceptance.Verify(game);\n'
      '        await DeployedFortificationAcceptance.Verify(game, player, Runtime);')
    replace('src/Twr.Godot/Scripts/RuntimeInteractionAcceptance.cs',
      '        forts._UnhandledInput(hammer);\n        forts.ClearDeployed();',
      '        if (deployed.DeployedVisual.WireSegments < 96)\n'
      '            throw new InvalidOperationException("Actual hammer deployment lost its wire-coil model");\n'
      '        forts._UnhandledInput(hammer);\n        forts.ClearDeployed();')
    replace('src/Twr.Godot/Scripts/Pass76ObjectiveFortificationAcceptance.cs',
      '        await Frames(3);\n        if (turret.ShotsFired76 != 1 ||',
      '        if (!turret.DeployedVisual.ShotFlashVisible || turret.RemainingShots76 != 20)\n'
      '            throw new InvalidOperationException("A real mounted shot did not update its presentation and ammunition");\n'
      '        await Frames(3);\n        if (turret.ShotsFired76 != 1 ||')
    replace('src/Twr.Godot/Scripts/Bootstrap.cs',
      '        var args=OS.GetCmdlineUserArgs();\n',
      '        var args=OS.GetCmdlineUserArgs();\n'
      '        if(args.Contains("--capture-presentation", StringComparer.Ordinal))\n'
      '        {\n'
      '            AddChild(new RuntimePresentationCapture { Runtime=_runtime,Name="RuntimePresentationCapture" });\n'
      '            return;\n'
      '        }\n')
    replace('src/Twr.Godot/Scripts/LocalSessionNode.cs',
      '        var store = new JsonProfileStore(save);\n',
      '        var presentationCapture = OS.GetCmdlineUserArgs().Contains("--capture-presentation", StringComparer.Ordinal);\n'
      '        if (presentationCapture) save = ProjectSettings.GlobalizePath("user://presentation-capture-profile.json");\n'
      '        var store = new JsonProfileStore(save);\n'
      '        if (presentationCapture) store.Save(new Profile());\n')
    replace('src/Twr.Godot/Scripts/RuntimeReadinessAcceptance.cs',
      '        await DeployedFortificationAcceptance.Verify(game, player, Runtime);',
      '        await DeployedFortificationAcceptance.Verify(game, player, Runtime);\n'
      '        await TimedJackAcceptance.Verify(game, player, Runtime);')
    path='src/Twr.Godot/Scripts/FortificationActor.cs'
    replace(path,'    private Label3D _nameplate = null!;',
      '    private Label3D _nameplate = null!;\n'
      '    private double _jackRemaining = JackDetonationRules.ReferenceDelaySeconds;\n'
      '    private bool _detonated;')
    replace(path,
      '        if (Definition.Damage.HasValue && Definition.Radius.HasValue)\n            TickAreaDevice();',
      '        if (Definition.Name == "Jack")\n'
      '        {\n'
      '            _jackRemaining = Math.Max(0, _jackRemaining - Math.Max(0, delta));\n'
      '            _nameplate.Text = $"JACK | {Math.Ceiling(_jackRemaining)}s";\n'
      '            if (_jackRemaining <= 0) DetonateAreaDevice(true);\n'
      '            return;\n'
      '        }\n'
      '        if (Definition.Damage.HasValue && Definition.Radius.HasValue)\n            TickAreaDevice();')
    replace(path,
      '        if (trigger is null) return;\n\n        // STRONGLY INFERRED:',
      '        if (trigger is null) return;\n'
      '        DetonateAreaDevice(false);\n'
      '    }\n\n'
      '    private void DetonateAreaDevice(bool timedJack)\n'
      '    {\n'
      '        if (_detonated || IsQueuedForDeletion()) return;\n'
      '        _detonated = true;\n'
      '        // Existing synthesized cue, not the unrecovered original Jack audio.\n'
      '        Player?.Audio?.Play("launcher");\n\n'
      '        // STRONGLY INFERRED:')
    replace(path,
      '                infected.ApplyDamage(Definition.Damage!.Value*DamageMultiplier,\n                    false, "Explosion");',
      '                infected.ApplyDamage(Definition.Damage!.Value*DamageMultiplier *\n'
      '                    (timedJack ? JackDetonationRules.ApproximateFalloff(\n'
      '                        GlobalPosition.DistanceTo(infected.GlobalPosition),radius) : 1f),\n'
      '                    false, "Explosion");')
    replace(path,
      '                tanker.ApplyDamage(Definition.Damage!.Value*DamageMultiplier, "Explosive");',
      '                tanker.ApplyDamage(Definition.Damage!.Value*DamageMultiplier *\n'
      '                    (timedJack ? JackDetonationRules.ApproximateFalloff(\n'
      '                        GlobalPosition.DistanceTo(tanker.GlobalPosition),radius) : 1f), "Explosive");')
if __name__=='__main__': apply()
