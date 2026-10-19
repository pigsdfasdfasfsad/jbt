using Godot;

namespace Twr.Godot;

/// <summary>Low-profile reconstructed loading pad; no collision, inventory,
/// timing or reward logic. Not a recovered original vehicle/model.</summary>
public static class LoadStagingVisual
{
    public static Node3D Build()
    {
        var root=new Node3D { Name="DepositTarget" };
        var deck=new StandardMaterial3D { AlbedoColor=new Color(.23f,.28f,.24f),Roughness=.9f };
        var frame=new StandardMaterial3D { AlbedoColor=new Color(.12f,.15f,.16f),Metallic=.6f,Roughness=.5f };
        var marking=new StandardMaterial3D { AlbedoColor=new Color(.78f,.64f,.16f),Roughness=.8f };
        void Box(string name,Vector3 at,Vector3 size,Material material)=>root.AddChild(new MeshInstance3D
        { Name=name,Position=at,Mesh=new BoxMesh { Size=size },MaterialOverride=material });
        Box("LoadingDeck",new(0,.075f,0),new(2.1f,.12f,2.1f),deck);
        foreach(var sign in new[]{-1,1})
        {
            Box("FrameX"+sign,new(sign*1.09f,.095f,0),new(.10f,.17f,2.28f),frame);
            Box("FrameZ"+sign,new(0,.095f,sign*1.09f),new(2.08f,.17f,.10f),frame);
            for(var i=-2;i<=2;i++)
                Box("EdgeMark"+sign+"_"+i,new(i*.39f,.184f,sign*1.09f),new(.20f,.012f,.09f),marking);
        }
        root.SetMeta("OriginalGeometryRecovered",false);
        root.SetMeta("PresentationOnly",true);
        return root;
    }
}
