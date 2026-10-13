using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass108: analytic camera-frustum test for the ACTUAL mesh bounds of the
/// Pass106 objective prop, independent of the unrendered camera origin.
/// This measures complete primitive silhouettes, not merely the root anchor.
/// Values are normalized fractions of viewport width and height.
/// This is not an image/pixel fidelity test or a substitute for screenshots.
/// </summary>
public static class Pass108CarryFrustum
{
    public readonly record struct Bounds(
        bool Valid, int Corners, float Left, float Top,
        float Right, float Bottom)
    {
        // The objective stays below/left of the center reticle and is wholly
        // inside the player's field of view, including a small edge margin.
        public bool EntirePropSafe =>
            Valid && Corners > 0 && Left >= .015f && Top >= .10f &&
            Right <= .49f && Bottom <= .985f;
        public bool IntersectsCrosshair =>
            Valid && Left < .54f && Right > .46f &&
            Top < .58f && Bottom > .42f;
    }

    public static Bounds Measure(
        Node3D held, float cameraVerticalFovDegrees, Vector2I viewportSize)
    {
        if (held is null || !GodotObject.IsInstanceValid(held) ||
            !held.Visible || !float.IsFinite(cameraVerticalFovDegrees))
            return default;

        // Headless Windows smoke may expose a zero-size presentation surface;
        // preserve the project-default aspect ratio for math-only validation.
        var viewport = viewportSize.X > 0 && viewportSize.Y > 0
            ? viewportSize : new Vector2I(1280, 720);
        var aspect = Mathf.Clamp(
            (float)viewport.X / viewport.Y, 1f, 3.2f);
        var halfTangent = Mathf.Tan(Mathf.DegToRad(
            Mathf.Clamp(cameraVerticalFovDegrees, 20f, 100f)) * .5f);
        var left = float.PositiveInfinity;
        var top = float.PositiveInfinity;
        var right = float.NegativeInfinity;
        var bottom = float.NegativeInfinity;
        var corners = 0;

        // ObjectiveMaterialVisual106 uses direct primitive mesh children.
        // No bounding-size guesses, material colors or asset-name heuristics
        // are used by this camera-space acceptance check.
        foreach (var child in held.GetChildren())
        {
            if (child is not MeshInstance3D part ||
                !GodotObject.IsInstanceValid(part) || part.Mesh is null)
                continue;

            var cameraLocal = held.Transform * part.Transform;
            var geometry = part.GetAabb();
            for (var vertex = 0; vertex < 8; vertex++)
            {
                var point = cameraLocal * geometry.GetEndpoint(vertex);
                // Every polygon corner must be in front of the camera plane.
                if (point.Z >= -.08f)
                    return default;
                var halfH = -point.Z * halfTangent;
                var halfW = halfH * aspect;
                var x = .5f + point.X / (2f * halfW);
                var y = .5f - point.Y / (2f * halfH);
                if (!float.IsFinite(x) || !float.IsFinite(y))
                    return default;
                left = Mathf.Min(left, x);
                top = Mathf.Min(top, y);
                right = Mathf.Max(right, x);
                bottom = Mathf.Max(bottom, y);
                corners++;
            }
        }

        return corners > 0
            ? new Bounds(true, corners, left, top, right, bottom)
            : default;
    }

    /// <summary>
    /// A display-only bounded scale correction for narrow viewports.
    /// Pass107 chooses the actual camera-space pose and handles ADS stow.
    /// We never change any world objective mesh, physics body or item state.
    /// </summary>
    public static Bounds Fit(
        Node3D held, float cameraVerticalFovDegrees, Vector2I viewportSize)
    {
        if (held is null || !GodotObject.IsInstanceValid(held) ||
            !held.Visible)
            return default;
        var original = held.Scale;
        var result = Measure(held, cameraVerticalFovDegrees, viewportSize);
        for (var n = 0; n < 3 && !result.EntirePropSafe; n++)
        {
            // Pass107's .40 base scale, with a hard .34 minimum to retain
            // the previously validated hip anchor/size compatibility.
            held.Scale = original * (1f - .05f * (n + 1));
            result = Measure(held, cameraVerticalFovDegrees, viewportSize);
        }
        return result;
    }
}
