using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass114: tilt passive fallen-infected render meshes toward a measured
/// terrain normal and solve a mesh-to-PLANE support distance on slopes.
/// Nothing here owns physics, collision, actors, damage, rewards or joints.
/// Full original Roblox ragdoll/constraint values are not recoverable.
/// </summary>
public static class Pass114CorpseSlopeSupport
{
    // More steeply inclined surfaces are rejected by the existing Pass113
    // walkable-normal test. Keep visual tilt to a modest independent bound.
    public const float MaximumVisualTiltRadians = .349066f; // 20 degrees
    public const float MinimumVisualTiltRadians = .05236f;  // 3 degrees
    public const float MaximumMeshDistanceMeters = 8f;

    public static Basis TiltToNormal(
        Vector3 measuredNormal,float fallFraction,out float appliedRadians)
    {
        appliedRadians=0f;
        if(!float.IsFinite(measuredNormal.X) ||
           !float.IsFinite(measuredNormal.Y) ||
           !float.IsFinite(measuredNormal.Z) ||
           measuredNormal.Y<Pass113CorpseGroundContact.MinimumWalkableNormalY ||
           measuredNormal.LengthSquared()<.1f)
            return Basis.Identity;

        var normal=measuredNormal.Normalized();
        var axis=Vector3.Up.Cross(normal);
        var axisLength=axis.Length();
        if(axisLength<.0001f)
            return Basis.Identity;

        var angle=Mathf.Acos(Mathf.Clamp(normal.Y,-1f,1f));
        if(angle<MinimumVisualTiltRadians)
            return Basis.Identity;

        var progress=Mathf.Clamp((fallFraction-.27f)/.73f,0f,1f);
        var smooth=progress*progress*(3f-2f*progress);
        appliedRadians=Mathf.Min(angle,MaximumVisualTiltRadians)*smooth;
        return appliedRadians<=0f
            ? Basis.Identity
            : new Basis(axis/axisLength,appliedRadians);
    }

    // At each frame the actual tilted R6 limbs move. Measure *every* one
    // of their mesh corners against the collision-plane equation so the
    // corpse does not settle to a horizontal reference on a slope.
    public static bool TryLowestSignedDistance(
        Node corpse,Vector3 deathOrigin,Vector3 planeOrigin,
        Vector3 planeNormal,out float minimumSignedDistance)
    {
        minimumSignedDistance=float.PositiveInfinity;
        if(!float.IsFinite(planeNormal.Y) ||
           planeNormal.Y<Pass113CorpseGroundContact.MinimumWalkableNormalY)
            return false;
        var normal=planeNormal.Normalized();
        SampleMeshes(corpse,deathOrigin,planeOrigin,normal,
            ref minimumSignedDistance);
        return float.IsFinite(minimumSignedDistance);
    }

    private static void SampleMeshes(
        Node node,Vector3 deathOrigin,Vector3 planeOrigin,
        Vector3 normal,ref float smallest)
    {
        foreach(var child in node.GetChildren())
        {
            if(child is MeshInstance3D mesh &&
               mesh.Visible && mesh.Mesh is not null)
            {
                var bounds=mesh.Mesh.GetAabb();
                var min=bounds.Position;
                var max=bounds.End;
                var transform=mesh.GlobalTransform;
                for(var vertex=0;vertex<8;vertex++)
                {
                    var corner=new Vector3(
                        (vertex&1)==0?min.X:max.X,
                        (vertex&2)==0?min.Y:max.Y,
                        (vertex&4)==0?min.Z:max.Z);
                    var point=transform*corner;
                    if(!float.IsFinite(point.X) ||
                       !float.IsFinite(point.Y) ||
                       !float.IsFinite(point.Z) ||
                       point.DistanceTo(deathOrigin)>MaximumMeshDistanceMeters)
                        continue;
                    smallest=Math.Min(
                        smallest,(point-planeOrigin).Dot(normal));
                }
            }
            SampleMeshes(child,deathOrigin,planeOrigin,normal,ref smallest);
        }
    }

    public static float VerticalCorrection(
        float minimumSignedDistance,Vector3 normal)
    {
        if(!float.IsFinite(minimumSignedDistance) ||
           !float.IsFinite(normal.Y) ||
           normal.Y<Pass113CorpseGroundContact.MinimumWalkableNormalY)
            return 0f;
        return Mathf.Clamp(
            (Pass113CorpseGroundContact.ClearanceMeters-
                minimumSignedDistance)/normal.Y,
            -Pass113CorpseGroundContact.MaximumCorrectionMeters,
            Pass113CorpseGroundContact.MaximumCorrectionMeters);
    }
}
