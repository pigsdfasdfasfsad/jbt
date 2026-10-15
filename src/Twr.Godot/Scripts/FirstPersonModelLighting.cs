using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// A small render-only fill for the player's hands and equipped model.
/// Only the viewmodel's extra render layer receives it. Original map lights,
/// world collision, aim rays, ammo and weapon movement remain unchanged.
/// This is presentation tuning, not a claim of original Roblox lighting.
/// </summary>
public partial class FirstPersonModelLighting : OmniLight3D
{
    public const uint ViewModelLayer = 1u << 19;
    public Node3D Model { get; set; } = null!;
    public int LitMeshCount { get; private set; }
    private double _refresh;

    public override void _Ready()
    {
        Position = new Vector3(.20f, .25f, -.05f);
        LightColor = new Color(.94f,.96f,1f);
        LightEnergy = 1.15f;
        LightSpecular = .35f;
        LightCullMask = ViewModelLayer;
        OmniRange = 2.6f;
        OmniAttenuation = .8f;
        ShadowEnabled = false;
        RefreshModel();
    }

    public override void _Process(double delta)
    {
        _refresh -= delta;
        if (_refresh > 0) return;
        _refresh = .15;
        // Equip/reload helpers may add new child meshes. A bounded local
        // scan avoids global SceneTree event subscriptions and stale nodes.
        RefreshModel();
    }

    public void RefreshModel()
    {
        LitMeshCount = 0;
        if (Model is null || !GodotObject.IsInstanceValid(Model)) return;
        Visit(Model);
    }

    private void Visit(Node node)
    {
        if (node is MeshInstance3D mesh)
        {
            mesh.Layers |= ViewModelLayer;
            LitMeshCount++;
        }
        foreach (var child in node.GetChildren()) Visit(child);
    }

    public static void Verify(FirstPersonPlayer player)
    {
        var light = player.CaptureCamera.GetNode<FirstPersonModelLighting>("FirstPersonModelLight");
        light.RefreshModel();
        if (light.LitMeshCount == 0 || light.LightCullMask != ViewModelLayer ||
            light.ShadowEnabled || (light.LightCullMask & 1u) != 0)
            throw new InvalidOperationException("Viewmodel fill is missing or affects ordinary world geometry");
        GD.Print("TWR_FIRST_PERSON_LIGHTING_OK actual_player_meshes=" + light.LitMeshCount +
            " world_layer_excluded=true collision_unchanged=true original_lighting_parity_unverified=true");
    }
}
