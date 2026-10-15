using System;
using Godot;

namespace Twr.Godot;

public partial class SupplyDropRuntime : Node3D
{
    // Compatibility hint for a bounded failure plane, NEVER a landing floor.
    public float GroundY { get; set; }
    public Action<Vector3>? Landed { get; set; }
    public Action<string>? Failed { get; set; }
    public FirstPersonPlayer? SourcePlayer { get; set; }
    public bool PlanNearSourcePlayer { get; set; }
    public bool Landed76 => _landed;
    public Vector3[] PayloadPositions { get; private set; } = [];
    public bool FailedDelivery { get; private set; }
    private bool _landed;
    private bool _resolved;
    private int _initialFrames=2;
    private double _elapsed;
    private int _nextCandidate;
    private Vector3 _dispatchPlayerFeet;
    private bool _notificationQueued;
    private string _failureReason="";
    private Vector3 _landing;
    private SupplyLandingPlanner.Plan? _plan;

    // APPROXIMATED: the source does not specify a fixed pallet landing time.
    private const float DescentSpeed=3.0f;
    private const double MaximumDescentSeconds=30.0;

    public override void _Ready()
    {
        AddChild(new MeshInstance3D
        {
            Mesh=new BoxMesh
            {
                Size=new Vector3(2.4f,.45f,2.4f),
                Material=new StandardMaterial3D
                {
                    AlbedoColor=new Color(.29f,.23f,.12f),Roughness=.95f
                }
            }
        });
        AddChild(new Label3D
        {
            Text="SUPPLY PALLET",Position=new Vector3(0,1,0),FontSize=38,
            Billboard=BaseMaterial3D.BillboardModeEnum.Enabled
        });
        if(PlanNearSourcePlayer) Visible=false;
    }

    public override void _PhysicsProcess(double delta)
    {
        if(_resolved || _notificationQueued) return;
        if(!double.IsFinite(delta) || delta<=0 || !IsInsideTree()) return;
        if(PlanNearSourcePlayer)
        {
            if(SourcePlayer is null || !GodotObject.IsInstanceValid(SourcePlayer) ||
                !SourcePlayer.GameplayControlsEnabled)
            {
                QueueFailure("player_unavailable");
                return;
            }
            if(_plan is null)
            {
                if(--_initialFrames>0) return;
                if(_nextCandidate==0) _dispatchPlayerFeet=SourcePlayer.GlobalPosition;
                for(var tested=0; tested<2 && _nextCandidate<SupplyLandingPlanner.MaximumCandidates; tested++)
                {
                    _plan=SupplyLandingPlanner.TryCandidate(GetWorld3D(),_dispatchPlayerFeet,
                        SourcePlayer.GetRid(),_nextCandidate++);
                    if(_plan is not null) break;
                }
                if(_plan is null)
                {
                    if(_nextCandidate>=SupplyLandingPlanner.MaximumCandidates)
                        QueueFailure("no_supported_accessible_drop_zone");
                    return;
                }
                GlobalPosition=_plan.PalletCentre+Vector3.Up*15f;
                GroundY=_plan.PalletCentre.Y;
                Visible=true;
            }
        }
        _elapsed+=delta;
        var previous=GlobalPosition;
        var position=previous-Vector3.Up*DescentSpeed*(float)delta;
        using var ray=PhysicsRayQueryParameters3D.Create(previous+Vector3.Up*.10f,position+Vector3.Down*.30f);
        ray.CollisionMask=1u | Pass36SourceFragmentsRuntime.SourceServerCollisionLayer;
        if(SourcePlayer is not null && GodotObject.IsInstanceValid(SourcePlayer))
            ray.Exclude=new global::Godot.Collections.Array<Rid> { SourcePlayer.GetRid() };
        var hit=GetWorld3D().DirectSpaceState.IntersectRay(ray);
        if(hit.Count>0)
        {
            if(((Vector3)hit["normal"]).Y<.70f)
            {
                QueueFailure("obstructed_descent");return;
            }
            var floor=(Vector3)hit["position"];
            position.Y=floor.Y+.23f;
            if(_plan is not null)
            {
                if(position.DistanceTo(_plan.PalletCentre)>.18f)
                {
                    QueueFailure("landing_obstructed_after_dispatch");return;
                }
                var finalPlan=SupplyLandingPlanner.AtFloor(GetWorld3D(),floor,
                    _dispatchPlayerFeet,SourcePlayer!.GetRid(),false);
                if(finalPlan is null)
                {
                    QueueFailure("payload_floor_or_access_changed");return;
                }
                PayloadPositions=finalPlan.PickupCentres;
            }
            _landing=position;
            GlobalPosition=position;
            _landed=true;
            QueueNotification();
            return;
        }
        // No collision means NO supplies, even when the old guessed GroundY
        // was crossed. Cleanup is bounded rather than falling forever.
        if(position.Y<GroundY-3f || _elapsed>MaximumDescentSeconds)
        {
            QueueFailure("physical_landing_floor_missing");return;
        }
        GlobalPosition=position;
    }

    private void QueueFailure(string reason)
    {
        if(_resolved || _notificationQueued) return;
        FailedDelivery=true;
        _failureReason=reason;
        QueueNotification();
    }

    private void QueueNotification()
    {
        _notificationQueued=true;
        SetPhysicsProcess(false);
        // Domain loot and its visual children are instantiated on idle,
        // after physics queries have returned, never during their flush.
        Callable.From(NotifyOnIdle).CallDeferred();
    }

    private void NotifyOnIdle()
    {
        if(!GodotObject.IsInstanceValid(this) || IsQueuedForDeletion() || _resolved) return;
        _resolved=true;
        if(PlanNearSourcePlayer && (SourcePlayer is null ||
            !GodotObject.IsInstanceValid(SourcePlayer) || !SourcePlayer.GameplayControlsEnabled))
        {
            FailedDelivery=true;_failureReason="player_unavailable";_landed=false;
        }
        if(FailedDelivery) Failed?.Invoke(_failureReason);
        else Landed?.Invoke(_landing);
        QueueFree();
    }
}
