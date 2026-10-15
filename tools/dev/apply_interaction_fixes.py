"""Apply inspected interaction fixes without modifying original assets."""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[2]
PREFIX = 'src/Twr.Godot/Scripts/'


def transform(root=ROOT):
    pending = {}
    def edit(name, old, new):
        path = PREFIX + name
        text = pending.get(path, (root/path).read_text(encoding='utf8'))
        if new in text:
            return
        if text.count(old) != 1:
            raise ValueError(f'Unexpected source context in {name}: {old[:80]!r}')
        pending[path] = text.replace(old, new, 1)

    for signature in ('public override void _Process(double delta)', 'private bool TryCollect()'):
        old = '    ' + signature + '\n    {\n'
        returns = 'false' if signature.startswith('private bool') else ''
        edit('PickupActor.cs', old, old +
             f'        if (Player?.GameplayControlsEnabled != true) return {returns};\n'.replace('return ;', 'return;'))

    edit('FortificationController.cs',
         '    public override void _Process(double delta)\n    {\n',
         '    public override void _Process(double delta)\n    {\n'
         '        if (Player?.GameplayControlsEnabled != true)\n'
         '        {\n'
         '            if (_hammerMode)\n'
         '            {\n'
         '                _hammerMode = false;\n'
         '                _swingsCompleted = 0;\n'
         '                if (Player is not null && GodotObject.IsInstanceValid(Player))\n'
         '                    Player.SetHammerMode(false);\n'
         '            }\n'
         '            return;\n'
         '        }\n')
    for signature in ('public override void _UnhandledInput(InputEvent @event)', 'private void TrySwing()'):
        old = '    ' + signature + '\n    {\n'
        edit('FortificationController.cs', old, old +
             '        if (Player?.GameplayControlsEnabled != true) return;\n')

    edit('FortificationController.cs',
         '            Runtime = Runtime,\n            GlobalPosition = groundedPosition\n',
         '            Runtime = Runtime\n')
    edit('FortificationController.cs',
         '        _deployed.Add(actor);\n        AddChild(actor);\n        PublishStatus();',
         '        _deployed.Add(actor);\n'
         '        AddChild(actor);\n'
         '        // World transforms require an attached Node3D. _Ready only\n'
         '        // builds local visual children; position it before any tick.\n'
         '        actor.GlobalPosition = groundedPosition;\n'
         '        PublishStatus();')

    edit('FortificationActor.cs',
         '    public override void _Process(double delta)\n    {\n',
         '    public override void _Process(double delta)\n    {\n'
         '        if (Player?.GameplayControlsEnabled != true)\n'
         '        {\n'
         '            if (_mounted && Player is not null && GodotObject.IsInstanceValid(Player))\n'
         '                SetMounted(false);\n'
         '            return;\n'
         '        }\n')
    for signature in ('public override void _UnhandledInput(InputEvent @event)', 'private void Fire50Cal()'):
        old = '    ' + signature + '\n    {\n'
        edit('FortificationActor.cs', old, old +
             '        if (Player?.GameplayControlsEnabled != true) return;\n')
    edit('FortificationActor.cs',
         '        if(_mounted)Player?.SetMountedMode(false);',
         '        if(_mounted && Player is not null && GodotObject.IsInstanceValid(Player))\n'
         '            Player.SetMountedMode(false);')
    edit('FortificationActor.cs',
         '            var query=PhysicsRayQueryParameters3D.Create(origin,origin+direction*remaining);',
         '            using var query=PhysicsRayQueryParameters3D.Create(origin,origin+direction*remaining);')

    edit('RuntimeReadinessAcceptance.cs',
         '        player.SetGameplayControlsEnabled(true);\n        player._UnhandledInput(equip);',
         '        await RuntimeInteractionAcceptance.Verify(game, player, Runtime);\n'
         '        player.SetGameplayControlsEnabled(true);\n        player._UnhandledInput(equip);')
    return pending


if __name__ == '__main__':
    edits = transform()
    for relative, content in edits.items():
        (ROOT/relative).write_text(content, encoding='utf8', newline='\n')
    print(json.dumps({'interaction_source_edits': sorted(edits)}, indent=2))
