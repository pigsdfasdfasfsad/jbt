using Godot;

namespace Twr.Godot;

public partial class ThrowableProjectileRuntime : Node3D
{
    public string ThrowableType {get;set;}="Frag";
    public FirstPersonPlayer? SourcePlayer {get;set;}
    public Vector3 Direction {get;set;}=Vector3.Forward;

    private Vector3 _velocity;
    private int _contacts;
    private double _life;

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
        _life-=delta;
        _velocity.Y-=Gravity*(float)delta;

        var start=GlobalPosition;
        var end=start+_velocity*(float)delta;
        var query=PhysicsRayQueryParameters3D.Create(start,end);
        if(SourcePlayer is not null)
            query.Exclude=new global::Godot.Collections.Array<Rid>{SourcePlayer.GetRid()};
        var hit=GetWorld3D().DirectSpaceState.IntersectRay(query);

        if(hit.Count>0)
        {
            GlobalPosition=hit["position"].AsVector3();
            _contacts++;

            if(ThrowableType=="Frag")
            {
                // CONTESTED: recovered module says MaxBounces=math.huge while
                // surviving gameplay documentation describes one useful bounce.
                if(_contacts==1)Bounce(hit["normal"].AsVector3());
                else _velocity=Vector3.Zero;
            }
            else if(_contacts>=2)
            {
                DeployLingeringHazard();
                return;
            }
            else
            {
                Bounce(hit["normal"].AsVector3());
            }
        }
        else GlobalPosition=end;

        if(ThrowableType=="Frag" && _life<=0)
        {
            DetonateFrag();
            return;
        }

        if(ThrowableType!="Frag" && _life<=0)
            DeployLingeringHazard();
    }

    private void Bounce(Vector3 normal)
    {
        _velocity=_velocity.Bounce(normal)*0.42f; // APPROXIMATED restitution.
    }

    private void DetonateFrag()
    {
        foreach(var node in GetTree().GetNodesInGroup("infected"))
        {
            if(node is not InfectedAgent infected || !GodotObject.IsInstanceValid(infected))continue;
            var distance=infected.GlobalPosition.DistanceTo(GlobalPosition);
            if(distance>FragRadiusMeters)continue;
            // APPROXIMATED falloff equation; source confirms falloff but not curve.
            var factor=Mathf.Lerp(1f,0.35f,Math.Clamp(distance/FragRadiusMeters,0f,1f));
            infected.ApplyDamage(FragDamage*factor,false,"Explosion");
        }

        foreach(var node in GetTree().GetNodesInGroup("damage_objective"))
            if(node is DamageObjectiveTarget tanker &&
               tanker.GlobalPosition.DistanceTo(GlobalPosition)<=FragRadiusMeters)
                tanker.ApplyDamage(FragDamage,"Explosive");

        QueueFree();
    }

    private void DeployLingeringHazard()
    {
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
