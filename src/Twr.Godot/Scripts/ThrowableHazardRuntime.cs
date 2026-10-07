using Godot;

namespace Twr.Godot;

public partial class ThrowableHazardRuntime : Node3D
{
    public string HazardType {get;set;}="Molotov";
    public double DurationSeconds {get;set;}=32;
    public float Radius {get;set;}=30;

    private double _tick;

    // FITTED reconstruction values. Source proves the effects/durations but
    // does not give Molotov tick damage or Nerve Gas slow magnitude.
    private const float MolotovTickDamage=12f;
    private const float NerveGasSlowFactor=0.55f;

    public override void _Ready()
    {
        AddChild(new MeshInstance3D
        {
            Position=new Vector3(0,0.06f,0),
            Mesh=new CylinderMesh
            {
                TopRadius=Radius,
                BottomRadius=Radius,
                Height=0.12f,
                Material=new StandardMaterial3D
                {
                    AlbedoColor=HazardType=="Molotov"
                        ? new Color(0.85f,0.25f,0.04f,0.20f)
                        : new Color(0.18f,0.65f,0.22f,0.18f),
                    Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
                    EmissionEnabled=true,
                    Emission=HazardType=="Molotov"
                        ? new Color(0.35f,0.08f,0.01f)
                        : new Color(0.04f,0.18f,0.05f)
                }
            }
        });
    }

    public override void _Process(double delta)
    {
        DurationSeconds-=delta;
        _tick-=delta;
        if(_tick<=0)
        {
            _tick=0.5; // FITTED cadence; source does not quantify lingering tick rate.
            foreach(var node in GetTree().GetNodesInGroup("infected"))
            {
                if(node is not InfectedAgent infected || !GodotObject.IsInstanceValid(infected))continue;
                if(infected.GlobalPosition.DistanceTo(GlobalPosition)>Radius)continue;
                if(infected.InfectedType=="Hazmat")continue;

                if(HazardType=="Molotov")
                    infected.ApplyDamage(MolotovTickDamage,false,"Fire");
                else
                    infected.ApplySlow(NerveGasSlowFactor,0.7);
            }
        }

        if(DurationSeconds<=0)QueueFree();
    }
}
