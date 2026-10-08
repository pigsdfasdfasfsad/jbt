using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Source-faithful positions with a finite active-light budget. Roblox scene
/// emitters are held in memory and reselected as the player traverses the map.
/// No network access or runtime asset downloading is performed.
/// </summary>
public sealed record LaboratorySourceLight(
    string Class, Vector3 Position, Basis Rotation, Color Color,
    float Brightness, float Range, float Angle, int Face);

public partial class LaboratoryLightStreamer : Node3D
{
    public const int ActiveLimit = 128;
    private LaboratorySourceLight[] _source = [];
    private readonly Light3D?[] _slots = new Light3D?[ActiveLimit];
    private readonly int[] _assigned = Enumerable.Repeat(-1, ActiveLimit).ToArray();
    private Vector3 _referencePosition;
    private Node3D? _target;
    private double _refreshWait;

    public void Configure(IEnumerable<LaboratorySourceLight> sources, Vector3 initialPosition)
    {
        _source = sources.ToArray();
        _referencePosition = initialPosition;
    }

    public void Track(Node3D target)
    {
        _target = target;
        _refreshWait = 0;
    }

    public override void _Ready()
    {
        Refresh(_referencePosition);
    }

    public override void _Process(double delta)
    {
        if (_target is null || !GodotObject.IsInstanceValid(_target)) return;
        _refreshWait -= delta;
        if (_refreshWait > 0) return;
        _refreshWait = 0.75;
        var next = _target.GlobalPosition;
        if (next.DistanceSquaredTo(_referencePosition) < 16f) return;
        Refresh(next);
    }

    private void Refresh(Vector3 viewpoint)
    {
        _referencePosition = viewpoint;
        var selection = Enumerable.Range(0, _source.Length)
            .OrderBy(i => _source[i].Position.DistanceSquaredTo(viewpoint))
            .Take(ActiveLimit).ToArray();
        for (var slot = 0; slot < ActiveLimit; slot++)
        {
            var index = slot < selection.Length ? selection[slot] : -1;
            if (_assigned[slot] == index) continue;
            _assigned[slot] = index;
            if (index < 0)
            {
                if (_slots[slot] is not null) _slots[slot]!.Visible = false;
                continue;
            }
            UpdateSlot(slot, _source[index]);
        }
    }

    private void UpdateSlot(int slot, LaboratorySourceLight emitter)
    {
        var directional = emitter.Class != "PointLight";
        var light = _slots[slot];
        if (light is null || (light is SpotLight3D) != directional)
        {
            if (light is not null) light.QueueFree();
            light = directional
                ? new SpotLight3D()
                : new OmniLight3D();
            light.Name = "SourceLight" + slot;
            light.ShadowEnabled = false;
            AddChild(light);
            _slots[slot] = light;
        }
        light.Visible = true;
        light.Position = emitter.Position;
        light.LightColor = emitter.Color;
        light.LightEnergy = emitter.Brightness;
        if (light is OmniLight3D omni)
        {
            omni.OmniRange = Math.Max(0.1f, emitter.Range);
        }
        else if (light is SpotLight3D spot)
        {
            spot.SpotRange = Math.Max(0.1f, emitter.Range);
            // A Roblox SurfaceLight illuminates a face rather than a point;
            // this spot approximation preserves source direction/range.
            spot.SpotAngle = Math.Clamp(emitter.Angle, 1f, 179f);
            var face = emitter.Face switch
            {
                0 => Vector3.Right,
                1 => Vector3.Up,
                2 => Vector3.Forward,
                3 => Vector3.Left,
                4 => Vector3.Down,
                _ => Vector3.Back,
            };
            var direction = emitter.Rotation * face;
            var up = Math.Abs(direction.Dot(Vector3.Up)) > 0.98f
                ? Vector3.Forward : Vector3.Up;
            spot.LookAt(spot.GlobalPosition + direction, up);
        }
    }
}
