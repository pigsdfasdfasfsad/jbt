"""Pass46 owner-local visual asset pack and actual Godot menu/armory smoke tests."""
from __future__ import annotations
import hashlib
import io
import json
from pathlib import Path
import subprocess
import sys
from zipfile import ZipFile
from PIL import Image,ImageDraw
from conftest import ROOT

TOOLS=ROOT/'tools'/'validation'

def test_fabricated_ci_source_art_has_ten_maps_three_weapons_and_safe_cleanup(tmp_path):
    fixture=TOOLS/'make_pass46_synthetic.py'
    subprocess.run([sys.executable,str(fixture),'--output',str(tmp_path),'--create'],check=True)
    root=tmp_path/'Content'/'Art'
    manifest=json.loads((root/'visuals.json').read_text())
    assert manifest['format']=='twr-pass27-offline-visual-reference-v1'
    assert len(manifest['map_cards'])==10 and len(manifest['weapon_icons'])==3
    assert set(manifest['weapon_icons'])=={'glock17','aa12','m4a1'}
    for entries in (manifest['map_cards'],manifest['weapon_icons']):
        for entry in entries.values():
            source=root/entry['file']
            assert source.read_bytes().startswith(b'\x89PNG\r\n\x1a\n')
            assert hashlib.sha256(source.read_bytes()).hexdigest()==entry['sha256']
    # Refuse to delete if a real user folder lacks the deliberate CI marker.
    (root/'PASS46_CI_SYNTHETIC_ONLY.marker').write_text('not a fake fixture\n')
    fail=subprocess.run([sys.executable,str(fixture),'--output',str(tmp_path),'--clean'],
                        capture_output=True,text=True)
    assert fail.returncode!=0
    (root/'PASS46_CI_SYNTHETIC_ONLY.marker').write_text(
        'PASS46_CI_SYNTHETIC_ONLY\n')
    subprocess.run([sys.executable,str(fixture),'--output',str(tmp_path),'--clean'],check=True)
    assert not root.exists()

def test_full_91_weapon_catalog_offline_reference_packing_is_deterministic(tmp_path):
    # Never use the user's actual images in public CI. Build fabricated image
    # archive with the same name/path contracts and test every catalog entry.
    sys.path.insert(0,str(TOOLS))
    import build_pass46_source_art as builder
    catalog=ROOT/'content'/'weapons'/'catalog.json'
    weapons=json.loads(catalog.read_text())['weapons']
    assert len(weapons)==91
    src=tmp_path/'images.zip'
    image=Image.new('RGBA',(64,40),(0,0,0,0))
    d=ImageDraw.Draw(image)
    d.rectangle((5,6,58,34),fill=(20,48,90,240))
    payload=io.BytesIO()
    image.save(payload,format='PNG')
    raw=payload.getvalue()
    omit={'Ruger 10-22','Sawn Off Shotgun','Spiked Baseball Bat'}
    with ZipFile(src,'w') as zipfile:
        for map_name in builder.MAPS:
            zipfile.writestr('images/'+map_name+'Icon.png',raw)
        for weapon in weapons:
            name=weapon['name']
            if name in omit:continue
            stem=builder.ALIASES.get(name,
                ''.join(c for c in name if c.isascii() and c.isalnum()))
            zipfile.writestr('images/'+stem+'-Default.png',raw)
        # A stale dynamic HUD screenshot must NOT be selected as a weapon icon.
        zipfile.writestr('images/Ruger-UI.png',raw)
    out1=tmp_path/'art1'
    out2=tmp_path/'art2'
    first=builder.build(src,catalog,out1)
    second=builder.build(src,catalog,out2)
    assert first['map_cards']==second['map_cards']==10
    assert first['weapon_artworks']==second['weapon_artworks']==88
    assert first['catalog_weapons']==91
    assert first['missing_original_sprite_references']==sorted(
        omit,key=lambda name:[w['name'] for w in weapons].index(name))
    assert first['original_roblox_3d_meshes_restored'] is False
    assert first['ui_only_no_gameplay_effect'] is True
    assert (out1/'visuals.json').read_bytes()==(out2/'visuals.json').read_bytes()
    manifest=json.loads((out1/'visuals.json').read_text())
    assert len(manifest['map_cards'])==10
    assert len(manifest['weapon_icons'])==88
    assert 'glock17' in manifest['weapon_icons']
    assert 'ruger1022' not in manifest['weapon_icons']
    for group,size in (('map_cards',(512,288)),('weapon_icons',(256,144))):
        for metadata in manifest[group].values():
            art=out1/metadata['file']
            with Image.open(art) as picture:
                assert picture.size==size
            assert hashlib.sha256(art.read_bytes()).hexdigest()==metadata['sha256']

def test_original_source_art_is_optin_and_genuine_armory_controls_survive():
    art=(ROOT/'src/Twr.Godot/Scripts/Pass27SourceArtCatalog.cs').read_text()
    decorate=(ROOT/'src/Twr.Godot/Scripts/Pass46ArmoryArtDecoration.cs').read_text()
    menu=(ROOT/'src/Twr.Godot/Scripts/Bootstrap.cs').read_text()
    dial=(ROOT/'src/Twr.Godot/Scripts/WeaponDialHud.cs').read_text()
    assert 'MapArtCount' in art and 'WeaponArtCount' in art
    assert 'Path.Combine(baseDir, "Content", "Art")' in art
    assert 'SHA256.HashData(png)' in art
    assert 'MaxPngBytes' in art and 'mapFiles.Count != Maps.Length' in art
    assert 'Pass27SourceArtCatalog.WeaponIcon(weaponName)' in decorate
    assert 'if (icon is null) return false;' in decorate
    assert 'MouseFilter = Control.MouseFilterEnum.Ignore' in decorate
    assert 'SourceWeaponSilhouette' in decorate
    assert 'Pass46ArmoryArtDecoration.Apply(button, spec.Name)' in menu
    assert 'HandleArmoryWeapon(selected)' in menu
    assert 'Pass27MapCardDecoration.Apply(button, map.Name)' in menu
    assert 'HasOfflineReferenceWeaponArt' in dial
    assert 'Pass27SourceArtCatalog.WeaponIcon(name)' in dial

def test_native_windows_smoke_proves_menu_armory_hud_art_and_no_asset_leak():
    boot=(ROOT/'src/Twr.Godot/Scripts/Bootstrap.cs').read_text()
    workflow=(ROOT/'.github/workflows/windows-build.yml').read_text()
    assert 'RunPass46Smoke' in boot
    assert '--smoke-pass46' in boot and '--smoke-pass46' in workflow
    assert 'TWR_SMOKE_PASS46_ART_OK' in boot and 'TWR_SMOKE_PASS46_ART_OK' in workflow
    assert 'cards!=10' in boot
    assert 'all.Length!=91' in boot
    assert 'decorated.Length!=3' in boot
    assert 'HasOfflineReferenceWeaponArt' in boot
    assert 'make_pass46_synthetic.py --output $outputDir --create' in workflow
    assert 'make_pass46_synthetic.py --output $outputDir --clean' in workflow
    assert 'TWR-Pass46-Windows-x64-NoPrivateAssets' in workflow
    assert "Content/Art" in workflow
    assert 'twr-pass46-offline-original-art' in workflow
