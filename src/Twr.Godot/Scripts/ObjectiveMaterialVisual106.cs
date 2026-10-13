using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass106: render-only, explicitly approximate equipment silhouettes for
/// Load/Repair materials recovered by NAME from the source objective list.
/// Both world and carried views use the same parts and color scheme.
/// No mesh files, floor geometry, colliders or original source pack are modified.
/// </summary>
public static class ObjectiveMaterialVisual106
{
    public static Node3D Create(
        string materialName, Vector3 localPosition, bool carried)
    {
        var model = new Node3D
        {
            Name = carried ? "Pass106CarriedObjectiveMaterial" : materialName,
            Position = localPosition
        };
        if (carried)
        {
            model.Scale = new Vector3(.56f,.56f,.56f);
            model.RotationDegrees = new Vector3(9f,-18f,5f);
        }

        string kind;
        if (materialName.StartsWith("Generator", StringComparison.Ordinal))
        {
            kind="Generator";
            // Pass109: the owner-provided Generator-Market.png depicts
            // a pale industrial generator with a dark skid, front service
            // doors/vents, tiny illuminated indicator and top exhausts.
            // These are local primitive reconstructions of its *visual
            // language*, NOT recovered Roblox triangles or source scale.
            // Direct render-only children deliberately retain the Pass108
            // complete-mesh frustum audit and the original E interaction.
            var paleSteel=new Color(.48f,.48f,.44f);
            var darkSteel=new Color(.135f,.15f,.16f);
            var shadowSteel=new Color(.23f,.26f,.27f);
            Box(model,"Housing",new Vector3(0,-.02f,0),
                new Vector3(.62f,.45f,.43f),paleSteel,.21f);
            Box(model,"SkidBase",new Vector3(0,-.293f,0),
                new Vector3(.71f,.09f,.48f),darkSteel,.35f);
            Box(model,"TopAccessLid",new Vector3(0,.235f,0),
                new Vector3(.66f,.055f,.465f),shadowSteel,.28f);
            Box(model,"ServiceDoor",new Vector3(-.063f,-.034f,-.223f),
                new Vector3(.43f,.334f,.022f),
                new Color(.43f,.45f,.42f),.12f);
            Box(model,"DoorSeam",new Vector3(.153f,-.03f,-.236f),
                new Vector3(.012f,.31f,.015f),darkSteel,.25f);
            Box(model,"FrontControlConsole",
                new Vector3(.207f,.089f,-.236f),
                new Vector3(.155f,.116f,.036f),darkSteel,.26f);
            Box(model,"InstrumentDisplay",
                new Vector3(.208f,.105f,-.258f),
                new Vector3(.097f,.04f,.012f),
                new Color(.28f,.54f,.44f));
            Box(model,"StatusRed",
                new Vector3(.168f,.061f,-.258f),
                new Vector3(.025f,.019f,.012f),
                new Color(.76f,.23f,.16f));
            Box(model,"StatusGreen",
                new Vector3(.214f,.061f,-.258f),
                new Vector3(.025f,.019f,.012f),
                new Color(.32f,.78f,.40f));
            for (var vent=0; vent<5; vent++)
                Box(model,"IntakeVent"+vent,
                    new Vector3(-.239f+vent*.085f,-.112f,-.251f),
                    new Vector3(.046f,.021f,.014f),darkSteel,.20f);
            Box(model,"LiftingRail",new Vector3(0,.297f,-.13f),
                new Vector3(.50f,.038f,.045f),darkSteel,.35f);
            Box(model,"LeftRailStand",new Vector3(-.22f,.26f,-.13f),
                new Vector3(.036f,.1f,.045f),shadowSteel,.33f);
            Box(model,"RightRailStand",new Vector3(.22f,.26f,-.13f),
                new Vector3(.036f,.1f,.045f),shadowSteel,.33f);
            Cylinder(model,"LeftExhaustPipe",
                new Vector3(-.222f,.343f,.104f),
                .041f,.19f,shadowSteel,.4f);
            Cylinder(model,"RightExhaustPipe",
                new Vector3(-.125f,.332f,.104f),
                .036f,.169f,shadowSteel,.4f);
            Cylinder(model,"LeftExhaustCap",
                new Vector3(-.222f,.443f,.104f),
                .047f,.025f,darkSteel,.42f);
            Cylinder(model,"RightExhaustCap",
                new Vector3(-.125f,.424f,.104f),
                .043f,.025f,darkSteel,.42f);
            Box(model,"LeftSkidFoot",new Vector3(-.252f,-.35f,0),
                new Vector3(.09f,.036f,.42f),darkSteel,.3f);
            Box(model,"RightSkidFoot",new Vector3(.252f,-.35f,0),
                new Vector3(.09f,.036f,.42f),darkSteel,.3f);
        }
        else if (materialName.StartsWith("Tool Box",StringComparison.Ordinal))
        {
            kind="ToolBox";
            Box(model,"Toolbox",new Vector3(0,-.07f,0),
                new Vector3(.63f,.39f,.34f),new Color(.62f,.16f,.12f),.25f);
            Box(model,"Lid",new Vector3(0,.15f,0),
                new Vector3(.67f,.09f,.38f),new Color(.80f,.26f,.17f),.3f);
            Box(model,"Handle",new Vector3(0,.25f,0),
                new Vector3(.26f,.075f,.08f),new Color(.17f,.18f,.21f));
            Box(model,"Latch",new Vector3(0,.06f,-.197f),
                new Vector3(.12f,.12f,.018f),new Color(.77f,.78f,.76f),.7f);
        }
        else if (materialName.StartsWith("Propane Tank",StringComparison.Ordinal))
        {
            kind="PropaneTank";
            Cylinder(model,"PressureCylinder",new Vector3(0,-.05f,0),
                .23f,.54f,new Color(.70f,.72f,.75f),.58f);
            Cylinder(model,"TankCollar",new Vector3(0,.23f,0),
                .16f,.09f,new Color(.42f,.47f,.52f),.6f);
            Cylinder(model,"Valve",new Vector3(0,.31f,0),
                .075f,.11f,new Color(.75f,.49f,.12f),.55f);
            Box(model,"Grip",new Vector3(0,.39f,0),
                new Vector3(.18f,.035f,.06f),new Color(.38f,.43f,.45f),.6f);
        }
        else if (materialName.StartsWith("Water Jug",StringComparison.Ordinal))
        {
            kind="WaterJug";
            Box(model,"Container",new Vector3(0,-.06f,0),
                new Vector3(.39f,.55f,.34f),new Color(.29f,.65f,.79f));
            Cylinder(model,"Cap",new Vector3(0,.275f,0),
                .095f,.08f,new Color(.18f,.34f,.72f));
            Box(model,"JugGrip",new Vector3(.24f,.17f,0),
                new Vector3(.10f,.24f,.11f),new Color(.17f,.45f,.62f));
        }
        else if (materialName.StartsWith("Can Package",StringComparison.Ordinal))
        {
            kind="CanPackage";
            Box(model,"PackageBase",new Vector3(0,-.20f,0),
                new Vector3(.60f,.075f,.38f),new Color(.47f,.34f,.20f));
            for(var x=0;x<3;x++)
                for(var z=0;z<2;z++)
                    Cylinder(model,"Can"+x+"_"+z,
                        new Vector3((x-1)*.18f,-.015f,(z-.5f)*.19f),
                        .073f,.34f,new Color(.65f,.68f,.68f),.45f);
        }
        else if (materialName.StartsWith("Spark Plug",StringComparison.Ordinal))
        {
            kind="SparkPlug";
            Cylinder(model,"SteelSleeve",new Vector3(0,-.10f,0),
                .13f,.31f,new Color(.66f,.68f,.69f),.72f);
            Cylinder(model,"WhiteInsulator",new Vector3(0,.135f,0),
                .085f,.28f,new Color(.90f,.88f,.78f));
            Cylinder(model,"Terminal",new Vector3(0,.29f,0),
                .055f,.07f,new Color(.75f,.72f,.60f),.7f);
        }
        else if (materialName.StartsWith("Wheel",StringComparison.Ordinal))
        {
            kind="Wheel";
            Cylinder(model,"RubberTire",Vector3.Zero,
                .34f,.22f,new Color(.105f,.11f,.12f),.08f,90f);
            Cylinder(model,"WheelHub",new Vector3(.125f,0,0),
                .18f,.035f,new Color(.68f,.69f,.70f),.85f,90f);
            Cylinder(model,"HubCentre",new Vector3(.15f,0,0),
                .065f,.045f,new Color(.26f,.28f,.29f),.5f,90f);
        }
        else if (materialName.StartsWith("Jerry Can",StringComparison.Ordinal))
        {
            kind="JerryCan";
            Box(model,"FuelCanBody",new Vector3(0,-.055f,0),
                new Vector3(.40f,.53f,.19f),new Color(.49f,.20f,.13f),.35f);
            Box(model,"FuelCanGrip",new Vector3(0,.275f,0),
                new Vector3(.28f,.07f,.11f),new Color(.32f,.14f,.10f),.3f);
            Cylinder(model,"Spout",new Vector3(.13f,.31f,0),
                .06f,.11f,new Color(.70f,.68f,.55f),.75f);
        }
        else
        {
            kind="Generic";
            Box(model,"RecoveredNamePlaceholder",Vector3.Zero,
                new Vector3(.55f,.7f,.55f),new Color(.74f,.58f,.18f));
        }

        model.SetMeta("Pass106VisualKind",kind);
        model.SetMeta("Pass106RecoveredItemName",materialName);
        return model;
    }

    private static void Box(
        Node3D root,string name,Vector3 position,Vector3 size,
        Color color,float metallic=0)
    {
        root.AddChild(new MeshInstance3D
        {
            Name=name,
            Position=position,
            Mesh=new BoxMesh
            {
                Size=size,
                Material=new StandardMaterial3D
                {
                    AlbedoColor=color,
                    Metallic=metallic,
                    Roughness=.65f
                }
            }
        });
    }

    private static void Cylinder(
        Node3D root,string name,Vector3 position,
        float radius,float height,Color color,
        float metallic=0,float zRotation=0)
    {
        root.AddChild(new MeshInstance3D
        {
            Name=name,
            Position=position,
            RotationDegrees=new Vector3(0,0,zRotation),
            Mesh=new CylinderMesh
            {
                TopRadius=radius,
                BottomRadius=radius,
                Height=height,
                RadialSegments=12,
                Material=new StandardMaterial3D
                {
                    AlbedoColor=color,
                    Metallic=metallic,
                    Roughness=.65f
                }
            }
        });
    }
}
