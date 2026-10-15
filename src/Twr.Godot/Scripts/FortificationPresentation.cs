using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Render-only deployed equipment. Uses reference-guided local models, not
/// recovered mesh topology. Gameplay radii, ammunition, damage and collision
/// remain exclusively in FortificationActor and the domain inventory.
/// </summary>
public partial class FortificationPresentation : Node3D
{
    public string FortificationType { get; set; } = "Barbed Wire";
    public int WireSegments { get; private set; }
    public bool ShotFlashVisible => _flash?.Visible == true;
    private Node3D? _gimbal;
    private Node3D? _gun;
    private MeshInstance3D? _flash;
    private float _recoil;
    private double _flashRemaining;
    private readonly Dictionary<string, StandardMaterial3D> _materials = new();

    public override void _Ready()
    {
        SetMeta("OriginalGeometryRecovered", false);
        SetMeta("ReconstructionKind", "reference-guided-deployed-fortification");
        if (FortificationType == "Barbed Wire") BuildWire();
        else if (FortificationType == "50 Cal") BuildMountedGun();
        else
        {
            var visual = ReconstructedPickupVisuals.Build(
                FortificationType, new Color(.28f,.30f,.25f));
            var bounds = ReconstructedPickupVisuals.Measure(visual);
            var envelope = new Vector3(.75f,.48f,.75f);
            var factor = Math.Min(envelope.X/bounds.Size.X,
                Math.Min(envelope.Y/bounds.Size.Y,envelope.Z/bounds.Size.Z));
            visual.Scale = Vector3.One*factor;
            visual.Position = new Vector3(
                -(bounds.Position.X+bounds.Size.X*.5f)*factor,
                -bounds.Position.Y*factor+.015f,
                -(bounds.Position.Z+bounds.Size.Z*.5f)*factor);
            AddChild(visual);
        }
    }

    public override void _Process(double delta)
    {
        if (_gun is null) return;
        _recoil = Math.Max(0, _recoil-(float)delta*.40f);
        _gun.Position = Vector3.Back*_recoil;
        _flashRemaining = Math.Max(0, _flashRemaining-delta);
        if (_flash is not null) _flash.Visible = _flashRemaining > 0;
    }

    public void Aim(Vector3 worldDirection)
    {
        if (_gimbal is null || !IsInsideTree() || !worldDirection.IsFinite() ||
            worldDirection.LengthSquared() < .00001f) return;
        var forward=worldDirection.Normalized();
        var up=Math.Abs(forward.Dot(Vector3.Up))>.98f ? Vector3.Back : Vector3.Up;
        _gimbal.GlobalBasis=Basis.LookingAt(forward,up);
    }

    public void PresentShot()
    {
        if (_gun is null || _flash is null) return;
        _recoil=.045f;
        _flashRemaining=.045;
        _flash.Visible=true;
    }

    private StandardMaterial3D Material(Color color)
    {
        var key=color.ToHtml();
        if (_materials.TryGetValue(key,out var found)) return found;
        var material=new StandardMaterial3D
        {
            AlbedoColor=color, Metallic=.55f, Roughness=.64f
        };
        _materials.Add(key,material);
        return material;
    }

    private void Box(Node3D parent,string name,Vector3 position,Vector3 size,Color color)
    {
        parent.AddChild(new MeshInstance3D
        {
            Name=name, Position=position,
            Mesh=new BoxMesh { Size=size }, MaterialOverride=Material(color)
        });
    }

    private static Transform3D RodTransform(Vector3 from,Vector3 to,float width)
    {
        var vector=to-from;
        var y=vector.Normalized();
        var seed=Math.Abs(y.Y)>.9f ? Vector3.Right : Vector3.Up;
        var x=seed.Cross(y).Normalized();
        var z=x.Cross(y).Normalized();
        return new Transform3D(new Basis(x*width,y*vector.Length(),z*width),
            (from+to)*.5f);
    }

    private void Rod(Node3D parent,string name,Vector3 from,Vector3 to,float width,Color color)
    {
        parent.AddChild(new MeshInstance3D
        {
            Name=name, Transform=RodTransform(from,to,width),
            Mesh=new CylinderMesh { TopRadius=.5f,BottomRadius=.5f,Height=1f,
                RadialSegments=12,Rings=1 },
            MaterialOverride=Material(color)
        });
    }

    private void BuildWire()
    {
        // Polygonal coils and barbs reflect the supplied deployed-wire
        // screenshot. All segments share one instanced mesh/draw batch.
        var segments=new List<Transform3D>();
        const int loops=12, sides=8;
        Vector3 Point(int i)
        {
            var angle=Mathf.Tau*i/sides;
            return new Vector3(-1.32f+2.64f*i/(loops*sides),
                .18f+MathF.Sin(angle)*.13f,MathF.Cos(angle)*.23f);
        }
        for (var i=0;i<loops*sides;i++)
        {
            var a=Point(i); var b=Point(i+1);
            segments.Add(RodTransform(a,b,.012f));
            if (i%3!=0) continue;
            var center=(a+b)*.5f;
            var angle=Mathf.Tau*(i+.5f)/sides;
            var barb=new Vector3(.024f,MathF.Sin(angle)*.024f,MathF.Cos(angle)*.024f);
            segments.Add(RodTransform(center-barb,center+barb,.009f));
            var crossed=new Vector3(-barb.X,barb.Y,barb.Z);
            segments.Add(RodTransform(center-crossed,center+crossed,.009f));
        }
        var multi=new MultiMesh
        {
            TransformFormat=MultiMesh.TransformFormatEnum.Transform3D,
            Mesh=new CylinderMesh { TopRadius=.5f,BottomRadius=.5f,Height=1f,
                RadialSegments=8,Rings=1 },
            InstanceCount=segments.Count,
            CustomAabb=new Aabb(new Vector3(-1.4f,0,-.275f),new Vector3(2.8f,.36f,.55f))
        };
        for(var i=0;i<segments.Count;i++) multi.SetInstanceTransform(i,segments[i]);
        AddChild(new MultiMeshInstance3D
        {
            Name="InstancedWireCoils",Multimesh=multi,
            MaterialOverride=Material(new Color(.48f,.47f,.43f))
        });
        WireSegments=segments.Count;
        for(var sign=-1;sign<=1;sign+=2)
            Box(this,"WireStake"+sign,new Vector3(sign*1.37f,.175f,0),
                new Vector3(.028f,.35f,.045f),new Color(.34f,.30f,.23f));
    }

    private void BuildMountedGun()
    {
        var dark=new Color(.13f,.15f,.13f);
        var steel=new Color(.30f,.32f,.30f);
        var olive=new Color(.25f,.30f,.17f);
        var hub=new Vector3(0,.95f,.10f);
        Rod(this,"TripodLeft",new Vector3(-.42f,.03f,.45f),hub,.045f,steel);
        Rod(this,"TripodRight",new Vector3(.42f,.03f,.45f),hub,.045f,steel);
        Rod(this,"TripodFront",new Vector3(0,.03f,-.48f),hub,.045f,steel);
        Rod(this,"Pedestal",hub,new Vector3(0,1.30f,0),.08f,steel);
        _gimbal=new Node3D { Name="GunAimPivot",Position=new Vector3(0,1.30f,0) };
        AddChild(_gimbal);
        _gun=new Node3D { Name="RecoilingGun" };
        _gimbal.AddChild(_gun);
        Box(_gun,"Receiver",new Vector3(0,.04f,.02f),new Vector3(.24f,.20f,.62f),dark);
        Box(_gun,"FeedCover",new Vector3(0,.16f,-.02f),new Vector3(.26f,.045f,.39f),steel);
        Rod(_gun,"Barrel",new Vector3(0,.05f,-.29f),new Vector3(0,.05f,-1.15f),.065f,dark);
        Rod(_gun,"BarrelJacket",new Vector3(0,.05f,-.3f),new Vector3(0,.05f,-.65f),.11f,steel);
        Box(_gun,"AmmoBox",new Vector3(.29f,-.07f,.07f),new Vector3(.27f,.28f,.33f),olive);
        Box(_gun,"FeedTray",new Vector3(.16f,.08f,.02f),new Vector3(.16f,.055f,.15f),steel);
        Box(_gun,"RearSight",new Vector3(0,.21f,.22f),new Vector3(.07f,.08f,.04f),dark);
        Box(_gun,"FrontSight",new Vector3(0,.14f,-.87f),new Vector3(.03f,.10f,.035f),dark);
        for(var sign=-1;sign<=1;sign+=2)
            Rod(_gun,"SpadeGrip"+sign,new Vector3(sign*.16f,-.09f,.36f),
                new Vector3(sign*.16f,.12f,.36f),.05f,dark);
        _flash=new MeshInstance3D
        {
            Name="MuzzleFlash",Position=new Vector3(0,.05f,-1.19f),Visible=false,
            Mesh=new SphereMesh { Radius=.065f,Height=.13f,RadialSegments=8,Rings=4 },
            MaterialOverride=new StandardMaterial3D
            {
                AlbedoColor=new Color(1f,.68f,.15f),
                ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,
                EmissionEnabled=true,Emission=new Color(1f,.55f,.07f)
            }
        };
        _gun.AddChild(_flash);
    }
}
