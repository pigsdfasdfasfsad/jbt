using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass110: source-NAME-informed, deliberately APPROXIMATED repair vehicle.
/// Two source repair quotas are recovered (Silverado: 4 plugs, 2 wheels,
/// 4 jerry cans; Tundra: 4 plugs, 2 wheels, 6 cans). The original truck
/// geometry, exact dimensions/placement, paint and installation sequence
/// are NOT recovered. All geometry is a passive visual child with no bodies,
/// collision layers, inventory authority, objective progress or navigation.
/// </summary>
public partial class Pass110RepairVehicleVisual : Node3D
{
    public bool LargeVariant { get; set; }
    public string SourceVehicle => LargeVariant ? "Large Tundra" : "Small Silverado";
    public int ExpectedFuelCans => LargeVariant ? 6 : 4;
    public int ExpectedPartCount => 4 + 2 + ExpectedFuelCans;

    public int InstalledSparkPlugs { get; private set; }
    public int InstalledWheels { get; private set; }
    public int AddedFuelCans { get; private set; }
    public int MeshPartCount { get; private set; }
    public int AcceptedDeposits => _installedNames.Count;
    public bool AssemblyComplete =>
        InstalledSparkPlugs == 4 && InstalledWheels == 2 &&
        AddedFuelCans == ExpectedFuelCans;

    public bool RearLeftWheelVisible => _rearLeft?.Visible == true;
    public bool RearRightWheelVisible => _rearRight?.Visible == true;
    public bool CompletionLampVisible => _completeLamp?.Visible == true;

    private readonly HashSet<string> _installedNames =
        new(StringComparer.Ordinal);
    private readonly MeshInstance3D[] _coils = new MeshInstance3D[4];
    private MeshInstance3D? _rearLeft;
    private MeshInstance3D? _rearRight;
    private MeshInstance3D? _fuelFill;
    private MeshInstance3D? _completeLamp;

    private static readonly Color Body = new(.38f, .43f, .42f);
    private static readonly Color Dark = new(.105f, .12f, .13f);
    private static readonly Color Bed = new(.24f, .27f, .26f);
    private static readonly Color Glass = new(.11f, .19f, .22f);
    private static readonly Color Rust = new(.41f, .225f, .155f);
    private static readonly Color Steel = new(.54f, .56f, .54f);

    public override void _Ready()
    {
        Name = "Pass110RepairVehicle";
        // Side-offset from existing verified deposit coordinates; never
        // moves the authoritative E-key depot or original source markers.
        Position = new Vector3(2.9f, 0, 1.25f);
        Scale = Vector3.One * (LargeVariant ? 1.15f : 1f);
        BuildVisual();
    }

    /// <summary>
    /// Pure render feedback invoked ONLY after the player's original E
    /// deposit has succeeded. Duplicate/unknown names cannot grant
    /// anything, and replays cannot visually install twice.
    /// </summary>
    public bool PresentSuccessfulDeposit(string? materialName)
    {
        if (string.IsNullOrWhiteSpace(materialName) ||
            _installedNames.Contains(materialName))
            return false;

        if(materialName.StartsWith("Spark Plug ",StringComparison.Ordinal))
        {
            if(InstalledSparkPlugs >= 4) return false;
            _coils[InstalledSparkPlugs].Visible = true;
            InstalledSparkPlugs++;
        }
        else if(materialName.StartsWith("Wheel ",StringComparison.Ordinal))
        {
            if(InstalledWheels >= 2) return false;
            if(InstalledWheels == 0 && _rearLeft is not null)
                _rearLeft.Visible = true;
            if(InstalledWheels == 1 && _rearRight is not null)
                _rearRight.Visible = true;
            InstalledWheels++;
        }
        else if(materialName.StartsWith("Jerry Can ",StringComparison.Ordinal))
        {
            if(AddedFuelCans >= ExpectedFuelCans) return false;
            AddedFuelCans++;
            if(_fuelFill is not null)
            {
                _fuelFill.Visible = true;
                _fuelFill.Scale = new Vector3(
                    (float)AddedFuelCans / ExpectedFuelCans, 1f, 1f);
            }
        }
        else return false;

        _installedNames.Add(materialName);
        if(_completeLamp is not null && AssemblyComplete)
            _completeLamp.Visible = true;
        return true;
    }

    private void BuildVisual()
    {
        // One readable parked pickup silhouette facing camera-local -Z.
        // The smaller source quota uses a shorter wheelbase; the larger
        // one is uniformly scaled, preserving scene node structure.
        Box("Chassis",new Vector3(0,.68f,0),
            new Vector3(1.54f,.27f,3.28f),Dark);
        Box("CabinShell",new Vector3(0,1.25f,-.51f),
            new Vector3(1.48f,.79f,1.32f),Body);
        Box("CabinRoof",new Vector3(0,1.71f,-.53f),
            new Vector3(1.53f,.11f,1.39f),Bed);
        Box("FrontWindshield",new Vector3(0,1.39f,-1.187f),
            new Vector3(1.24f,.53f,.035f),Glass,.34f);
        Box("RearWindow",new Vector3(0,1.39f,.171f),
            new Vector3(1.19f,.47f,.035f),Glass,.34f);
        Box("FrontHood",new Vector3(0,1.02f,-1.37f),
            new Vector3(1.52f,.23f,.77f),Body);
        Box("FrontGrille",new Vector3(0,.90f,-1.78f),
            new Vector3(.94f,.28f,.05f),Dark,.23f);
        Box("GrilleSeparator",new Vector3(0,.9f,-1.818f),
            new Vector3(.83f,.035f,.02f),Steel,.50f);
        Box("PickupBed",new Vector3(0,1.01f,1.03f),
            new Vector3(1.56f,.37f,1.43f),Body);
        Box("BedLining",new Vector3(0,1.225f,1.03f),
            new Vector3(1.36f,.035f,1.16f),Dark);
        Box("BedRailLeft",new Vector3(-.77f,1.22f,1.03f),
            new Vector3(.09f,.23f,1.4f),Bed);
        Box("BedRailRight",new Vector3(.77f,1.22f,1.03f),
            new Vector3(.09f,.23f,1.4f),Bed);
        Box("Tailgate",new Vector3(0,1.08f,1.78f),
            new Vector3(1.54f,.40f,.09f),Rust);
        Box("FrontBumper",new Vector3(0,.63f,-1.86f),
            new Vector3(1.73f,.17f,.15f),Steel,.55f);
        Box("RearBumper",new Vector3(0,.62f,1.86f),
            new Vector3(1.75f,.18f,.16f),Steel,.52f);
        Box("LeftDoor",new Vector3(-.755f,1.26f,-.46f),
            new Vector3(.035f,.59f,1.06f),Bed);
        Box("RightDoor",new Vector3(.755f,1.26f,-.46f),
            new Vector3(.035f,.59f,1.06f),Bed);
        Box("LeftDoorHandle",new Vector3(-.779f,1.39f,-.38f),
            new Vector3(.018f,.07f,.19f),Steel,.42f);
        Box("RightDoorHandle",new Vector3(.779f,1.39f,-.38f),
            new Vector3(.018f,.07f,.19f),Steel,.42f);
        Box("LeftMirror",new Vector3(-.865f,1.42f,-1.06f),
            new Vector3(.18f,.16f,.17f),Dark,.20f);
        Box("RightMirror",new Vector3(.865f,1.42f,-1.06f),
            new Vector3(.18f,.16f,.17f),Dark,.20f);

        for(var side=0;side<2;side++)
        {
            var x=side==0 ? -.61f : .61f;
            Box(side==0?"HeadlightLeft":"HeadlightRight",
                new Vector3(x,1.04f,-1.775f),
                new Vector3(.21f,.12f,.045f),
                new Color(.80f,.81f,.68f));
            Box(side==0?"BrakeLightLeft":"BrakeLightRight",
                new Vector3(x,1.2f,1.835f),
                new Vector3(.20f,.19f,.055f),
                new Color(.68f,.15f,.11f));
        }

        // Front pair has always been on the vehicle. Real E-deposited
        // "Wheel A"/"Wheel B" make the remaining REAR pair appear.
        Wheel("FrontWheelLeft",-.835f,-1.15f,true);
        Wheel("FrontWheelRight",.835f,-1.15f,true);
        _rearLeft=Wheel("RearWheelLeft",-.835f,1.17f,false);
        _rearRight=Wheel("RearWheelRight",.835f,1.17f,false);

        // Four recovered spark plugs. Visual coils appear in an exposed
        // front-engine service tray only after the matching E deposits.
        Box("ServiceTray",new Vector3(0,1.14f,-1.37f),
            new Vector3(.61f,.052f,.33f),Dark,.24f);
        for(var index=0;index<_coils.Length;index++)
        {
            _coils[index]=Box("EnginePlug"+(index+1),
                new Vector3(-.21f+index*.14f,1.189f,-1.37f),
                new Vector3(.064f,.07f,.20f),Steel,.38f);
            _coils[index].Visible=false;
        }

        Box("FuelGaugeTrack",new Vector3(.02f,1.30f,1.79f),
            new Vector3(.67f,.098f,.024f),Dark);
        _fuelFill=Box("FuelGaugeFill",
            new Vector3(.02f,1.30f,1.811f),
            new Vector3(.60f,.058f,.018f),
            new Color(.31f,.71f,.42f));
        _fuelFill.Visible=false;
        _completeLamp=Box("RepairCompletedLamp",
            new Vector3(-.51f,1.30f,1.83f),
            new Vector3(.07f,.07f,.028f),
            new Color(.33f,.87f,.46f));
        _completeLamp.Visible=false;

        SetMeta("Pass110SourceName",SourceVehicle);
        SetMeta("Pass110RecoveredGeometry",false);
    }

    private MeshInstance3D Box(
        string label,Vector3 location,Vector3 dimensions,
        Color color,float metallic=0f)
    {
        var part=new MeshInstance3D
        {
            Name=label,Position=location,
            Mesh=new BoxMesh
            {
                Size=dimensions,
                Material=new StandardMaterial3D
                {
                    AlbedoColor=color,Metallic=metallic,Roughness=.71f
                }
            }
        };
        AddChild(part);
        MeshPartCount++;
        return part;
    }

    private MeshInstance3D Wheel(
        string label,float x,float z,bool visible)
    {
        var part=new MeshInstance3D
        {
            Name=label,
            Position=new Vector3(x,.43f,z),
            RotationDegrees=new Vector3(0,0,90),
            Visible=visible,
            Mesh=new CylinderMesh
            {
                TopRadius=.35f,BottomRadius=.35f,Height=.22f,
                RadialSegments=18,
                Material=new StandardMaterial3D
                {
                    AlbedoColor=Dark,Roughness=.92f
                }
            }
        };
        AddChild(part);
        MeshPartCount++;
        return part;
    }
}
