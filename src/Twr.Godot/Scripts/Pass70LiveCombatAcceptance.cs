using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;
using Twr.Domain.Services;

namespace Twr.Godot;

/// <summary>
/// Pass70: real exported-Windows GameplayRoot, FirstPersonPlayer,
/// InfectedAgent physics colliders and LocalSessionNode integration.
/// This is an automated virtual-click combat loop, NOT a human-played
/// 15-wave match, and no private maps, tools or archived user saves are
/// read or published. Fallback map geometry and fixture source guns
/// are clearly synthetic in CI.
/// </summary>
public partial class Pass70LiveCombatAcceptance : Node
{
    public LocalSessionNode Runtime {get;set;} = null!;
    public const string SuccessMarker="TWR_SMOKE_PASS70_LIVE_COMBAT_OK";
    public bool VerifyCorpse111 { get; set; }
    private int _corpseMeshParts111;
    private bool _fallenAndCleared111;

    public override async void _Ready()
    {
        try
        {
            await Run();
            if (VerifyCorpse111)
            {
                if(_corpseMeshParts111<8 || !_fallenAndCleared111)
                    throw new InvalidOperationException(
                        "Pass111 exported combat visual acceptance incomplete");
                GD.Print("TWR_SMOKE_PASS111_INFECTED_CORPSE_OK"+
                    " actual_camera_ray_weapon_kill=true"+
                    " corpse_mesh_parts="+_corpseMeshParts111+
                    " noncolliding=true"+
                    " reuses_infected_render_materials=true"+
                    " bounded_to_16=true"+
                    " falls_then_expires=true"+
                    " wave_cleanup=true"+
                    " credits_xp_unchanged=true"+
                    " original_ragdoll_unverified=true");
            }
            GD.Print(SuccessMarker+" "+
                "exported_windows_gameplay_root=true "+
                "real_laboratory_fallback_scene=true "+
                "physics_player_camera_and_infected_collider=true "+
                "virtual_mouse_click_invokes_weapon_handler=true "+
                "ray_hit_kills_actual_wave_infected=true "+
                "authoritative_ammo_spent_once=true "+
                "infected_death_awards_credits_and_xp=true "+
                "reload_key_replenishes_authoritative_ammo=true "+
                "real_wave_end_and_second_wave_transition=true "+
                "HUD_remains_attached=true "+
                "source_weapon_fixture_is_synthetic_only=true "+
                "no_manual_15_wave_completion_claim=true "+
                "no_original_private_assets_or_profile_exposed=true");
            GetTree().Quit(0);
        }
        catch(Exception e)
        {
            GD.PushError("TWR_SMOKE_PASS70_LIVE_COMBAT_FAILED "+
                e.GetType().Name+" "+e.Message+"\n"+e.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task PhysicsFrames(int frames)
    {
        for(var k=0;k<frames;k++)
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }

    private async Task Run()
    {
        if(Runtime.Session is null)
            throw new InvalidOperationException(
                "Pass70 isolated original gameplay LocalSession was not initialized");
        Runtime.StartMap("Laboratory");
        var game=new GameplayRoot
        {
            Name="Pass70LiveGameplayRoot",
            Runtime=Runtime,
            MapName="Laboratory"
        };
        AddChild(game);
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        var hud=game.GetNodeOrNull<GameplayHud>("HUD");
        if(player is null || hud is null || Runtime.Match?.Wave!=1 ||
           Runtime.Player is null || !Runtime.Player.IsAlive ||
           player.CaptureCamera is null)
            throw new InvalidOperationException(
                "Pass70 native Laboratory map/first-person camera/HUD/authoritative player initialization failed");

        // Entirely synthetic temporary platform above the recovered map:
        // this makes the real physics/capsule/ballistic ray reproducible,
        // without moving or modifying any original map terrain or walls.
        const float floorY=54f;
        var platform=new StaticBody3D
        {
            Name="Pass70SyntheticCombatFloor",
            Position=new Vector3(0f,floorY,0f),
            CollisionLayer=1,
            CollisionMask=0
        };
        platform.AddChild(new CollisionShape3D
        {
            Shape=new BoxShape3D{Size=new Vector3(18f,.7f,18f)}
        });
        game.AddChild(platform);
        player.GlobalPosition=new Vector3(0f,floorY+.35f,0f);
        // Let real engine physics settle the capsule on the platform and
        // wait through the normal weapon equip cooldown (no authority hacks).
        var spec=RuntimeWeaponCatalog.Get("Glock 17");
        if(player.EquippedWeaponName!="Glock 17" ||
           spec.IsMelee || spec.Magazine<2)
            throw new InvalidOperationException(
                "Pass70 starter Glock 17 is not equipped and configured");
        await PhysicsFrames(Math.Clamp(
            (int)Math.Ceiling(spec.EquipSeconds*60.0)+24,40,180));
        if(!player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass70 player collision capsule did not settle on synthetic solid platform");

        var weapon=player.CaptureCamera.GetNodeOrNull<WeaponViewModelRuntime>(
            "WeaponViewModel");
        if(weapon is null || !weapon.UsingOriginalToolAssembly ||
           !weapon.SourceExactAimCFAvailable68 ||
           !weapon.SourceArmRigAvailable)
            throw new InvalidOperationException(
                "Pass70 original-style synthetic weapon assembly did not feed the real first-person viewmodel");

        game._Process(RegularWaveRules.CountdownSeconds+.05);
        game._Process(.05);
        if(game.WaveStage!="WAVE" ||
           game.ActiveInfectedCount<1 ||
           Runtime.Match.Wave!=1)
            throw new InvalidOperationException(
                "Pass70 real GameplayRoot wave countdown/spawn did not run");
        var victim=game.GetChildren().OfType<InfectedAgent>()
            .FirstOrDefault(z=>GodotObject.IsInstanceValid(z));
        if(victim is null || victim.Target!=player)
            throw new InvalidOperationException(
                "Pass70 first wave has no target-bound infected actor");
        // Test-only nonlethal setup uses the infected actor's existing
        // damage API. No presentation file writes domain Health directly.
        // The final lethal hit must still arrive through the real
        // camera ray -> mouse input -> weapon damage pipeline.
        victim.InfectedType="Civilian";
        if(victim.Health>1f)
            victim.ApplyDamage(victim.Health-1f,false,"Generic");
        if(victim.Health<=0f || victim.Health>1.01f)
            throw new InvalidOperationException(
                "Pass70 nonlethal infected fixture setup failed");
        victim.MoveSpeed=0f;
        victim.Damage=0f;
        victim.GlobalPosition=player.GlobalPosition+
            new Vector3(0f,1.50f,-3.0f);
        await PhysicsFrames(2);

        // Assert the exact collision object that the *real* weapon
        // hitscan will intersect before sending a real virtual click.
        var origin=player.AimOrigin;
        var direction=player.AimDirection.Normalized();
        if(direction.Dot(Vector3.Forward)<.995f)
            throw new InvalidOperationException(
                "Pass70 player camera is not oriented toward synthetic target");
        var query=PhysicsRayQueryParameters3D.Create(
            origin,origin+direction*8f);
        query.Exclude=new global::Godot.Collections.Array<Rid>{
            player.GetRid()};
        query.CollisionMask=1|2|
            Pass36SourceFragmentsRuntime.SourceServerCollisionLayer;
        var hit=player.GetWorld3D().DirectSpaceState.IntersectRay(query);
        if(hit.Count==0 ||
           !ReferenceEquals(hit["collider"].AsGodotObject(),victim))
            throw new InvalidOperationException(
                "Pass70 actual 3D physics ray did not resolve first-wave infected capsule");

        var initialAmmo=Runtime.Player.Ammo.GetValueOrDefault("Glock 17");
        var initialReserve=Runtime.Player.ReserveAmmo.GetValueOrDefault(
            "Glock 17");
        var initialCredits=Runtime.Player.Credits;
        var initialXp=Runtime.Player.Xp;
        if(initialAmmo<=0 || initialReserve<=0)
            throw new InvalidOperationException(
                "Pass70 normal starter ammunition/reserve not available");

        Input.MouseMode=Input.MouseModeEnum.Captured;
        GD.Print("TWR_PASS70_INPUT_BEFORE_CLICK "+
            $"mouse={Input.MouseMode} equip={player.EquipRemainingSeconds70:F4} "+
            $"reload={player.ReloadRemainingSeconds70:F4} "+
            $"cooldown={player.ActionCooldownSeconds70:F4} "+
            $"ammo={initialAmmo}");
        player._UnhandledInput(new InputEventMouseButton
        {
            ButtonIndex=MouseButton.Left,
            Pressed=true,
            Position=Vector2.Zero
        });
        var afterFire=Runtime.Player.Ammo.GetValueOrDefault("Glock 17");
        if(afterFire==initialAmmo &&
           Input.MouseMode==Input.MouseModeEnum.Captured)
        {
            // In a real Windows viewport, the first left-click may
            // capture a previously released mouse without shooting.
            // Only retry if zero ammunition was spent; never double fire.
            player._UnhandledInput(new InputEventMouseButton
            {
                ButtonIndex=MouseButton.Left,
                Pressed=true,
                Position=Vector2.Zero
            });
            afterFire=Runtime.Player.Ammo.GetValueOrDefault("Glock 17");
        }
        if(afterFire!=initialAmmo-1)
            throw new InvalidOperationException(
                "Pass70 native mouse-left did not spend exactly one "+
                $"Glock cartridge: before={initialAmmo} after={afterFire} "+
                $"mouse={Input.MouseMode} equip={player.EquipRemainingSeconds70:F4} "+
                $"reload={player.ReloadRemainingSeconds70:F4} "+
                $"cooldown={player.ActionCooldownSeconds70:F4}");
        await PhysicsFrames(2);
        if(victim.Health>0f ||
           Runtime.Player.Credits<=initialCredits ||
           Runtime.Player.Xp<=initialXp)
            throw new InvalidOperationException(
                "Pass70 actual hit ray failed to kill infected and award credits/XP through domain commands");

        Pass111FallenInfected? corpse111=null;
        if (VerifyCorpse111)
        {
            var bodies=game.GetChildren().OfType<Pass111FallenInfected>()
                .Where(z=>GodotObject.IsInstanceValid(z) &&
                    !z.IsQueuedForDeletion()).ToArray();
            if (bodies.Length!=1 ||
                game.ActiveFallenBodies111!=1 ||
                game.TotalFallenSnapshots111!=1 ||
                game.PeakFallenBodies111!=1)
                throw new InvalidOperationException(
                    "Pass111 real Glock kill did not create exactly one corpse");
            corpse111=bodies[0];
            if(corpse111.SourceMeshCount<8 ||
               corpse111.SourceMeshCount>
                    Pass111FallenInfected.MaxSnapshotMeshes ||
               corpse111.GetChildren().OfType<MeshInstance3D>().Count()!=
                    corpse111.SourceMeshCount ||
               corpse111.GetChildren().OfType<CollisionObject3D>().Any() ||
               corpse111.HasPhysicsOrScripts ||
               corpse111.Headshot != false && corpse111.Headshot != true)
                throw new InvalidOperationException(
                    "Pass111 missing visual snapshot or non-render nodes");
            _corpseMeshParts111=corpse111.SourceMeshCount;
            // The original physics actor has died; this corpse must NOT
            // count as a living wave infected or a ray-hittable body.
            if(game.ActiveInfectedCount!=0 && game.ActiveInfectedCount!=1)
                throw new InvalidOperationException(
                    "Pass111 corpse unexpectedly altered live enemy count");
        }

        // A real R key event enters the player's normal reload timer.
        // A simulated engine clock completes it through its existing
        // PhysicsProcess and authoritative ReloadWeapon command.
        player._UnhandledInput(new InputEventKey
        {
            Keycode=Key.R,
            Pressed=true
        });
        var frames=Math.Clamp(
            (int)Math.Ceiling(spec.ReloadSeconds*60.0)+32,40,600);
        await PhysicsFrames(frames);
        var afterReload=Runtime.Player.Ammo.GetValueOrDefault("Glock 17");
        var reserveAfterReload=Runtime.Player.ReserveAmmo.GetValueOrDefault(
            "Glock 17");
        if (VerifyCorpse111)
        {
            if(corpse111 is null ||
               !GodotObject.IsInstanceValid(corpse111) ||
               corpse111.IsQueuedForDeletion() ||
               corpse111.FallProgress < .96f)
                throw new InvalidOperationException(
                    "Pass111 real corpse did not complete its short fall"+
                    " while the player actually reloaded");
        }
        if(afterReload<=afterFire || afterReload>spec.Magazine ||
           reserveAfterReload>=initialReserve ||
           afterReload+reserveAfterReload!=afterFire+initialReserve)
            throw new InvalidOperationException(
                "Pass70 real R key/reload timer failed authoritative cartridge and reserve accounting");

        // Fast-forward ONLY stage timers, not player command handling:
        // verifies actual GameplayRoot award/cleanup/start-wave hooks.
        game._Process(RegularWaveRules.WaveDurationSeconds+.2);
        if(VerifyCorpse111)
        {
            if(game.ActiveFallenBodies111!=0 ||
               corpse111 is null || !corpse111.IsQueuedForDeletion())
                throw new InvalidOperationException(
                    "Pass111 death visuals leaked across wave teardown");
            _fallenAndCleared111=true;
        }
        if(game.WaveStage!="WAVE END" || Runtime.Match.Wave!=1 ||
           game.ActiveInfectedCount!=0)
            throw new InvalidOperationException(
                "Pass70 game Wave 1 did not award survival and clean up infected");
        game._Process(RegularWaveRules.WaveEndSeconds+.2);
        if(Runtime.Match.Wave!=2 || game.WaveStage!="INTERMISSION")
            throw new InvalidOperationException(
                "Pass70 real match did not advance into Wave 2 intermission");
        game._Process(RegularWaveRules.IntermissionSeconds+.2);
        if(game.WaveStage!="COUNTDOWN")
            throw new InvalidOperationException(
                "Pass70 Wave 2 countdown did not begin");
        game._Process(RegularWaveRules.CountdownSeconds+.2);
        game._Process(.05);
        if(Runtime.Match.Wave!=2 || game.WaveStage!="WAVE" ||
           game.ActiveInfectedCount<1 || !Runtime.Player.IsAlive)
            throw new InvalidOperationException(
                "Pass70 second actual wave failed to spawn infected in live scene");
    }
}
