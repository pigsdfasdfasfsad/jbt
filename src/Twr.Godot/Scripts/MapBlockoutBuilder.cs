using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass77 fallback-only Manor stair endpoints, measured from the ACTUAL
/// locally reconstructed collision surface. These do not identify owner
/// source navigation waypoints or an original Roblox stair mesh.
/// </summary>
public sealed record Pass77StairLink(
    string Name, Vector3 LowerFeet, Vector3 UpperFeet);

// Pass80 is a clearly labelled NEW fallback walkable crossing, never
// claimed to be a surveyed stair or bridge from the source game.
public sealed record Pass80UpperCrossing(
    string StairName, Vector3 CentreFeet, Vector3 LeftFeet, Vector3 RightFeet);

public sealed record RuntimeMapLayout(
    Vector3 PlayerSpawn,
    IReadOnlyList<Vector3> InfectedSpawns,
    IReadOnlyList<Vector3> PickupPoints,
    IReadOnlyList<Vector3> ObjectivePoints)
{
    // When source markers are recovered, do not move spawns into arbitrary
    // offsets that might intersect the original collision geometry.
    public bool UseExactInfectedSpawns { get; init; }
    public IReadOnlyList<Vector3> FortificationPoints { get; init; } = [];
    public IReadOnlyList<Pass77StairLink> ApproximateStairs77 { get; init; } = [];
    public IReadOnlyList<Pass80UpperCrossing> ApproximateCrossings80 { get; init; } = [];
    public IReadOnlyList<Vector3> ApproximateUpperPickups82 { get; init; } = [];
    public IReadOnlyList<Vector3> ApproximateUpperObjectives82 { get; init; } = [];
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
        InteriorShell(root, "PlazaNorth", new Vector3(4, 2.7f, -25), new Vector3(42, 5.4f, 14), new Color(0.38f, 0.34f, 0.28f), frontDoorRelief91:true);
        InteriorShell(root, "PlazaSouth", new Vector3(6, 2.7f, 25), new Vector3(42, 5.4f, 14), new Color(0.34f, 0.32f, 0.28f), frontDoorRelief91:true);
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
        // Pass102: the prior +0.20m cabin floor lip isolated the interior
        // Load depot from normal player spawn. Only this PUBLIC fallback
        // shell receives the already-validated Pass91 flush-door floor.
        InteriorShell(root, "CabinHouse", new Vector3(0, 3, 0), new Vector3(34, 6, 30), new Color(0.25f, 0.18f, 0.13f), frontDoorRelief91:true);
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
        // Reference-guided elevated expressway reconstruction; original raw
        // Expressway map/asset binaries have not been recovered.
        return ExpresswaySceneBuilder.Build(root);
    }

    private static RuntimeMapLayout BuildPrison(Node3D root)
    {
        BaseArena(root, 106, 94, new Color(0.22f, 0.23f, 0.20f));
        // Pass91 native Prison Escort reached the real CellBlockA south
        // door (X=-23.32 Z=-24.59) but was unable to step over the
        // approximate 20cm entrance lip into its goal room. Reuse the
        // collision-grounded flush doorway and partition clearances
        // already tested on Bypass; this changes PUBLIC FALLBACKS ONLY.
        InteriorShell(root, "CellBlockA", new Vector3(-25, 4.0f, -5), new Vector3(24, 8, 38), new Color(0.30f, 0.31f, 0.31f),
            frontDoorRelief91:true);
        InteriorShell(root, "CellBlockB", new Vector3(25, 4.0f, -5), new Vector3(24, 8, 38), new Color(0.30f, 0.31f, 0.31f),
            frontDoorRelief91:true);
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
        // Pass102: native W-key travel from the actual Reception spawn
        // reached the Research door but its previous +20cm slab stopped
        // the verified full-capsule route. Reuse the flush fallback-only
        // threshold (the owner-original Laboratory scene is untouched).
        InteriorShell(root, "Research", new Vector3(-16, 4, -3), new Vector3(35, 8, 42), new Color(0.36f, 0.39f, 0.41f), frontDoorRelief91:true);
        InteriorShell(root, "ParkingGarage", new Vector3(28, 4, 0), new Vector3(28, 8, 46), new Color(0.27f, 0.29f, 0.30f));
        InteriorShell(root, "ServerRoom", new Vector3(-18, 3.2f, -34), new Vector3(28, 6.4f, 18), new Color(0.18f, 0.24f, 0.29f));
        DecorativeBox(root, "HospitalWard", new Vector3(17, 0.45f, -30), new Vector3(28, 0.9f, 18), new Color(0.58f, 0.62f, 0.61f));
        // Pass115: visual-only dressing is scoped to THIS fallback
        // builder. The source-pack path calls LaboratorySourceLoader
        // instead, so original Roblox owner geometry remains untouched.
        var props115=Pass115LaboratoryDressing.AddToFallback(root);
        // Pass116 is material-only for the SAME source-absent blockout.
        // Repaint existing mesh materials, never static collision bodies,
        // player spawn/physics, original owner geometry or objective paths.
        Pass116LaboratorySurfaceMaterials.AddToFallback(root,props115);
        return Layout(new Vector3(0, 1, 39),
            Points((-45,42),(45,42),(-42,-20),(42,-20),(0,-45),(25,-42)),
            Points((0,34),(-7,16),(8,16),(16,-28),(-16,-28),(28,5)),
            Points((0,10),(-5,-24)));
    }

    private static RuntimeMapLayout BuildManor(Node3D root)
    {
        BaseArena(root, 78, 90, new Color(0.12f, 0.13f, 0.12f));
        InteriorShell(root, "WestWing", new Vector3(-21, 2.8f, -7), new Vector3(19, 5.6f, 50), new Color(0.22f, 0.20f, 0.18f), .15f, 9.55f);
        // Pass102: the source-absent WestWing has a 20cm interior slab
        // but its south opening had no step-free physical entrance. Use a
        // very short walkable threshold without moving either the 3.06m
        // upper stair landing or the previously verified stair geometry.
        AddWestWingDoorThreshold102(root);
        InteriorShell(root, "EastWing", new Vector3(21, 2.8f, -7), new Vector3(19, 5.6f, 50), new Color(0.22f, 0.20f, 0.18f), -23.45f, -14.15f);
        InteriorShell(root, "FrontHall", new Vector3(0, 2.7f, 31), new Vector3(28, 5.4f, 11), new Color(0.25f, 0.22f, 0.19f));
        // RECOVERED topology: Manor has usable rooms on both floors and a front
        // staircase plus a west staircase. Exact dimensions remain APPROXIMATED.
        var westStair77 = AddUpperFloor(root,"WestWingUpper",new Vector3(-21,2.95f,-7),new Vector3(18.1f,0.22f,49.0f),new Color(0.20f,0.18f,0.16f),true);
        var eastStair77 = AddUpperFloor(root,"EastWingUpper",new Vector3(21,2.95f,-7),new Vector3(18.1f,0.22f,49.0f),new Color(0.20f,0.18f,0.16f),false);
        var frontStair77 = AddUpperFloor(root,"FrontHallUpper",new Vector3(0,2.85f,31),new Vector3(27.0f,0.22f,10.0f),new Color(0.22f,0.20f,0.17f),true);
        for (var z = -25; z <= 18; z += 14)
        {
            DecorativeBox(root, "CourtyardCoverL" + z, new Vector3(-6, 1, z), new Vector3(2, 2, 5), new Color(0.25f, 0.26f, 0.24f));
            DecorativeBox(root, "CourtyardCoverR" + z, new Vector3(6, 1, z), new Vector3(2, 2, 5), new Color(0.25f, 0.26f, 0.24f));
        }
        // Pass82: new fallback-Manor-only upstairs gameplay markers.
        // They are physically aligned to three previously verified decks.
        // All source-original scene packs and the other nine maps retain
        // their own item/objective marker locations.
        var upperPickups82 = Pass82ManorUpperSpawnPlan.BuildPickupMarkers(
            westStair77, eastStair77, frontStair77);
        var upperObjectives82 = Pass82ManorUpperSpawnPlan.BuildObjectiveMarkers(
            westStair77, eastStair77, frontStair77);
        return Layout(new Vector3(0, 1, 20),
            EdgeSpawns(36,42),
            Points((-7,18),(7,18),(-7,4),(7,4),(-7,-14),(7,-14))
                .Concat(upperPickups82).ToArray(),
            Points((0,-8),(10,12))
                .Concat(upperObjectives82).ToArray()) with
        {
            ApproximateStairs77 = [westStair77, eastStair77, frontStair77],
            ApproximateCrossings80 = [
                MakeUpperCrossing80(westStair77),
                MakeUpperCrossing80(eastStair77),
                MakeUpperCrossing80(frontStair77)],
            ApproximateUpperPickups82 = upperPickups82,
            ApproximateUpperObjectives82 = upperObjectives82
        };
    }

    private static void AddWestWingDoorThreshold102(Node3D root)
    {
        // This COLLISION-ONLY accessibility patch is restricted to the
        // public fallback Manor blockout. At the south door (Z=18) the
        // interior floor is 0.20m above the ground: a normal W walk cannot
        // step straight up. Rise gently over 2m, from Z=20/Y=0 to Z=18/
        // Y=0.20, centred inside the existing 4m-wide open doorway.
        // Do not substitute this invented threshold for owner source art.
        const float rise=.20f;
        const float run=2.0f;
        const float thickness=.16f;
        var pitch=Mathf.Atan2(rise,run);
        var slopeLength=Mathf.Sqrt(run*run+rise*rise);
        var size=new Vector3(3.10f,thickness,slopeLength+.08f);
        var threshold=new StaticBody3D
        {
            Name="WestWingSouthWalkableThreshold102",
            Position=new Vector3(-21f,
                rise*.5f-thickness*.5f*Mathf.Cos(pitch),19f),
            Rotation=new Vector3(pitch,0f,0f),
            CollisionLayer=1u,CollisionMask=0u
        };
        threshold.AddChild(new CollisionShape3D
        {
            Shape=new BoxShape3D{Size=size}
        });
        threshold.AddChild(BoxMeshFor(size,
            new Color(.23f,.20f,.17f)));
        root.AddChild(threshold);
    }

    private static Pass80UpperCrossing MakeUpperCrossing80(Pass77StairLink stair)
    {
        var sign = Math.Sign(stair.UpperFeet.Z-stair.LowerFeet.Z);
        var center = stair.UpperFeet +
            new Vector3(0f, 0f, sign*1.40f);
        return new Pass80UpperCrossing(stair.Name, center,
            center+Vector3.Left*4.25f, center+Vector3.Right*4.25f);
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

    private static Pass77StairLink AddUpperFloor(Node3D root,string name,
        Vector3 center,Vector3 size,Color color,bool stairsFromSouth)
    {
        // APPROXIMATED Manor geometry, not source-recovered retail meshes.
        // The earlier eight independent 32cm-high box steps could not be
        // walked by Godot CharacterBody3D without repeated jumping, and
        // left a 4m-wide disconnected opening between both upper decks.
        // Pass77 gives these same visible stair treads a separate continuous,
        // walkable 25-degree collision ramp and a physical upper landing.
        var half=(size.X-4.0f)*0.5f;
        StaticBox(root,name+"_DeckL",new Vector3(center.X-(2.0f+half*0.5f),center.Y,center.Z),
            new Vector3(half,size.Y,size.Z),color);
        StaticBox(root,name+"_DeckR",new Vector3(center.X+(2.0f+half*0.5f),center.Y,center.Z),
            new Vector3(half,size.Y,size.Z),color);

        var startZ=center.Z+(stairsFromSouth ? size.Z*0.30f : -size.Z*0.30f);
        var zDir=stairsFromSouth ? -1f : 1f;
        const int steps=8;
        const float treadSpacing=.75f;
        const float rampThickness=.16f;
        var lower = new Vector3(center.X,.20f,
            startZ-zDir*.60f);
        var upper = new Vector3(center.X,center.Y+size.Y*.5f,
            startZ+zDir*(steps-1)*treadSpacing);
        var run = Math.Abs(upper.Z-lower.Z);
        var rise = upper.Y-lower.Y;
        var angle = Mathf.Atan2(rise,run) * (stairsFromSouth ? 1f : -1f);
        var slopeLength = Mathf.Sqrt(run*run+rise*rise);
        if (Math.Abs(angle) >= Mathf.DegToRad(35f))
            throw new InvalidOperationException("Manor reconstructed stair slope unsafe");

        var ramp = new StaticBody3D
        {
            Name = name+"_WalkableRamp77",
            Position = (lower+upper)*.5f +
                Vector3.Down*rampThickness*.5f*Mathf.Cos(angle),
            Rotation = new Vector3(angle,0,0),
            CollisionLayer=1,
            CollisionMask=0
        };
        // A 5.6m top-to-bottom collision width reaches the two side decks
        // directly. The former 3.2m corridor collided with the widened
        // connector's internal side face as the zombie approached the top.
        var rampSize = new Vector3(5.60f,rampThickness,slopeLength+.20f);
        ramp.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = rampSize }
        });
        ramp.AddChild(BoxMeshFor(rampSize,color.Darkened(.06f)));
        root.AddChild(ramp);

        // Bridge the actual split deck so the upper ramp exit reaches both
        // left and right balcony platforms without a void/ledge jump.
        // A flat cross-landing still introduced a riser at its downhill
        // edge (native player stopped at 2.48m, then 2.84m after shortening
        // it, below the true 2.96m upper deck). Instead, overlap the upper
        // ramp with a WIDER section of that very same slope. The centre
        // has a continuous no-lip collision plane and its outer ±2.8m
        // physically overlap both side deck tops near the upper endpoint.
        const float bridgeThickness=.16f;
        var bridgeSize=new Vector3(5.6f,bridgeThickness,1.40f);
        var bridge=new StaticBody3D
        {
            Name=name+"_Landing77",
            // Keep this continuation BELOW the main ramp's top plane.
            // Two exactly coplanar solids create an internal collision
            // seam for character capsules; a recessed overlap avoids it
            // while retaining a solid 5.6m bridge over the centre gap.
            Position=upper+Vector3.Down*(
                bridgeThickness*.5f*Mathf.Cos(angle)+.16f),
            Rotation=new Vector3(angle,0,0),
            CollisionLayer=1,
            CollisionMask=0
        };
        bridge.AddChild(new CollisionShape3D
        {
            Shape=new BoxShape3D { Size=bridgeSize }
        });
        bridge.AddChild(BoxMeshFor(bridgeSize,color.Lightened(.02f)));
        root.AddChild(bridge);

        // Keep the existing eight recognizable visual stair treads, but they
        // are now meshes only: the continuous sloped ramp owns collision.
        for(var i=0;i<steps;i++)
        {
            var t=(i+.5f)/steps;
            var surface=lower.Lerp(upper,t);
            DecorativeBox(root,name+"_Stair"+i,
                surface+Vector3.Up*.035f,
                new Vector3(3.12f,.025f,.24f),color.Lightened(.03f));
        }
        // Pass80: a solid crosswalk JUST PAST the walkable ramp. The
        // upper surface is exactly level with the side decks, avoiding
        // an obstructing internal vertical seam at the balcony edge.
        // It does not pretend to replace retail Manor collision meshes.
        var crossingZ=upper.Z+zDir*1.40f;
        // Pass94: the full-size Escort now approaches the stair
        // laterally from an existing side deck, not through the pitched
        // landing's steep front collision face. Restore the unchanged,
        // Pass78/83-verified source-approximate upper crosswalk geometry.
        // The real capsule sweep must still audit every side approach.
        StaticBox(root,name+"_UpperCrossing80",
            new Vector3(center.X,upper.Y-.08f,crossingZ),
            new Vector3(9.0f,.16f,1.20f),color.Lightened(.065f));
        return new Pass77StairLink(name,lower,upper);
    }
    private static void InteriorShell(Node3D root,string name,Vector3 center,Vector3 size,Color color,
        float openingMinZ80=float.NaN,float openingMaxZ80=float.NaN,
        bool frontDoorRelief91=false)
    {
        // APPROXIMATED reconstruction: exact wall/door transforms are not recovered.
        // Unlike the earlier solid placeholder, this shell is intentionally traversable.
        var thickness=0.45f;
        var bottom=center.Y-size.Y*0.5f;
        var wallY=bottom+size.Y*0.5f;
        var door=Math.Min(4.0f,Math.Max(2.8f,size.X*0.25f));

        // Pass91 Bypass: a 0.20m raised floor lip directly behind the
        // two exterior door openings previously stopped non-jumping
        // CharacterBody3D survivors at Z=-17.41. Match the verified
        // fallback arena ground level instead of requiring an impossible
        // step-up. The original-map loader never uses this blockout.
        var floorCenterY91=frontDoorRelief91
            ? bottom-.10f  // 0.20m slab ends flush at Y=bottom
            : bottom+.10f; // untouched other reconstructed interiors
        StaticBox(root,name+"_Floor",new Vector3(center.X,floorCenterY91,center.Z),
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
            // Normal scenes retain their original approximate divider.
            // Pass80 carves a documented, fallback-Manor-only stair slot
            // through the obstruction, preserving both wall segments.
            if (float.IsNaN(openingMinZ80) || float.IsNaN(openingMaxZ80))
            {
                if (frontDoorRelief91)
                {
                    // Pass91: the blockout's Bypass plaza center divider
                    // previously met BOTH exterior doors without a turning
                    // pocket. A .42m-radius Escort body reached X=3.58,
                    // Z=-17.41 and could not follow the graph into the
                    // North plaza. Keep the central partition but recess
                    // each end 2.70m from the doorway, on BOTH plazas.
                    // Geometry is explicitly approximation-only.
                    const float clearance91=2.70f;
                    AddRecessedDivider91(root,name+"_DividerN",center.X,wallY,
                        center.Z-zOffset-depthSegment*.5f,
                        center.Z-zOffset+depthSegment*.5f,
                        clearance91,0f,thickness,size.Y,color);
                    AddRecessedDivider91(root,name+"_DividerS",center.X,wallY,
                        center.Z+zOffset-depthSegment*.5f,
                        center.Z+zOffset+depthSegment*.5f,
                        0f,clearance91,thickness,size.Y,color);
                }
                else
                {
                    StaticBox(root,name+"_DividerN",new Vector3(center.X,wallY,center.Z-zOffset),
                        new Vector3(thickness,size.Y,depthSegment),color.Lightened(0.04f));
                    StaticBox(root,name+"_DividerS",new Vector3(center.X,wallY,center.Z+zOffset),
                        new Vector3(thickness,size.Y,depthSegment),color.Lightened(0.04f));
                }
            }
            else
            {
                AddDividedManorWall80(root,name+"_DividerN",
                    center.X,wallY,center.Z-size.Z*.5f,center.Z-gap*.5f,
                    openingMinZ80,openingMaxZ80,thickness,size.Y,color);
                AddDividedManorWall80(root,name+"_DividerS",
                    center.X,wallY,center.Z+gap*.5f,center.Z+size.Z*.5f,
                    openingMinZ80,openingMaxZ80,thickness,size.Y,color);
            }
        }
    }
    /// <summary>
    /// Recess approximate interior partition ends without shifting any
    /// source-recovered building collision. Actual static wall still exists,
    /// but leaves a door-throat turning pocket for larger NPC capsules.
    /// </summary>
    private static void AddRecessedDivider91(Node3D root,string name,
        float x,float y,float beginZ,float endZ,float trimBegin,float trimEnd,
        float width,float height,Color color)
    {
        var begin=beginZ+trimBegin;
        var end=endZ-trimEnd;
        if(end-begin<.50f)return;
        StaticBox(root,name,
            new Vector3(x,y,(begin+end)*.5f),
            new Vector3(width,height,end-begin),
            color.Lightened(.04f));
    }

    private static void AddDividedManorWall80(Node3D root,string name,
        float x,float wallY,float firstZ,float lastZ,
        float gapFirst,float gapLast,float width,float height,Color color)
    {
        // Each piece remains a real static wall collider; the empty slot
        // is physically open from ground level to the top of the shell.
        // This corrected fallback wall must never modify original maps.
        var leftEnd=Math.Min(lastZ,gapFirst);
        var rightStart=Math.Max(firstZ,gapLast);
        if(leftEnd-firstZ >= .5f)
            StaticBox(root,name+"_Before80",
                new Vector3(x,wallY,(firstZ+leftEnd)*.5f),
                new Vector3(width,height,leftEnd-firstZ),color.Lightened(.04f));
        if(lastZ-rightStart >= .5f)
            StaticBox(root,name+"_After80",
                new Vector3(x,wallY,(rightStart+lastZ)*.5f),
                new Vector3(width,height,lastZ-rightStart),color.Lightened(.04f));
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
