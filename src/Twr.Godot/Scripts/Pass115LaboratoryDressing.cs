using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass115: owner-screenshot-guided *approximate* dressing for the
/// SOURCE-ABSENT Laboratory map blockout only. Screenshot landmarks are
/// recognized; their positions, proportions and meshes are not recovered
/// Roblox source geometry. All dressing is render-only and offline.
/// </summary>
public partial class Pass115LaboratoryDressing : Node3D
{
    public const int ExpectedLandmarks = 7;
    public const int MaxVisualMeshes = 260;
    public int VisualMeshCount { get; private set; }
    public int LocalLightCount { get; private set; }
    public int LandmarkCount => _counts.Count;
    public bool HasAnyCollisionObjects => ContainsCollision(this);
    public IReadOnlyDictionary<string, int> VisualMeshesByLandmark => _counts;

    private readonly Dictionary<string,int> _counts =
        new(StringComparer.Ordinal);

    private static readonly Color Concrete = new(.47f,.49f,.50f);
    private static readonly Color CounterWhite = new(.72f,.73f,.70f);
    private static readonly Color CounterDark = new(.16f,.19f,.22f);
    private static readonly Color Steel = new(.35f,.39f,.41f);
    private static readonly Color Screen = new(.09f,.38f,.59f);
    private static readonly Color ScreenGlass = new(.09f,.14f,.17f);
    private static readonly Color Rack = new(.13f,.18f,.20f);
    private static readonly Color RackGreen = new(.17f,.58f,.37f);
    private static readonly Color BedGreen = new(.25f,.35f,.33f);

    public static Pass115LaboratoryDressing AddToFallback(Node3D root)
    {
        var dressing=new Pass115LaboratoryDressing
        {
            Name="Pass115LaboratoryDressing"
        };
        root.AddChild(dressing);
        dressing.Build();
        return dressing;
    }

    private Node3D Landmark(string name)
    {
        var area=new Node3D{Name=name};
        AddChild(area);
        _counts.Add(name,0);
        return area;
    }

    private void Box(Node3D area,string name,Vector3 pos,
        Vector3 dimensions,Color tint,float metallic=.04f)
    {
        if(VisualMeshCount>=MaxVisualMeshes)
            throw new InvalidOperationException(
                "Pass115 visual scene budget exceeded");
        var part=new MeshInstance3D
        {
            Name=name,Position=pos,
            Mesh=new BoxMesh
            {
                Size=dimensions,
                Material=new StandardMaterial3D
                {
                    AlbedoColor=tint,
                    Metallic=metallic,
                    Roughness=.76f
                }
            },
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off
        };
        area.AddChild(part);
        VisualMeshCount++;
        _counts[area.Name.ToString()]++;
    }

    private void Cylinder(Node3D area,string name,Vector3 pos,
        float radius,float height,Color color)
    {
        if(VisualMeshCount>=MaxVisualMeshes)
            throw new InvalidOperationException(
                "Pass115 visual scene budget exceeded");
        var part=new MeshInstance3D
        {
            Name=name,Position=pos,
            Mesh=new CylinderMesh
            {
                TopRadius=radius,BottomRadius=radius,
                Height=height,RadialSegments=12,
                Material=new StandardMaterial3D
                {
                    AlbedoColor=color,Roughness=.85f
                }
            },
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off
        };
        area.AddChild(part);
        VisualMeshCount++;
        _counts[area.Name.ToString()]++;
    }

    private void Fluorescent(Node3D area,string name,Vector3 pos)
    {
        Box(area,name+"Housing",pos,new Vector3(2.0f,.08f,.20f),
            CounterDark);
        Box(area,name+"Diffuser",pos+Vector3.Down*.055f,
            new Vector3(1.75f,.022f,.13f),new Color(.82f,.89f,.86f));
        area.AddChild(new OmniLight3D
        {
            Name=name+"LocalLight",
            Position=pos+Vector3.Down*.15f,
            LightColor=new Color(.73f,.84f,.89f),
            LightEnergy=.21f,
            OmniRange=7f,
            ShadowEnabled=false
        });
        LocalLightCount++;
    }

    private void Build()
    {
        // Sources: owner images.zip: Laboratory-ReceptionDesk,
        // ServerRoom, ResearchRoom, ExaminationRoom, ParkingGarage,
        // FacilityEntrance and Cafeteria; dossier also documents ward.
        BuildReception();
        BuildServerRoom();
        BuildResearch();
        BuildExamination();
        BuildGarage();
        BuildWard();
        BuildCafeteria();
        SetMeta("Pass115OwnerOriginalGeometry",false);
        SetMeta("Pass115SourceReferencesBundled",false);
    }

    private void BuildReception()
    {
        var area=Landmark("ReceptionDressing");
        // Original screenshot: broad curved pale reception counter, a
        // dark corporate sign and cyan-blue lit entrance machinery.
        // These segments are a rectilinear visual approximation.
        Box(area,"ReceptionCounterFace",new Vector3(-8,1.06f,39.7f),
            new Vector3(5.5f,1.45f,1.05f),CounterWhite);
        Box(area,"ReceptionCounterCap",new Vector3(-8,1.82f,39.7f),
            new Vector3(5.75f,.12f,1.3f),CounterDark);
        Box(area,"ReceptionCounterReturn",new Vector3(-10.45f,1.05f,38.2f),
            new Vector3(.9f,1.45f,3.0f),CounterWhite);
        Box(area,"ReceptionDeskMonitor",new Vector3(-8,2.17f,39.6f),
            new Vector3(.72f,.46f,.07f),ScreenGlass);
        Box(area,"ReceptionDeskDisplay",new Vector3(-8,2.17f,39.54f),
            new Vector3(.61f,.34f,.025f),Screen);
        Box(area,"AlythSignBacking",new Vector3(-9.0f,2.65f,40.10f),
            new Vector3(2.8f,.60f,.12f),CounterDark);
        area.AddChild(new Label3D
        {
            Name="AlythReferenceSign",
            Text="ALYTH",
            Position=new Vector3(-9.0f,2.65f,40.01f),
            FontSize=36,
            PixelSize=.008f,
            Modulate=new Color(.87f,.89f,.86f)
        });
        Box(area,"BlueDoorScannerTower",new Vector3(11.3f,1.48f,37.4f),
            new Vector3(.80f,2.8f,.65f),CounterDark);
        Box(area,"BlueDoorScannerScreen",new Vector3(11.3f,1.8f,37.04f),
            new Vector3(.58f,.97f,.03f),Screen);
        Box(area,"BlueDoorScannerIndicator",new Vector3(11.3f,2.54f,37.03f),
            new Vector3(.42f,.16f,.035f),RackGreen);
        for(var i=0;i<2;i++)
        {
            var x=i==0?-12.6f:12.6f;
            Box(area,"EntranceBench"+i,new Vector3(x,.54f,41.0f),
                new Vector3(2.0f,.16f,.60f),Steel);
            Box(area,"EntranceBenchBack"+i,new Vector3(x,.96f,41.30f),
                new Vector3(2.0f,.86f,.09f),CounterDark);
        }
        for(var i=0;i<2;i++)
            Fluorescent(area,"ReceptionCeiling"+i,
                new Vector3(-5f+i*11f,3.91f,39f));
    }

    private void BuildServerRoom()
    {
        var area=Landmark("ServerDressing");
        // Owner server-room screenshot: dark racks with small green
        // indicators along a narrow industrial balcony/hallway.
        for(var bank=0;bank<2;bank++)
            for(var row=0;row<3;row++)
            {
                var x=bank==0?-28f:-9.0f;
                var z=-40.0f+row*5.6f;
                var name="Rack"+bank+"_"+row;
                Box(area,name+"Cabinet",
                    new Vector3(x,1.49f,z),
                    new Vector3(1.55f,2.84f,1.45f),Rack,.3f);
                Box(area,name+"AccessDoor",
                    new Vector3(x,1.47f,z-.74f),
                    new Vector3(1.30f,2.55f,.045f),CounterDark,.16f);
                for(var led=0;led<3;led++)
                    Box(area,name+"StatusLED"+led,
                        new Vector3(x-.37f+led*.36f,2.38f,z-.772f),
                        new Vector3(.09f,.045f,.018f),
                        led==2?Screen:RackGreen);
            }
        for(var i=0;i<3;i++)
            Fluorescent(area,"ServerCeiling"+i,
                new Vector3(-26f+i*7.6f,5.96f,-34f));
        // Visual-only upper catwalk cues. Collision, lower walk route,
        // source stairs and accessible floors are NOT fabricated here.
        for(var i=0;i<4;i++)
            Box(area,"ServerOverheadRail"+i,
                new Vector3(-27.9f+i*5.8f,4.2f,-37f),
                new Vector3(4.9f,.06f,.07f),Steel,.35f);
    }

    private void BuildResearch()
    {
        var area=Landmark("ResearchDressing");
        // Research-room photo shows sterile wall cabinets and dark
        // countertops/monitors, not an empty evenly-lit shell.
        for(var side=0;side<2;side++)
            for(var row=0;row<3;row++)
            {
                var x=side==0?-29f:-5.2f;
                var z=-15.0f+row*9.0f;
                var label="LabBench"+side+"_"+row;
                Box(area,label+"Bench",new Vector3(x,.8f,z),
                    new Vector3(2.20f,.15f,1.06f),CounterDark);
                Box(area,label+"UnderCabinet",new Vector3(x,.37f,z),
                    new Vector3(1.8f,.73f,.85f),Concrete);
                Box(area,label+"Monitor",new Vector3(x,1.22f,z-.13f),
                    new Vector3(.66f,.53f,.07f),Rack);
                Box(area,label+"Screen",new Vector3(x,1.23f,z-.174f),
                    new Vector3(.54f,.42f,.014f),ScreenGlass);
                Box(area,label+"WallCabinet",new Vector3(x,2.60f,z),
                    new Vector3(1.85f,.66f,.48f),CounterWhite);
            }
        for(var i=0;i<3;i++)
            Fluorescent(area,"ResearchOverhead"+i,
                new Vector3(-27f+i*10f,7.28f,-3f));
    }

    private void BuildExamination()
    {
        var area=Landmark("ExaminationDressing");
        // ExaminationRoom screenshot: long white tiled treatment room,
        // wall-height cabinets and thin examination tables.
        for(var i=0;i<3;i++)
        {
            var z=-15f+i*9.0f;
            Box(area,"ExamGurney"+i,new Vector3(-10f,.85f,z),
                new Vector3(1.22f,.16f,2.03f),CounterWhite);
            Box(area,"ExamMattress"+i,new Vector3(-10f,.96f,z),
                new Vector3(1.10f,.12f,1.75f),BedGreen);
            for(var leg=0;leg<4;leg++)
                Box(area,"ExamGurney"+i+"Leg"+leg,
                    new Vector3(-10.5f+(leg%2)*1.0f,.43f,
                        z-.82f+(leg/2)*1.64f),
                    new Vector3(.075f,.78f,.075f),Steel);
            Box(area,"ExamMedicineCabinet"+i,new Vector3(-5.0f,2.22f,z),
                new Vector3(.48f,1.08f,1.35f),CounterWhite);
            Box(area,"ExamMedicineHandle"+i,new Vector3(-5.28f,2.11f,z),
                new Vector3(.035f,.41f,.07f),Steel);
        }
    }

    private void BuildGarage()
    {
        var area=Landmark("GarageDressing");
        // Screenshot: sparse dim garage with concrete posts, ordinary
        // parked cars and overhead fluorescent lighting.
        for(var i=0;i<2;i++)
        {
            var x=i==0?18.9f:37.3f;
            var z=i==0?-13.0f:12.0f;
            var prefix="ParkedCar"+i;
            Box(area,prefix+"Body",new Vector3(x,.72f,z),
                new Vector3(1.80f,.56f,3.40f),
                i==0?new Color(.31f,.31f,.30f):CounterWhite);
            Box(area,prefix+"Cabin",new Vector3(x,1.19f,z-.19f),
                new Vector3(1.42f,.63f,1.70f),Steel,.28f);
            Box(area,prefix+"Windshield",new Vector3(x,1.28f,z-1.07f),
                new Vector3(1.24f,.47f,.035f),ScreenGlass);
            for(var wheel=0;wheel<4;wheel++)
                Cylinder(area,prefix+"Tire"+wheel,
                    new Vector3(x+(wheel%2==0?-.86f:.86f),
                        .39f,z+(wheel/2==0?-1.1f:1.1f)),
                    .36f,.16f,Rack);
            Box(area,prefix+"RearLamp",new Vector3(x, .76f,z+1.74f),
                new Vector3(1.3f,.10f,.025f),
                new Color(.52f,.16f,.12f));
        }
        for(var i=0;i<4;i++)
        {
            var x=i%2==0?15.4f:40.0f;
            var z=i/2==0?-14f:14f;
            Box(area,"ConcretePillarVisual"+i,
                new Vector3(x,2.95f,z),
                new Vector3(.52f,5.7f,.52f),Concrete);
        }
        for(var i=0;i<2;i++)
            Fluorescent(area,"GarageLamp"+i,
                new Vector3(19.1f+i*17.0f,7.37f,0f));
    }

    private void BuildWard()
    {
        var area=Landmark("WardDressing");
        // The existing FALLBACK HospitalWard slab rises to Y=.90. Keep
        // the beds/partitions visibly ABOVE it, not embedded in the
        // historical decorative platform; no collider is introduced.
        area.Position=Vector3.Up*.82f;
        // Owner wiki: open-ceiling makeshift hospital with emergency beds.
        // Beds/canvas partitions are visual-only, so combat and pickups
        // still use existing verified physics/nav locations.
        for(var i=0;i<4;i++)
        {
            var x=i%2==0?9.2f:25.4f;
            var z=i/2==0?-35.1f:-25.5f;
            var prefix="EmergencyCot"+i;
            Box(area,prefix+"Frame",new Vector3(x,.65f,z),
                new Vector3(.88f,.09f,1.91f),Steel);
            Box(area,prefix+"Sheet",new Vector3(x,.73f,z),
                new Vector3(.82f,.095f,1.79f),BedGreen);
            Box(area,prefix+"Pillow",new Vector3(x,.82f,z-.60f),
                new Vector3(.63f,.11f,.35f),CounterWhite);
            for(var leg=0;leg<4;leg++)
                Box(area,prefix+"Support"+leg,
                    new Vector3(x+(leg%2==0?-.35f:.35f),
                        .35f,z+(leg/2==0?-.76f:.76f)),
                    new Vector3(.04f,.60f,.05f),Rack);
        }
        for(var i=0;i<2;i++)
        {
            var x=i==0?5.9f:28.8f;
            Box(area,"WardCurtain"+i,
                new Vector3(x,1.16f,-30.3f),
                new Vector3(.085f,2.15f,7.15f),
                new Color(.58f,.68f,.65f));
            Box(area,"WardCurtainRail"+i,
                new Vector3(x,2.28f,-30.3f),
                new Vector3(.09f,.06f,7.15f),Steel);
        }
    }

    private void BuildCafeteria()
    {
        var area=Landmark("CafeteriaDressing");
        // Cafeteria photo: simple row seating, dark tables and green
        // plants below long cool-white fluorescent fittings.
        // Placement is APPROXIMATE; no source floor coordinate survives.
        for(var i=0;i<4;i++)
        {
            // The older opaque BridgeEast proxy occupies X=6.5..13.5.
            // Seat rows to either side so the new furniture is visible,
            // while the pass never touches that verified bridge geometry.
            var x=3.4f+(i%2)*13.0f;
            var z=25.7f+(i/2)*5.1f;
            var prefix="CafeteriaTable"+i;
            Box(area,prefix+"Top",new Vector3(x,.88f,z),
                new Vector3(2.4f,.11f,1.04f),CounterDark);
            Box(area,prefix+"Leg",new Vector3(x,.45f,z),
                new Vector3(.16f,.81f,.18f),Steel);
            Box(area,prefix+"SeatFront",new Vector3(x,.50f,z-.87f),
                new Vector3(2.25f,.13f,.35f),BedGreen);
            Box(area,prefix+"SeatBack",new Vector3(x,.50f,z+.87f),
                new Vector3(2.25f,.13f,.35f),BedGreen);
        }
        for(var i=0;i<2;i++)
        {
            Cylinder(area,"IndoorPlanter"+i,
                new Vector3(1.5f+i*17.3f,.39f,29f),
                .46f,.65f,Steel);
            Cylinder(area,"IndoorFoliage"+i,
                new Vector3(1.5f+i*17.3f,1.22f,29f),
                .50f,.70f,new Color(.16f,.27f,.20f));
        }
    }

    private static bool ContainsCollision(Node node)
    {
        foreach(var child in node.GetChildren())
            if(child is CollisionObject3D || child is CollisionShape3D ||
               ContainsCollision(child))
                return true;
        return false;
    }
}
