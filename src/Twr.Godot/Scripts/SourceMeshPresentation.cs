using System;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Visual-only conversion of documented Roblox built-in meshes. A SpecialMesh
/// does not change its parent Part's collision. FileMesh's original asset-space
/// dimensions are not available in every private pack, so its existing fallback
/// remains explicitly approximate; never apply parent-size scaling to FileMesh.
/// Reference: Roblox DataModelMesh.Scale, SpecialMesh.MeshType and PartType.
/// </summary>
public static class SourceMeshPresentation
{
    public static string Kind(JsonElement source)
    {
        if (!source.TryGetProperty("specialMeshType", out var kind)) return "";
        return kind.ValueKind switch
        {
            JsonValueKind.String => kind.GetString() ?? "",
            JsonValueKind.Number => kind.GetInt32().ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => ""
        };
    }

    public static bool TryBuiltinSize(JsonElement source, Vector3 parentSize,
        out Vector3 visualSize)
    {
        visualSize = parentSize;
        var kind = Kind(source);
        if (kind is not ("2" or "3" or "4" or "6")) return false;
        var scale = Vector3.One;
        if (source.TryGetProperty("specialMeshScale", out var value))
        {
            if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 3)
                throw new System.IO.InvalidDataException("Built-in mesh scale must have three components");
            scale = new Vector3(value[0].GetSingle(), value[1].GetSingle(), value[2].GetSingle());
        }
        if (!scale.IsFinite() || !parentSize.IsFinite())
            throw new System.IO.InvalidDataException("Non-finite built-in mesh dimensions");
        // Brick=6, Sphere=3 and Wedge=2 scale RELATIVE TO Part.Size.
        // Scale is dimensionless here, not an absolute number of studs.
        visualSize = parentSize * scale;
        if (kind == "4")
        {
            // Roblox SpecialMesh Cylinder has its height on local X and
            // retains a circular cross-section using the smaller Y/Z size.
            var diameter = Math.Min(visualSize.Y, visualSize.Z);
            visualSize.Y = diameter;
            visualSize.Z = diameter;
        }
        // Avoid singular Godot transforms for deliberately collapsed meshes;
        // negative mirrored scales need a separate winding policy.
        visualSize = new Vector3(Math.Max(.001f, visualSize.X),
            Math.Max(.001f, visualSize.Y), Math.Max(.001f, visualSize.Z));
        return true;
    }

    public static Mesh? BuiltinMesh(JsonElement source) => Kind(source) switch
    {
        "6" => new BoxMesh { Size = Vector3.One },
        "3" => new SphereMesh { Radius = .5f, Height = 1f },
        "2" => RobloxPrimitiveGeometry.WedgeMesh(),
        "4" => CylinderAlongX(),
        _ => null
    };

    public static ArrayMesh CylinderAlongX()
    {
        // Godot cylinders are Y-aligned. Rotate their actual vertices and
        // normals, not the source CFrame, so later non-uniform instance
        // scaling and original transforms retain their correct local axes.
        using var primitive = new CylinderMesh
        {
            TopRadius = .5f, BottomRadius = .5f, Height = 1f,
            RadialSegments = 32, Rings = 1
        };
        using var arrays = primitive.GetMeshArrays();
        var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = new Vector3(vertices[i].Y, -vertices[i].X, vertices[i].Z);
        for (var i = 0; i < normals.Length; i++)
            normals[i] = new Vector3(normals[i].Y, -normals[i].X, normals[i].Z);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        if (arrays[(int)Mesh.ArrayType.Tangent].VariantType != Variant.Type.Nil)
        {
            var tangent = arrays[(int)Mesh.ArrayType.Tangent].AsFloat32Array();
            for (var i = 0; i + 3 < tangent.Length; i += 4)
            {
                var x = tangent[i];
                tangent[i] = tangent[i + 1];
                tangent[i + 1] = -x;
            }
            arrays[(int)Mesh.ArrayType.Tangent] = tangent;
        }
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }
}
