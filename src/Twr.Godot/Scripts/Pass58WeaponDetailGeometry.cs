using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass58: bounded source-guided weapon silhouette proxies. No Roblox
/// MeshPart triangles are available in the uploaded TestPlace.rbxlx.
/// Every contour here is explicitly RECONSTRUCTED, never original mesh data.
/// The caller applies the genuine source CFrame, dimensions and material.
/// Owner-installed OBJ meshes always take precedence.
/// </summary>
public static class Pass58WeaponDetailGeometry
{
    private static readonly Dictionary<string, Mesh> Cached =
        new(StringComparer.Ordinal);
    private const int MaxPolygonCorners = 12;

    // Polygon coordinates are unit-space (Y,Z), extruded through original X.
    // They improve silhouettes without altering any source-authored bounds.
    private static readonly Vector2[] MagazineProfile = [
        new(-.50f,-.47f),new(.39f,-.50f),new(.50f,.29f),
        new(.31f,.50f),new(-.41f,.46f),new(-.50f,.21f)
    ];
    private static readonly Vector2[] StockProfile = [
        new(-.49f,-.50f),new(.22f,-.50f),new(.44f,-.31f),
        new(.39f,.12f),new(.16f,.48f),new(-.47f,.50f)
    ];
    private static readonly Vector2[] GripProfile = [
        new(-.50f,-.45f),new(.30f,-.50f),new(.50f,-.29f),
        new(.29f,.43f),new(.05f,.50f),new(-.45f,.45f)
    ];
    // Bevelled frame in XY, extruded along the original weapon-local Z.
    private static readonly Vector2[] ChassisProfile = [
        new(-.39f,-.50f),new(.39f,-.50f),new(.50f,-.39f),
        new(.50f,.39f),new(.39f,.50f),new(-.39f,.50f),
        new(-.50f,.39f),new(-.50f,-.39f)
    ];

    public static bool TryCreate(string sourceClass,string originalName,
        Vector3 authoredSize,out Mesh? mesh,out string detailKind)
    {
        mesh=null;
        detailKind="";
        if(sourceClass!="MeshPart" || !FiniteBounds(authoredSize) ||
           string.IsNullOrWhiteSpace(originalName) || originalName.Length>96)
            return false;
        var name=new string(originalName.Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant).ToArray());
        if(IsLens(name) && TryLensAxis(authoredSize,out var thinAxis))
        {
            detailKind="lens";
            // A flat round proxy with 24 sides, preserving source opacity.
            mesh=Pass57SourceGeometryProxy.Tube(thinAxis);
            return true;
        }
        var broad=Math.Max(authoredSize.Y,authoredSize.Z);
        var thinX=authoredSize.X<=broad*.72f;
        if(thinX && name is "magazine" or "mag" or "pistolmagazine")
            detailKind="magazine";
        else if(thinX && name is "stock" or "buttstock" or "stockpad" or "buttpad")
            detailKind="stock";
        else if(thinX && name is "pistolgrip" or "gripmodel" or "foregrip" or "handgrip")
            detailKind="grip";
        else if(thinX && name is "triggerguard" or "guardtrigger")
            detailKind="trigger_guard";
        else if(authoredSize.Z >=
                Math.Max(authoredSize.X,authoredSize.Y)*.75f &&
                name is "body" or "receiver" or "upperreceiver" or
                "lowerreceiver" or "slide" or "bolt" or
                "handguard" or "handguardupper" or "handguardlower" or
                "pump" or "rails" or "picatinnyrail")
            detailKind="chassis";
        if(detailKind.Length==0)return false;
        if(Cached.TryGetValue(detailKind,out var existing))
        {
            mesh=existing;
            return true;
        }
        var built=detailKind switch {
            "magazine" => Extrude(MagazineProfile,0),
            "stock" => Extrude(StockProfile,0),
            "grip" => Extrude(GripProfile,0),
            "trigger_guard" => RingGuard(),
            _ => Extrude(ChassisProfile,2)
        };
        Cached.Add(detailKind,built);
        mesh=built;
        return true;
    }

    public static bool FiniteBounds(Vector3 size) =>
        float.IsFinite(size.X) && float.IsFinite(size.Y) &&
        float.IsFinite(size.Z) && size.X>.00001f &&
        size.Y>.00001f && size.Z>.00001f &&
        size.X<1000f && size.Y<1000f && size.Z<1000f;

    private static bool IsLens(string name) =>
        name is "reticleglass" or "opticglass" or "reflectiveglass"
            or "glass" or "scopelens" or "opticallens";

    public static bool TryLensAxis(Vector3 size,out int axis)
    {
        axis=0;
        if(!FiniteBounds(size))return false;
        var d=new[]{size.X,size.Y,size.Z};
        axis=Array.IndexOf(d,d.Min());
        var a=d[(axis+1)%3];
        var b=d[(axis+2)%3];
        return d[axis]<=Math.Min(a,b)*.7f &&
               Math.Min(a,b)>=Math.Max(a,b)*.60f;
    }

    // Build an actual closed, triangulated prism instead of a BoxMesh.
    // The source CFrame/Size is applied externally; each coordinate here
    // remains inside the normalized [-0.5,0.5] bounding volume.
    private static Mesh Extrude(Vector2[] polygon,int majorAxis)
    {
        if(polygon.Length is <3 or >MaxPolygonCorners ||
           majorAxis is not (0 or 2))
            throw new ArgumentOutOfRangeException(nameof(polygon));
        var vertices=new List<Vector3>(polygon.Length*18);
        var normals=new List<Vector3>(polygon.Length*18);
        var uvs=new List<Vector2>(polygon.Length*18);

        Vector3 Point(float height,Vector2 p) =>
            majorAxis==0
                ? new Vector3(height,p.X,p.Y)
                : new Vector3(p.X,p.Y,height);

        void Triangle(Vector3 a,Vector3 b,Vector3 c,
            Vector2 ua,Vector2 ub,Vector2 uc)
        {
            var n=(b-a).Cross(c-a);
            if(n.LengthSquared()<1e-12f)
                throw new InvalidOperationException("Degenerate source proxy triangle");
            n=n.Normalized();
            vertices.Add(a);vertices.Add(b);vertices.Add(c);
            normals.Add(n);normals.Add(n);normals.Add(n);
            uvs.Add(ua);uvs.Add(ub);uvs.Add(uc);
        }

        Vector2 center=Vector2.Zero;
        foreach(var p in polygon)center+=p;
        center/=polygon.Length;
        for(var i=0;i<polygon.Length;i++)
        {
            var a=polygon[i];
            var b=polygon[(i+1)%polygon.Length];
            var frontA=Point(.5f,a);
            var frontB=Point(.5f,b);
            var backA=Point(-.5f,a);
            var backB=Point(-.5f,b);
            var uvA=a+new Vector2(.5f,.5f);
            var uvB=b+new Vector2(.5f,.5f);
            var uvC=center+new Vector2(.5f,.5f);
            // Source polygons use counterclockwise coordinates.
            Triangle(Point(.5f,center),frontA,frontB,uvC,uvA,uvB);
            Triangle(Point(-.5f,center),backB,backA,uvC,uvB,uvA);
            var s=(float)i/polygon.Length;
            var t=(float)(i+1)/polygon.Length;
            Triangle(frontA,backB,frontB,
                new Vector2(s,0),new Vector2(t,1),new Vector2(t,0));
            Triangle(frontA,backA,backB,
                new Vector2(s,0),new Vector2(s,1),new Vector2(t,1));
        }
        return CreateMesh(vertices,normals,uvs);
    }

    /// <summary>
    /// Closed four-bar trigger-guard ring with a genuine center opening.
    /// Built from compact extruded solids, not a filled rectangle.
    /// </summary>
    private static Mesh RingGuard()
    {
        var vertices=new List<Vector3>(144);
        var normals=new List<Vector3>(144);
        var uvs=new List<Vector2>(144);
        void Face(Vector3 a,Vector3 b,Vector3 c)
        {
            var n=(b-a).Cross(c-a).Normalized();
            vertices.Add(a);vertices.Add(b);vertices.Add(c);
            normals.Add(n);normals.Add(n);normals.Add(n);
            uvs.Add(new Vector2(a.Y+.5f,a.Z+.5f));
            uvs.Add(new Vector2(b.Y+.5f,b.Z+.5f));
            uvs.Add(new Vector2(c.Y+.5f,c.Z+.5f));
        }
        void Beam(float y0,float y1,float z0,float z1)
        {
            var min=new Vector3(-.5f,y0,z0);
            var max=new Vector3(.5f,y1,z1);
            Vector3 P(float x,float y,float z)=>new(x,y,z);
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                Face(a,b,c); Face(a,c,d);
            }
            Quad(P(max.X,min.Y,min.Z),P(max.X,max.Y,min.Z),
                P(max.X,max.Y,max.Z),P(max.X,min.Y,max.Z));
            Quad(P(min.X,min.Y,max.Z),P(min.X,max.Y,max.Z),
                P(min.X,max.Y,min.Z),P(min.X,min.Y,min.Z));
            Quad(P(min.X,max.Y,min.Z),P(min.X,max.Y,max.Z),
                P(max.X,max.Y,max.Z),P(max.X,max.Y,min.Z));
            Quad(P(max.X,min.Y,min.Z),P(max.X,min.Y,max.Z),
                P(min.X,min.Y,max.Z),P(min.X,min.Y,min.Z));
            Quad(P(max.X,min.Y,max.Z),P(max.X,max.Y,max.Z),
                P(min.X,max.Y,max.Z),P(min.X,min.Y,max.Z));
            Quad(P(min.X,min.Y,min.Z),P(min.X,max.Y,min.Z),
                P(max.X,max.Y,min.Z),P(max.X,min.Y,min.Z));
        }
        Beam(.33f,.50f,-.50f,.50f);
        Beam(-.50f,-.33f,-.50f,.50f);
        Beam(-.33f,.33f,-.50f,-.33f);
        Beam(-.33f,.33f,.33f,.50f);
        return CreateMesh(vertices,normals,uvs);
    }

    private static Mesh CreateMesh(List<Vector3> vertices,
        List<Vector3> normals,List<Vector2> uvs)
    {
        if(vertices.Count<3 || vertices.Count>2000 ||
           vertices.Count!=normals.Count || vertices.Count!=uvs.Count)
            throw new InvalidOperationException("Source detail proxy budget exceeded");
        var arrays=new global::Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex]=vertices.ToArray();
        arrays[(int)Mesh.ArrayType.Normal]=normals.ToArray();
        arrays[(int)Mesh.ArrayType.TexUV]=uvs.ToArray();
        var mesh=new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);
        return mesh;
    }
}
