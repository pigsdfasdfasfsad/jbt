using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass 51: 3D armory preview attached to the five genuine source Loadout
/// CFrames recovered in Pass49. This is a reconstruction of display behavior,
/// not proof of original weapon poses. Authorized owner tool assemblies take
/// priority; absent source meshes retain explicitly approximate stand-ins.
/// </summary>
public partial class Pass51OriginalLoadoutDisplayRuntime : Node3D
{
    public const string StageName = "Pass51OriginalLoadoutDisplay";
    private static readonly string[] SourceSlots =
        ["Primary", "Secondary", "Melee", "Utility", "View"];
    private static readonly string[] EquippedSlots =
        ["Primary", "Secondary", "Melee", "Utility"];

    private readonly Dictionary<string, Node3D> _anchors =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, WeaponViewModelRuntime> _models =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _displayNames =
        new(StringComparer.Ordinal);
    private Node3D? _previewPivot;
    private Profile? _skinProfile;
    public int VisibleCosmeticProxyParts =>
        _models.Values.Sum(model => model.CosmeticProxyTintParts);
    public float PreviewYawDegrees { get; private set; }
    public float PreviewPitchDegrees { get; private set; }
    public int AuthoredOffsetCount => Pass52SourceLoadoutOffsets.SourceOffsetCount;

    public bool OwnerLobbyVerified { get; private set; }
    public int AuthoredAnchorCount => _anchors.Count;
    public int VisibleWeaponModels => _models.Count;
    public string PreviewWeaponName { get; private set; } = "";
    public bool PreviewUsesSourceAssembly { get; private set; }
    public bool PreviewUsesPreparedScene { get; private set; }
    public int PreviewVisibleParts { get; private set; }
    public int PreviewMissingSourceMeshes { get; private set; }
    public bool HasAnchor(string slot) => _anchors.ContainsKey(slot);

    public string PreviewStatus
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PreviewWeaponName))
                return "Select a weapon to preview.";
            var provenance = PreviewUsesSourceAssembly
                ? OriginalWeaponSourceRuntime.OwnerSourcePackVerified
                    ? "RECOVERED ORIGINAL TOOL ASSEMBLY"
                    : "SYNTHETIC SOURCE FIXTURE (TEST ONLY)"
                : PreviewUsesPreparedScene
                    ? "LOCALLY PREPARED WEAPON MODEL"
                    : "APPROXIMATE WEAPON SHAPE - SOURCE MESH NOT INSTALLED";
            var meshNote = PreviewUsesSourceAssembly && PreviewMissingSourceMeshes > 0
                ? $" | {PreviewMissingSourceMeshes} UNRESOLVED SOURCE MESHES"
                : "";
            return $"3D PREVIEW: {PreviewWeaponName} | {provenance}{meshNote}";
        }
    }

    public static Pass51OriginalLoadoutDisplayRuntime? TryBuild(
        Pass49OriginalLobbyRuntime lobby)
    {
        var stage = new Pass51OriginalLoadoutDisplayRuntime {
            Name = StageName,
            Visible = false,
            OwnerLobbyVerified = lobby.OwnerSourceVerified
        };
        try
        {
            foreach (var slot in SourceSlots)
            {
                if (!lobby.TryGetLoadoutPoint(slot, out var frame))
                    throw new InvalidOperationException(
                        "Missing authored source loadout CFrame: " + slot);
                var anchor = new Node3D {
                    Name = "OriginalLoadoutAnchor_" + slot,
                    Transform = frame
                };
                stage.AddChild(anchor);
                stage._anchors.Add(slot, anchor);
            }
            stage._previewPivot = new Node3D { Name = "SourceSelectedPreviewTilt" };
            stage._anchors["View"].AddChild(stage._previewPivot);
            lobby.AddChild(stage);
            GD.Print("TWR_PASS51_LOADOUT_ANCHORS_READY anchors=5 source_lobby_verified=" +
                stage.OwnerLobbyVerified);
            return stage;
        }
        catch (Exception error)
        {
            GD.PushWarning("TWR_PASS51_LOADOUT_FALLBACK: " + error.Message);
            if (stage.GetParent() is not null)
                stage.GetParent()!.RemoveChild(stage);
            stage.Free();
            return null;
        }
    }

    /// <summary>
    /// Refreshes displayed equipped weapons without mutating ownership,
    /// purchases, credits, ammunition, or the saved loadout.
    /// </summary>
    public void ShowForProfile(Profile? profile)
    {
        _skinProfile = profile;
        foreach (var slot in EquippedSlots)
        {
            var name = profile?.Loadout.GetValueOrDefault(slot) ?? "";
            var spec = FindWeapon(name);
            if (spec is null) ClearSlot(slot);
            else SetModel(slot, spec, 0.58f);
        }
        Visible = true;

        var candidate = profile?.Loadout.GetValueOrDefault("Primary");
        if (FindWeapon(candidate) is null)
            candidate = profile?.Loadout.GetValueOrDefault("Secondary");
        if (FindWeapon(candidate) is null)
            candidate = "Glock 17";
        if (!Preview(candidate))
            ClearSlot("View");
    }

    public void HideDisplay() => Visible = false;

    /// <summary>
    /// Hovering an armory row changes only the preview. All purchase and equip
    /// authority remains in LocalSessionNode and its command handlers.
    /// </summary>
    public bool Preview(string? weaponName)
    {
        var spec = FindWeapon(weaponName);
        if (spec is null)
            return false;
        var changed = PreviewWeaponName != spec.Name;
        var model = SetModel("View", spec, 0.85f);
        model.ApplyCosmeticOverlay(
            _skinProfile?.EquippedWeaponSkins.GetValueOrDefault(spec.Name));
        if (changed && _previewPivot is not null)
        {
            PreviewYawDegrees = 0;
            PreviewPitchDegrees = 0;
            _previewPivot.RotationDegrees = Vector3.Zero;
        }
        PreviewWeaponName = spec.Name;
        PreviewUsesSourceAssembly = model.UsingOriginalToolAssembly;
        PreviewUsesPreparedScene = model.UsingPreparedScene;
        PreviewVisibleParts = model.VisualPartCount;
        PreviewMissingSourceMeshes = model.SourceMissingMeshProxies;
        return true;
    }

    /// <summary>
    /// Explicit right-mouse showroom rotation. The original client clamps
    /// vertical tilt to +/-25 degrees; yaw sensitivity is an approximation.
    /// Never changes weapons, credits, the saved loadout or match camera.
    /// </summary>
    public bool RotatePreview(float deltaX, float deltaY)
    {
        if (!Visible || _previewPivot is null || !_models.ContainsKey("View") ||
            !float.IsFinite(deltaX) || !float.IsFinite(deltaY))
            return false;
        PreviewYawDegrees = (PreviewYawDegrees +
            Math.Clamp(deltaX, -600f, 600f) * .30f) % 360f;
        PreviewPitchDegrees = Math.Clamp(PreviewPitchDegrees +
            Math.Clamp(deltaY, -600f, 600f) * .30f, -25f, 25f);
        _previewPivot.RotationDegrees = new Vector3(
            -PreviewPitchDegrees, PreviewYawDegrees, 0f);
        return true;
    }

    private static RuntimeWeaponDefinition? FindWeapon(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        return RuntimeWeaponCatalog.All().FirstOrDefault(
            weapon => string.Equals(weapon.Name, name, StringComparison.Ordinal));
    }

    private WeaponViewModelRuntime SetModel(
        string slot, RuntimeWeaponDefinition spec, float scale)
    {
        if (_models.TryGetValue(slot, out var current) &&
            GodotObject.IsInstanceValid(current) &&
            _displayNames.GetValueOrDefault(slot) == spec.Name)
        {
            current.ApplyCosmeticOverlay(
                _skinProfile?.EquippedWeaponSkins.GetValueOrDefault(spec.Name));
            return current;
        }

        ClearSlot(slot);
        var holder = slot == "View"
            ? _previewPivot ?? _anchors["View"] : _anchors[slot];
        var model = new WeaponViewModelRuntime {
            Name = "DisplayedWeapon_" + slot,
            Scale = Vector3.One * scale
        };
        holder.AddChild(model);
        model.SetWeapon(spec);
        model.ApplyCosmeticOverlay(
            _skinProfile?.EquippedWeaponSkins.GetValueOrDefault(spec.Name));
        // Source Shop/Lobby.lua uses authored LoadoutPoints[slot] CFrame
        // * each weapon module's LoadoutOffset. The selected View position
        // is separate and originally based on Handle/MidToHandle instead.
        if (slot != "View" &&
            Pass52SourceLoadoutOffsets.TryGet(spec.Name, out var sourceOffset))
        {
            model.Transform = sourceOffset;
            model.Scale = Vector3.One * scale;
        }
        model.SetProcess(false); // Static showroom model, not firing gameplay rig.
        _models[slot] = model;
        _displayNames[slot] = spec.Name;
        return model;
    }

    private void ClearSlot(string slot)
    {
        if (_models.Remove(slot, out var previous) &&
            GodotObject.IsInstanceValid(previous))
        {
            if (previous.GetParent() is not null)
                previous.GetParent()!.RemoveChild(previous);
            previous.Free();
        }
        _displayNames.Remove(slot);
        if (slot != "View") return;
        PreviewWeaponName = "";
        PreviewUsesSourceAssembly = false;
        PreviewUsesPreparedScene = false;
        PreviewVisibleParts = 0;
        PreviewMissingSourceMeshes = 0;
    }
}
