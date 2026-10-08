using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Reference-guided reconstruction of Expressway's elevated highway,
/// traffic jam and medical screening checkpoint. The original decorated
/// Expressway map is not present in the available Roblox place export.
/// Dimensions and vehicle positions here are reconstructed/approximate,
/// not claimed as exact original Roblox instance transforms.
/// </summary>
public static class ExpresswaySceneBuilder
{
    private static readonly Color Asphalt = new(0.105f, 0.115f, 0.13f);
    private static readonly Color OldAsphalt = new(0.155f, 0.165f, 0.175f);
    private static readonly Color Concrete = new(0.43f, 0.45f, 0.45f);
    private static readonly Color ConcreteDark = new(0.25f, 0.27f, 0.28f);
    private static readonly Color Guardrail = new(0.48f, 0.50f, 0.51f);
    private static readonly Color Sand = new(0.52f, 0.47f, 0.37f);
    private static readonly Color Window = new(0.12f, 0.19f, 0.23f);
    private static readonly Color WarmLight = new(0.93f, 0.84f, 0.65f);
    private static readonly Color RoadWhite = new(0.68f, 0.69f, 0.66f);
    private static readonly Color RoadYellow = new(0.70f, 0.53f, 0.19f);

    private static readonly StandardMaterial3D RoadMat = Paint(Asphalt, 0.97f);
    private static readonly StandardMaterial3D CementMat = Paint(Concrete, 0.93f);
    private static readonly StandardMaterial3D RailMat = Paint(Guardrail, 0.64f, true);
    private static readonly StandardMaterial3D SandMat = Paint(Sand, 0.96f);
    private static readonly StandardMaterial3D WhiteMat = Paint(RoadWhite, 0.84f);
    private static readonly StandardMaterial3D YellowMat = Paint(RoadYellow, 0.84f);
    private static readonly StandardMaterial3D GlassMat = Paint(Window, 0.18f, true);
    private static readonly StandardMaterial3D TireMat = Paint(new Color(0.058f, 0.062f, 0.065f), 0.95f);

    public static RuntimeMapLayout Build(Node3D host)
    {
        var map = new Node3D { Name = "ExpresswayReconstruction" };
        host.AddChild(map);
        BuildSky(host, map);
        BuildElevatedRoad(map);
        BuildCityAndLowerRoad(map);
        BuildStreetlights(map);
        BuildTraffic(map);
        BuildScreeningCheckpoint(map);
        BuildPeripheralDetail(map);
        map.AddChild(new ExpresswayNavigationRuntime { Name = "HighwayNavigation" });

        // Until the real original spawn folders can be recovered, these are
        // navigation-safe layout estimates, not exact original placements.
        return new RuntimeMapLayout(
            new Vector3(0, 1.1f, 12f),
            new[]
            {
                new Vector3(-10f, 1.1f, -61f),
                new Vector3(12f, 1.1f, -62f),
                new Vector3(-17f, 1.1f, 59f),
                new Vector3(17f, 1.1f, 59f)
            },
            new[]
            {
                new Vector3(-3f, 0.8f, 18f),
                new Vector3(5f, 0.8f, 4f),
                new Vector3(-5f, 0.8f, -19f),
                new Vector3(4f, 0.8f, -38f),
                new Vector3(-17f, 0.8f, 34f),
                new Vector3(17f, 0.8f, -10f)
            },
            new[]
            {
                new Vector3(0f, 0.9f, 29f),
                new Vector3(-4f, 0.9f, -26f),
                new Vector3(5f, 0.9f, 7f)
            });
    }

    private static void BuildSky(Node3D host, Node3D map)
    {
        var environment = host.GetNodeOrNull<WorldEnvironment>("WorldEnvironment");
        if (environment?.Environment is not null)
        {
            environment.Environment.BackgroundMode = global::Godot.Environment.BGMode.Sky;
            environment.Environment.Sky = new Sky
            {
                SkyMaterial = new ProceduralSkyMaterial
                {
                    SkyTopColor = new Color(0.18f, 0.21f, 0.27f),
                    SkyHorizonColor = new Color(0.36f, 0.36f, 0.39f),
                    GroundBottomColor = new Color(0.12f, 0.15f, 0.18f),
                    GroundHorizonColor = new Color(0.26f, 0.27f, 0.29f)
                }
            };
            environment.Environment.AmbientLightSource =
                global::Godot.Environment.AmbientSource.Sky;
            environment.Environment.AmbientLightEnergy = 0.47f;
            environment.Environment.FogEnabled = true;
            environment.Environment.FogDensity = 0.006f;
            environment.Environment.FogLightColor = new Color(0.31f, 0.33f, 0.37f);
        }

        // Distant storm banks: loose layers rather than a featureless flat sky.
        // The source game has a much richer cloudy HDR skybox.
        var cloud = Paint(new Color(0.23f, 0.26f, 0.30f, 0.48f),
            1f, false, true);
        for (var layer = 0; layer < 3; layer++)
        {
            for (var i = 0; i < 8; i++)
            {
                var angle = Mathf.Tau * i / 8f + layer * 0.4f;
                var radius = 105f + layer * 12f;
                var mesh = new MeshInstance3D
                {
                    Name = "StormCloud",
                    Position = new Vector3(Mathf.Cos(angle) * radius,
                        37f + layer * 8f, Mathf.Sin(angle) * radius),
                    Scale = new Vector3(28f, 3.5f, 13f),
                    Mesh = new SphereMesh { Radius = 0.5f, Height = 1f },
                    MaterialOverride = cloud
                };
                map.AddChild(mesh);
            }
        }
    }

    private static void BuildElevatedRoad(Node3D map)
    {
        var bridge = new Node3D { Name = "ElevatedBridge" };
        map.AddChild(bridge);

        // The playable bridge surface is y=0. The underpass is far below.
        Solid(bridge, "StructuralBridgeDeck", new Vector3(0, -1.1f, 0),
            new Vector3(49f, 2.2f, 138f), ConcreteDark);
        Visual(bridge, "AsphaltRoadway", new Vector3(0, 0.022f, 0),
            new Vector3(46f, 0.06f, 136f), RoadMat);
        Visual(bridge, "AsphaltWearStripL", new Vector3(-7f, 0.058f, 0),
            new Vector3(3f, 0.008f, 133f), Paint(OldAsphalt, 0.98f));
        Visual(bridge, "AsphaltWearStripR", new Vector3(7f, 0.058f, 0),
            new Vector3(3f, 0.008f, 133f), Paint(OldAsphalt, 0.98f));

        // Lane markings, shoulder stripes and patched surface joints.
        foreach (var x in new[] { -11.1f, 0f, 11.1f })
            for (var z = -64; z < 66; z += 10)
                Visual(bridge, "DashedLaneStripe", new Vector3(x, 0.073f, z),
                    new Vector3(0.23f, 0.01f, 4.7f), WhiteMat);

        foreach (var x in new[] { -21.5f, 21.5f })
            Visual(bridge, "YellowShoulderStripe", new Vector3(x, 0.073f, 0),
                new Vector3(0.19f, 0.01f, 132f), YellowMat);

        for (var z = -65; z <= 65; z += 16)
        {
            Visual(bridge, "BridgeExpansionJoint",
                new Vector3(0f, 0.064f, z),
                new Vector3(45f, 0.014f, 0.075f),
                Paint(new Color(0.07f, 0.075f, 0.08f), 1f));
        }

        foreach (var side in new[] { -1, 1 })
        {
            var x = side * 23.4f;
            // Solid raised sides must stop players from falling onto the
            // decorative underpass, but they do not block highway traffic.
            Solid(bridge, "BridgeParapet", new Vector3(x, 0.61f, 0),
                new Vector3(0.9f, 1.22f, 137f), Concrete);
            Visual(bridge, "ParapetCoping",
                new Vector3(x, 1.27f, 0),
                new Vector3(1.15f, 0.15f, 137f), CementMat);
            Visual(bridge, "ParapetOuterLip",
                new Vector3(x + side * 0.48f, -0.36f, 0),
                new Vector3(0.19f, 0.4f, 137f), RailMat);
            for (var z = -62; z <= 62; z += 6)
                Visual(bridge, "ConcreteJoint",
                    new Vector3(x - side * 0.45f, 0.61f, z),
                    new Vector3(0.016f, 1.1f, 0.08f),
                    Paint(new Color(0.26f, 0.29f, 0.30f), 0.98f));
        }
        // Supporting I-beams and pylons are visible from the lower freeway.
        for (var z = -57; z <= 58; z += 23)
        {
            for (var x = -15; x <= 15; x += 30)
            {
                Solid(bridge, "BridgeSupportPillar",
                    new Vector3(x, -9.5f, z),
                    new Vector3(2.7f, 17f, 2.7f), ConcreteDark);
                Visual(bridge, "PillarBase",
                    new Vector3(x, -17.4f, z),
                    new Vector3(4f, 1.2f, 4f), CementMat);
            }
            Visual(bridge, "TransverseDeckBeam",
                new Vector3(0f, -2.7f, z),
                new Vector3(44f, 1.1f, 0.75f), CementMat);
        }
        for (var x = -15; x <= 15; x += 10)
            Visual(bridge, "LongitudinalDeckBeam",
                new Vector3(x, -2.7f, 0),
                new Vector3(0.85f, 1.0f, 136f), CementMat);

        // Beyond each playable end: geometry reads as continuous road,
        // while solid end blocks prevent walking out of the level.
        for (var zSign = -1; zSign <= 1; zSign += 2)
        {
            Visual(bridge, "HighwayContinuation",
                new Vector3(0, -0.035f, zSign * 81f),
                new Vector3(44f, 0.07f, 24f), RoadMat);
            Solid(bridge, "EndBoundary",
                new Vector3(0, 2.0f, zSign * 68.5f),
                new Vector3(47f, 4f, 0.65f),
                new Color(0.20f, 0.23f, 0.24f));
        }
    }

    private static void BuildCityAndLowerRoad(Node3D map)
    {
        var city = new Node3D { Name = "IndianapolisBackdrop" };
        map.AddChild(city);
        Visual(city, "LowerHighway", new Vector3(0, -17.9f, 0),
            new Vector3(240f, 0.25f, 300f),
            Paint(new Color(0.105f,0.115f,0.125f), 0.97f));
        for (var z = -135; z <= 135; z += 12)
            for (var x = -90; x <= 90; x += 30)
                Visual(city, "UnderpassMarking", new Vector3(x, -17.74f, z),
                    new Vector3(0.22f, 0.01f, 5.4f), WhiteMat);

        // Distant silhouette is intentionally uncollidable: it is a visual
        // skyline only, not recovered source architecture.
        for (var side = -1; side <= 1; side += 2)
            for (var row = 0; row < 3; row++)
                for (var i = 0; i < 19; i++)
                {
                    var z = -139f + i * 15f + row * 3f;
                    var height = 13f + (i * 17 + row * 13) % 29;
                    var width = 9f + (i * 11 + row * 2) % 12;
                    var x = side * (42f + row * 15f +
                        (i * 7 % 8));
                    var building = new Node3D
                    {
                        Name = "BackdropTower",
                        Position = new Vector3(x, -18f, z)
                    };
                    city.AddChild(building);
                    var stone = Paint(new Color(
                        0.10f + row * 0.025f,
                        0.13f + row * 0.025f,
                        0.17f + row * 0.02f), 0.94f);
                    Visual(building, "BuildingShell",
                        new Vector3(0, height * 0.5f, 0),
                        new Vector3(width, height, 11f + (i % 4) * 4f), stone);
                    Visual(building, "RoofLine",
                        new Vector3(0, height + 0.3f, 0),
                        new Vector3(width + 0.3f, 0.5f, 13f),
                        Paint(new Color(0.08f, 0.10f, 0.13f), 0.85f));
                    // Sparse lit windows add depth without pretending this is
                    // authentic map-building geometry.
                    if (row == 0 && i % 3 != 0)
                        for (var floor = 0; floor < (int)(height / 5f); floor++)
                            for (var col = -1; col <= 1; col++)
                                Visual(building, "DistantWindow",
                                    new Vector3(-side * (width * 0.5f + 0.012f),
                                        3f + floor * 4.5f,
                                        col * 2.9f),
                                    new Vector3(0.04f, 0.55f, 0.65f),
                                    Paint(new Color(0.48f,0.48f,0.39f), 0.76f,
                                        false, false, true));
                }
    }

    private static void BuildStreetlights(Node3D map)
    {
        var lights = new Node3D { Name = "HighwayStreetlamps" };
        map.AddChild(lights);
        for (var side = -1; side <= 1; side += 2)
            for (var z = -58; z <= 59; z += 20)
            {
                var pivot = new Node3D
                {
                    Name = "CurvedLightPole",
                    Position = new Vector3(side * 22.55f, 0f, z)
                };
                lights.AddChild(pivot);
                Cylinder(pivot, "LampShaft",
                    new Vector3(0, 4.2f, 0),
                    0.10f, 8.4f, RailMat);
                Cylinder(pivot, "LampOutrigger",
                    new Vector3(-side * 1.55f, 8.3f, 0),
                    0.075f, 3.25f, RailMat,
                    new Vector3(0,0,90));
                Visual(pivot, "LampHousing",
                    new Vector3(-side * 3.14f, 8.06f, 0),
                    new Vector3(0.80f, 0.24f, 0.42f),
                    Paint(new Color(0.20f,0.21f,0.23f), 0.6f, true));
                Visual(pivot, "LampGlowingPanel",
                    new Vector3(-side * 3.14f, 7.92f, 0),
                    new Vector3(0.64f, 0.03f, 0.32f),
                    Paint(new Color(0.90f,0.86f,0.71f), 0.16f,
                        false, false, true));
                pivot.AddChild(new OmniLight3D
                {
                    Name = "LampIllumination",
                    Position = new Vector3(-side * 3.14f, 7.88f, 0),
                    LightColor = WarmLight,
                    LightEnergy = 0.70f,
                    OmniRange = 15f,
                    ShadowEnabled = false
                });
            }
    }

    private static void BuildTraffic(Node3D map)
    {
        var traffic = new Node3D { Name = "AbandonedTraffic" };
        map.AddChild(traffic);
        // Vehicles are placed to match the recognizable road-jam silhouette.
        // Layout stays navigable, with unobstructed central escape corridors.
        var cars = new (float X, float Z, float Yaw, string Type, Color Paint)[]
        {
            (-16f,-53f,-4f,"sedan",new Color(.31f,.30f,.27f)),
            ( 15f,-54f, 5f,"truck",new Color(.26f,.30f,.32f)),
            (-15f,-38f, 6f,"pickup",new Color(.17f,.27f,.38f)),
            ( 16f,-38f, 3f,"sedan",new Color(.34f,.17f,.16f)),
            (-13f,-18f,-7f,"pickup",new Color(.24f,.32f,.48f)),
            ( 14f,-21f, 4f,"van",new Color(.25f,.25f,.26f)),
            (-15f, -3f, 4f,"sedan",new Color(.33f,.31f,.30f)),
            ( 14f,  0f,-6f,"pickup",new Color(.58f,.59f,.55f)),
            (-14f, 17f, 5f,"pickup",new Color(.17f,.27f,.42f)),
            ( 15f, 22f,-4f,"sedan",new Color(.29f,.12f,.12f)),
            (-16f, 38f, 7f,"van",new Color(.22f,.24f,.23f)),
            ( 15f, 42f, 9f,"pickup",new Color(.38f,.39f,.35f)),
            (-12f, 55f,-5f,"sedan",new Color(.41f,.41f,.40f))
        };
        var index = 0;
        foreach (var car in cars)
            BuildVehicle(traffic, "AbandonedVehicle" + index++,
                car.X, car.Z, car.Yaw, car.Type, car.Paint);

        // Distinct stranded semi-truck beyond the distant infected approach.
        BuildSemiTruck(traffic, new Vector3(0, 0, -64), -2f);
    }

    private static void BuildVehicle(
        Node3D host, string name, float x, float z,
        float yaw, string kind, Color paint)
    {
        var vehicle = new Node3D
        {
            Name = name,
            Position = new Vector3(x, 0.08f, z),
            RotationDegrees = new Vector3(0, yaw, 0)
        };
        host.AddChild(vehicle);

        var pickup = kind == "pickup";
        var van = kind == "van";
        var truck = kind == "truck";
        var length = truck ? 6.7f : van ? 5.5f : pickup ? 5.3f : 4.6f;
        var width = truck ? 2.8f : 2.25f;
        var body = new StaticBody3D { Name = "VehicleCollider" };
        body.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D
            {
                Size = new Vector3(width, truck ? 2.65f : 1.8f, length)
            },
            Position = new Vector3(0, truck ? 1.3f : 0.9f, 0)
        });
        vehicle.AddChild(body);

        var carPaint = Paint(paint, 0.47f, true);
        Visual(vehicle, "PaintedChassis",
            new Vector3(0, 0.76f, 0),
            new Vector3(width, 0.69f, length), carPaint);
        Visual(vehicle, "PassengerCab",
            new Vector3(0, truck ? 1.85f : 1.48f, pickup ? -0.80f : -0.02f),
            new Vector3(width * 0.85f, truck ? 1.55f : 0.96f,
                van ? 3.95f : pickup ? 2.35f : 2.75f), carPaint);
        Visual(vehicle, "FrontWindshield",
            new Vector3(0, truck ? 1.96f : 1.50f,
                pickup ? -2.02f : -1.43f),
            new Vector3(width * 0.73f, truck ? 0.95f : 0.64f, 0.07f),
            GlassMat);
        Visual(vehicle, "RearGlass",
            new Vector3(0, 1.52f,
                pickup ? 0.43f : van ? 1.85f : 1.44f),
            new Vector3(width * 0.68f, 0.59f, 0.07f), GlassMat);
        foreach (var side in new[] { -1, 1 })
        {
            for (var axle = -1; axle <= 1; axle += 2)
                Cylinder(vehicle, "RubberWheel",
                    new Vector3(side * (width * 0.49f),
                        0.42f, axle * (length * 0.33f)),
                    0.45f, 0.28f, TireMat, new Vector3(0,0,90));
            Visual(vehicle, "SideWindow",
                new Vector3(side * (width * .43f), 1.52f,
                    pickup ? -0.79f : -0.07f),
                new Vector3(0.06f, 0.47f, pickup ? 1.65f : 2.16f),
                GlassMat);
            Visual(vehicle, "FrontHeadlight",
                new Vector3(side * (width * .34f), .79f,
                    -length * .5f - .035f),
                new Vector3(.33f, .18f, .06f),
                Paint(new Color(.89f,.85f,.72f), .23f,
                    false, false, true));
            Visual(vehicle, "RedTailLamp",
                new Vector3(side * (width * .34f), .80f,
                    length * .5f + .035f),
                new Vector3(.31f, .17f, .055f),
                Paint(new Color(.59f,.09f,.08f), .30f,
                    false, false, true));
        }
        Visual(vehicle, "FrontGrille", new Vector3(0, .67f,
            -length * .5f - .035f),
            new Vector3(width * .44f,.27f,.09f),
            Paint(new Color(.09f,.10f,.11f), .69f, true));
        Visual(vehicle, "FrontBumper", new Vector3(0,.45f,
            -length * .5f - .11f),
            new Vector3(width * .97f,.16f,.20f), RailMat);
        Visual(vehicle, "RearBumper", new Vector3(0,.45f,
            length * .5f + .11f),
            new Vector3(width * .97f,.15f,.20f), RailMat);

        if (pickup)
            Visual(vehicle, "PickupBed",
                new Vector3(0, 1.0f, 1.55f),
                new Vector3(width * .95f, .21f, 1.75f), carPaint);
        if (truck)
        {
            Visual(vehicle, "CargoCanopy",
                new Vector3(0, 2.28f, 1.43f),
                new Vector3(width * .96f, 1.45f, 3.2f),
                Paint(new Color(.17f,.22f,.18f), .96f));
            Visual(vehicle, "TruckStep", new Vector3(0, .45f,-1.8f),
                new Vector3(2.55f,.12f,.45f), RailMat);
        }
    }

    private static void BuildSemiTruck(Node3D host, Vector3 place, float yaw)
    {
        var semi = new Node3D
        {
            Name = "StrandedSemiTruck",
            Position = place,
            RotationDegrees = new Vector3(0, yaw, 0)
        };
        host.AddChild(semi);
        Visual(semi, "WhiteCab", new Vector3(0, 1.8f,-2.2f),
            new Vector3(3.0f,3.1f,4.1f),
            Paint(new Color(.49f,.51f,.49f),.62f,true));
        Visual(semi, "Trailer", new Vector3(0,2.05f,5.2f),
            new Vector3(3.2f,3.9f,10.9f),
            Paint(new Color(.31f,.33f,.34f),.81f));
        Visual(semi, "TrailerUnderbody", new Vector3(0,.65f,5f),
            new Vector3(2.4f,.65f,9.5f), RailMat);
        foreach (var z in new[] {-3.1f,-1.0f,7.0f,9.2f})
            foreach (var side in new[] {-1,1})
                Cylinder(semi, "TruckTire",
                    new Vector3(side * 1.55f,.56f,z),.53f,.30f,TireMat,
                    new Vector3(0,0,90));
        // Its own collision remains beyond the playable boundary to avoid
        // invalidating zombie spawn and navigation zones.
    }

    private static void BuildScreeningCheckpoint(Node3D map)
    {
        var checkpoint = new Node3D { Name = "MedicalScreeningCheckpoint" };
        map.AddChild(checkpoint);

        // Two parked armored vehicles anchor the characteristic silhouette.
        BuildHumvee(checkpoint, -9f, -39f);
        BuildHumvee(checkpoint, 9f, -39f);

        // Concrete screening dividers; all passages have a navigable gap.
        foreach (var x in new[] { -18f, -11f, 11f, 18f })
            Solid(checkpoint, "PrecastBarrier",
                new Vector3(x, 1.05f, -27f),
                new Vector3(4.2f, 2.1f, 0.83f), Concrete);
        foreach (var x in new[] { -19f, -13f, 13f, 19f })
            Solid(checkpoint, "ScreeningDivider",
                new Vector3(x, .95f, 37f),
                new Vector3(3.8f,1.9f,.8f), Concrete);

        // The two approach ramps are traversable rather than painted shapes.
        Ramp(checkpoint, "WestApproachRamp",
            new Vector3(-17.7f,.28f,31.4f),
            new Vector3(3.2f,.55f,5.5f), -7f);
        Ramp(checkpoint, "EastApproachRamp",
            new Vector3(17.7f,.28f,31.4f),
            new Vector3(3.2f,.55f,5.5f), -7f);

        SandbagPosition(checkpoint, "WestSandbags", -16f,-25f,7);
        SandbagPosition(checkpoint, "EastSandbags", 16f,-25f,7);
        SandbagPosition(checkpoint, "CenterSandbags",0f,-25f,5);
        SandbagPosition(checkpoint, "SouthernSandbags",-10f,34f,5);
        SandbagPosition(checkpoint, "SouthernSandbags",10f,34f,5);

        Fence(checkpoint, -22f, -10f, -16.0f);
        Fence(checkpoint, 11f, 22f, -16.0f);
        Fence(checkpoint, -22f, -13f, 28.0f);
        Fence(checkpoint, 13f, 22f, 28.0f);
        StopSign(checkpoint, new Vector3(-19.5f,0,-18.8f), 0f);
        StopSign(checkpoint, new Vector3(19.5f,0,-18.8f), 0f);
        StopSign(checkpoint, new Vector3(-19.0f,0,37.8f), 180f);
        StopSign(checkpoint, new Vector3(19.0f,0,37.8f), 180f);

        for (var i = 0; i < 7; i++)
        {
            Cone(checkpoint, new Vector3(-19f + i * 1.0f, 0,-22f), 0f);
            Cone(checkpoint, new Vector3(19f - i * 1.0f, 0,-22f), 0f);
        }
        for (var i = 0; i < 6; i++)
        {
            Cone(checkpoint, new Vector3(-19f + i * 0.85f, 0, 30f), 0f);
            Cone(checkpoint, new Vector3(19f - i * 0.85f, 0, 30f), 0f);
        }

        BuildMedicalTent(checkpoint,-18f,-6f);
        BuildMedicalTent(checkpoint,18f,-6f);
        for (var i = 0; i < 6; i++)
            PalletAndCrate(checkpoint,
                new Vector3(i%2==0 ? -19.1f : 19.1f,0,
                    -14f + i*4.5f),i);
        BuildHelicopter(checkpoint,new Vector3(13.7f,0,53f));
        BuildPatrolSUV(checkpoint,-12f,48f);
    }

    private static void BuildHumvee(Node3D parent, float x, float z)
    {
        var car = new Node3D
        {
            Name = "AbandonedMilitaryHumvee",
            Position = new Vector3(x,0,z)
        };
        parent.AddChild(car);
        var green = Paint(new Color(.20f,.27f,.20f), .85f, true);
        var dark = Paint(new Color(.09f,.11f,.11f),.75f);
        var collision = new StaticBody3D { Name = "HumveeCollider" };
        collision.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(3.3f, 2.35f, 5.3f) },
            Position = new Vector3(0,1.18f,0)
        });
        car.AddChild(collision);
        Visual(car,"HumveeArmoredBody",new Vector3(0,1.0f,0),
            new Vector3(3.0f,1.3f,5.1f),green);
        Visual(car,"HumveeCab",new Vector3(0,2.04f,-.15f),
            new Vector3(2.8f,1.25f,2.9f),green);
        Visual(car,"ArmoredGlass",new Vector3(0,2.10f,-1.64f),
            new Vector3(2.20f,.71f,.08f),GlassMat);
        Visual(car,"FrontBullbar",new Vector3(0,.78f,-2.73f),
            new Vector3(2.9f,.21f,.25f),RailMat);
        Visual(car,"Grille",new Vector3(0,1.07f,-2.58f),
            new Vector3(1.3f,.55f,.085f),dark);
        Visual(car,"RoofHatch",new Vector3(0,2.79f,0),
            new Vector3(1.10f,.19f,1.0f),dark);
        foreach(var side in new[]{-1,1})
        {
            foreach(var axle in new[]{-1,1})
                Cylinder(car,"HumveeTire",
                    new Vector3(side*1.55f,.56f,axle*1.65f),
                    .55f,.36f,TireMat,new Vector3(0,0,90));
            Visual(car,"HumveeHeadlight",
                new Vector3(side*1.12f,1.04f,-2.60f),
                new Vector3(.43f,.24f,.06f),
                Paint(new Color(.98f,.95f,.81f),.18f,false,false,true));
            car.AddChild(new OmniLight3D
            {
                Name = "HumveeHeadlamp",
                Position = new Vector3(side * 1.10f,1.08f,-2.74f),
                LightColor = new Color(.93f,.91f,.78f),
                LightEnergy = .65f,
                OmniRange = 10f,
                ShadowEnabled = false
            });
        }
    }

    private static void Ramp(Node3D root,string name,
        Vector3 location, Vector3 dimensions,float angle)
    {
        var ramp = new StaticBody3D
        {
            Name = name,
            Position = location,
            RotationDegrees = new Vector3(angle,0,0)
        };
        ramp.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = dimensions }
        });
        ramp.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = dimensions },
            MaterialOverride = CementMat
        });
        root.AddChild(ramp);
    }

    private static void SandbagPosition(
        Node3D host,string name,float centerX,float centerZ,int bagsAcross)
    {
        var pile = new Node3D
        {
            Name = name,
            Position = new Vector3(centerX,0,centerZ)
        };
        host.AddChild(pile);
        var collision = new StaticBody3D { Name = "SandbagCollider" };
        collision.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D
            {
                Size = new Vector3(bagsAcross * .65f, 1.0f, 1.35f)
            },
            Position = new Vector3(0,.49f,0)
        });
        pile.AddChild(collision);
        for(var level=0;level<3;level++)
        {
            var count=bagsAcross-(level%2);
            for(var b=0;b<count;b++)
            {
                var bag=new MeshInstance3D
                {
                    Name="Sandbag",
                    Mesh=new SphereMesh { Radius=.5f, Height=1f },
                    Position=new Vector3(
                        (b-(count-1)*.5f)*.66f, .22f+level*.29f,
                        ((b+level)%2==0 ? -.23f : .23f)),
                    Scale=new Vector3(.77f,.34f,.57f),
                    MaterialOverride=SandMat
                };
                pile.AddChild(bag);
            }
        }
    }

    private static void Fence(Node3D root,float x0,float x1,float z)
    {
        var fence = new Node3D
        {
            Name = "CheckpointFence",
            Position = new Vector3((x0+x1)*.5f,0,z)
        };
        root.AddChild(fence);
        var width = x1-x0;
        var posts = (int)Math.Ceiling(width/1.5f);
        for(var i=0;i<=posts;i++)
            Cylinder(fence,"ChainlinkPost",
                new Vector3(-width*.5f+i*width/posts,1.5f,0),
                .038f,3f,RailMat);
        for(var row=0;row<11;row++)
            Visual(fence,"ChainlinkCrosswire",
                new Vector3(0, .2f+row*.25f, 0),
                new Vector3(width,.022f,.020f),RailMat);
        Visual(fence,"TopFenceRail",new Vector3(0,2.97f,0),
            new Vector3(width,.065f,.065f),RailMat);
        // Keep colliders thin to prevent invisible obstruction of the
        // doorway gap between the separate fence runs.
        Solid(fence,"FenceCollision",new Vector3(0,1.5f,0),
            new Vector3(width,3f,.09f),
            new Color(.36f,.39f,.38f),false);
    }

    private static void StopSign(Node3D root,Vector3 position,float yaw)
    {
        var sign = new Node3D
        {
            Name="STOP",
            Position=position,
            RotationDegrees=new Vector3(0,yaw,0)
        };
        root.AddChild(sign);
        Cylinder(sign,"Signpost",new Vector3(0,1.35f,0),
            .045f,2.7f,RailMat);
        Cylinder(sign,"StopOctagon",new Vector3(0,2.53f,-.03f),
            .37f,.075f,
            Paint(new Color(.67f,.12f,.11f),.65f,false),
            new Vector3(90,0,0),8);
        sign.AddChild(new Label3D
        {
            Name="StopText",
            Text="STOP",
            Position=new Vector3(0,2.39f,-.14f),
            PixelSize=.0025f,
            FontSize=52,
            Billboard=BaseMaterial3D.BillboardModeEnum.Enabled
        });
    }

    private static void Cone(Node3D root,Vector3 place,float unused)
    {
        var cone=new Node3D
        {
            Name="OrangeTrafficCone",
            Position=place
        };
        root.AddChild(cone);
        var orange=Paint(new Color(.90f,.34f,.07f),.85f);
        Visual(cone,"Base",new Vector3(0,.045f,0),
            new Vector3(.46f,.09f,.46f),
            Paint(new Color(.11f,.12f,.12f),.92f));
        cone.AddChild(new MeshInstance3D
        {
            Name="SafetyCone",
            Position=new Vector3(0,.41f,0),
            Mesh=new CylinderMesh
            {
                TopRadius=.047f, BottomRadius=.22f, Height=.73f
            },
            MaterialOverride=orange
        });
        Cylinder(cone,"ReflectiveStripe",
            new Vector3(0,.36f,0),.15f,.072f,
            WhiteMat);
    }

    private static void BuildMedicalTent(Node3D root,float x,float z)
    {
        var tent=new Node3D
        {
            Name="ScreeningCanvasShelter",
            Position=new Vector3(x,0,z)
        };
        root.AddChild(tent);
        foreach(var side in new[]{-1,1})
            for(var sideZ=-1;sideZ<=1;sideZ+=2)
                Cylinder(tent,"TentPole",
                    new Vector3(side*2.6f,1.6f,sideZ*3.2f),
                    .06f,3.2f,RailMat);
        Visual(tent,"CanvasTop",
            new Vector3(0,3.18f,0),
            new Vector3(6.0f,.25f,7.3f),
            Paint(new Color(.47f,.48f,.44f),.96f));
        Visual(tent,"MedicalTable",
            new Vector3(0,1.05f,0),
            new Vector3(2.1f,.16f,1.15f),CementMat);
        Visual(tent,"AidCrate",
            new Vector3(-1.9f,.45f,-1.7f),
            new Vector3(.9f,.9f,.9f),
            Paint(new Color(.40f,.35f,.24f),.9f));
    }

    private static void PalletAndCrate(Node3D root,Vector3 pos,int index)
    {
        var storage=new Node3D
        {
            Name="PalletWithSupplies",
            Position=pos,
            RotationDegrees = new Vector3(0,index%3*10f,0)
        };
        root.AddChild(storage);
        var wood=Paint(new Color(.38f,.27f,.19f),.92f);
        for(var plank=0;plank<4;plank++)
            Visual(storage,"WoodenPallet",
                new Vector3(-.55f+plank*.36f,.10f,0),
                new Vector3(.25f,.11f,1.4f),wood);
        Solid(storage,"SupplyCrate",new Vector3(0,.69f,0),
            new Vector3(1.15f,1.04f,1.0f),
            new Color(.39f,.32f,.23f));
        Visual(storage,"CrateMetalBand",
            new Vector3(0,1.12f,0),
            new Vector3(1.2f,.06f,1.05f),RailMat);
    }

    private static void BuildPatrolSUV(Node3D root,float x,float z)
    {
        BuildVehicle(root,"AlythAbandonedPatrolSUV",x,z,5f,
            "van",new Color(.10f,.12f,.16f));
    }

    private static void BuildHelicopter(Node3D root,Vector3 place)
    {
        var heli=new Node3D
        {
            Name="AbandonedMedicalHelicopter",
            Position=place,
            RotationDegrees=new Vector3(0,-21f,0)
        };
        root.AddChild(heli);
        var olive=Paint(new Color(.21f,.31f,.26f),.68f,true);
        var dark=Paint(new Color(.08f,.13f,.15f),.38f,true);
        var body=new StaticBody3D { Name="HelicopterCollider" };
        body.AddChild(new CollisionShape3D
        {
            Shape=new BoxShape3D { Size=new Vector3(3.1f,2.6f,5.1f) },
            Position=new Vector3(0,1.45f,0)
        });
        heli.AddChild(body);
        var fuselage=new MeshInstance3D
        {
            Name="HelicopterFuselage",
            Position=new Vector3(0,1.50f,0),
            Scale=new Vector3(3.0f,1.9f,5.0f),
            Mesh=new SphereMesh {Radius=.5f,Height=1f},
            MaterialOverride=olive
        };
        heli.AddChild(fuselage);
        Visual(heli,"Windscreen",new Vector3(0,1.7f,-2.0f),
            new Vector3(1.7f,.8f,.2f),dark);
        Visual(heli,"TailBoom",new Vector3(0,1.7f,3.6f),
            new Vector3(.4f,.35f,5.2f),olive);
        Visual(heli,"VerticalTail",new Vector3(0,2.32f,5.85f),
            new Vector3(.18f,1.4f,.6f),olive);
        Visual(heli,"MainRotor",new Vector3(0,3.04f,0),
            new Vector3(11f,.065f,.20f),
            Paint(new Color(.14f,.15f,.15f),.73f));
        Visual(heli,"RotorCross",new Vector3(0,3.07f,0),
            new Vector3(.22f,.05f,9.7f),
            Paint(new Color(.14f,.15f,.15f),.73f));
        for(var side=-1;side<=1;side+=2)
        {
            Cylinder(heli,"LandingSkid",
                new Vector3(side*1.16f,.50f,0),
                .085f,4.3f,RailMat,new Vector3(90,0,0));
            Visual(heli,"SkidStrut",
                new Vector3(side*.95f,.74f,-.85f),
                new Vector3(.09f,.80f,.10f),RailMat);
        }
    }

    private static void BuildPeripheralDetail(Node3D map)
    {
        var details=new Node3D { Name="ScatteredRoadDebris" };
        map.AddChild(details);
        var metal=Paint(new Color(.34f,.36f,.37f),.72f,true);
        var trash=Paint(new Color(.16f,.16f,.15f),.94f);
        for(var i=0;i<38;i++)
        {
            var x = -20f + (i*37%41);
            var z = -59f + (i*29%119);
            if(Mathf.Abs(x) < 6f)continue;
            Visual(details,"ScrapPaper",
                new Vector3(x,.083f,z),
                new Vector3(.37f,.007f,.26f),i%3==0?WhiteMat:trash);
            if(i%6==0)
                Cylinder(details,"SpentFuelCan",
                    new Vector3(x,.22f,z+1.4f),.18f,.35f,metal);
        }

        // Grounded overhead hazard / checkpoint signage.
        Visual(details,"CheckpointBannerFrame",
            new Vector3(0,7.25f,-31f),
            new Vector3(15f,.15f,.15f),RailMat);
        foreach(var x in new[]{-7.6f,7.6f})
            Cylinder(details,"BannerSupport",
                new Vector3(x,3.7f,-31f),.12f,7.4f,RailMat);
        Visual(details,"ScreeningNotice",
            new Vector3(0,6.30f,-31f),
            new Vector3(12.7f,1.1f,.09f),
            Paint(new Color(.17f,.22f,.22f),.88f));
        details.AddChild(new Label3D
        {
            Name="CheckpointNotice",
            Text="MEDICAL SCREENING  -  STOP",
            Position=new Vector3(0,6.12f,-31.10f),
            FontSize=49,
            PixelSize=.004f,
            Billboard=BaseMaterial3D.BillboardModeEnum.Enabled
        });
    }

    private static void Solid(
        Node3D root,string name,Vector3 position,Vector3 size,
        Color color,bool visible=true)
    {
        var body=new StaticBody3D {Name=name,Position=position};
        body.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D {Size=size}
        });
        if(visible)
            body.AddChild(new MeshInstance3D
            {
                Mesh=new BoxMesh {Size=size},
                MaterialOverride=Paint(color,.89f)
            });
        root.AddChild(body);
    }

    private static void Visual(
        Node3D root,string name,Vector3 position,Vector3 size,
        StandardMaterial3D material)
    {
        root.AddChild(new MeshInstance3D
        {
            Name=name,
            Position=position,
            Mesh=new BoxMesh {Size=size},
            MaterialOverride=material
        });
    }

    private static void Cylinder(
        Node3D root,string name,Vector3 position,float radius,float height,
        StandardMaterial3D material,Vector3? rotation=null,int sides=16)
    {
        root.AddChild(new MeshInstance3D
        {
            Name=name,
            Position=position,
            RotationDegrees=rotation ?? Vector3.Zero,
            Mesh=new CylinderMesh
            {
                TopRadius=radius,
                BottomRadius=radius,
                Height=height,
                RadialSegments=sides
            },
            MaterialOverride=material
        });
    }

    private static StandardMaterial3D Paint(
        Color color,float roughness,bool metal=false,bool transparent=false,
        bool emissive=false)
        => new()
        {
            AlbedoColor=color,
            Roughness=roughness,
            Metallic=metal ? .64f : 0f,
            Transparency=transparent || color.A < .995f
                ? BaseMaterial3D.TransparencyEnum.Alpha
                : BaseMaterial3D.TransparencyEnum.Disabled,
            EmissionEnabled=emissive,
            Emission=emissive ? color : Colors.Black
        };
}
