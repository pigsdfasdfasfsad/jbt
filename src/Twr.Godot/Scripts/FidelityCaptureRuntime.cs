using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Owner-local camera/visual-fidelity capture. Never uploads images. Keep
/// captured screenshots and pose JSON in the platform's local user-data folder.
/// </summary>
public static class FidelityCaptureRuntime
{
    public const string CaptureFolder = "user://fidelity-captures";

    public static string MetadataJson(
        string map, Vector3 position, Basis basis, float fov, string imageName)
    {
        return JsonSerializer.Serialize(new
        {
            format = "twr-fidelity-camera-v1",
            map,
            image = imageName,
            source_reference = "unmatched",
            camera_position_metres = new[] { position.X, position.Y, position.Z },
            camera_basis_columns = new[]
            {
                new[] { basis.X.X, basis.X.Y, basis.X.Z },
                new[] { basis.Y.X, basis.Y.Y, basis.Y.Z },
                new[] { basis.Z.X, basis.Z.Y, basis.Z.Z }
            },
            camera_fov_degrees = fov,
            scene_is_original = false,
            is_private_local_capture = true
        },new JsonSerializerOptions { WriteIndented = true });
    }

    public static bool TryCapture(Viewport viewport, Camera3D camera, string mapName)
    {
        var local = ProjectSettings.GlobalizePath(CaptureFolder);
        var mkdir = DirAccess.MakeDirRecursiveAbsolute(local);
        if (mkdir != Error.Ok)
        {
            GD.PushWarning("TWR_FIDELITY_CAPTURE_FAILED mkdir: " + mkdir);
            return false;
        }
        var safeName = new string(mapName.Where(char.IsLetterOrDigit).ToArray());
        if (safeName.Length == 0) safeName = "UnknownMap";
        var stem = safeName + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
        var imageFile = stem + ".png";
        var imagePath = Path.Combine(local, imageFile);
        var metadataPath = Path.Combine(local, stem + ".json");
        var image = viewport.GetTexture().GetImage();
        if (image.IsEmpty() || image.SavePng(imagePath) != Error.Ok)
        {
            GD.PushWarning("TWR_FIDELITY_CAPTURE_FAILED PNG");
            return false;
        }

        var metadata = MetadataJson(mapName, camera.GlobalPosition,
            camera.GlobalTransform.Basis, camera.Fov, imageFile);
        using var output = global::Godot.FileAccess.Open(metadataPath,
            global::Godot.FileAccess.ModeFlags.Write);
        if (output is null)
        {
            GD.PushWarning("TWR_FIDELITY_CAPTURE_FAILED JSON");
            return false;
        }
        output.StoreString(metadata);
        GD.Print("TWR_FIDELITY_CAPTURE_OK image=" + imagePath +
            " camera_json=" + metadataPath);
        return true;
    }
}
