using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass103 native acceptance: entire REAL Wave1 Load objective on Ranch
/// and Manor, with physics walking from production spawn to every item and
/// back. No acceptance actor is teleported or granted inventory/rewards.
/// Headless isolated combat-free mission test, not human 15-wave survival.
/// </summary>
public partial class Pass103PhysicalLoadLifecycleAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;
    private int _completed;
    private int _deposits;
    private float _walkedMetres;
    // Pass104 extends the existing, fully verified real-input Wave1 Load
    // acceptance through an actual Wave1->Wave2 progression and a complete
    // physical Repair mission. The default Pass103 mode is unchanged.
    public bool ExtendToRepair104 { get; set; }
    // Pass108: isolated native RMB aiming/stow check during genuine carry.
    public bool VerifyCarryAim108 { get; set; }
    public bool VerifyReferenceVisuals109 { get; set; }
    public bool VerifyRepairVehicle110 { get; set; }
    private int _completedRepairs104;
    private int _repairDeposits104;
    private float _repairWalkedMetres104;
    private int _visibleCarryPickups106;
    private int _clearedCarryDeposits106;
    private int _verifiedAimStows108;
    private int _virtualFrustumCases108;
    private int _generatorWorldChecks109;
    private int _generatorHeldChecks109;
    private int _repairBadgeShown109;
    private int _repairBadgeCleared109;
    private int _repairVehiclesSeen110;
    private int _repairVisualInstallChecks110;
    private int _repairAssemblyCompletions110;
    private int _smallRepairVehicles110;
    private int _largeRepairVehicles110;
    private const int MaxWalkFrames=540;

    public override async void _Ready()
    {
        try
        {
            if(Runtime.Session is null ||
               Runtime.Match?.Phase!=MatchPhase.Lobby)
                throw new InvalidOperationException(
                    "Pass103 requires clean offline lobby");
            await VerifyMap("Ranch");
            await VerifyMap("Manor");
            if(_completed!=2 || _deposits<20 ||
               _walkedMetres<80f)
                throw new InvalidOperationException(
                    "Pass103 inadequate full physical mission coverage"+
                    " maps="+_completed+" deposits="+_deposits+
                    " metres="+_walkedMetres);
            GD.Print("TWR_SMOKE_PASS103_FULL_PHYSICAL_LOAD_OK"+
                " maps=Ranch,Manor completed_production_wave1_loads="+_completed+
                " material_pickup_deposits="+_deposits+
                " walked_metres="+_walkedMetres.ToString("F1")+
                " actual_W_E_keys=true player_teleports=0"+
                " objective_completion_receipts_once=true"+
                " accelerated_wave_start=true"+
                " isolated_enemy_combat=true"+
                " retail_art_fidelity_unverified=true");
            if(ExtendToRepair104)
            {
                if(_completedRepairs104!=2 ||
                   _repairDeposits104<20 ||
                   _repairWalkedMetres104<40f)
                    throw new InvalidOperationException(
                        "Pass104 incomplete real Wave2 Repair acceptance"+
                        " maps="+_completedRepairs104+
                        " deposits="+_repairDeposits104+
                        " metres="+_repairWalkedMetres104);
                GD.Print("TWR_SMOKE_PASS104_FULL_PHYSICAL_REPAIR_OK"+
                    " maps=Ranch,Manor completed_wave1_loads="+_completed+
                    " completed_wave2_repairs="+_completedRepairs104+
                    " wave2_material_deposits="+_repairDeposits104+
                    " wave2_walked_metres="+
                        _repairWalkedMetres104.ToString("F1")+
                    " total_material_deposits="+
                        (_deposits+_repairDeposits104)+
                    " actual_W_E_keys=true player_teleports=0"+
                    " wave1_to_wave2_runtime_progression=true"+
                    " exactly_once_rewards=true persisted_results=true"+
                    " enemy_spawning_frozen_during_missions=true"+
                    " full_realtime_survival_unverified=true");
            }
            var expectedVisuals=_deposits+
                (ExtendToRepair104 ? _repairDeposits104 : 0);
            if(_visibleCarryPickups106!=expectedVisuals ||
               _clearedCarryDeposits106!=expectedVisuals)
                throw new InvalidOperationException(
                    "Pass106 missing first-person carry models"+
                    " visual_pickups="+_visibleCarryPickups106+
                    " visual_deposits="+_clearedCarryDeposits106+
                    " expected="+expectedVisuals);
            GD.Print("TWR_SMOKE_PASS106_FULL_CARRY_VISUALS_OK"+
                " families="+(ExtendToRepair104?"Load,Repair":"Load")+
                " maps=Ranch,Manor"+
                " visual_pickups="+_visibleCarryPickups106+
                " cleared_deposits="+_clearedCarryDeposits106+
                " render_only=true camera_attached=true"+
                " physics_interactions_unchanged=true");
            if (VerifyCarryAim108)
            {
                if (!ExtendToRepair104 || _verifiedAimStows108 != 4 ||
                    _virtualFrustumCases108 < 160)
                    throw new InvalidOperationException(
                        "Pass108 native RMB carry coverage incomplete count="+
                        _verifiedAimStows108);
                GD.Print("TWR_SMOKE_PASS108_CARRY_ADS_FRUSTUM_OK"+
                    " maps=Ranch,Manor families=Load,Repair"+
                    " genuine_RMB_aim_stows="+_verifiedAimStows108+
                    " virtual_viewport_cases="+_virtualFrustumCases108+
                    " physically_collected_items="+_visibleCarryPickups106+
                    " physically_deposited_items="+_clearedCarryDeposits106+
                    " carried_material_retained_during_ADS=true"+
                    " full_primitive_silhouette_framed=true"+
                    " reticle_clear_during_hip_and_ADS=true"+
                    " source_pictures_not_pixel_compared=true");
            }
            if (VerifyReferenceVisuals109)
            {
                if (!ExtendToRepair104 ||
                    _generatorWorldChecks109!=2 ||
                    _generatorHeldChecks109!=2 ||
                    _repairBadgeShown109!=2 ||
                    _repairBadgeCleared109!=2 ||
                    _visibleCarryPickups106<40 ||
                    _visibleCarryPickups106!=_clearedCarryDeposits106)
                    throw new InvalidOperationException(
                        "Pass109 production model/HUD acceptance incomplete"+
                        " generators_world="+_generatorWorldChecks109+
                        " generators_held="+_generatorHeldChecks109+
                        " repair_shown="+_repairBadgeShown109+
                        " repair_cleared="+_repairBadgeCleared109);
                GD.Print("TWR_SMOKE_PASS109_REFERENCE_VISUALS_OK"+
                    " maps=Ranch,Manor"+
                    " generators_world="+_generatorWorldChecks109+
                    " generators_held="+_generatorHeldChecks109+
                    " repair_badges_shown="+_repairBadgeShown109+
                    " repair_badges_cleared="+_repairBadgeCleared109+
                    " full_material_pickups="+_visibleCarryPickups106+
                    " full_material_deposits="+_clearedCarryDeposits106+
                    " actual_W_E_input=true"+
                    " collider_free_visuals=true"+
                    " owner_image_files_not_bundled=true");
            }
            if(VerifyRepairVehicle110)
            {
                if(!ExtendToRepair104 || _repairVehiclesSeen110!=2 ||
                   _repairAssemblyCompletions110!=2 ||
                   _repairVisualInstallChecks110!=_repairDeposits104 ||
                   _repairVisualInstallChecks110<20 ||
                   _visibleCarryPickups106!=_clearedCarryDeposits106 ||
                   _smallRepairVehicles110+_largeRepairVehicles110!=2)
                    throw new InvalidOperationException(
                        "Pass110 actual Repair vehicle assembly incomplete"+
                        " vehicles="+_repairVehiclesSeen110+
                        " completed="+_repairAssemblyCompletions110+
                        " visual_installs="+_repairVisualInstallChecks110+
                        " actual_deposits="+_repairDeposits104);
                GD.Print("TWR_SMOKE_PASS110_REPAIR_VEHICLE_OK"+
                    " maps=Ranch,Manor"+
                    " vehicles="+_repairVehiclesSeen110+
                    " completed_assemblies="+_repairAssemblyCompletions110+
                    " visual_install_checks="+_repairVisualInstallChecks110+
                    " physical_repair_deposits="+_repairDeposits104+
                    " small_variant="+_smallRepairVehicles110+
                    " large_variant="+_largeRepairVehicles110+
                    " native_W_E_actions=true"+
                    " no_added_colliders=true"+
                    " exact_original_mesh_unverified=true");
            }
            GetTree().Quit(0);
        }
        catch(Exception e)
        {
            InputEvent(Key.W,false);
            InputEvent(Key.E,false);
            GD.PushError((ExtendToRepair104
                    ? "TWR_SMOKE_PASS104_FULL_PHYSICAL_REPAIR_FAILED "
                    : "TWR_SMOKE_PASS103_FULL_PHYSICAL_LOAD_FAILED ")+
                e.GetType().Name+" "+e.Message+"\n"+e.StackTrace);
            GetTree().Quit(1);
        }
    }

    // Every real E pickup must show the matching 3D prop on the
    // actual first-person camera, without adding physics colliders.
    private void CheckCarriedVisual106(
        ObjectiveRuntime objective,FirstPersonPlayer player,string context)
    {
        var held=player.CaptureCamera.GetNodeOrNull<Node3D>(
            "Pass106CarriedObjectiveMaterial");
        if(!objective.Pass106HeldMaterialVisible ||
           objective.Pass106HeldMaterialMeshParts<2 ||
           string.IsNullOrWhiteSpace(objective.Pass106HeldMaterialName) ||
           held is null || !held.Visible ||
           held.GetChildCount()!=objective.Pass106HeldMaterialMeshParts ||
           !held.HasMeta("Pass106VisualKind") ||
           !objective.Pass108HeldSilhouetteSafe ||
           !Pass108CarryFrustum.Measure(
               held,player.CaptureCamera.Fov,
               new Vector2I((int)GetViewport().GetVisibleRect().Size.X,
                   (int)GetViewport().GetVisibleRect().Size.Y)
               ).EntirePropSafe ||
           !Pass107CarryPresentation.HasSafeAnchor(
               held, player.CaptureCamera.Fov,
               new Vector2I((int)GetViewport().GetVisibleRect().Size.X,
                   (int)GetViewport().GetVisibleRect().Size.Y)) ||
           held.GetMeta("Pass106RecoveredItemName").AsString()!=
                objective.Pass106HeldMaterialName ||
           held.GetChildren().OfType<CollisionObject3D>().Any())
            throw new InvalidOperationException(
                "Pass106 no matching collision-free carried model "+context);
        CheckCommonViewports108(held,player,context);
        _visibleCarryPickups106++;
    }

    private void CheckGenerator109(Node3D? model,string context,bool held)
    {
        if(!VerifyReferenceVisuals109)
            return;
        if(model is null || !model.HasMeta("Pass106VisualKind") ||
           model.GetMeta("Pass106VisualKind").AsString()!="Generator" ||
           model.GetChildCount()<20 ||
           model.GetChildren().OfType<CollisionObject3D>().Any())
            throw new InvalidOperationException(
                "Pass109 generator was absent, incomplete or collidable "+
                context);
        foreach (var part in new[]
        {
            "Housing","SkidBase","TopAccessLid","ServiceDoor",
            "FrontControlConsole","InstrumentDisplay","StatusRed",
            "StatusGreen","IntakeVent0","IntakeVent4",
            "LeftExhaustPipe","RightExhaustPipe",
            "LeftSkidFoot","RightSkidFoot"
        })
            if(model.GetNodeOrNull<MeshInstance3D>(part) is not { Mesh: not null })
                throw new InvalidOperationException(
                    "Pass109 missing generator assembly "+part+" "+context);
        if(held)_generatorHeldChecks109++;
        else _generatorWorldChecks109++;
    }

    // Independently test the complete *same* carried mesh at 4:3, 16:9,
    // 21:9 and ultrawide ratios, without changing the real game viewport.
    // Real player position, item state and weapon FOV remain unmodified.
    private void CheckCommonViewports108(
        Node3D held,FirstPersonPlayer player,string context)
    {
        if (!VerifyCarryAim108)
            return;
        var original=held.Transform;
        var originallyVisible=held.Visible;
        try
        {
            foreach (var size in new[]
            {
                new Vector2I(1024,768),
                new Vector2I(1920,1080),
                new Vector2I(2560,1080),
                new Vector2I(3440,1440)
            })
            {
                Pass107CarryPresentation.Apply(
                    held,0f,player.CaptureCamera.Fov,
                    size,0f,0f);
                var bounds=Pass108CarryFrustum.Fit(
                    held,player.CaptureCamera.Fov,size);
                if (!bounds.EntirePropSafe || bounds.IntersectsCrosshair ||
                    bounds.Corners < 16)
                    throw new InvalidOperationException(
                        "Pass108 clipped full carried mesh "+
                        context+" viewport="+size+
                        " bounds="+bounds);
                _virtualFrustumCases108++;
            }
        }
        finally
        {
            held.Transform=original;
            held.Visible=originallyVisible;
        }
    }

    private void CheckClearedVisual106(
        ObjectiveRuntime objective,FirstPersonPlayer player,string context)
    {
        // Completing the final item can legitimately QueueFree the
        // objective before PressE's release frames have finished.
        if((GodotObject.IsInstanceValid(objective) &&
            objective.Pass106HeldMaterialVisible) ||
           player.CaptureCamera.GetNodeOrNull<Node3D>(
               "Pass106CarriedObjectiveMaterial") is not null)
            throw new InvalidOperationException(
                "Pass106 carried material survived a deposit "+context);
        _clearedCarryDeposits106++;
    }

    private static void PressRight108(bool pressed)
    {
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex=MouseButton.Right,
            Pressed=pressed
        });
    }

    private async Task CheckRealAimStow108(
        ObjectiveRuntime objective,FirstPersonPlayer player,string context)
    {
        if (!VerifyCarryAim108)
            return;
        var itemName=objective.Pass106HeldMaterialName;
        var itemRemaining=objective.Pass98RemainingMaterials;
        var alreadyDeposited=objective.Pass98DepositedMaterials;
        var held=player.CaptureCamera.GetNodeOrNull<Node3D>(
            "Pass106CarriedObjectiveMaterial");
        if (held is null || !held.Visible ||
            !objective.Pass108HeldSilhouetteSafe ||
            !objective.Pass98CarryingMaterial || player.Pass107Aiming ||
            player.CaptureCamera.Fov < 54f)
            throw new InvalidOperationException(
                "Pass108 invalid physical hip carry baseline "+context);

        try
        {
            PressRight108(true);
            await Frames(65);
            // The local input is a genuine RMB event: FirstPersonPlayer's
            // ordinary UpdateAim and the real weapon FOV path must execute.
            // Only the OS mouse-capture prerequisite is disabled for this
            // exported headless smoke mode, never for a normal game.
            if (!player.Pass107Aiming || held.Visible ||
                objective.Pass106HeldMaterialVisible ||
                !objective.Pass98CarryingMaterial ||
                objective.Pass106HeldMaterialName != itemName ||
                objective.Pass98RemainingMaterials != itemRemaining ||
                objective.Pass98DepositedMaterials != alreadyDeposited)
                throw new InvalidOperationException(
                    "Pass108 RHS ADS stow changed authoritative material "+
                    context);
        }
        finally
        {
            PressRight108(false);
        }

        await Frames(65);
        if (player.Pass107Aiming || !held.Visible ||
            !objective.Pass98CarryingMaterial ||
            objective.Pass106HeldMaterialName != itemName ||
            objective.Pass98RemainingMaterials != itemRemaining ||
            objective.Pass98DepositedMaterials != alreadyDeposited ||
            !objective.Pass108HeldSilhouetteSafe)
            throw new InvalidOperationException(
                "Pass108 real RMB release did not restore safe hip carry "+
                context);
        _verifiedAimStows108++;
    }

    private async Task Frames(int count)
    {
        for(var n=0;n<count;n++)
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }

    private static void InputEvent(Key key,bool pressed)
    {
        Input.ParseInputEvent(new InputEventKey
        {
            Keycode=key,
            PhysicalKeycode=key,
            Pressed=pressed
        });
    }

    private async Task PressE()
    {
        InputEvent(Key.E,true);
        await Frames(5);
        InputEvent(Key.E,false);
        await Frames(5);
    }

    private async Task<float> WalkTo(
        FirstPersonPlayer player,Vector3 feet,string context)
    {
        var distance=0f;
        var arrived=false;
        InputEvent(Key.W,true);
        try
        {
            for(var n=0;n<MaxWalkFrames;n++)
            {
                var horizontal=feet-player.GlobalPosition;
                horizontal.Y=0;
                if(horizontal.Length()<.70f)
                {
                    arrived=true;
                    break;
                }
                player.Rotation=new Vector3(
                    0,Mathf.Atan2(-horizontal.X,-horizontal.Z),0);
                var before=player.GlobalPosition;
                await Frames(1);
                var step=player.GlobalPosition.DistanceTo(before);
                if(step>.55f)
                    throw new InvalidOperationException(
                        "Pass103 movement discontinuity "+context+
                        " step="+step);
                distance+=step;
                if(Runtime.Player?.IsAlive!=true)
                    throw new InvalidOperationException(
                        "Pass103 player died while walking "+context);
            }
        }
        finally
        {
            InputEvent(Key.W,false);
        }
        await Frames(4);
        var delta=feet-player.GlobalPosition;
        delta.Y=0;
        if(!arrived || delta.Length()>1.05f ||
           !player.IsOnFloor() ||
           Math.Abs(player.GlobalPosition.Y-feet.Y)>.55f)
            throw new InvalidOperationException(
                "Pass103 real player W walk blocked "+context+
                " current="+player.GlobalPosition+" goal="+feet+
                " metres="+distance.ToString("F1"));
        return distance;
    }

    private async Task<float> WalkSafely(
        GameplayRoot game,FirstPersonPlayer player,
        Vector3 destination,string context)
    {
        var world=game.GetWorld3D();
        var exclude=new global::Godot.Collections.Array<Rid>
        {
            player.GetRid()
        };
        Vector3[] legs;
        var from=player.GlobalPosition;
        if(Pass89EscortSurfaceSafety.CanTravel(
                world,from,destination,exclude))
            legs=[destination];
        else
        {
            var nav=game.GetNodeOrNull<Pass73GroundNavigationRuntime>(
                "Pass73GroundNavigation");
            if(nav is null || !nav.IsGraphReady)
                throw new InvalidOperationException(
                    "Pass103 no physical ground graph "+context);
            legs=nav.GetVerifiedEscortRoute89(
                from,destination,player.GetRid());
            if(legs.Length==0 || legs.Length>160)
                throw new InvalidOperationException(
                    "Pass103 no body-safe route "+context+
                    " origin="+from+" target="+destination);
        }
        var travelled=0f;
        foreach(var leg in legs)
        {
            if(!Pass89EscortSurfaceSafety.CanTravel(
                    world,player.GlobalPosition,leg,exclude))
                throw new InvalidOperationException(
                    "Pass103 route changed under player "+context+
                    " current="+player.GlobalPosition+" leg="+leg);
            var d=leg-player.GlobalPosition;
            d.Y=0;
            if(d.Length()>.7f)
                travelled+=await WalkTo(player,leg,context+"/route");
        }
        if(new Vector2(player.GlobalPosition.X-destination.X,
                       player.GlobalPosition.Z-destination.Z).Length()>1.10f)
            throw new InvalidOperationException(
                "Pass103 route incomplete "+context);
        return travelled;
    }

    private static (int credits,int xp,int level) RewardAfterObjective(
        PlayerState player)
    {
        var credits=player.Credits+4000;
        var xp=player.Xp+3500;
        var level=player.Level;
        while(xp>=ProgressionRules.RequiredForNextLevel(level))
        {
            xp-=ProgressionRules.RequiredForNextLevel(level);
            level++;
            credits+=ProgressionRules.CreditRewardForLevel(level);
        }
        return (credits,xp,level);
    }

    private void ValidateReward(
        (int credits,int xp,int level) expected,string context)
    {
        if(Runtime.Player?.Credits!=expected.credits ||
           Runtime.Player?.Xp!=expected.xp ||
           Runtime.Player?.Level!=expected.level)
            throw new InvalidOperationException(
                "Pass103 production reward incorrect "+context+
                " expected="+expected+" credits="+Runtime.Player?.Credits+
                " xp="+Runtime.Player?.Xp+
                " level="+Runtime.Player?.Level);
    }

    private void CheckRepairVehicle110(
        Pass110RepairVehicleVisual vehicle,int deposits,string context)
    {
        if(!VerifyRepairVehicle110)
            return;
        var plugs=Math.Min(4,deposits);
        var wheels=Math.Clamp(deposits-4,0,2);
        var cans=Math.Max(0,deposits-6);
        if(!GodotObject.IsInstanceValid(vehicle) ||
           vehicle.MeshPartCount < 36 ||
           vehicle.AcceptedDeposits != deposits ||
           vehicle.InstalledSparkPlugs != plugs ||
           vehicle.InstalledWheels != wheels ||
           vehicle.AddedFuelCans != cans ||
           vehicle.RearLeftWheelVisible != (wheels>=1) ||
           vehicle.RearRightWheelVisible != (wheels>=2) ||
           vehicle.CompletionLampVisible !=
                (deposits==vehicle.ExpectedPartCount) ||
           vehicle.GetChildren().OfType<CollisionObject3D>().Any())
            throw new InvalidOperationException(
                "Pass110 non-authoritative visual assembly inaccurate "+
                context+" expected_deposits="+deposits+
                " observed_visual_deposits="+vehicle.AcceptedDeposits+
                " wheels="+vehicle.InstalledWheels+
                " plugs="+vehicle.InstalledSparkPlugs+
                " fuel="+vehicle.AddedFuelCans);
        _repairVisualInstallChecks110++;
    }

    /// <summary>
    /// After the complete, real-input Wave1 Load mission, use the normal
    /// stage state machine (not a modified wave number) to start Wave2.
    /// The running stage clock stays frozen during item carrying, as in
    /// Pass103; physically spawned players and objective markers are never
    /// translated or modified by the acceptance harness.
    /// </summary>
    private async Task WalkRepairWave104(
        GameplayRoot game,FirstPersonPlayer player,
        string map,string completedLoadId)
    {
        if(Runtime.Match?.Phase!=MatchPhase.Wave ||
           Runtime.Match.Wave!=1 ||
           game.WaveStage!="WAVE" ||
           Runtime.Match.CompletedObjectives.Contains(completedLoadId)!=true)
            throw new InvalidOperationException(
                "Pass104 requires completed real Wave1 Load "+map);

        // These are the exact production Stage.Wave -> WaveEnd ->
        // Intermission -> Countdown transitions used by the game. The
        // regular EndWave teardown clears old objective actors and
        // zombies; no gameplay scene is restarted between the waves.
        game._Process(RegularWaveRules.WaveDurationSeconds+.25);
        game._Process(RegularWaveRules.WaveEndSeconds+.25);
        game._Process(RegularWaveRules.IntermissionSeconds+.25);
        if(Runtime.Match?.Wave!=2 || game.WaveStage!="COUNTDOWN")
            throw new InvalidOperationException(
                "Pass104 actual Wave1->2 state transition failed "+map);
        game._Process(RegularWaveRules.CountdownSeconds+.25);
        game.SetProcess(false);
        await Frames(5);
        if(Runtime.Match?.Phase!=MatchPhase.Wave ||
           Runtime.Match.Wave!=2 || game.WaveStage!="WAVE" ||
           Runtime.Player?.IsAlive!=true ||
           !player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass104 no living player in real Wave2 "+map);

        var objective=game.GetChildren().OfType<ObjectiveRuntime>()
            .FirstOrDefault(x=>GodotObject.IsInstanceValid(x) &&
                !x.IsQueuedForDeletion() && x.Family=="Repair");
        if(objective is null ||
           !objective.UseFallbackMaterialGrounding96 ||
           !objective.Pass96GroundedMaterialLayout ||
           objective.Pass98RequiredMaterials<10)
            throw new InvalidOperationException(
                "Pass104 actual Wave2 Repair objective not spawned "+map);

        Pass110RepairVehicleVisual? vehicle110=null;
        if(VerifyRepairVehicle110)
        {
            vehicle110=objective.Pass110RepairVehicle;
            var expectedVariant=objective.GetMeta("RepairVariant").AsString();
            if(vehicle110 is null || !GodotObject.IsInstanceValid(vehicle110) ||
               vehicle110.Name != "Pass110RepairVehicle" ||
               (expectedVariant=="Large") != vehicle110.LargeVariant ||
               vehicle110.ExpectedPartCount != objective.Pass98RequiredMaterials ||
               vehicle110.MeshPartCount < 36 ||
               vehicle110.AcceptedDeposits != 0 ||
               vehicle110.InstalledWheels != 0 ||
               vehicle110.InstalledSparkPlugs != 0 ||
               vehicle110.AddedFuelCans != 0 ||
               vehicle110.RearLeftWheelVisible ||
               vehicle110.RearRightWheelVisible ||
               vehicle110.CompletionLampVisible ||
               vehicle110.GetChildren().OfType<CollisionObject3D>().Any() ||
               !vehicle110.HasMeta("Pass110RecoveredGeometry") ||
               vehicle110.GetMeta("Pass110RecoveredGeometry").AsBool())
                throw new InvalidOperationException(
                    "Pass110 bad initial source-absent repair truck "+map);
            _repairVehiclesSeen110++;
            if(vehicle110.LargeVariant)_largeRepairVehicles110++;
            else _smallRepairVehicles110++;
        }

        if(VerifyReferenceVisuals109)
        {
            var hud=game.GetNodeOrNull<GameplayHud>("HUD");
            var badge=hud?.GetNodeOrNull<Pass109RepairObjectiveBadge>(
                "Pass109RepairObjectiveBadge");
            if(hud is null || badge is null || !badge.Visible ||
               !hud.Pass109RepairBadgeVisible ||
               badge.GetChildCount()!=0)
                throw new InvalidOperationException(
                    "Pass109 real Repair HUD badge missing "+map);
            _repairBadgeShown109++;
        }

        var feet=objective.Pass96MaterialFeet.ToArray();
        var count=feet.Length;
        var depot=objective.GlobalPosition;
        var id=objective.ObjectiveId;
        var callbacks=0;
        var vehicleCompleted110=false;
        objective.Completed+=_ =>
        {
            callbacks++;
            if(VerifyRepairVehicle110 && vehicle110 is not null)
                vehicleCompleted110 = vehicle110.AssemblyComplete &&
                    vehicle110.AcceptedDeposits==count &&
                    vehicle110.CompletionLampVisible;
        };
        var expected=RewardAfterObjective(Runtime.Player!);
        var wave2Start=player.GlobalPosition;
        var metres=await WalkSafely(
            game,player,depot,map+"/wave2-to-production-Repair");

        for(var item=0;item<count;item++)
        {
            if(!GodotObject.IsInstanceValid(objective) ||
               Runtime.Player?.IsAlive!=true ||
               objective.Pass98RemainingMaterials!=count-item ||
               objective.Pass98DepositedMaterials!=item ||
               objective.Pass98CarryingMaterial ||
               callbacks!=0)
                throw new InvalidOperationException(
                    "Pass104 stale real Repair material state "+map+
                    " item="+item);
            metres+=await WalkSafely(
                game,player,feet[item],map+"/wave2-Repair-material-"+item);
            await PressE();
            if(!objective.Pass98CarryingMaterial ||
               objective.Pass98RemainingMaterials!=count-item-1 ||
               objective.Pass98DepositedMaterials!=item)
                throw new InvalidOperationException(
                    "Pass104 failed physical E pickup "+map+
                    " item="+item);
            CheckCarriedVisual106(objective,player,map+"/Repair/"+item);
            if (item==0)
                await CheckRealAimStow108(
                    objective,player,map+"/Repair-first-material");
            metres+=await WalkSafely(
                game,player,depot,map+"/wave2-Repair-deposit-"+item);
            await PressE();
            if(item<count-1 &&
               (!GodotObject.IsInstanceValid(objective) ||
                objective.Pass98CarryingMaterial ||
                objective.Pass98DepositedMaterials!=item+1 ||
                callbacks!=0))
                throw new InvalidOperationException(
                    "Pass104 failed physical E deposit "+map+
                    " item="+item);
            CheckClearedVisual106(objective,player,map+"/Repair/"+item);
            if(VerifyRepairVehicle110 && vehicle110 is not null)
            {
                if(item<count-1)
                    CheckRepairVehicle110(
                        vehicle110,item+1,map+"/Repair-deposit-"+item);
                else
                {
                    if(!vehicleCompleted110)
                        throw new InvalidOperationException(
                            "Pass110 final E deposit did not fully assemble "+
                            map);
                    _repairAssemblyCompletions110++;
                    // The completed objective may already be QueueFree,
                    // but its callback inspected the real final visuals.
                    _repairVisualInstallChecks110++;
                }
            }
            _repairDeposits104++;
        }

        await Frames(5);
        if(callbacks!=1 ||
           Runtime.Match?.CompletedObjectives.Contains(id)!=true ||
           Runtime.Match.CompletedObjectives.Contains(completedLoadId)!=true)
            throw new InvalidOperationException(
                "Pass104 domain did not complete both waves' missions "+
                map+" repair_callbacks="+callbacks);
        ValidateReward(expected,map+"/wave2-Repair-completed");
        if(VerifyReferenceVisuals109)
        {
            var hud=game.GetNodeOrNull<GameplayHud>("HUD");
            if(hud is null || hud.Pass109RepairBadgeVisible)
                throw new InvalidOperationException(
                    "Pass109 Repair badge survived objective completion "+
                    map);
            _repairBadgeCleared109++;
        }
        Runtime.CompleteObjective(id,"Repair");
        await Frames(3);
        ValidateReward(expected,map+"/wave2-Repair-replayed-receipt");
        if(callbacks!=1)
            throw new InvalidOperationException(
                "Pass104 duplicate Repair callback "+map);

        _completedRepairs104++;
        _repairWalkedMetres104+=metres;
        GD.Print("TWR_PASS104_MAP_FULL_REPAIR_OK map="+map+
            " production_wave=2 family=Repair"+
            " material_items="+count+
            " pickups="+count+" deposits="+count+
            " walked_metres="+metres.ToString("F1")+
            " wave2_start="+wave2Start+
            " no_position_assignment=true"+
            " actual_W_E_keys=true"+
            " callbacks_once=true rewards_once=true"+
            " previous_load_completion_retained=true");
    }

    private async Task VerifyMap(string map)
    {
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass103 previous map did not restore lobby");
        Runtime.StartMap(map);
        var game=new GameplayRoot
        {
            Name="Pass103Production"+map,
            MapName=map,
            Runtime=Runtime
        };
        AddChild(game);
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if(player is null || Runtime.Player?.IsAlive!=true)
            throw new InvalidOperationException(
                "Pass103 no physical production player "+map);
        await Frames(90);
        if(!player.IsOnFloor() ||
           game.Pass87FallbackObjectiveMarkers.Count==0)
            throw new InvalidOperationException(
                "Pass103 missing supported fallback "+map);

        // Start the REAL first production wave and then hold its stage
        // clock. No time-based new infected spawns during this isolated
        // mission acceptance; normal objective/player PhysicsProcess runs.
        game._Process(RegularWaveRules.CountdownSeconds+.25);
        if(Runtime.Match?.Phase!=MatchPhase.Wave ||
           Runtime.Match?.Wave!=1 || game.WaveStage!="WAVE")
            throw new InvalidOperationException(
                "Pass103 real wave did not begin "+map);
        game.SetProcess(false);
        foreach(var pickup in game.GetChildren().OfType<PickupActor>())
            pickup.QueueFree();
        await Frames(5);
        var objective=game.GetChildren().OfType<ObjectiveRuntime>()
            .FirstOrDefault(x=>GodotObject.IsInstanceValid(x) &&
                !x.IsQueuedForDeletion() && x.Family=="Load");
        if(objective is null || !objective.UseFallbackMaterialGrounding96 ||
           !objective.Pass96GroundedMaterialLayout ||
           objective.Pass98RequiredMaterials<10)
            throw new InvalidOperationException(
                "Pass103 real Wave1 Load mission absent "+map);

        if(VerifyReferenceVisuals109)
        {
            var hud=game.GetNodeOrNull<GameplayHud>("HUD");
            if(hud is null || hud.Pass109RepairBadgeVisible)
                throw new InvalidOperationException(
                    "Pass109 Repair badge incorrectly shown during Load "+map);
            CheckGenerator109(
                objective.GetNodeOrNull<Node3D>("Generator"),
                map+"/world-Generator",false);
        }

        var feet=objective.Pass96MaterialFeet.ToArray();
        var count=feet.Length;
        var depot=objective.GlobalPosition;
        var id=objective.ObjectiveId;
        var completedCallbacks=0;
        objective.Completed+=_=>completedCallbacks++;
        var expected=RewardAfterObjective(Runtime.Player!);
        var spawn=player.GlobalPosition;
        var metres=await WalkSafely(
            game,player,depot,map+"/spawn-to-production-Load");

        for(var item=0;item<count;item++)
        {
            if(!GodotObject.IsInstanceValid(objective) ||
               Runtime.Player?.IsAlive!=true ||
               objective.Pass98RemainingMaterials!=count-item ||
               objective.Pass98DepositedMaterials!=item ||
               objective.Pass98CarryingMaterial)
                throw new InvalidOperationException(
                    "Pass103 stale production materials before item="+item+
                    " map="+map);
            var target=feet[item];
            metres+=await WalkSafely(
                game,player,target,map+"/material-"+item);
            await PressE();
            if(!objective.Pass98CarryingMaterial ||
               objective.Pass98RemainingMaterials!=count-item-1 ||
               objective.Pass98DepositedMaterials!=item)
                throw new InvalidOperationException(
                    "Pass103 actual E pickup failed "+map+
                    " item="+item);
            CheckCarriedVisual106(objective,player,map+"/Load/"+item);
            if (item==0 && VerifyReferenceVisuals109)
                CheckGenerator109(
                    player.CaptureCamera.GetNodeOrNull<Node3D>(
                        "Pass106CarriedObjectiveMaterial"),
                    map+"/held-Generator",true);
            if (item==0)
                await CheckRealAimStow108(
                    objective,player,map+"/Load-first-material");
            metres+=await WalkSafely(
                game,player,depot,map+"/return-to-depot-"+item);
            await PressE();
            if(item<count-1)
            {
                if(!GodotObject.IsInstanceValid(objective) ||
                   objective.Pass98CarryingMaterial ||
                   objective.Pass98DepositedMaterials!=item+1 ||
                   completedCallbacks!=0)
                    throw new InvalidOperationException(
                        "Pass103 premature/failed material deposit "+
                        map+" item="+item);
            }
            CheckClearedVisual106(objective,player,map+"/Load/"+item);
            _deposits++;
        }
        await Frames(5);
        if(completedCallbacks!=1 ||
           Runtime.Match?.CompletedObjectives.Contains(id)!=true)
            throw new InvalidOperationException(
                "Pass103 production objective not fully completed "+map+
                " callbacks="+completedCallbacks);
        ValidateReward(expected,map+"/complete");
        Runtime.CompleteObjective(id,"Load");
        await Frames(3);
        ValidateReward(expected,map+"/replayed-receipt");
        if(completedCallbacks!=1)
            throw new InvalidOperationException(
                "Pass103 duplicate completion callback "+map);
        _completed++;
        _walkedMetres+=metres;
        GD.Print("TWR_PASS103_MAP_FULL_LOAD_OK map="+map+
            " production_wave=1 material_items="+count+
            " pickups="+count+" deposits="+count+
            " walked_metres="+metres.ToString("F1")+
            " original_spawn="+spawn+
            " no_position_assignment=true"+
            " actual_W_E_keys=true"+
            " callbacks_once=true rewards_once=true");
        if(ExtendToRepair104)
            await WalkRepairWave104(game,player,map,id);
        Runtime.FailMatch(ExtendToRepair104
            ? "Pass104 isolated physical Load and Repair missions"
            : "Pass103 isolated physical Load mission");
        if(Runtime.Match?.Phase!=MatchPhase.Results ||
           !Runtime.Match.SaveCommitted)
            throw new InvalidOperationException(
                "Pass103 offline result did not persist "+map);
        game.QueueFree();
        await Frames(5);
        Runtime.ReturnToLobby();
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass103 could not restore local offline lobby "+map);
    }
}
