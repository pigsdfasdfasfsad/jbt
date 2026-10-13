using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass112: explicit, approximation-only R6 limb pose changes for a fallen
/// visual. Source-name-supported limb pivots are used when recoverable;
/// no original Roblox Motor6D/ball-socket constraint data or animations
/// are claimed. The live infected actor/physics are never changed.
/// </summary>
public static class Pass112R6CorpseArticulation
{
    public static bool TryFindAnchor(
        MeshInstance3D mesh, InfectedVisualAssembler visual,
        out string group, out Node3D? sourcePivot)
    {
        group = "";
        sourcePivot = null;
        // Recovered source pivots are "LeftArmPivot" etc; the
        // standard procedural R6 fallback has LeftShoulder/LeftHip.
        // Traverse descendants of the existing visual only.
        for (Node? parent = mesh.GetParent();
             parent is not null && parent != visual;
             parent = parent.GetParent())
        {
            if (parent is not Node3D node) continue;
            var region = node.Name.ToString() switch
            {
                "LeftArmPivot" or "LeftShoulder" => "LeftArm",
                "RightArmPivot" or "RightShoulder" => "RightArm",
                "LeftLegPivot" or "LeftHip" => "LeftLeg",
                "RightLegPivot" or "RightHip" => "RightLeg",
                _ => ""
            };
            if (region.Length == 0) continue;
            group = region;
            sourcePivot = node;
            return true;
        }
        return false;
    }

    // Each limb's rotation applies on TOP of its actual death-frame pose,
    // not on a fabricated rest pose. Arms spread while the torso drops;
    // legs buckle asynchronously. Every value here is an approximation.
    public static Vector3 Delta(
        string group, float fraction, bool headshot)
    {
        var t = Mathf.Clamp(fraction,0f,1f);
        var smooth = t * t * (3f - 2f * t);
        var rebound = Mathf.Sin(t * Mathf.Pi * 2f) *
            (1f - t) * .08f;
        var forward = headshot ? -1f : 1f;
        return group switch
        {
            "LeftArm" => new Vector3(
                -.70f * smooth * forward + rebound,
                -.09f * smooth,.39f * smooth),
            "RightArm" => new Vector3(
                -.55f * smooth * forward - rebound,
                .08f * smooth,-.41f * smooth),
            "LeftLeg" => new Vector3(
                .31f * smooth + rebound,0f,.12f * smooth),
            "RightLeg" => new Vector3(
                .22f * smooth - rebound,0f,-.15f * smooth),
            _ => Vector3.Zero
        };
    }
}
