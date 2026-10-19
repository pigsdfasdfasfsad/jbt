using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Owner-local verification of installed source geometry. Never run in the
/// public CI matrix: output contains private source-derived coordinates.
/// Requires an installed original scene AND its matching navigation file.
/// </summary>
public partial class PrivateSourceTraversalAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;
    public override async void _Ready()
    {
        try
        {
            var arg=OS.GetCmdlineUserArgs().FirstOrDefault(x=>x.StartsWith("--private-report="));
            if (arg is null) throw new ArgumentException("Explicit private report path required");
            var output=Path.GetFullPath(arg["--private-report=".Length..]);
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            Runtime.StartMap("Laboratory");
            var game=new GameplayRoot { Name="PrivateSourceGameplay",MapName="Laboratory",Runtime=Runtime };
            AddChild(game);
            game.SetProcess(false);
            await Frames(30);
            var player=game.GetNode<FirstPersonPlayer>("Player");
            var nav=game.GetChildren().OfType<Pass25SourceNavigationRuntime>().SingleOrDefault();
            if (game.VerifiedOriginalPlayerSpawn121!=1 || nav is null || !player.IsOnFloor())
                throw new InvalidOperationException("Original player/map/navigation is not installed and grounded");
            var original=player.GlobalPosition;
            var preferred=new[]{original+new Vector3(7,0,-5),original+new Vector3(-7,0,-5)};
            var excluded=new global::Godot.Collections.Array<Rid>{player.GetRid()};
            var originFloor=Pass90EscortArrivalSafety.TryGroundDestination(game.GetWorld3D(),original,excluded);
            GD.Print("PRIVATE_ROUTE_DIAGNOSTIC origin_floor="+originFloor);
            var diagnostic=nav.GetProvisionalCandidates(original,preferred[0],24,4f,20f).Select(at=>
            {
                var supported=Pass122ProvisionalObjectiveSafety.TryFind(game.GetWorld3D(),new[]{at},0,original,player.GetRid());
                var path=nav.GetRoute(original,at);
                var ok=SourceTraversalRoutes.TryPlan(game.GetWorld3D(),original,at,player.GetRid(),nav.GetRoute,out var checkedPath);
                return new { at=Point(at), supported=supported.HasValue, path_nodes=path.Length, checked_ok=ok };
            }).ToArray();
            File.WriteAllText(Path.ChangeExtension(output,"diagnostic.json"),JsonSerializer.Serialize(diagnostic));
            var reports=new List<object>();
            var plans=new Dictionary<string,SourceObjectiveLayout>();
            foreach (var family in new[]{"Radio","Load","Repair"})
            {
                var timer=Stopwatch.StartNew();
                var plan=SourceObjectiveLayout.TryPlan(game.GetWorld3D(),original,player.GetRid(),family,
                    preferred,Array.Empty<Vector3>(),nav);
                timer.Stop();
                if(plan is not null) plans[family]=plan;
                reports.Add(new { family, planned=plan is not null, milliseconds=timer.ElapsedMilliseconds,
                    route=plan?.PlayerRoute.Select(Point).ToArray(), materials=plan?.MaterialFeet.Select(Point).ToArray() });
            }
            var result=new { private_original_scene=true, original_player_grounded=true,
                original_source_position=Point(original), graph_nodes=nav.PointCount, graph_edges=nav.EdgeCount,
                connected_regions=nav.ConnectedComponentCount, original_weapon_pack_verified=player.SourceWeaponPackVerified,
                original_weapon_active=player.SourceWeaponModelActive, visible_weapon_parts=player.SourceWeaponVisibleParts,
                missing_weapon_meshes=player.SourceWeaponMissingMeshProxies, plans=reports };
            File.WriteAllText(output,JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
            GD.Print("TWR_PRIVATE_TRAVERSAL_PLAN_RESULT families="+string.Join(",",plans.Keys));
            if (OS.GetCmdlineUserArgs().Contains("--exercise-private-materials",StringComparer.Ordinal))
            {
                // Isolated mission verification: leave original world geometry,
                // player movement and domain reward paths unchanged. Remove
                // unrelated natural loot only so E cannot target a different
                // pickup while the scripted driver tests a material circuit.
                foreach(var pickup in game.GetChildren().OfType<PickupActor>()) pickup.QueueFree();
                await Frames(3);
                float walked=0;
                var receipts=0;
                var deposited=0;
                foreach(var family in new[]{"Load","Repair"})
                {
                    if(!plans.TryGetValue(family,out var plan))
                        throw new InvalidOperationException("Missing complete source material plan: "+family);
                    walked+=await AcceptanceKeyboardDriver.Walk(this,player,plan.PlayerRoute);
                    var id="Laboratory:PrivateTraversal:"+family;
                    var mission=new ObjectiveRuntime { Name="Private"+family,Family=family,
                        ObjectiveId=id,Runtime=Runtime,Player=player,Position=plan.Position,
                        SourceMaterialLayout=plan,Completed=_=>receipts++ };
                    game.AddChild(mission); await Frames(3);
                    for(var i=0;i<plan.MaterialFeet.Length;i++)
                    {
                        walked+=await AcceptanceKeyboardDriver.Walk(this,player,plan.MaterialRoutes[i]);
                        await AcceptanceKeyboardDriver.PressE(this);
                        if(!mission.Pass98CarryingMaterial) throw new InvalidOperationException("Original scene material E pickup failed: "+family+" "+i);
                        walked+=await AcceptanceKeyboardDriver.Walk(this,player,plan.MaterialRoutes[i].Reverse().ToArray());
                        await AcceptanceKeyboardDriver.PressE(this);
                        if(mission.Pass98DepositedMaterials!=i+1) throw new InvalidOperationException("Original scene material deposit failed: "+family+" "+i);
                        deposited++;
                    }
                    var credits=Runtime.Player!.Credits;
                    Runtime.CompleteObjective(id,family);
                    if(Runtime.Player.Credits!=credits) throw new InvalidOperationException("Original scene reward replay duplicated credits");
                    GD.Print("TWR_PRIVATE_SOURCE_MATERIAL_WALK_OK family="+family+" materials="+plan.MaterialFeet.Length+" real_keyboard=true real_E=true");
                    mission.QueueFree(); await Frames(3);
                    walked+=await AcceptanceKeyboardDriver.Walk(this,player,plan.PlayerRoute.Reverse().ToArray());
                }
                if(receipts!=2 || deposited!=22) throw new InvalidOperationException("Incomplete private source material acceptance");
                File.WriteAllText(Path.ChangeExtension(output,"gameplay.json"),JsonSerializer.Serialize(new {
                    private_original_scene=true, real_keyboard_movement=true, player_position_teleported=false,
                    real_E_key=true, full_Load_and_Repair_completed=true, deposits=deposited, completed_objectives=receipts,
                    walked_metres=walked, original_mesh_and_terrain_fidelity_complete=false,
                    isolated_mission_test=true, real_time_survival_match_test=false },new JsonSerializerOptions{WriteIndented=true}));
            }
            if (DisplayServer.GetName()!="headless")
            {
                await Frames(4);
                using var image=GetViewport().GetTexture().GetImage();
                image.SavePng(Path.ChangeExtension(output,"png"));
            }
            Runtime.FailMatch("Owner-local traversal inspection complete");
            RuntimeShutdownCoordinator.Request(GetTree(),0);
        }
        catch(Exception e)
        {
            GD.PushError("TWR_PRIVATE_TRAVERSAL_INSPECTION_FAILED "+e);
            RuntimeShutdownCoordinator.Request(GetTree(),1);
        }
    }
    private static float[] Point(Vector3 p)=>[p.X,p.Y,p.Z];
    private async Task Frames(int n)
    {
        for(var i=0;i<n;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }
}
