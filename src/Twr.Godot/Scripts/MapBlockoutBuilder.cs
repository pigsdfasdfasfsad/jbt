using Godot;

namespace Twr.Godot;

public sealed record RuntimeMapLayout(
    Vector3 PlayerSpawn,
    IReadOnlyList<Vector3> InfectedSpawns,
    IReadOnlyList<Vector3> PickupPoints,
    IReadOnlyList<Vector3> ObjectivePoints)
{
    // When source markers are recovered, do not move spawns into arbitrary
    // offsets that might intersect the original collision geometry.
    public bool UseExactInfectedSpawns { get; init; }
}

public static class MapBlockoutBuilder
{
    public static RuntimeMapLayout Build(Node3D root, RuntimeMapDefinition map)
    {
        AddEnvironment(root, map.Skybox);

        // APPROXIMATED geometry: topology/landmark choices follow surviving
        // map references, but dimensions and exact coordinates are not source-surveyed.
        return map.Name switch
        {
            "Ranch" => BuildRanch(root),
            "Mill" => BuildMill(root),
            "Bypass" => BuildBypass(root),
            "Cabin" => BuildCabin(root),
            "Cargo" => BuildCargo(root),
            "District" => BuildDistrict(root),
            "Expressway" => BuildExpressway(root),
            "Prison" => BuildPrison(root),
            "Laboratory" => BuildLaboratory(root),
            _ => BuildManor(root)
        };
    }

    private static RuntimeMapLayout BuildRanch(Node3D root)
    {
        BaseArena(root, 96, 82, new Color(0.34f, 0.29f, 0.14f));
        InteriorShell(root, "House", new Vector3(-20, 2.5f, -5), new Vector3(22, 5, 18), new Color(0.40f, 0.32f, 0.23f));
        InteriorShell(root, "Barn", new Vector3(20, 3.5f, -12), new Vector3(22, 7, 24), new Color(0.34f, 0.12f, 0.08f));
        InteriorShell(root, "Stable", new Vector3(21, 2.2f, 19), new Vector3(20, 4.4f, 10), new Color(0.30f, 0.22f, 0.14f));
        InteriorShell(root, "Storehouse", new Vector3(-24, 2.2f, 20), new Vector3(14, 4.4f, 12), new Color(0.29f, 0.24f, 0.17f));
        DecorativeBox(root, "Greenhouse", new Vector3(-4, 1.3f, 25), new Vector3(10, 2.6f, 7), new Color(0.33f, 0.48f, 0.30f));
        return Layout(new Vector3(0, 1, 8),
            EdgeSpawns(43, 36),
            Points((-16,8),(14,7),(-21,25),(20,22),(-12,-19),(12,-22)),
            Points((0,-2),(7,18)));
    }

    private static RuntimeMapLayout BuildMill(Node3D root)
    {
        BaseArena(root, 110, 86, new Color(0.69f, 0.72f, 0.75f));
        DecorativeBox(root, "FrozenLake", new Vector3(-27, 0.02f, 0), new Vector3(46, 0.04f, 78), new Color(0.50f, 0.68f, 0.78f));
        InteriorShell(root, "Sawmill", new Vector3(18, 3, -10), new Vector3(30, 6, 26), new Color(0.34f, 0.28f, 0.20f));
        InteriorShell(root, "Warehouse", new Vector3(23, 2.8f, 22), new Vector3(27, 5.6f, 18), new Color(0.31f, 0.32f, 0.34f));
        InteriorShell(root, "Office", new Vector3(13, 2.3f, -31), new Vector3(18, 4.6f, 10), new Color(0.36f, 0.38f, 0.40f));
        InteriorShell(root, "Tunnel", new Vector3(48, 2.8f, 5), new Vector3(8, 5.6f, 18), new Color(0.18f, 0.19f, 0.20f));
        return Layout(new Vector3(24, 1, 3),
            Points((-50,-29),(-50,0),(-50,29),(50,4),(33,-39)),
            Points((15,-2),(26,20),(17,-29),(30,-15),(20,30),(3,12)),
            Points((13,8),(28,-23)));
    }

    private static RuntimeMapLayout BuildBypass(Node3D root)
    {
        BaseArena(root, 116, 86, new Color(0.25f, 0.25f, 0.23f));
        InteriorShell(root, "Tunnel", new Vector3(-48, 3, 0), new Vector3(14, 6, 22), new Color(0.14f, 0.15f, 0.16f));
        InteriorShell(root, "PlazaNorth", new Vector3(4, 2.7f, -25), new Vector3(42, 5.4f, 14), new Color(0.38f, 0.34f, 0.28f));
        InteriorShell(root, "PlazaSouth", new Vector3(6, 2.7f, 25), new Vector3(42, 5.4f, 14), new Color(0.34f, 0.32f, 0.28f));
        InteriorShell(root, "CivilianBarracks", new Vector3(36, 2.4f, 8), new Vector3(20, 4.8f, 16), new Color(0.27f, 0.31f, 0.24f));
        DecorativeBox(root, "ArchBridge", new Vector3(30, 1.2f, -9), new Vector3(40, 2.4f, 6), new Color(0.28f, 0.29f, 0.30f));
        return Layout(new Vector3(0, 1, 0),
            Points((-55,0),(-35,-39),(-35,39),(54,-22),(54,22)),
            Points((-8,-21),(5,21),(35,8),(25,-8),(-35,4),(16,31)),
            Points((-20,0),(19,0)));
    }

    private static RuntimeMapLayout BuildCabin(Node3D root)
    {
        BaseArena(root, 76, 76, new Color(0.10f, 0.12f, 0.09f));
        InteriorShell(root, "CabinHouse", new Vector3(0, 3, 0), new Vector3(34, 6, 30), new Color(0.25f, 0.18f, 0.13f));
        for (var i = -32; i <= 32; i += 8)
        {
            DecorativeBox(root, "ForestN" + i, new Vector3(i, 3, -34), new Vector3(1.2f, 6, 1.2f), new Color(0.10f, 0.20f, 0.10f));
            DecorativeBox(root, "ForestS" + i, new Vector3(i, 3, 34), new Vector3(1.2f, 6, 1.2f), new Color(0.10f, 0.20f, 0.10f));
        }
        return Layout(new Vector3(0, 1, 18),
            EdgeSpawns(34,34),
            Points((-12,12),(10,10),(-10,-10),(10,-11),(0,4)),
            Points((-8,2),(8,-4)));
    }

    private static RuntimeMapLayout BuildCargo(Node3D root)
    {
        BaseArena(root, 68, 126, new Color(0.20f, 0.23f, 0.24f));
        StaticBox(root, "WestDock", new Vector3(-22, 0.8f, 4), new Vector3(14, 1.6f, 98), new Color(0.32f, 0.31f, 0.28f));
        StaticBox(root, "EastDock", new Vector3(22, 0.8f, 4), new Vector3(14, 1.6f, 98), new Color(0.32f, 0.31f, 0.28f));
        InteriorShell(root, "CargoShip", new Vector3(0, 2.0f, 18), new Vector3(20, 4, 62), new Color(0.20f, 0.24f, 0.27f));
        for (var z = -34; z <= 34; z += 17)
            DecorativeBox(root, "Container" + z, new Vector3(-22, 2.0f, z), new Vector3(8, 4, 12), new Color(0.45f, 0.19f, 0.12f));
        return Layout(new Vector3(0, 2.1f, 38),
            Points((-23,-58),(0,-58),(23,-58),(-32,-45),(32,-45)),
            Points((-22,35),(22,34),(0,38),(-21,18),(21,10),(0,8)),
            Points((0,-8),(0,27)));
    }

    private static RuntimeMapLayout BuildDistrict(Node3D root)
    {
        BaseArena(root, 92, 92, new Color(0.19f, 0.22f, 0.16f));
        InteriorShell(root, "Cafe", new Vector3(-27, 2.7f, -25), new Vector3(22, 5.4f, 18), new Color(0.34f, 0.24f, 0.18f));
        InteriorShell(root, "Foxoil", new Vector3(27, 2.2f, -25), new Vector3(22, 4.4f, 16), new Color(0.28f, 0.31f, 0.20f));
        InteriorShell(root, "MovieTheater", new Vector3(-27, 3.2f, 25), new Vector3(24, 6.4f, 21), new Color(0.24f, 0.18f, 0.22f));
        InteriorShell(root, "BookStore", new Vector3(25, 3.4f, 24), new Vector3(20, 6.8f, 20), new Color(0.27f, 0.22f, 0.17f));
        InteriorShell(root, "ConvenienceStore", new Vector3(30, 2.3f, 3), new Vector3(17, 4.6f, 13), new Color(0.31f, 0.30f, 0.22f));
        DecorativeBox(root, "CrossroadNS", Vector3.Zero, new Vector3(10, 0.08f, 86), new Color(0.11f, 0.11f, 0.11f));
        DecorativeBox(root, "CrossroadEW", Vector3.Zero, new Vector3(86, 0.08f, 10), new Color(0.11f, 0.11f, 0.11f));
        return Layout(new Vector3(0, 1, 0),
            EdgeSpawns(42,42),
            Points((-14,-8),(12,-9),(-12,10),(12,11),(30,7),(-29,2)),
            Points((-5,0),(8,0)));
    }

    private static RuntimeMapLayout BuildExpressway(Node3D root)
    {
        BaseArena(root, 58, 132, new Color(0.24f, 0.25f, 0.26f));
        DecorativeBox(root, "Highway", Vector3.Zero, new Vector3(48, 0.10f, 124), new Color(0.12f, 0.12f, 0.13f));
        for (var z = -42; z <= 42; z += 14)
            DecorativeBox(root, "Vehicle" + z, new Vector3((z / 14 % 2 == 0) ? -8 : 8, 0.75f, z), new Vector3(4, 1.5f, 7), new Color(0.28f, 0.28f, 0.30f));
        StaticBox(root, "ScreeningLeft", new Vector3(-15, 1.6f, 34), new Vector3(4, 3.2f, 24), new Color(0.42f, 0.42f, 0.40f));
        StaticBox(root, "ScreeningRight", new Vector3(15, 1.6f, 34), new Vector3(4, 3.2f, 24), new Color(0.42f, 0.42f, 0.40f));
        return Layout(new Vector3(0, 1, 12),
            Points((-18,-61),(18,-61),(-18,61),(18,61)),
            Points((-11,25),(9,27),(-10,-8),(10,-18),(0,42),(0,-40)),
            Points((0,30),(0,-20)));
    }

    private static RuntimeMapLayout BuildPrison(Node3D root)
    {
        BaseArena(root, 106, 94, new Color(0.22f, 0.23f, 0.20f));
        InteriorShell(root, "CellBlockA", new Vector3(-25, 4.0f, -5), new Vector3(24, 8, 38), new Color(0.30f, 0.31f, 0.31f));
        InteriorShell(root, "CellBlockB", new Vector3(25, 4.0f, -5), new Vector3(24, 8, 38), new Color(0.30f, 0.31f, 0.31f));
        InteriorShell(root, "Isolation", new Vector3(0, 4.5f, -35), new Vector3(28, 9, 14), new Color(0.24f, 0.25f, 0.25f));
        InteriorShell(root, "MessHall", new Vector3(-28, 2.7f, 32), new Vector3(28, 5.4f, 18), new Color(0.34f, 0.32f, 0.27f));
        InteriorShell(root, "Warehouse", new Vector3(30, 2.7f, 33), new Vector3(24, 5.4f, 17), new Color(0.28f, 0.29f, 0.27f));
        DecorativeBox(root, "BasketballCourt", new Vector3(0, 0.05f, 24), new Vector3(20, 0.1f, 18), new Color(0.24f, 0.31f, 0.34f));
        return Layout(new Vector3(0, 1, 15),
            Points((-49,-35),(0,-44),(49,-35),(-49,20),(49,20),(0,-32)),
            Points((-7,20),(7,20),(-27,28),(29,30),(-20,-2),(20,-2)),
            Points((0,24),(0,-18)));
    }

    private static RuntimeMapLayout BuildLaboratory(Node3D root)
    {
        BaseArena(root, 108, 98, new Color(0.19f, 0.21f, 0.22f));
        InteriorShell(root, "Reception", new Vector3(0, 2.2f, 39), new Vector3(30, 4.4f, 12), new Color(0.39f, 0.42f, 0.44f));
        DecorativeBox(root, "BridgeWest", new Vector3(-10, 1.0f, 24), new Vector3(7, 2, 20), new Color(0.34f, 0.37f, 0.39f));
        DecorativeBox(root, "BridgeEast", new Vector3(10, 1.0f, 24), new Vector3(7, 2, 20), new Color(0.34f, 0.37f, 0.39f));
        InteriorShell(root, "Research", new Vector3(-16, 4, -3), new Vector3(35, 8, 42), new Color(0.36f, 0.39f, 0.41f));
        InteriorShell(root, "ParkingGarage", new Vector3(28, 4, 0), new Vector3(28, 8, 46), new Color(0.27f, 0.29f, 0.30f));
        InteriorShell(root, "ServerRoom", new Vector3(-18, 3.2f, -34), new Vector3(28, 6.4f, 18), new Color(0.18f, 0.24f, 0.29f));
        DecorativeBox(root, "HospitalWard", new Vector3(17, 0.45f, -30), new Vector3(28, 0.9f, 18), new Color(0.58f, 0.62f, 0.61f));
        return Layout(new Vector3(0, 1, 39),
            Points((-45,42),(45,42),(-42,-20),(42,-20),(0,-45),(25,-42)),
            Points((0,34),(-7,16),(8,16),(16,-28),(-16,-28),(28,5)),
            Points((0,10),(-5,-24)));
    }

    private static RuntimeMapLayout BuildManor(Node3D root)
    {
        BaseArena(root, 78, 90, new Color(0.12f, 0.13f, 0.12f));
        InteriorShell(root, "WestWing", new Vector3(-21, 2.8f, -7), new Vector3(19, 5.6f, 50), new Color(0.22f, 0.20f, 0.18f));
        InteriorShell(root, "EastWing", new Vector3(21, 2.8f, -7), new Vector3(19, 5.6f, 50), new Color(0.22f, 0.20f, 0.18f));
        InteriorShell(root, "FrontHall", new Vector3(0, 2.7f, 31), new Vector3(28, 5.4f, 11), new Color(0.25f, 0.22f, 0.19f));
        // RECOVERED topology: Manor has usable rooms on both floors and a front
        // staircase plus a west staircase. Exact dimensions remain APPROXIMATED.
        AddUpperFloor(root,"WestWingUpper",new Vector3(-21,2.95f,-7),new Vector3(18.1f,0.22f,49.0f),new Color(0.20f,0.18f,0.16f),true);
        AddUpperFloor(root,"EastWingUpper",new Vector3(21,2.95f,-7),new Vector3(18.1f,0.22f,49.0f),new Color(0.20f,0.18f,0.16f),false);
        AddUpperFloor(root,"FrontHallUpper",new Vector3(0,2.85f,31),new Vector3(27.0f,0.22f,10.0f),new Color(0.22f,0.20f,0.17f),true);
        for (var z = -25; z <= 18; z += 14)
        {
            DecorativeBox(root, "CourtyardCoverL" + z, new Vector3(-6, 1, z), new Vector3(2, 2, 5), new Color(0.25f, 0.26f, 0.24f));
            DecorativeBox(root, "CourtyardCoverR" + z, new Vector3(6, 1, z), new Vector3(2, 2, 5), new Color(0.25f, 0.26f, 0.24f));
        }
        return Layout(new Vector3(0, 1, 20),
            EdgeSpawns(36,42),
            Points((-7,18),(7,18),(-7,4),(7,4),(-7,-14),(7,-14)),
            Points((0,-8),(10,12)));
    }

    private static RuntimeMapLayout Layout(Vector3 player, IReadOnlyList<Vector3> infected, IReadOnlyList<Vector3> pickups, IReadOnlyList<Vector3> objectives) =>
        new(player, infected, pickups, objectives);

    private static IReadOnlyList<Vector3> EdgeSpawns(float x, float z) =>
        Points((-x,-z),(0,-z),(x,-z),(-x,0),(x,0),(-x,z),(0,z),(x,z));

    private static IReadOnlyList<Vector3> Points(params (float X,float Z)[] values) =>
        values.Select(v => new Vector3(v.X, 1f, v.Z)).ToArray();

    private static void BaseArena(Node3D root, float width, float depth, Color ground)
    {
        StaticBox(root, "Ground", new Vector3(0,-0.5f,0), new Vector3(width,1,depth), ground);
        var wall = new Color(0.15f,0.16f,0.16f);
        StaticBox(root, "NorthBoundary", new Vector3(0,2.5f,-depth*0.5f), new Vector3(width,5,1), wall);
        StaticBox(root, "SouthBoundary", new Vector3(0,2.5f,depth*0.5f), new Vector3(width,5,1), wall);
        StaticBox(root, "WestBoundary", new Vector3(-width*0.5f,2.5f,0), new Vector3(1,5,depth), wall);
        StaticBox(root, "EastBoundary", new Vector3(width*0.5f,2.5f,0), new Vector3(1,5,depth), wall);
    }

    private static void AddEnvironment(Node3D root, string skybox)
    {
        var color = skybox switch
        {
            "Night" => new Color(0.015f,0.02f,0.05f),
            "Stormy Night" => new Color(0.025f,0.035f,0.055f),
            "Snowy" => new Color(0.45f,0.50f,0.56f),
            "Sunrise" => new Color(0.42f,0.27f,0.20f),
            "Sunset" => new Color(0.34f,0.20f,0.17f),
            "Cloudy Sunset" => new Color(0.30f,0.24f,0.24f),
            "Cloudy" => new Color(0.30f,0.33f,0.36f),
            _ => new Color(0.14f,0.16f,0.18f)
        };

        root.AddChild(new WorldEnvironment
        {
            Environment = new global::Godot.Environment
            {
                BackgroundMode = global::Godot.Environment.BGMode.Color,
                BackgroundColor = color,
                AmbientLightSource = global::Godot.Environment.AmbientSource.Color,
                AmbientLightColor = color.Lightened(0.25f),
                AmbientLightEnergy = 0.75f
            }
        });

        root.AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-55,-25,0),
            LightEnergy = skybox.Contains("Night", StringComparison.Ordinal) ? 0.45f : 1.05f,
            ShadowEnabled = true
        });
    }

    private static void AddUpperFloor(Node3D root,string name,Vector3 center,Vector3 size,Color color,bool stairsFromSouth)
    {
        // Split deck leaves an opening beside the stair run.
        var half=(size.X-4.0f)*0.5f;
        StaticBox(root,name+"_DeckL",new Vector3(center.X-(2.0f+half*0.5f),center.Y,center.Z),
            new Vector3(half,size.Y,size.Z),color);
        StaticBox(root,name+"_DeckR",new Vector3(center.X+(2.0f+half*0.5f),center.Y,center.Z),
            new Vector3(half,size.Y,size.Z),color);

        var startZ=center.Z+(stairsFromSouth ? size.Z*0.30f : -size.Z*0.30f);
        var zDir=stairsFromSouth ? -1f : 1f;
        const int steps=8;
        for(var i=0;i<steps;i++)
        {
            var y=0.25f+(center.Y-0.35f)*(i+1)/steps;
            var z=startZ+zDir*i*0.75f;
            StaticBox(root,name+"_Stair"+i,new Vector3(center.X,y,z),
                new Vector3(3.2f,0.32f,0.9f),color.Lightened(0.03f));
        }
    }
    private static void InteriorShell(Node3D root,string name,Vector3 center,Vector3 size,Color color)
    {
        // APPROXIMATED reconstruction: exact wall/door transforms are not recovered.
        // Unlike the earlier solid placeholder, this shell is intentionally traversable.
        var thickness=0.45f;
        var bottom=center.Y-size.Y*0.5f;
        var wallY=bottom+size.Y*0.5f;
        var door=Math.Min(4.0f,Math.Max(2.8f,size.X*0.25f));

        StaticBox(root,name+"_Floor",new Vector3(center.X,bottom+0.10f,center.Z),
            new Vector3(size.X,0.20f,size.Z),color.Darkened(0.18f));
        StaticBox(root,name+"_Roof",new Vector3(center.X,bottom+size.Y-0.10f,center.Z),
            new Vector3(size.X,0.20f,size.Z),color.Darkened(0.08f));

        StaticBox(root,name+"_WestWall",new Vector3(center.X-size.X*0.5f+thickness*0.5f,wallY,center.Z),
            new Vector3(thickness,size.Y,size.Z),color);
        StaticBox(root,name+"_EastWall",new Vector3(center.X+size.X*0.5f-thickness*0.5f,wallY,center.Z),
            new Vector3(thickness,size.Y,size.Z),color);

        var segment=Math.Max(0.5f,(size.X-door)*0.5f);
        var xOffset=door*0.5f+segment*0.5f;
        foreach(var zSign in new[]{-1f,1f})
        {
            var z=center.Z+zSign*(size.Z*0.5f-thickness*0.5f);
            StaticBox(root,name+"_DoorWallL_"+(zSign<0?"N":"S"),
                new Vector3(center.X-xOffset,wallY,z),new Vector3(segment,size.Y,thickness),color);
            StaticBox(root,name+"_DoorWallR_"+(zSign<0?"N":"S"),
                new Vector3(center.X+xOffset,wallY,z),new Vector3(segment,size.Y,thickness),color);
        }

        if(size.Z>=14f && size.X>=10f)
        {
            // One interior divider creates rooms while keeping a central passage.
            var gap=3.2f;
            var depthSegment=(size.Z-gap)*0.5f;
            var zOffset=gap*0.5f+depthSegment*0.5f;
            StaticBox(root,name+"_DividerN",new Vector3(center.X,wallY,center.Z-zOffset),
                new Vector3(thickness,size.Y,depthSegment),color.Lightened(0.04f));
            StaticBox(root,name+"_DividerS",new Vector3(center.X,wallY,center.Z+zOffset),
                new Vector3(thickness,size.Y,depthSegment),color.Lightened(0.04f));
        }
    }
    private static void StaticBox(Node3D root, string name, Vector3 position, Vector3 size, Color color)
    {
        var body = new StaticBody3D { Name = name, Position = position };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        body.AddChild(BoxMeshFor(size,color));
        root.AddChild(body);
    }

    private static void DecorativeBox(Node3D root, string name, Vector3 position, Vector3 size, Color color)
    {
        var mesh = BoxMeshFor(size,color);
        mesh.Name = name;
        mesh.Position = position;
        root.AddChild(mesh);
    }

    private static MeshInstance3D BoxMeshFor(Vector3 size, Color color) => new()
    {
        Mesh = new BoxMesh
        {
            Size = size,
            Material = new StandardMaterial3D { AlbedoColor = color, Roughness = 0.95f }
        }
    };
}
