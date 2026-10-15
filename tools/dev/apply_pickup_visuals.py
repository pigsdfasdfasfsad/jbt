"""Replace only the pickup presentation; preserve existing gameplay handlers."""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[2]
PREFIX = 'src/Twr.Godot/Scripts/'


def transform(root=ROOT):
    pending = {}
    def edit(name, old, new):
        path = PREFIX + name
        source = pending.get(path, (root/path).read_text(encoding='utf8'))
        if new in source:
            return
        if source.count(old) != 1:
            raise ValueError(f'Unexpected pickup source context: {name}')
        pending[path] = source.replace(old, new, 1)

    old_visual = '''        AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh
            {
                Size = new Vector3(0.55f, 0.35f, 0.55f),
                Material = new StandardMaterial3D
                {
                    AlbedoColor = PickupColor(PickupType),
                    Roughness = 0.8f
                }
            }
        });'''
    edit('PickupActor.cs', old_visual,
         '        // Locally reconstructed silhouettes; no original asset pixels.\n'
         '        // Bounds preserve the existing grounded pickup envelope.\n'
         '        AddChild(ReconstructedPickupVisuals.Build(PickupType, PickupColor(PickupType)));')
    edit('RuntimeReadinessAcceptance.cs',
         '        await RuntimeInteractionAcceptance.Verify(game, player, Runtime);',
         '        await RuntimePickupVisualAcceptance.Verify(game);\n'
         '        await RuntimeInteractionAcceptance.Verify(game, player, Runtime);')
    return pending


if __name__ == '__main__':
    changes = transform()
    for relative, content in changes.items():
        (ROOT/relative).write_text(content, encoding='utf8', newline='\n')
    print(json.dumps({'pickup_visual_source_edits': sorted(changes)}, indent=2))
