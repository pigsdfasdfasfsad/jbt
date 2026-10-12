using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass76 exported Windows integration. Ranch and Manor are actual GameplayRoot
/// scenes and use real domain, physics, inventory, wave actors and controllers.
/// Raised floor and blockers are synthetic fixtures; original retail meshes,
/// stairs, supply probabilities and full playable waves remain unverified.
/// </summary>
public partial class Pass76ObjectiveFortificationAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;
    public const string SuccessMarker =
        "TWR_SMOKE_PASS76_OBJECTIVE_FORTIFICATION_OK";
    private const float FloorY = 54f;
    private int _validated;

    public override async void _Ready()
    {
        try
        {
            if (Runtime.Session is null ||
                Runtime.Match?.Phase != MatchPhase.Lobby)
                throw new InvalidOperationException(
                    "Pass76 must use isolated new offline match profile");
            await ValidateRanchInteractionAndClap();
            await ValidateManorTurretAndSupply();
            if (_validated != 2)
                throw new InvalidOperationException(
                    "Pass76 missing one or more real gameplay map tests");
            GD.Print(SuccessMarker +
                " maps=Ranch,Manor" +
                " one_E_press_one_pickup=true" +
                " pickup_world_wall_occlusion=true" +
                " load_item_world_wall_occlusion=true" +
                " objective_pickup_and_deposit=true" +
                " hammer_invalid_ground_preserves_inventory=true" +
                " hammer_valid_ground_consumes_once=true" +
                " clap_world_occlusion=true" +
                " clap_roblox_stud_radius=true" +
                " turret_layer2_damage_and_rewards=true" +
                " turret_dismount_on_death=true" +
                " pallet_raised_floor_contact_once=true" +
                " original_map_interiors_unverified=true");
            GetTree().Quit(0);
        }
        catch (Exception e)
        {
            GD.PushError("TWR_SMOKE_PASS76_OBJECTIVE_FORTIFICATION_FAILED " +
                "maps=" + _validated + " " + e.GetType().Name +
                " " + e.Message + "\n" + e.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task Frames(int count)
    {
        for (var n = 0; n < count; n++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private static void KeyInput(Key key, bool pressed)
    {
        Input.ParseInputEvent(new InputEventKey
        {
            Keycode = key,
            PhysicalKeycode = key,
            Pressed = pressed
        });
    }

    private static void MouseInput(bool pressed)
    {
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = pressed,
            Position = Vector2.Zero
        });
    }

    private async Task PressE()
    {
        KeyInput(Key.E, true);
        await Frames(3);
        KeyInput(Key.E, false);
        await Frames(3);
    }

    private async Task<(GameplayRoot Game, FirstPersonPlayer Player,
        InfectedAgent Enemy)> Begin(string map)
    {
        if (Runtime.Match?.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass76 previous map not cleanly returned to lobby");
        Runtime.StartMap(map);
        var game = new GameplayRoot
        {
            Name = "Pass76" + map,
            MapName = map,
            Runtime = Runtime
        };
        AddChild(game);
        var player = game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if (player is null || player.CaptureCamera is null ||
            game.GetNodeOrNull<GameplayHud>("HUD") is null ||
            Runtime.Player?.IsAlive != true)
            throw new InvalidOperationException(
                "Pass76 missing production gameplay HUD or player on " + map);

        var floor = new StaticBody3D
        {
            Name = "Pass76SyntheticArenaFloor",
            Position = new Vector3(0, FloorY, 0),
            CollisionLayer = 1,
            CollisionMask = 0
        };
        floor.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D
            {
                Size = new Vector3(45f, .70f, 45f)
            }
        });
        game.AddChild(floor);
        player.GlobalPosition = new Vector3(0, FloorY + .35f, 0);
        await Frames(40);
        if (!player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass76 physical player failed to settle on solid floor");

        game._Process(RegularWaveRules.CountdownSeconds + .05);
        game._Process(.05);
        var enemy = game.GetChildren().OfType<InfectedAgent>()
            .FirstOrDefault(GodotObject.IsInstanceValid);
        if (enemy is null || enemy.Target != player ||
            game.WaveStage != "WAVE")
            throw new InvalidOperationException(
                "Pass76 real first wave did not spawn physics infected");
        game.SetProcess(false);
        enemy.InfectedType = "Civilian";
        enemy.MoveSpeed = 0;
        enemy.Damage = 0;
        enemy.GlobalPosition = new Vector3(0, FloorY + 1.15f, -13f);
        await Frames(4);
        return (game, player, enemy);
    }

    private static StaticBody3D Wall(GameplayRoot game, string name,
        Vector3 location, Vector3 size)
    {
        var wall = new StaticBody3D
        {
            Name = name,
            Position = location,
            CollisionLayer = 1,
            CollisionMask = 0
        };
        wall.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = size }
        });
        game.AddChild(wall);
        return wall;
    }

    private static PickupActor Pickup(GameplayRoot game,
        FirstPersonPlayer player, LocalSessionNode session,
        string kind, Vector3 position)
    {
        var actor = new PickupActor
        {
            Name = "Pass76" + kind.Replace(" ", ""),
            PickupType = kind,
            Player = player,
            Runtime = session,
            Position = position
        };
        game.AddChild(actor);
        return actor;
    }

    private async Task End(GameplayRoot game)
    {
        Runtime.FailMatch("Pass76 isolated native interaction acceptance");
        if (Runtime.Match?.Phase != MatchPhase.Results ||
            Runtime.Match.SaveCommitted != true)
            throw new InvalidOperationException(
                "Pass76 results did not commit via authoritative domain");
        game.QueueFree();
        await Frames(3);
        Runtime.ReturnToLobby();
        if (Runtime.Match?.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass76 failed to restore isolated lobby");
        _validated++;
    }

    private async Task ValidateRanchInteractionAndClap()
    {
        var (game, player, enemy) = await Begin("Ranch");
        var initialFrag = Runtime.Player!.Inventory.GetValueOrDefault("Frag");
        var initialMolotov = Runtime.Player.Inventory.GetValueOrDefault("Molotov");
        var initialJack = Runtime.Player.Inventory.GetValueOrDefault("Jack");

        var near = Pickup(game, player, Runtime, "Frag",
            player.GlobalPosition + new Vector3(0, 0, -.72f));
        var second = Pickup(game, player, Runtime, "Molotov",
            player.GlobalPosition + new Vector3(0, 0, -1.05f));
        var behind = Pickup(game, player, Runtime, "Jack",
            player.GlobalPosition + new Vector3(0, 0, -1.95f));
        var pickupWall = Wall(game, "Pass76PickupSolidWall",
            new Vector3(0, FloorY + 1.42f, -1.48f),
            new Vector3(5, 2.6f, .28f));
        await Frames(2);

        await PressE();
        if (Runtime.Player.Inventory.GetValueOrDefault("Frag") != initialFrag + 1 ||
            Runtime.Player.Inventory.GetValueOrDefault("Molotov") != initialMolotov ||
            Runtime.Player.Inventory.GetValueOrDefault("Jack") != initialJack ||
            !near.IsQueuedForDeletion())
            throw new InvalidOperationException(
                "Pass76 single E press collected multiple overlapping pickups");
        await PressE();
        if (Runtime.Player.Inventory.GetValueOrDefault("Molotov") !=
                initialMolotov + 1 ||
            Runtime.Player.Inventory.GetValueOrDefault("Jack") != initialJack ||
            !second.IsQueuedForDeletion())
            throw new InvalidOperationException(
                "Pass76 next E press did not select second nearest pickup");
        await PressE();
        if (Runtime.Player.Inventory.GetValueOrDefault("Jack") != initialJack ||
            behind.IsQueuedForDeletion())
            throw new InvalidOperationException(
                "Pass76 E key looted Jack through solid wall");
        pickupWall.QueueFree();
        await Frames(3);
        await PressE();
        if (Runtime.Player.Inventory.GetValueOrDefault("Jack") != initialJack + 1)
            throw new InvalidOperationException(
                "Pass76 visible Jack was not collected after wall removed");

        // Live load-objective interact: reject an item across a solid
        // wall, then carry and deposit through its original E-key loop.
        var load = new ObjectiveRuntime
        {
            Name = "Pass76LiveLoadObjective",
            ObjectiveId = "Ranch:Pass76:Load",
            Family = "Load",
            Player = player,
            Runtime = Runtime,
            Position = new Vector3(0, FloorY + .35f, -5f)
        };
        var objectiveStatus = "";
        load.StatusChanged = value => objectiveStatus = value;
        game.AddChild(load);
        player.GlobalPosition = new Vector3(7.50f, FloorY + .35f, -5f);
        var itemWall = Wall(game, "Pass76LoadItemWall",
            new Vector3(6.65f, FloorY + 1.40f, -5f),
            new Vector3(.32f, 2.65f, 4.0f));
        await Frames(3);
        await PressE();
        if (objectiveStatus.Contains("CARRYING ITEM", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Pass76 Load objective took materials through a solid wall");

        itemWall.QueueFree();
        await Frames(3);
        await PressE();
        if (!objectiveStatus.Contains("CARRYING ITEM", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Pass76 clear-path Load item was not carried on E");
        player.GlobalPosition = load.GlobalPosition +
            new Vector3(0f, 0f, .8f);
        await Frames(2);
        await PressE();
        if (!objectiveStatus.Contains("1/10 INSERTED", StringComparison.Ordinal) ||
            Runtime.Match!.CompletedObjectives.Contains(load.ObjectiveId))
            throw new InvalidOperationException(
                "Pass76 Load deposit did not increment objective correctly");
        load.QueueFree();

        // Building through obstacles or into unsupported floor should never
        // spend the authoritative fortification inventory.
        player.GlobalPosition = new Vector3(0, FloorY + .35f, 0);
        await Frames(4);
        if (!Runtime.GrantItem("Clap Bomb", 1, 1))
            throw new InvalidOperationException(
                "Pass76 isolated inventory could not grant Clap Bomb");
        var count = Runtime.Player.Inventory.GetValueOrDefault("Clap Bomb");
        var controller = game.GetNode<FortificationController>("Fortifications");
        var placementWall = Wall(game, "Pass76InvalidHammerWall",
            new Vector3(0, FloorY + 1.2f, -3f),
            new Vector3(2.5f, 1.7f, .8f));
        controller._UnhandledInput(new InputEventKey
        {
            Keycode = Key.F, Pressed = true
        });
        if (!controller.HammerMode76)
            throw new InvalidOperationException(
                "Pass76 F key did not select real hammer mode");

        MouseInput(true);
        controller._Process(.6);
        MouseInput(false);
        if (Runtime.Player.Inventory.GetValueOrDefault("Clap Bomb") != count ||
            controller.DeployedCount76 != 0)
            throw new InvalidOperationException(
                "Pass76 hammer consumed an item despite invalid wall placement");
        placementWall.QueueFree();
        await Frames(3);
        MouseInput(true);
        controller._Process(.6);
        MouseInput(false);
        await Frames(2);
        if (Runtime.Player.Inventory.GetValueOrDefault("Clap Bomb") != count - 1 ||
            controller.DeployedCount76 != 1)
            throw new InvalidOperationException(
                "Pass76 grounded hammer did not deploy one actual Clap Bomb");

        controller._UnhandledInput(new InputEventKey
        {
            Keycode = Key.F, Pressed = true
        });
        var bomb = controller.GetChildren().OfType<FortificationActor>()
            .FirstOrDefault(x => GodotObject.IsInstanceValid(x) &&
                x.Definition.Name == "Clap Bomb");
        if (bomb is null)
            throw new InvalidOperationException(
                "Pass76 hammer produced no production Clap Bomb actor");

        var distantTanker = new DamageObjectiveTarget
        {
            Name = "Pass76OutOfStudRadiusTanker",
            Position = bomb.GlobalPosition + new Vector3(9f, 0, 0)
        };
        game.AddChild(distantTanker);
        var startingTankerHp = distantTanker.Health;
        var startingEnemyHp = enemy.Health;
        var credits = Runtime.Player.Credits;
        var clapWall = Wall(game, "Pass76ClapWorldWall",
            bomb.GlobalPosition + new Vector3(0, 1.05f, -.90f),
            new Vector3(4f, 2.1f, .32f));
        enemy.GlobalPosition = bomb.GlobalPosition +
            new Vector3(0, .80f, -1.70f);
        await Frames(6);
        if (Math.Abs(enemy.Health - startingEnemyHp) > .001f ||
            bomb.IsQueuedForDeletion())
            throw new InvalidOperationException(
                "Pass76 Clap Bomb triggered or damaged through solid wall");

        clapWall.QueueFree();
        await Frames(4);
        if (GodotObject.IsInstanceValid(enemy) && enemy.Health > 0 ||
            Runtime.Player.Credits <= credits ||
            Math.Abs(distantTanker.Health - startingTankerHp) > .001f)
            throw new InvalidOperationException(
                "Pass76 visible Clap failed or its 25-stud radius was treated as metres");

        GD.Print("TWR_PASS76_RANCH_INTERACTIONS_OK " +
            "one_press_one_pickup=true wall_blocked_pickup=true " +
            "load_wall_blocked=true load_deposited=1/10 " +
            "invalid_hammer_kept_inventory=true placed_once=true " +
            "clap_wall_blocked=true clap_kill_rewards=true " +
            "distant_tanker_out_of_stud_radius=true");
        await End(game);
    }

    private async Task ValidateManorTurretAndSupply()
    {
        var (game, player, enemy) = await Begin("Manor");
        enemy.GlobalPosition = new Vector3(0, FloorY + 1.15f, -6.0f);
        if (enemy.Health > 50f)
            enemy.ApplyDamage(enemy.Health - 50f, false, "Generic");
        await Frames(4);
        var turretDefinition = FortificationCatalogRuntime.Load()
            .First(x => x.Name == "50 Cal");
        var turret = new FortificationActor
        {
            Name = "Pass76NativeMounted50Cal",
            Definition = turretDefinition,
            Runtime = Runtime,
            Player = player,
            Position = new Vector3(0, FloorY + .20f, -1.1f)
        };
        game.AddChild(turret);
        await Frames(2);
        await PressE();
        if (!turret.Mounted76 || !player.MountedMode)
            throw new InvalidOperationException(
                "Pass76 native E press failed to mount turret");
        var credits = Runtime.Player!.Credits;
        MouseInput(true);
        turret._Process(.25);
        MouseInput(false);
        await Frames(3);
        if (turret.ShotsFired76 != 1 ||
            (GodotObject.IsInstanceValid(enemy) && enemy.Health > 0) ||
            Runtime.Player.Credits <= credits)
            throw new InvalidOperationException(
                "Pass76 turret layer-2 ray missed infected or failed domain reward " +
                "shots=" + turret.ShotsFired76 +
                " health=" + (GodotObject.IsInstanceValid(enemy) ? enemy.Health : -1));

        // Actual physically elevated ground contact, NOT the unreliable
        // configured 0.3 metre fallback for original source maps.
        var landCount = 0;
        var landing = Vector3.Zero;
        var supply = new SupplyDropRuntime
        {
            Name = "Pass76ElevatedSupplyPallet",
            SourcePlayer = player,
            Position = new Vector3(8f, FloorY + 10f, 4f),
            GroundY = .3f,
            Landed = position =>
            {
                landCount++;
                landing = position;
            }
        };
        game.AddChild(supply);
        supply._Process(4.0); // accelerate descent, not collider authority
        if (!supply.Landed76 || landCount != 1 ||
            Math.Abs(landing.Y - (FloorY + .58f)) > .12f)
            throw new InvalidOperationException(
                "Pass76 supply pallet ignored real elevated physics floor " +
                "landed=" + supply.Landed76 +
                " callbacks=" + landCount + " y=" + landing.Y);
        supply._Process(1.0);
        if (landCount != 1)
            throw new InvalidOperationException(
                "Pass76 supply landed callback fired more than once");

        Runtime.DamagePlayer(1000, "Pass76 isolated fatal safety test");
        turret._Process(.016);
        if (turret.Mounted76 || player.MountedMode)
            throw new InvalidOperationException(
                "Pass76 dead player remained mounted on functional turret");
        GD.Print("TWR_PASS76_MANOR_EQUIPMENT_OK " +
            "real_50cal_layer2_kill_and_credit=true " +
            "elevated_supply_contact=true callback_once=true " +
            "fatal_dismount=true");
        await End(game);
    }
}
