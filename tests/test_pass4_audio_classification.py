"""Sound IDs classified from the owner's metadata, never downloaded at runtime."""
import csv
import importlib.util
from conftest import ROOT
spec=importlib.util.spec_from_file_location(
    "audio_classifier",ROOT/"tools/audio/classify_sound_ids.py")
mod=importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)

def test_categories_are_heuristic_not_asset_downloads():
    assert mod.categorize({"source_paths":
        "ReplicatedStorage/CMaps/Cargo/Local Sounds/AmbientWavesSound/Sound",
        "sound_names":"Sound"})[0]=="environment"
    assert mod.categorize({"source_paths":
        "Workspace/Infected/Sprinter/Growl","sound_names":"Growl"})[0]=="zombie_vocal"
    assert mod.categorize({"source_paths":
        "Tools/AK-47/Reload","sound_names":"Reload"})[0]=="weapon_reload"

def test_output_keeps_original_id_metadata(tmp_path):
    src=tmp_path/"ids.csv"
    src.write_text("id,sound_names,source_paths\n42,Engine,Environment/EngineIdleSoundTruck\n")
    output=tmp_path/"classified.csv"
    mod.process(src,output)
    with output.open(newline="") as file:
        row=next(csv.DictReader(file))
    assert row["id"]=="42"
    assert row["inferred_category"]=="vehicle"
    assert row["basis"]=="SOURCE PATHS ONLY - REVIEW REQUIRED"
