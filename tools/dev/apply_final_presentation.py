"""Inspected finishing edits for the source-fidelity release checkpoint."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]

def replace(path,old,new):
    file=ROOT/path;text=file.read_text(encoding='utf8')
    if new in text:return
    if text.count(old)!=1:raise RuntimeError('Changed integration anchor: '+path)
    file.write_text(text.replace(old,new),encoding='utf8',newline='\n')

def apply():
    replace('src/Twr.Godot/Scripts/FirstPersonPlayer.cs',
        '        _camera.AddChild(_viewModel);',
        '        _camera.AddChild(_viewModel);\n'
        '        _camera.AddChild(new FirstPersonModelLighting\n'
        '        {\n'
        '            Name="FirstPersonModelLight", Model=_viewModel\n'
        '        });')
    replace('src/Twr.Godot/Scripts/RuntimeReadinessAcceptance.cs',
        '        await RuntimeInteractionAcceptance.Verify(game, player, Runtime);',
        '        await RuntimeInteractionAcceptance.Verify(game, player, Runtime);\n'
        '        FirstPersonModelLighting.Verify(player);')
    replace('src/Twr.Godot/Scripts/RuntimePresentationCapture.cs',
        '            await Frames(90);\n            player.SetGameplayControlsEnabled(false);',
        '            await Frames(90);\n'
        '            // Let the actual gameplay HUD read authoritative ammo,\n'
        '            // health and match state before capturing its view.\n'
        '            game.SetProcess(true);\n'
        '            await Frames(3);\n'
        '            game.SetProcess(false);\n'
        '            player.SetGameplayControlsEnabled(false);')
    replace('src/Twr.Godot/Scripts/RuntimePresentationCapture.cs',
        'Position=new Vector3(0,4.5f,12.5f)',
        'Position=new Vector3(0,3.2f,9.0f)')
    path='src/Twr.Godot/Scripts/TimedJackAcceptance.cs'
    replace(path,
        '        var blocked=Target("OccludedTarget",origin+new Vector3(0,.8f,3f));',
        '        var blocked=Target("OccludedTarget",origin+new Vector3(0,.8f,3f));\n'
        '        var tanker=new DamageObjectiveTarget\n'
        '        { Name="JackDamageObjectiveTarget", Position=origin+new Vector3(-4,0,-3) };\n'
        '        holder.AddChild(tanker);')
    replace(path,
        'if(near.Health!=1000 || far.Health!=1000 || blocked.Health!=1000 || jack.IsQueuedForDeletion())',
        'if(near.Health!=1000 || far.Health!=1000 || blocked.Health!=1000 || tanker.Health!=2500 || jack.IsQueuedForDeletion())')
    replace(path,
        'if(!jack.IsQueuedForDeletion() || nearDamage<=farDamage || farDamage<=0 || blocked.Health!=1000)',
        'if(!jack.IsQueuedForDeletion() || nearDamage<=farDamage || farDamage<=0 || blocked.Health!=1000 || tanker.Health>=2500)')
    replace(path,
        '            jack._Process(100);',
        '            var tankerAfterFirst=tanker.Health;\n'
        '            jack._Process(100);\n'
        '            if(tanker.Health!=tankerAfterFirst)\n'
        '                throw new InvalidOperationException("Jack damaged the objective twice");')
    replace(path,
        '                " wall_blocked=true detonation_once=true source_peak_damage="+jack.Definition.Damage+',
        '                " wall_blocked=true detonation_once=true actual_tanker_damage="+(2500-tanker.Health)+\n'
        '                " source_peak_damage="+jack.Definition.Damage+')

if __name__=='__main__':apply()
