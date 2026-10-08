"""Synthetic marker-pack extraction: no owner Roblox asset bytes needed."""
import gzip
import hashlib
import importlib.util
import json
import xml.etree.ElementTree as ET
import zipfile
from conftest import ROOT

spec = importlib.util.spec_from_file_location(
    "lab_pickup_markers", ROOT/"tools/maps/augment_laboratory_pickups.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def test_lab_marker_augmentation_from_validated_folders(tmp_path):
    doc = ET.Element("roblox")
    number = 0
    def make(parent, cls, name, pos=None):
        nonlocal number
        number += 1
        node = ET.SubElement(parent, "Item", {"referent": f"X{number}", "class": cls})
        props = ET.SubElement(node, "Properties")
        ET.SubElement(props, "string", {"name": "Name"}).text = name
        if pos is not None:
            frame = ET.SubElement(props, "CoordinateFrame", {"name": "CFrame"})
            for axis, value in zip("XYZ", pos):
                ET.SubElement(frame, axis).text = str(value)
        return node

    spawn_boxes = make(make(make(doc, "Workspace", "Workspace"),
                    "Folder", "Ignore"), "Folder", "Spawn Boxes")
    for name, total in (("ItemPickupBox", 127), ("FortificationPickupBox", 47)):
        parent = make(spawn_boxes, "Folder",
                      "Item Spawn Boxes" if name == "ItemPickupBox"
                      else "Fortification Spawn Boxes")
        for i in range(total):
            make(parent, "Part", name, [i + 2, 4.0, -i])
    xml = ET.tostring(doc)
    source = tmp_path/"TestPlace.zip"
    with zipfile.ZipFile(source, "w") as z:
        z.writestr("TestPlace/TestPlace.rbxlx", xml)
    old_pack = tmp_path/"old.gz"
    with gzip.open(old_pack, "wt") as out:
        out.write(json.dumps({
            "format": "twr-laboratory-scene-v1", "map": "Laboratory",
            "source_sha256": hashlib.sha256(xml).hexdigest(),
            "counts": {"geometry": 0}}) + "\n")
    new_pack = tmp_path/"new.gz"
    result = module.augment(source, old_pack, new_pack)
    assert result["item_markers"] == 127
    assert result["fortification_markers"] == 47
    with gzip.open(new_pack, "rt") as f:
        h = json.loads(next(f))
        records = [json.loads(line) for line in f]
    assert h["counts"]["item_markers"] == 127
    assert len(records) == 174
    assert sum(x["group"] == "Item" for x in records) == 127
    assert sum(x["group"] == "Fortification" for x in records) == 47
    assert all(x["kind"] == "pickup_spawn" for x in records)


def test_incorrect_source_snapshot_fails_without_writing_pack(tmp_path):
    zip_path = tmp_path/"bad.zip"
    with zipfile.ZipFile(zip_path, "w") as z:
        z.writestr("TestPlace/TestPlace.rbxlx", "<roblox/>")
    header = tmp_path/"old.gz"
    with gzip.open(header, "wt") as f:
        f.write('{"format":"twr-laboratory-scene-v1","map":"Laboratory","source_sha256":"BAD","counts":{}}\n')
    output=tmp_path/"new.gz"
    import pytest
    with pytest.raises(ValueError):
        module.augment(zip_path, header, output)
    assert not output.exists()
