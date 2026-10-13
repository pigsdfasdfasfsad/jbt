using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass107: camera-local, viewport-aware PRESENTATION ONLY of material carried
/// through the real Pass98/99 Load/Repair interactions. These offsets are
/// deliberately approximate until graphical original/reference comparisons.
/// Never influences hit rays, E interaction, item capacity or world collision.
/// </summary>
public static class Pass107CarryPresentation
{
    private const float HipDistance = 1.12f;
    private const float HipScale = .40f;
    private const float LeftOfCentre = -.56f;
    private const float BelowCentre = -.43f;

    public static bool ShouldPresent(float aimBlend, float cameraFovDegrees)
    {
        // Keep the physical carry slot while temporarily stowing ONLY its
        // camera prop as the player starts ADS. Narrow zoom would otherwise
        // enlarge a close camera-space mesh over the original scope/sights.
        return float.IsFinite(aimBlend) &&
            float.IsFinite(cameraFovDegrees) &&
            aimBlend < .08f && cameraFovDegrees >= 54f;
    }

    public static Vector3 HipPosition(
        float cameraFovDegrees, Vector2I viewportSize,
        float bobSeconds, float walkSpeed)
    {
        // Wide displays move the prop farther to the left in camera space
        // while keeping the same fraction of screen width. No HUD/UI nodes
        // need to be moved or hidden.
        var aspect = viewportSize.Y > 0 && viewportSize.X > 0
            ? Mathf.Clamp((float)viewportSize.X / viewportSize.Y,
                1f, 3.2f)
            : 16f / 9f;
        var fov = Mathf.Clamp(cameraFovDegrees, 54f, 75f);
        var verticalHalf = HipDistance *
            Mathf.Tan(Mathf.DegToRad(fov) * .5f);
        var horizontalHalf = verticalHalf * aspect;
        var speedBlend = Mathf.Clamp(walkSpeed / 4.5f, 0f, 1f);
        var bob = Mathf.Sin(bobSeconds * 9f) * .012f * speedBlend;
        return new Vector3(
            horizontalHalf * LeftOfCentre,
            verticalHalf * BelowCentre + bob,
            -HipDistance);
    }

    public static void Apply(
        Node3D held, float aimBlend, float cameraFovDegrees,
        Vector2I viewportSize, float bobSeconds, float walkSpeed)
    {
        if (held is null || !GodotObject.IsInstanceValid(held) ||
            held.IsQueuedForDeletion())
            return;
        if (!ShouldPresent(aimBlend, cameraFovDegrees))
        {
            held.Visible = false;
            return;
        }
        held.Position = HipPosition(cameraFovDegrees, viewportSize,
            bobSeconds, walkSpeed);
        held.Scale = Vector3.One * HipScale;
        held.RotationDegrees = new Vector3(8f, 20f, -7f);
        held.Visible = true;
    }

    // Read-only bounds on the designed anchor. A *graphical* screenshot is
    // still needed to validate entire source/fallback weapon mesh coverage.
    public static bool HasSafeAnchor(
        Node3D held, float cameraFovDegrees, Vector2I viewportSize)
    {
        if (held is null || !held.Visible ||
            !float.IsFinite(cameraFovDegrees))
            return false;
        var aspect = viewportSize.Y > 0 && viewportSize.X > 0
            ? Mathf.Clamp((float)viewportSize.X / viewportSize.Y, 1f, 3.2f)
            : 16f / 9f;
        var halfHeight = HipDistance * Mathf.Tan(
            Mathf.DegToRad(Mathf.Clamp(cameraFovDegrees, 54f, 75f))*.5f);
        var halfWidth = halfHeight * aspect;
        var normalizedX = held.Position.X / halfWidth;
        var normalizedY = held.Position.Y / halfHeight;
        return normalizedX <= -.54f && normalizedX >= -.59f &&
            normalizedY <= -.39f && normalizedY >= -.47f &&
            held.Position.Z < -.95f &&
            held.Scale.X > .32f && held.Scale.X < .45f;
    }
}
