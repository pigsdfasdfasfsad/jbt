"""Expressway road and concrete should not render as featureless grey panels."""
from conftest import ROOT

def test_expressway_road_has_repeatable_non_network_aggregate_texture():
    source=(ROOT/'src/Twr.Godot/Scripts/ExpresswaySceneBuilder.cs').read_text()
    assert 'RoadMat = TexturedSurface(Asphalt' in source
    assert 'CementMat = TexturedSurface(Concrete' in source
    assert 'Image.CreateEmpty(size,size,false,Image.Format.Rgb8)' in source
    assert 'ImageTexture.CreateFromImage(image)' in source
    assert 'material.Uv1Scale = new Vector3(repeat,repeat,1f);' in source
    assert 'state ^= state << 13;' in source
    assert 'HttpClient' not in source
