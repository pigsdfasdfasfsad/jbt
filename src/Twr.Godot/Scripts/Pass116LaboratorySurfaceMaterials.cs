using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass116: procedurally tiled and light-emitting SURFACES for the
/// Laboratory's approximate fallback blockout only. Owners' recovered
/// source scenes are loaded instead of this builder and are untouched.
/// These are locally authored textures, NOT original Roblox materials.
/// </summary>
public partial class Pass116LaboratorySurfaceMaterials : Node3D
{
    public const int PatternPixels = 64;
    public const int ExpectedFloorSurfaces = 5;
    public const int ExpectedRoofSurfaces = 4;
    public const int ExpectedWallSurfaces = 4;
    public const int MaximumGarageDecals = 12;

    private readonly Dictionary<string, ImageTexture> _textures =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, StandardMaterial3D> _materials =
        new(StringComparer.Ordinal);
    private readonly List<string> _surfaces = [];

    public int PatternCount116 => _textures.Count;
    public int RepaintedSurfaces116 => _surfaces.Count;
    public int FloorSurfaces116 { get; private set; }
    public int RoofSurfaces116 { get; private set; }
    public int WallSurfaces116 { get; private set; }
    public int EmissiveDetails116 { get; private set; }
    public int GarageDecals116 { get; private set; }
    public bool HasAddedCollision116 => ContainsCollision(this);
    public IReadOnlyList<string> RepaintedNames116 => _surfaces;

    public static Pass116LaboratorySurfaceMaterials AddToFallback(
        Node3D root, Pass115LaboratoryDressing dressing)
    {
        var surfaces = new Pass116LaboratorySurfaceMaterials
        {
            Name = "Pass116LaboratorySurfaceMaterials"
        };
        root.AddChild(surfaces);
        surfaces.Build(root, dressing);
        return surfaces;
    }

    private void Build(Node3D root, Pass115LaboratoryDressing dressing)
    {
        // Small deterministic, entirely offline textures, not runtime
        // network downloads or copies of original reference PNGs.
        CreatePattern("clinical", new Color(.72f,.75f,.76f),
            new Color(.47f,.50f,.52f),32, .028f);
        CreatePattern("research", new Color(.63f,.67f,.68f),
            new Color(.35f,.39f,.42f),32, .037f);
        CreatePattern("industrial", new Color(.26f,.30f,.32f),
            new Color(.15f,.18f,.19f),16, .046f);
        CreatePattern("vinyl", new Color(.34f,.43f,.42f),
            new Color(.23f,.32f,.31f),32, .019f);
        CreatePattern("acoustic", new Color(.70f,.75f,.76f),
            new Color(.48f,.54f,.56f),32, .012f);

        // Only MeshInstance3D.MaterialOverride changes: existing floor
        // collision shapes, player capsule, navigators, objectives,
        // pick-ups and infected spawn locations remain identical.
        RepaintStatic(root,"Reception_Floor","clinical",8f,6f,"floor");
        RepaintStatic(root,"Research_Floor","research",10f,9f,"floor");
        RepaintStatic(root,"ServerRoom_Floor","industrial",7f,5f,"floor");
        RepaintStatic(root,"ParkingGarage_Floor","industrial",10f,9f,"floor");
        RepaintDecorative(root,"HospitalWard","vinyl",6f,4f,"floor");

        RepaintStatic(root,"Reception_Roof","acoustic",7f,4f,"roof");
        RepaintStatic(root,"Research_Roof","acoustic",10f,8f,"roof");
        RepaintStatic(root,"ServerRoom_Roof","industrial",8f,5f,"roof");
        RepaintStatic(root,"ParkingGarage_Roof","industrial",10f,9f,"roof");

        RepaintStatic(root,"Reception_WestWall","clinical",3f,7f,"wall");
        RepaintStatic(root,"Reception_EastWall","clinical",3f,7f,"wall");
        RepaintStatic(root,"Research_WestWall","research",5f,9f,"wall");
        RepaintStatic(root,"Research_EastWall","research",5f,9f,"wall");

        IlluminateDetails(dressing);
        BuildGarageLanePaint();

        SetMeta("Pass116OriginalSourceMaterials",false);
        SetMeta("Pass116PrivateScreenshotBytes",false);
        SetMeta("Pass116CollisionGeometryModified",false);
    }

    private void CreatePattern(string key,Color baseColor,
        Color groutColor,int tileStride,float noiseStrength)
    {
        var image=Image.CreateEmpty(PatternPixels,PatternPixels,false,
            Image.Format.Rgba8);
        for(var y=0;y<PatternPixels;y++)
        for(var x=0;x<PatternPixels;x++)
        {
            var grout=x%tileStride<2 || y%tileStride<2;
            // Pure integer hash; no non-deterministic RandomNumberGenerator.
            var hash=unchecked((uint)(
                (x+1)*73856093 ^ (y+1)*19349663 ^ key.Length*83492791));
            var offset=(((hash>>5)&255u)/255f-.5f)*noiseStrength;
            var chosen=grout?groutColor:baseColor;
            image.SetPixel(x,y,new Color(
                Mathf.Clamp(chosen.R+offset,0f,1f),
                Mathf.Clamp(chosen.G+offset,0f,1f),
                Mathf.Clamp(chosen.B+offset,0f,1f),
                1f));
        }
        _textures.Add(key,ImageTexture.CreateFromImage(image));
    }

    private StandardMaterial3D SurfaceMaterial(
        string pattern,float repeatX,float repeatZ)
    {
        var cacheKey=pattern+":"+repeatX+":"+repeatZ;
        if(_materials.TryGetValue(cacheKey,out var existing))
            return existing;
        var material=new StandardMaterial3D
        {
            AlbedoColor=Colors.White,
            AlbedoTexture=_textures[pattern],
            Uv1Scale=new Vector3(repeatX,repeatZ,1f),
            Roughness=pattern=="industrial"?.92f:.79f,
            Metallic=pattern=="industrial"?.09f:0f
        };
        _materials.Add(cacheKey,material);
        return material;
    }

    private void ApplyToMesh(MeshInstance3D target,
        string name,string pattern,float repeatX,float repeatZ,
        string surfaceKind)
    {
        if(_surfaces.Contains(name))
            throw new InvalidOperationException(
                "Pass116 duplicate visual surface "+name);
        if(target.Mesh is not BoxMesh)
            throw new InvalidOperationException(
                "Pass116 may only repaint fallback primitive "+name);
        target.MaterialOverride=SurfaceMaterial(pattern,repeatX,repeatZ);
        _surfaces.Add(name);
        switch(surfaceKind)
        {
            case "floor": FloorSurfaces116++;break;
            case "roof": RoofSurfaces116++;break;
            case "wall": WallSurfaces116++;break;
            default: throw new InvalidOperationException(
                "Pass116 unknown surface class");
        }
    }

    private void RepaintStatic(Node3D root,
        string name,string pattern,float u,float v,string kind)
    {
        var body=root.GetNodeOrNull<StaticBody3D>(name)
            ?? throw new InvalidOperationException(
                "Pass116 expected Laboratory fallback shell "+name);
        var mesh=body.GetChildren().OfType<MeshInstance3D>().SingleOrDefault()
            ?? throw new InvalidOperationException(
                "Pass116 cannot locate sole floor/wall renderer "+name);
        ApplyToMesh(mesh,name,pattern,u,v,kind);
    }

    private void RepaintDecorative(Node3D root,
        string name,string pattern,float u,float v,string kind)
    {
        var mesh=root.GetNodeOrNull<MeshInstance3D>(name)
            ?? throw new InvalidOperationException(
                "Pass116 missing fallback-only ward slab "+name);
        ApplyToMesh(mesh,name,pattern,u,v,kind);
    }

    private void IlluminateDetails(Node node)
    {
        foreach(var child in node.GetChildren())
        {
            if(child is MeshInstance3D mesh && mesh.Mesh is not null)
            {
                var name=mesh.Name.ToString();
                var led=name.Contains("StatusLED",StringComparison.Ordinal);
                var fluorescent=name.EndsWith("Diffuser",
                    StringComparison.Ordinal);
                var scanner=name is "BlueDoorScannerIndicator" or
                    "BlueDoorScannerScreen" or "ReceptionDeskDisplay";
                if(led || fluorescent || scanner)
                {
                    var isBlue=name.Contains("Screen",
                        StringComparison.Ordinal) ||
                        name.EndsWith("LED2",StringComparison.Ordinal);
                    var emissive=fluorescent
                        ? new Color(.78f,.89f,.89f)
                        : isBlue
                            ? new Color(.13f,.48f,.67f)
                            : new Color(.22f,.62f,.37f);
                    mesh.MaterialOverride=new StandardMaterial3D
                    {
                        AlbedoColor=emissive,
                        EmissionEnabled=true,
                        Emission=emissive,
                        EmissionEnergyMultiplier=fluorescent?1.55f:1.9f,
                        Roughness=.35f
                    };
                    EmissiveDetails116++;
                }
            }
            IlluminateDetails(child);
        }
    }

    private void BuildGarageLanePaint()
    {
        // Only thin rendered markings over the original fallback
        // ParkingGarage slab (surface Y=.20), never extra car colliders.
        // Their arrangement is author-inferred, not extracted from rbxlx.
        var white=new StandardMaterial3D
        {
            AlbedoColor=new Color(.70f,.72f,.68f),
            Roughness=.96f
        };
        for(var bay=0;bay<2;bay++)
        for(var stripe=0;stripe<4;stripe++)
        {
            var x=(bay==0?16f:34.5f)+stripe*1.7f;
            AddGarageMark("Bay"+bay+"Stripe"+stripe,
                new Vector3(x,.212f,bay==0?-13f:12f),
                new Vector3(.08f,.018f,4.8f),white);
        }
        for(var dash=0;dash<2;dash++)
            AddGarageMark("LaneDash"+dash,
                new Vector3(28f,.212f,-6f+dash*12f),
                new Vector3(.08f,.018f,2.2f),white);
    }

    private void AddGarageMark(
        string name,Vector3 position,Vector3 dimensions,
        StandardMaterial3D material)
    {
        if(GarageDecals116>=MaximumGarageDecals)
            throw new InvalidOperationException(
                "Pass116 exceeded rendered parking line budget");
        AddChild(new MeshInstance3D
        {
            Name=name,
            Position=position,
            Mesh=new BoxMesh {Size=dimensions},
            MaterialOverride=material,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off
        });
        GarageDecals116++;
    }

    private static bool ContainsCollision(Node root)
    {
        foreach(var child in root.GetChildren())
            if(child is CollisionObject3D ||
               child is CollisionShape3D ||
               ContainsCollision(child))
                return true;
        return false;
    }
}
