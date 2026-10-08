using Godot;

namespace Twr.Godot;

/// <summary>
/// Unit-size Roblox block and wedge primitives. The prepared mesh pipeline
/// replaces proxies whenever owner-provided original mesh data is installed.
/// Source Part/Shape dimensions are supplied by instance transforms.
/// </summary>
public static class RobloxPrimitiveGeometry
{
    private static readonly Vector3[] WedgeCorners =
    [
        new(-0.5f, -0.5f, -0.5f),
        new( 0.5f, -0.5f, -0.5f),
        new( 0.5f, -0.5f,  0.5f),
        new(-0.5f, -0.5f,  0.5f),
        new(-0.5f,  0.5f,  0.5f),
        new( 0.5f,  0.5f,  0.5f)
    ];

    private static readonly int[] WedgeTriangles =
    [
        // Bottom, vertical back, left and right triangle caps, slope.
        0, 1, 2,  0, 2, 3,
        3, 2, 5,  3, 5, 4,
        0, 3, 4,  1, 5, 2,
        0, 4, 5,  0, 5, 1
    ];

    private static Mesh? _wedge;

    public static Mesh WedgeMesh()
    {
        if (_wedge is not null) return _wedge;
        var vertices = new Vector3[WedgeTriangles.Length];
        var normals = new Vector3[WedgeTriangles.Length];
        for (var i = 0; i < WedgeTriangles.Length; i += 3)
        {
            var a = WedgeCorners[WedgeTriangles[i]];
            var b = WedgeCorners[WedgeTriangles[i + 1]];
            var c = WedgeCorners[WedgeTriangles[i + 2]];
            var normal = (b - a).Cross(c - a).Normalized();
            vertices[i] = a; vertices[i + 1] = b; vertices[i + 2] = c;
            normals[i] = normal; normals[i + 1] = normal; normals[i + 2] = normal;
        }

        var surface = new global::Godot.Collections.Array();
        surface.Resize((int)Mesh.ArrayType.Max);
        surface[(int)Mesh.ArrayType.Vertex] = vertices;
        surface[(int)Mesh.ArrayType.Normal] = normals;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surface);
        _wedge = mesh;
        return mesh;
    }

    public static ConvexPolygonShape3D WedgeCollision(Vector3 size)
    {
        // Unlike scaling CollisionShape3D, scaled points remain valid for
        // nonuniformly sized source wedges, ramps and roof details.
        var vertices = new Vector3[WedgeCorners.Length];
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = new Vector3(
                WedgeCorners[i].X * size.X,
                WedgeCorners[i].Y * size.Y,
                WedgeCorners[i].Z * size.Z);
        return new ConvexPolygonShape3D { Points = vertices };
    }
}
