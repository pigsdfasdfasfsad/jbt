using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Locally authored pickup silhouettes. The owner's reference icons inform
/// recognizable item categories, not authenticated mesh topology or pixels.
/// No collision nodes, scripts, textures, files, network calls, or global
/// native-resource caches are introduced by this presentation-only factory.
/// </summary>
public static class ReconstructedPickupVisuals
{
    public const int MaximumMeshParts = 56;
    public static readonly Vector3 PickupEnvelope = new(.55f, .35f, .55f);
    public static IReadOnlyList<string> SupportedTypes { get; } = Array.AsReadOnly(new[]
    {
        "Bandages", "Medkit", "Body Armor", "Ammo", "Energy Drink", "Gas Mask",
        "Frag", "Molotov", "Nerve Gas", "Barbed Wire", "Clap Bomb", "Jack", "50 Cal"
    });

    private sealed class Builder
    {
        internal readonly Node3D Root = new() { Name = "ReconstructedPickupVisual" };
        private readonly Dictionary<string, StandardMaterial3D> _materials = new();
        private int _parts;

        private StandardMaterial3D Material(Color color, bool metal)
        {
            var key = color.ToHtml() + (metal ? "-metal" : "-matte");
            if (_materials.TryGetValue(key, out var material)) return material;
            material = new StandardMaterial3D
            {
                AlbedoColor = color, Roughness = metal ? .48f : .88f,
                Metallic = metal ? .65f : 0f
            };
            _materials.Add(key, material);
            return material;
        }

        private void Part(string name, Mesh mesh, Vector3 position, Color color,
            Vector3 rotation, bool metal)
        {
            if (++_parts > MaximumMeshParts)
                throw new InvalidOperationException("Pickup presentation mesh budget exceeded");
            Root.AddChild(new MeshInstance3D
            {
                Name = name, Mesh = mesh, Position = position,
                RotationDegrees = rotation, MaterialOverride = Material(color, metal)
            });
        }

        internal void Box(string name, Vector3 position, Vector3 size, Color color,
            Vector3 rotation = default, bool metal = false) =>
            Part(name, new BoxMesh { Size = size }, position, color, rotation, metal);

        internal void Tube(string name, Vector3 position, float radius, float height,
            Color color, Vector3 rotation = default, bool metal = false,
            float topRadius = -1f) =>
            Part(name, new CylinderMesh
            {
                BottomRadius = radius, TopRadius = topRadius < 0 ? radius : topRadius,
                Height = height, RadialSegments = 12, Rings = 1
            }, position, color, rotation, metal);

        internal void Ball(string name, Vector3 position, float radius, Color color,
            Vector3 rotation = default) =>
            Part(name, new SphereMesh
            {
                Radius = radius, Height = radius * 2f,
                RadialSegments = 12, Rings = 6
            }, position, color, rotation, false);
    }

    public static Node3D Build(string pickupType, Color fallbackColor)
    {
        var b = new Builder();
        var dark = new Color(.11f, .13f, .12f);
        var olive = new Color(.22f, .28f, .16f);
        var steel = new Color(.42f, .44f, .43f);
        var white = new Color(.85f, .84f, .76f);
        var red = new Color(.65f, .12f, .10f);
        switch (pickupType)
        {
            case "Bandages":
                for (var i = 0; i < 3; i++)
                {
                    var x = (i - 1) * .25f;
                    b.Tube("GauzeRoll" + i, new(x, 0, 0), .14f, .4f, white, new(90, 0, 0));
                    b.Tube("RollCore" + i, new(x, 0, -.208f), .055f, .014f, dark, new(90, 0, 0));
                }
                b.Box("Wrapper", new(0, -.07f, 0), new(.79f, .035f, .16f), new(.55f, .44f, .30f));
                break;
            case "Medkit":
                b.Box("MedicalCase", Vector3.Zero, new(.8f, .48f, .34f), red);
                b.Box("CaseLid", new(0, .23f, 0), new(.82f, .06f, .36f), new(.47f, .09f, .07f));
                b.Box("HandleTop", new(0, .39f, 0), new(.38f, .06f, .09f), dark);
                for (var i = -1; i <= 1; i += 2)
                    b.Box("HandleSide" + i, new(i * .17f, .31f, 0), new(.06f, .14f, .09f), dark);
                b.Box("MedicalCrossVertical", new(0, .01f, -.177f), new(.10f, .31f, .018f), white);
                b.Box("MedicalCrossHorizontal", new(0, .01f, -.18f), new(.30f, .10f, .018f), white);
                break;
            case "Body Armor":
                b.Box("VestBody", Vector3.Zero, new(.61f, .64f, .23f), dark);
                b.Box("FrontPlate", new(0, .04f, -.14f), new(.47f, .43f, .12f), new(.18f, .22f, .18f));
                for (var sign = -1; sign <= 1; sign += 2)
                {
                    b.Box("ShoulderStrap" + sign, new(sign * .22f, .4f, 0), new(.16f, .28f, .22f), olive);
                    b.Box("VestPocket" + sign, new(sign * .16f, -.23f, -.17f), new(.23f, .2f, .15f), olive);
                }
                b.Box("WaistBelt", new(0, -.2f, .14f), new(.75f, .1f, .09f), dark);
                break;
            case "Ammo":
                b.Box("AmmoTin", Vector3.Zero, new(.7f, .38f, .44f), olive, metal: true);
                b.Box("TinLid", new(0, .20f, 0), new(.75f, .065f, .48f), dark, metal: true);
                b.Box("Latch", new(0, .02f, -.245f), new(.09f, .21f, .04f), steel, metal: true);
                b.Box("CarryHandle", new(0, .31f, 0), new(.38f, .05f, .07f), steel, metal: true);
                for (var i = 0; i < 4; i++)
                    b.Tube("VisibleCartridge" + i, new((i - 1.5f) * .12f, -.15f, -.33f),
                        .035f, .26f, new(.57f, .43f, .18f), new(90, 0, 0), true);
                break;
            case "Energy Drink":
                b.Tube("CanBody", Vector3.Zero, .18f, .66f, new(.13f, .42f, .64f), metal: true);
                b.Tube("CanRim", new(0, .337f, 0), .185f, .025f, steel, metal: true);
                b.Tube("CanBase", new(0, -.337f, 0), .182f, .025f, steel, metal: true);
                b.Box("PullTab", new(0, .356f, -.01f), new(.065f, .018f, .12f), dark, metal: true);
                b.Box("CanStripe", new(0, 0, -.18f), new(.075f, .45f, .014f), white, new(0, 0, -18));
                break;
            case "Gas Mask":
                // Reference has a dark half-mask, head harness and twin
                // side filters, not a generic full-face military respirator.
                b.Ball("FaceCup", new(0, -.06f, 0), .25f, dark);
                b.Box("NoseBridge", new(0, .17f, -.1f), new(.22f, .27f, .26f), dark, new(-12, 0, 0));
                for (var sign = -1; sign <= 1; sign += 2)
                {
                    b.Box("SideFilter" + sign, new(sign * .34f, -.12f, -.06f), new(.22f, .35f, .22f), steel, new(0, 0, sign * -22));
                    b.Box("FilterInset" + sign, new(sign * .34f, -.12f, -.181f), new(.15f, .25f, .022f), dark, new(0, 0, sign * -22));
                    b.Box("HarnessSide" + sign, new(sign * .23f, .32f, .15f), new(.055f, .4f, .06f), dark, new(0, 0, sign * -13));
                }
                b.Box("HarnessTop", new(0, .52f, .15f), new(.4f, .055f, .06f), dark);
                b.Tube("LowerValve", new(0, -.3f, -.12f), .11f, .1f, dark, new(90, 0, 0));
                break;
            case "Frag":
                Grenade(b, "Frag", Vector3.Zero, olive, steel);
                break;
            case "Molotov":
                Bottle(b, "Bottle", Vector3.Zero, new(.23f, .30f, .14f), dark);
                b.Box("Cloth", new(.08f, .52f, 0), new(.25f, .065f, .075f), white, new(0, 0, 27));
                b.Box("ClothTail", new(.19f, .49f, 0), new(.15f, .055f, .07f), white, new(0, 0, -35));
                break;
            case "Nerve Gas":
                b.Tube("GasCylinder", Vector3.Zero, .2f, .7f, new(.18f, .24f, .13f), metal: true);
                b.Tube("GasCap", new(0, .37f, 0), .12f, .075f, dark, metal: true);
                b.Tube("IdentificationBand", new(0, .1f, 0), .205f, .12f, new(.65f, .57f, .16f));
                b.Box("TopLever", new(.08f, .43f, 0), new(.27f, .055f, .09f), steel, metal: true);
                b.Box("WarningPanel", new(0, -.14f, -.204f), new(.19f, .19f, .018f), white);
                break;
            case "Barbed Wire":
                b.Box("WireBundleBase", new(0, -.09f, 0), new(.95f, .12f, .34f), dark, metal: true);
                for (var rail = -1; rail <= 1; rail += 2)
                for (var i = 0; i < 7; i++)
                {
                    var x = (i - 3) * .13f;
                    b.Box("BentWire" + rail + "_" + i, new(x, .035f, rail * .13f),
                        new(.18f, .022f, .022f), steel, new(0, 0, i % 2 == 0 ? 52 : -52), true);
                    if (i % 2 == 0)
                        b.Box("Barb" + rail + "_" + i, new(x, .075f, rail * .13f),
                            new(.016f, .13f, .016f), steel, new(0, 0, 25), true);
                }
                break;
            case "Clap Bomb":
                Bottle(b, "DarkBody", Vector3.Zero, new(.11f, .18f, .14f), dark);
                b.Box("Module", new(0, -.05f, -.19f), new(.21f, .32f, .11f), steel);
                b.Box("UpperBand", new(0, .055f, 0), new(.43f, .045f, .47f), dark);
                b.Box("LowerBand", new(0, -.09f, 0), new(.43f, .045f, .47f), dark);
                b.Box("Indicator", new(.035f, -.04f, -.25f), new(.023f, .035f, .02f), red);
                b.Tube("DecorativeStem", new(0, .60f, 0), .07f, .33f, steel, metal: true);
                b.Tube("StemCap", new(0, .79f, 0), .055f, .045f, dark);
                break;
            case "Jack":
                // Authored approximation of the blue/red box, strapped
                // green attachments and yellow illustrated panel in refs.
                b.Box("JackBox", Vector3.Zero, new(.6f, .62f, .54f), new(.13f, .23f, .44f));
                b.Box("RedLid", new(0, .33f, 0), new(.65f, .06f, .59f), red);
                b.Box("FrontPanel", new(0, -.02f, -.278f), new(.34f, .42f, .014f), new(.64f, .50f, .18f));
                for (var sign = -1; sign <= 1; sign += 2)
                {
                    b.Ball("SideAttachment" + sign, new(sign * .37f, .03f, .04f), .23f, olive);
                    b.Box("AttachmentTop" + sign, new(sign * .37f, .29f, .04f), new(.13f, .16f, .15f), dark);
                }
                b.Box("UpperStrap", new(0, .14f, -.29f), new(1f, .075f, .06f), steel);
                b.Box("LowerStrap", new(0, -.13f, -.29f), new(1f, .075f, .06f), steel);
                break;
            case "50 Cal":
                b.Box("Receiver", new(0, .23f, 0), new(.24f, .2f, .45f), dark, metal: true);
                b.Tube("Barrel", new(0, .25f, -.52f), .038f, .72f, dark, new(90, 0, 0), true);
                b.Box("AmmoBox", new(.21f, .14f, 0), new(.2f, .28f, .3f), olive, metal: true);
                b.Tube("Mount", new(0, -.06f, .07f), .045f, .5f, steel, metal: true);
                for (var i = -1; i <= 1; i++)
                    b.Box("FoldedLeg" + i, new(i * .18f, -.25f, .07f), new(.035f, .5f, .045f),
                        dark, new(0, 0, -i * 35), true);
                break;
            default:
                b.Box("FallbackCase", Vector3.Zero, new(.7f, .4f, .5f), fallbackColor);
                b.Box("CaseBand", Vector3.Zero, new(.12f, .43f, .53f), dark);
                break;
        }
        Normalize(b.Root);
        b.Root.SetMeta("OriginalGeometryRecovered", false);
        b.Root.SetMeta("OriginalTexturePixels", false);
        b.Root.SetMeta("ReconstructionKind", "reference-guided-procedural-pickup");
        b.Root.SetMeta("PickupType", pickupType);
        return b.Root;
    }

    private static void Bottle(Builder b, string prefix, Vector3 at, Color body, Color cap)
    {
        b.Tube(prefix + "Body", at + new Vector3(0, -.08f, 0), .19f, .55f, body);
        b.Tube(prefix + "Shoulder", at + new Vector3(0, .235f, 0), .19f, .13f, body, topRadius: .08f);
        b.Tube(prefix + "Neck", at + new Vector3(0, .38f, 0), .08f, .2f, body);
        b.Tube(prefix + "Cap", at + new Vector3(0, .5f, 0), .09f, .04f, cap);
    }

    private static void Grenade(Builder b, string prefix, Vector3 at, Color body, Color metal)
    {
        b.Ball(prefix + "Body", at, .24f, body);
        b.Tube(prefix + "Crown", at + Vector3.Up * .23f, .10f, .14f, metal, metal: true);
        b.Box(prefix + "Lever", at + new Vector3(.08f, .31f, 0), new(.29f, .045f, .09f), metal, metal: true);
        b.Box(prefix + "LeverSide", at + new Vector3(.24f, .14f, 0), new(.045f, .35f, .085f), metal,
            new(0, 0, 12), true);
        b.Tube(prefix + "BodyBand", at, .245f, .06f, new(.13f, .17f, .11f));
    }

    public static Aabb Measure(Node3D model)
    {
        var low = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var high = -low;
        var parts = 0;
        foreach (var child in model.GetChildren())
        {
            if (child is not MeshInstance3D mesh || mesh.Mesh is null) continue;
            var box = mesh.Mesh.GetAabb();
            for (var i = 0; i < 8; i++)
            {
                var corner = box.Position + new Vector3(
                    (i & 1) == 0 ? 0 : box.Size.X,
                    (i & 2) == 0 ? 0 : box.Size.Y,
                    (i & 4) == 0 ? 0 : box.Size.Z);
                var point = mesh.Transform * corner;
                low = new Vector3(Math.Min(low.X, point.X), Math.Min(low.Y, point.Y), Math.Min(low.Z, point.Z));
                high = new Vector3(Math.Max(high.X, point.X), Math.Max(high.Y, point.Y), Math.Max(high.Z, point.Z));
            }
            parts++;
        }
        if (parts == 0) throw new InvalidOperationException("Empty pickup visual");
        return new Aabb(low, high - low);
    }

    private static void Normalize(Node3D model)
    {
        var bounds = Measure(model);
        var size = bounds.Size;
        if (size.X <= 0 || size.Y <= 0 || size.Z <= 0)
            throw new InvalidOperationException("Invalid pickup visual bounds");
        var scale = Math.Min(PickupEnvelope.X / size.X,
            Math.Min(PickupEnvelope.Y / size.Y, PickupEnvelope.Z / size.Z));
        var center = bounds.Position + bounds.Size * .5f;
        foreach (var child in model.GetChildren())
        {
            if (child is not MeshInstance3D mesh) continue;
            mesh.Position = (mesh.Position - center) * scale;
            mesh.Scale *= scale;
        }
        model.SetMeta("MeshParts", model.GetChildCount());
    }
}
