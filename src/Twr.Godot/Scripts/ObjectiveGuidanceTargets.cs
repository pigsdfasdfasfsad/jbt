using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

public sealed record ObjectiveGuidanceTarget(ObjectiveRuntime Owner, Vector3 Position,
    string Instruction, bool Carrying, Rid? IgnoreBody = null);

public partial class ObjectiveRuntime
{
    /// <summary>Read-only live targets. This never collects, deposits, grants a
    /// reward, moves a player or changes the mission's selection/authority.</summary>
    public IEnumerable<ObjectiveGuidanceTarget> RouteGuideTargets()
    {
        if(_completed || IsQueuedForDeletion() || !IsInsideTree())yield break;
        if(Family is "Load" or "Repair")
        {
            if(_carrying)
            {
                yield return new(this,GlobalPosition,$"RETURN MATERIAL | {Family.ToUpperInvariant()} {_deposited}/{_required}",true);
                yield break;
            }
            foreach(var item in _items)
                if(GodotObject.IsInstanceValid(item) && !item.IsQueuedForDeletion() && item.IsInsideTree())
                    yield return new(this,item.GlobalPosition,$"COLLECT {item.Name} | {Family.ToUpperInvariant()} {_deposited}/{_required}",false);
        }
        else if(Family=="Escort" && _escort is not null && GodotObject.IsInstanceValid(_escort))
            yield return new(this,_escort.GlobalPosition,"ESCORT | STAY WITH SURVIVOR",false,_escort.GetRid());
        else if(Family is "Radio" or "Unpack")
            yield return new(this,GlobalPosition,$"{Family.ToUpperInvariant()} | SECURE THE AREA",false);
    }
}
