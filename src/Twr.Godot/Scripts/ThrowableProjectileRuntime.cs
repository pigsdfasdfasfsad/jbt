using Godot;

namespace Twr.Godot;

public partial class ThrowableProjectileRuntime : Node3D
{
    public string ThrowableType {get;set;}="Frag";
    public FirstPersonPlayer? SourcePlayer {get;set;}
    public Vector3 Direction {get;set;}=Vector3.Forward;

    public const float CollisionRadius = .18f;
    public float LaunchOffset { get; set; }
    public bool Resolved { get; private set; }
    public bool CancelledInvalidOrigin { get; private set; }
    public int ContactCount => _contacts;
    private ProjectileSweep? _sweep;
    private bool _launchPending = true;
    private Vector3 _velocity;
    private int _contacts;
    private double _life;
    // Pass75: a grenade cannot remain on the exact collision boundary and
    // fall through its support after the second physics contact.
    private bool _restingOnGround;
    public bool RestingOnGround75 => _restingOnGround;

    private const float Gravity=20f;
    private const float ThrowSpeed=18f; // APPROXIMATED: source does not document throw force.
    private const float FragDamage=300f; // RECOVERED ModuleScript.Frag.
    private const float FragRadius=30f;  // RECOVERED ModuleScript.Frag.
    private const double FragFuse=3.0;   // RECOVERED ModuleScript.Frag.
    private const float LingeringRadius=30f; // RECOVERED throwable module radius.
    private static float FragRadiusMeters => RobloxUnits.Distance(FragRadius);

    public override void _Ready()
    {
        Direction=Direction.Normalized();
        _sweep = new ProjectileSweep(CollisionRadius);
        AddToGroup("twr-projectile");
        _velocity=Direction*ThrowSpeed+Vector3.Up*5f;
        _life=ThrowableType=="Frag" ? FragFuse : 12.0;

        AddChild(new MeshInstance3D
        {
            Mesh=new SphereMesh
            {
                Radius=0.18f,
                Height=0.36f,
                Material=new StandardMaterial3D
                {
                    AlbedoColor=ThrowableType switch
                    {
                        "Molotov"=>new Color(0.82f,0.31f,0.05f),
                        "Nerve Gas"=>new Color(0.09f,0.16f,0.10f),
                        _=>new Color(0.18f,0.22f,0.17f)
                    }
                }
            }
        });
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Resolved || _sweep is null) return;
        _life-=delta;
        const uint mask = 1u | Pass36SourceFragmentsRuntime.SourceServerCollisionLayer;
        if (_launchPending)
        {
            _launchPending = false;
            var launch = _sweep.Move(GetWorld3D(), GlobalPosition,
                Direction * LaunchOffset, mask, SourcePlayer);
            if (!launch.Valid || launch.InitialOverlap) { CancelInvalidOrigin(); return; }
            GlobalPosition = launch.Centre;
            if (launch.Hit) { _contacts++; Bounce(launch.Normal); }
        }
        if (_restingOnGround)
        {
            // The support can disappear (moving/breakable source geometry).
            // Recheck it; never leave a grenade suspended on a deleted floor.
            var support = _sweep.Move(GetWorld3D(), GlobalPosition,
                Vector3.Down * .06f, mask, SourcePlayer);
            if (!support.Valid || support.InitialOverlap) { CancelInvalidOrigin(); return; }
            if (support.Hit && support.Normal.Y >= .60f)
            {
                if (ThrowableType=="Frag" && _life<=0) DetonateFrag();
                return;
            }
            _restingOnGround = false;
        }
        _velocity.Y-=Gravity*(float)delta;
        var step = _sweep.Move(GetWorld3D(), GlobalPosition,
            _velocity * (float)delta, mask, SourcePlayer);
        if (!step.Valid || step.InitialOverlap) { CancelInvalidOrigin(); return; }
        GlobalPosition = step.Centre;
        if (step.Hit)
        {
            var normal = step.Normal;
            _contacts++;
            if(ThrowableType=="Frag")
            {
                // Preserve the pre-existing reconstructed bounce policy.
                if(_contacts==1) Bounce(normal);
                else if (normal.Y>=.60f)
                {
                    _velocity=Vector3.Zero;
                    _restingOnGround=true;
                }
                else Bounce(normal);
            }
            else if(_contacts>=2)
            {
                DeployLingeringHazard();
                return;
            }
            else Bounce(normal);
        }
        if(ThrowableType=="Frag" && _life<=0) DetonateFrag();
        else if(ThrowableType!="Frag" && _life<=0) DeployLingeringHazard();
    }

    private void CancelInvalidOrigin()
    {
        Resolved = true;
        CancelledInvalidOrigin = true;
        SetPhysicsProcess(false);
        QueueFree();
    }

    public override void _ExitTree()
    {
        _sweep?.Dispose();
        _sweep = null;
    }

    private void Bounce(Vector3 normal)
    {
        _velocity=_velocity.Bounce(normal)*0.42f; // APPROXIMATED restitution.
    }

    private void DetonateFrag()
    {
        if (Resolved) return;
        Resolved = true;
        SetPhysicsProcess(false);
        foreach(var node in GetTree().GetNodesInGroup("infected"))
        {
            if(node is not InfectedAgent infected || !GodotObject.IsInstanceValid(infected))continue;
            var distance=infected.GlobalPosition.DistanceTo(GlobalPosition);
            if(distance>FragRadiusMeters)continue;
            if(!Pass75BlastOcclusion.CanReachTarget(
                    GetWorld3D(), GlobalPosition, infected,
                    FragRadiusMeters, SourcePlayer)) continue;
            // APPROXIMATED falloff equation; source confirms falloff but not curve.
            var factor=Mathf.Lerp(1f,0.35f,Math.Clamp(distance/FragRadiusMeters,0f,1f));
            infected.ApplyDamage(FragDamage*factor,false,"Explosion");
        }

        foreach(var node in GetTree().GetNodesInGroup("damage_objective"))
            if(node is DamageObjectiveTarget tanker &&
               Pass75BlastOcclusion.CanReachTarget(
                   GetWorld3D(), GlobalPosition, tanker,
                   FragRadiusMeters, SourcePlayer))
                tanker.ApplyDamage(FragDamage,"Explosive");

        QueueFree();
    }

    private void DeployLingeringHazard()
    {
        if (Resolved) return;
        Resolved = true;
        SetPhysicsProcess(false);
        var parent=GetParent();
        if(parent is null){QueueFree();return;}

        var hazard=new ThrowableHazardRuntime
        {
            Name=ThrowableType+"Hazard",
            HazardType=ThrowableType,
            Radius=LingeringRadius,
            DurationSeconds=ThrowableType=="Molotov" ? 32.0 : 34.0
        };
        parent.AddChild(hazard);
        hazard.GlobalPosition=GlobalPosition;
        QueueFree();
    }
}
