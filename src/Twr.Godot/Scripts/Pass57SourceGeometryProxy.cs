using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass57 deterministic, render-only fallback for source MeshPart bounds.
/// Original remote triangle streams are ABSENT from owner TestPlace.rbxlx.
/// Tubular shapes are suggested only by a narrow semantic part name AND
/// strong original bounding-box aspect, never described as recovered meshes.
/// Every coordinate still comes from the original CFrame and Size.
/// </summary>
public static class Pass57SourceGeometryProxy
{
    public const int CircularSides=24;
    private static readonly Mesh?[] CachedCircular=[null,null,null];

    public static Mesh ForMissingOriginal(
        string originalClass,string originalName,Vector3 sourceSize,
        out bool circular)
    {
        circular=false;
        if(originalClass=="WedgePart")
            return RobloxPrimitiveGeometry.WedgeMesh();

        if(originalClass=="MeshPart" &&
           IdentifiableTube(originalName) &&
           TryTubeAxis(sourceSize,out var axis))
        {
            circular=true;
            return Tube(axis);
        }
        // Every unrecognized MeshPart/UnionOperation is labelled as a
        // bounding proxy, not an authentic triangle mesh.
        return new BoxMesh {Size=Vector3.One};
    }

    public static bool IdentifiableTube(string name)
    {
        if(string.IsNullOrWhiteSpace(name) || name.Length>96)
            return false;
        // Deliberately exclude non-tubular components e.g. UnderBarrel,
        // BarrelGrip, stock, receiver, magazine and trigger housing.
        var word=new string(name.Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant).ToArray());
        return word switch {
            "barrel" or "forebarrel" or "midbarrel" or "barrelextra" or
            "muzzle" or "nozzle" or "tube" or "magtube" or "gastube" or
            "airtube" or "fueltube" or "cleaningrod" or "suppressor" or
            "silencer" or "pipe" or "rod" or "cylinder" or "scopetube" => true,
            _ => false
        };
    }

    public static bool TryTubeAxis(Vector3 size,out int axis)
    {
        axis=0;
        if(!float.IsFinite(size.X)||!float.IsFinite(size.Y)||
           !float.IsFinite(size.Z)||size.X<=0||size.Y<=0||size.Z<=0)
            return false;
        var dims=new[]{size.X,size.Y,size.Z};
        axis=Array.IndexOf(dims,dims.Max());
        // Do not close over an out parameter: use direct source bounds.
        var crossA=dims[(axis+1)%3];
        var crossB=dims[(axis+2)%3];
        // A long slender authored volume with approximately round cross-
        // section is a much safer approximation than an unconditional
        // cylinder based only on its name.
        return dims[axis]>=Math.Max(crossA,crossB)*1.6f &&
               Math.Min(crossA,crossB)>=Math.Max(crossA,crossB)*.58f;
    }

    public static Mesh Tube(int majorAxis)
    {
        if(majorAxis is <0 or >2)
            throw new ArgumentOutOfRangeException(nameof(majorAxis));
        if(CachedCircular[majorAxis] is not null)
            return CachedCircular[majorAxis]!;
        var vertices=new List<Vector3>(CircularSides*12);
        var normals=new List<Vector3>(CircularSides*12);
        var uvs=new List<Vector2>(CircularSides*12);

        Vector3 Coordinate(float u,float v,float length) => majorAxis switch {
            0 => new Vector3(length,u,v),
            1 => new Vector3(u,length,-v),
            _ => new Vector3(u,v,length)
        };

        void Triangle(Vector3 a,Vector3 b,Vector3 c,
            Vector2 uvA,Vector2 uvB,Vector2 uvC)
        {
            var normal=(b-a).Cross(c-a).Normalized();
            vertices.Add(a);vertices.Add(b);vertices.Add(c);
            normals.Add(normal);normals.Add(normal);normals.Add(normal);
            uvs.Add(uvA);uvs.Add(uvB);uvs.Add(uvC);
        }

        for(var i=0;i<CircularSides;i++)
        {
            var a=2f*Mathf.Pi*i/CircularSides;
            var b=2f*Mathf.Pi*(i+1)/CircularSides;
            var ca=Mathf.Cos(a)*.5f;var sa=Mathf.Sin(a)*.5f;
            var cb=Mathf.Cos(b)*.5f;var sb=Mathf.Sin(b)*.5f;
            var loA=Coordinate(ca,sa,-.5f);
            var loB=Coordinate(cb,sb,-.5f);
            var hiA=Coordinate(ca,sa,.5f);
            var hiB=Coordinate(cb,sb,.5f);
            var u0=(float)i/CircularSides;
            var u1=(float)(i+1)/CircularSides;
            Triangle(loA,loB,hiB,
                new Vector2(u0,0),new Vector2(u1,0),new Vector2(u1,1));
            Triangle(loA,hiB,hiA,
                new Vector2(u0,0),new Vector2(u1,1),new Vector2(u0,1));
            var capA=new Vector2(.5f+ca,.5f+sa);
            var capB=new Vector2(.5f+cb,.5f+sb);
            var capCenter=new Vector2(.5f,.5f);
            Triangle(Coordinate(0,0,.5f),hiA,hiB,capCenter,capA,capB);
            Triangle(Coordinate(0,0,-.5f),loB,loA,capCenter,capB,capA);
        }
        var arrays=new global::Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex]=vertices.ToArray();
        arrays[(int)Mesh.ArrayType.Normal]=normals.ToArray();
        arrays[(int)Mesh.ArrayType.TexUV]=uvs.ToArray();
        var generated=new ArrayMesh();
        generated.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);
        CachedCircular[majorAxis]=generated;
        return generated;
    }
}
