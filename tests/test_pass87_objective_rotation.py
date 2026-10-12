"""Pass87: rotate static approximate objective anchors per wave."""
from conftest import ROOT

def text(p):return (ROOT/p).read_text(encoding="utf8")

def test_source_neutral_rotation_is_bounded_and_coprime():
    s=text("src/Twr.Godot/Scripts/Pass87FallbackObjectiveRotation.cs")
    assert "markerCount<=0" in s
    assert "wave<=0 || objectiveIndex<0" in s
    assert "markerCount>1 ? markerCount-1 : 1" in s
    assert "(starting+objectiveIndex)%markerCount" in s
    # Same behavior in the pure arithmetic contract over all fallback pool sizes.
    for size in range(1,20):
        stride=size-1 if size>1 else 1
        sequence=[((wave-1)*stride)%size for wave in range(1,size+1)]
        assert len(set(sequence))==size
        for wave in range(1,16):
            indices=[((wave-1)*stride+i)%size for i in range(min(2,size))]
            assert len(set(indices))==len(indices)

def test_original_map_pools_are_not_rotated_and_manor_upper_kept():
    game=text("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert 'if (_isFallbackScene86 && MapName != "Manor")' in game
    assert "Pass87FallbackObjectiveRotation.GetIndex(" in game
    assert "(wave-1+index) % _mapLayout.ObjectivePoints.Count" in game
    assert "return _mapLayout.ObjectivePoints[index % _mapLayout.ObjectivePoints.Count];" in game
    assert "Pass87FallbackObjectiveMarkers" in game
