"""Pass122 provisional-only objective placement contract."""
from conftest import ROOT

def read(path):
    return (ROOT/path).read_text(encoding="utf8")

def test_no_original_objective_locations_claimed():
    layout=read("src/Twr.Godot/Scripts/MapBlockoutBuilder.cs")
    loader=read("src/Twr.Godot/Scripts/LaboratorySourceLoader.cs")
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "ProvisionalSourceObjectives122 { get; init; }" in layout
    assert "ProvisionalSourceObjectives122 = true" in loader
    assert "if (_mapLayout.ProvisionalSourceObjectives122)" in game
    assert "Pass122ProvisionalObjectiveSafety.TryFind(" in game
    assert "RejectedProvisionalObjectives122++" in game
    assert "VerifiedProvisionalObjectives122++" in game
    assert "AdjustedProvisionalObjectives122++" in game

def test_provisional_physics_and_source_provenance():
    code=read("src/Twr.Godot/Scripts/Pass122ProvisionalObjectiveSafety.cs")
    for token in ("space.IntersectRay(ray)","space.IntersectShape(query,1)",
                  'hit["normal"]','hit["position"]',"CollideWithBodies=true",
                  "anchor.X,floor.Y+.12f,anchor.Z", "Exclude=excluded"):
        assert token in code
    assert "RandfRange" not in code
