using Godot;

namespace Twr.Godot;

/// <summary>Locally authored fuel-tanker silhouette, not recovered mesh bytes.
/// Presentation only; all parts fit inside the unchanged damage target box.</summary>
public static class ReconstructedTankerVisual
{
    public static Node3D Build()
    {
        var root = new Node3D { Name="ReconstructedFuelTanker" };
        var red = new StandardMaterial3D { AlbedoColor=new(.38f,.09f,.06f),Metallic=.35f,Roughness=.58f };
        var steel = new StandardMaterial3D { AlbedoColor=new(.20f,.22f,.24f),Metallic=.65f,Roughness=.52f };
        var rubber = new StandardMaterial3D { AlbedoColor=new(.07f,.07f,.07f),Roughness=.92f };
        var warning = new StandardMaterial3D { AlbedoColor=new(.78f,.57f,.12f),Roughness=.78f };
        void Part(string name, Mesh mesh, Vector3 position, Material material, Vector3 rotation=default)
        { root.AddChild(new MeshInstance3D {Name=name,Mesh=mesh,Position=position,MaterialOverride=material,RotationDegrees=rotation}); }
        Part("CylindricalTank",new CylinderMesh {TopRadius=.72f,BottomRadius=.72f,Height=3.8f,RadialSegments=24,Rings=1},new(0,1.27f,0),red,new(0,0,90));
        for (var side=-1;side<=1;side+=2)
        {
            Part("EndCap"+side,new SphereMesh {Radius=.72f,Height=1.44f,RadialSegments=24,Rings=12},new(side*1.88f,1.27f,0),red);
            var cap=(MeshInstance3D)root.GetChild(root.GetChildCount()-1); cap.Scale=new(.48f,1,1);
            Part("TankBand"+side,new CylinderMesh {TopRadius=.732f,BottomRadius=.732f,Height=.09f,RadialSegments=24,Rings=1},new(side*1.22f,1.27f,0),steel,new(0,0,90));
            for (var end=-1;end<=1;end+=2)
                Part("Wheel"+side+"_"+end,new CylinderMesh {TopRadius=.32f,BottomRadius=.32f,Height=.22f,RadialSegments=16,Rings=1},new(end*1.45f,.34f,side*.76f),rubber,new(90,0,0));
            Part("HazardPanel"+side,new BoxMesh {Size=new(.50f,.45f,.018f)},new(0,1.23f,side*.724f),warning);
            Part("HazardStripe"+side,new BoxMesh {Size=new(.075f,.34f,.022f)},new(0,1.23f,side*.738f),rubber,new(0,0,25));
        }
        Part("Chassis",new BoxMesh {Size=new(4.30f,.18f,1.4f)},new(0,.46f,0),steel);
        Part("TopHatch",new CylinderMesh {TopRadius=.22f,BottomRadius=.22f,Height=.12f,RadialSegments=16,Rings=1},new(0,2.04f,0),steel);
        root.SetMeta("OriginalGeometryRecovered",false);
        root.SetMeta("ReconstructionKind","procedural-tanker-within-existing-collider");
        return root;
    }
}
