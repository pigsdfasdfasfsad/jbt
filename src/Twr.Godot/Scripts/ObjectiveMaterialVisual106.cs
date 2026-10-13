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
            Box(model,"Housing",new Vector3(0,0,0),
                new Vector3(.59f,.48f,.47f),new Color(.19f,.23f,.26f));
            Box(model,"ControlPanel",new Vector3(0,.10f,-.246f),
                new Vector3(.36f,.20f,.028f),new Color(.69f,.68f,.46f));
            Box(model,"CarryBar",new Vector3(0,.33f,0),
                new Vector3(.52f,.07f,.08f),new Color(.43f,.46f,.48f),.6f);
            Box(model,"LeftBracket",new Vector3(-.24f,.23f,0),
                new Vector3(.06f,.20f,.08f),new Color(.40f,.44f,.46f),.6f);
            Box(model,"RightBracket",new Vector3(.24f,.23f,0),
                new Vector3(.06f,.20f,.08f),new Color(.40f,.44f,.46f),.6f);
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
