using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass113: bounded floor probe and visible-mesh contact estimates for
/// non-colliding infected death renderers. The existing map collision
/// layer is read-only. No physics bodies, navigation, rewards or new art.
/// This is an approximate visual landing, NOT original Roblox ragdoll.
/// </summary>
public static class Pass113CorpseGroundContact
{
    public const float ProbeDepthMeters = 4.2f;
    public const float MinimumWalkableNormalY = .58f;
    public const float ClearanceMeters = .035f;
    public const float MaximumCorrectionMeters = 2.75f;
    public const float MaximumMeshRadiusMeters = 8f;

    // The probe starts slightly above the victim's centre, then looks
    // down into the ACTUAL map and temporary physical test platform.
    // Use solid terrain only, never another infected/player or a trigger.
    public static bool TryProbe(
        Node3D corpse, Vector3 deathOrigin,
        out float groundHeight, out Vector3 normal)
    {
        groundHeight=0f;
        normal=Vector3.Up;
        if(!GodotObject.IsInstanceValid(corpse) ||
           !corpse.IsInsideTree())
            return false;

        var from=deathOrigin+Vector3.Up*.20f;
        var to=deathOrigin+Vector3.Down*ProbeDepthMeters;
        var request=PhysicsRayQueryParameters3D.Create(from,to);
        request.CollisionMask=1;
        request.CollideWithAreas=false;
        var hit=corpse.GetWorld3D().DirectSpaceState.IntersectRay(request);
        if(hit.Count==0 ||
           hit["collider"].AsGodotObject() is not StaticBody3D)
            return false;
        var surfaceNormal=hit["normal"].AsVector3();
        var contact=hit["position"].AsVector3();
        if(!float.IsFinite(contact.Y) ||
           !float.IsFinite(surfaceNormal.Y) ||
           surfaceNormal.Y<MinimumWalkableNormalY ||
           contact.Y>deathOrigin.Y+.205f)
            return false;
        groundHeight=contact.Y;
        normal=surfaceNormal.Normalized();
        return true;
    }

    // Test only visible mesh/part corners against the *current* limb pose.
    // This accounts for both recovered source R6 accessories and procedural
    // stand-ins without predeclaring character dimensions or mesh names.
    public static bool TryLowestVisiblePointY(
        Node corpseRoot, Vector3 deathOrigin, out float lowestY)
    {
        lowestY=float.PositiveInfinity;
        SampleCorners(corpseRoot,deathOrigin,ref lowestY);
        return float.IsFinite(lowestY);
    }

    private static void SampleCorners(
        Node parent,Vector3 deathOrigin,ref float lowestY)
    {
        foreach(var child in parent.GetChildren())
        {
            if(child is MeshInstance3D part &&
               part.Mesh is not null &&
               part.Visible)
            {
                var aabb=part.Mesh.GetAabb();
                var min=aabb.Position;
                var max=aabb.End;
                var xf=part.GlobalTransform;
                for(var corner=0;corner<8;corner++)
                {
                    var local=new Vector3(
                        (corner&1)==0?min.X:max.X,
                        (corner&2)==0?min.Y:max.Y,
                        (corner&4)==0?min.Z:max.Z);
                    var world=xf*local;
                    if(!float.IsFinite(world.Y) ||
                       Math.Abs(world.Y-deathOrigin.Y)>
                            MaximumMeshRadiusMeters)
                        continue;
                    lowestY=Math.Min(lowestY,world.Y);
                }
            }
            SampleCorners(child,deathOrigin,ref lowestY);
        }
    }

    public static float BoundedCorrection(float floorY,float meshMinY)
    {
        if(!float.IsFinite(floorY) || !float.IsFinite(meshMinY))
            return 0f;
        return Mathf.Clamp(
            floorY+ClearanceMeters-meshMinY,
            -MaximumCorrectionMeters,MaximumCorrectionMeters);
    }
}
