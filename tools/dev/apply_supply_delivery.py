"""Inspected complete supply request, physical landing, payload and cleanup edits."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]

def replace(path,old,new):
    p=ROOT/path;s=p.read_text(encoding='utf8')
    if new in s:return
    if s.count(old)!=1:raise RuntimeError('Changed source anchor: '+path+' '+old[:80])
    p.write_text(s.replace(old,new),encoding='utf8',newline='\n')

def apply():
    game='src/Twr.Godot/Scripts/GameplayRoot.cs'
    replace(game,
        '        var destination = new Vector3(_rng.RandfRange(-8f, 8f), 0.3f, _rng.RandfRange(-20f, 20f));',
        '        if (_finished || Runtime?.Player?.IsAlive != true) return;\n'
        '        var destination = _player.GlobalPosition;')
    replace(game,
        '            GroundY = destination.Y,\n            SourcePlayer = _player',
        '            GroundY = destination.Y,\n            PlanNearSourcePlayer = true,\n            SourcePlayer = _player')
    replace(game,
        '            _supplyDrops.Remove(pallet);\n            SpawnSupplyContents(position);',
        '            _supplyDrops.Remove(pallet);\n'
        '            if (_finished || Runtime?.Player?.IsAlive != true) return;\n'
        '            SpawnSupplyContents(position, pallet.PayloadPositions);')
    replace(game,
        '        _supplyDrops.Add(pallet);\n        AddChild(pallet);',
        '        pallet.Failed = reason =>\n'
        '        {\n'
        '            _supplyDrops.Remove(pallet);\n'
        '            if (!_finished && GodotObject.IsInstanceValid(_hud))\n'
        '                _hud.SetBanner("SUPPLY DROP UNAVAILABLE - NO SAFE LANDING");\n'
        '            GD.Print("TWR_SUPPLY_DELIVERY_CANCELLED reason=" + reason);\n'
        '        };\n'
        '        _supplyDrops.Add(pallet);\n        AddChild(pallet);')
    replace(game,
        '    private void SpawnSupplyContents(Vector3 center)\n    {',
        '    private void SpawnSupplyContents(Vector3 center, Vector3[]? groundedPayload = null)\n'
        '    {\n'
        '        if (groundedPayload is not null && groundedPayload.Length != 8)\n'
        '            throw new InvalidOperationException("Supply payload requires eight verified positions");\n'
        '        Vector3 SupplyPoint(int i) => groundedPayload is null\n'
        '            ? RingPoint(center,i,8,2.3f) : groundedPayload[i];')
    replace(game,'            SpawnPickup(kind,RingPoint(center,i,8,2.3f));',
                 '            SpawnPickup(kind,SupplyPoint(i));')
    replace(game,'                SpawnPickup("Ammo",RingPoint(center,i+4,8,2.3f));',
                 '                SpawnPickup("Ammo",SupplyPoint(i+4));')
    replace(game,'            SpawnPickup(fort.Name,RingPoint(center,i+4,8,2.3f),fort.Count);',
                 '            SpawnPickup(fort.Name,SupplyPoint(i+4),fort.Count);')
    replace(game,
        '        _fortifications.ClearDeployed();\n        Input.MouseMode = Input.MouseModeEnum.Visible;',
        '        _fortifications.ClearDeployed();\n'
        '        foreach (var supply in _supplyDrops)\n'
        '            if (GodotObject.IsInstanceValid(supply) && !supply.IsQueuedForDeletion())\n'
        '                supply.QueueFree();\n'
        '        _supplyDrops.Clear();\n'
        '        Input.MouseMode = Input.MouseModeEnum.Visible;')
    replace('src/Twr.Godot/Scripts/RuntimeReadinessAcceptance.cs',
        '        FirstPersonModelLighting.Verify(player);',
        '        FirstPersonModelLighting.Verify(player);\n'
        '        await SupplyDeliveryAcceptance.Verify(game, player);')
    replace('src/Twr.Godot/Scripts/Pass76ObjectiveFortificationAcceptance.cs',
        '        supply._Process(4.0); // accelerate descent, not collider authority',
        '        supply._PhysicsProcess(4.0); // accelerate descent, not collider authority\n'
        '        await Frames(2); // payload notification runs after the physics callback')
    replace('src/Twr.Godot/Scripts/Pass76ObjectiveFortificationAcceptance.cs',
        '        supply._Process(1.0);',
        '        supply._PhysicsProcess(1.0);')
    replace(game,
        '            if (child is SupplyDropRuntime drop) drop.Landed = null;',
        '            if (child is SupplyDropRuntime drop) { drop.Landed = null; drop.Failed = null; }')

if __name__=='__main__':apply()
