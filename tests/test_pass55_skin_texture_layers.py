"""Pass55: source texture references, local-only PNGs and reconstructed shimmer."""
from __future__ import annotations
import gzip
import json
import re
import subprocess
import sys

from conftest import ROOT


def read(path):
    return (ROOT/path).read_text(encoding="utf-8")


def test_source_layer_extractor_pins_original_and_never_fetches_texture_bytes():
    script=read("tools/skins/recover_source_skin_layers55.py")
    assert "aeff32781cc9eafdbbd5ae7bc12ff157cc32934e7dae6773c87d7bfba355a015" in script
    assert "scripts/ModuleScript.Skins.Source.txt" in script
    assert 'num_layers!=94 or num_mesh!=33' in script
    assert "if len(parsed)!=118" in script
    assert 'set(slots)!={1,2,3,4,5}' in script
    assert 'gzip.compress(data,mtime=0,compresslevel=9)' in script
    assert 'with args.output.open("xb")' in script
    assert "ZipFile" in script and "Color3" in script
    assert "urllib" not in script and "requests." not in script
    assert "os.system(" not in script and "eval(" not in script


def test_loaded_source_layer_metadata_is_bounded_and_authenticity_labelled():
    loader=read("src/Twr.Godot/Scripts/Pass55SourceSkinLayers.cs")
    assert "c6af38ce98042dc47f928b73720d5f7035b3f1d278936695855fe025d406bcdd" in loader
    assert "SourceSkinLayers55.json.gz" in loader
    assert 'root.GetProperty("skin_count").GetInt32()!=118' in loader
    assert 'root.GetProperty("layer_count").GetInt32()!=94' in loader
    assert 'root.GetProperty("mesh_texture_count").GetInt32()!=33' in loader
    assert "buffer.Length+len>262144" in loader
    assert "packed.Length is <20 or >98304" in loader
    assert 'if(synthetic && !test)' in loader
    assert "SkinCaseCatalog.FindSkin(skin.Name)" in loader
    assert 'layers.GetArrayLength()>2' in loader
    assert 'slot is >=1 and <=5' in loader
    assert 'float.IsFinite(result)' in loader
    assert "HttpClient" not in loader and "Process.Start" not in loader


def test_local_texture_reuses_safe_id_resolver_and_preserves_source_uv():
    renderer=read("src/Twr.Godot/Scripts/Pass55SkinTextureRenderer.cs")
    gun=read("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    assert "OfflineAssetResolver.Texture(id)" in renderer
    assert "GenerateFallback(id)" in renderer
    assert "new Vector3(1f/chosen.TileU,1f/chosen.TileV,1f)" in renderer
    assert "new Vector3(chosen.OffsetU,chosen.OffsetV,0)" in renderer
    assert "Math.Max(0,layers.Count-1)" in renderer
    assert "material.AlbedoTexture=texture" in renderer
    assert "material.AlbedoColor=new Color(" in renderer
    assert "Pass55SourceSkinLayers.TryGetSlot(skin.Id,i+1,out var slot)" in gun
    assert "SourceInstalledTextureParts++" in gun
    assert "SourceProceduralTextureParts++" in gun
    assert "SourceUnrenderedTextureLayers+=layer.UnrenderedLayers" in gun
    assert "PROCEDURAL TEXTURE PROXY - ORIGINAL IMAGE UNAVAILABLE" in gun
    assert "UV MAPPING APPROXIMATE" in gun
    assert "textureId" not in gun.lower() or "Pass55" in gun


def test_animated_effects_are_bounded_reconstructed_and_viewmodel_only():
    renderer=read("src/Twr.Godot/Scripts/Pass55SkinTextureRenderer.cs")
    gun=read("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    lobby=read("src/Twr.Godot/Scripts/Pass51OriginalLoadoutDisplayRuntime.cs")
    assert "ShimmerMultiplier" in renderer
    assert "ScrollOffset" in renderer
    assert "RECONSTRUCTIONS" in renderer
    assert "public void AdvanceCosmeticEffects(float dt)" in gun
    assert "Math.Clamp(dt,0f,.25f)" in gun
    assert "if(!float.IsFinite(dt)" in gun
    assert "EmissionEnergyMultiplier=" in gun
    assert "if(StaticShowroomMode)" in gun
    assert "_leftHandProxy.Visible=false" in gun
    assert "AdvanceCosmeticEffects(dt);" in gun
    assert "model.StaticShowroomMode=true" in lobby
    assert "model.SetProcess(true)" in lobby
    assert "RECONSTRUCTED SHIMMER (TIMING UNKNOWN)" in gun
    assert not re.search(r'\b(?:PurchaseWeapon|GrantAmmo|AwardKill)\s*\(',gun)


def test_fixture_produces_local_png_and_refuses_owner_file_overwrite(tmp_path):
    fixture=ROOT/"tools/skins/make_pass55_smoke_fixture.py"
    skin_dir=tmp_path/"Content/Skins"
    png_dir=tmp_path/"Content/Assets/Textures"
    cmd=[sys.executable,str(fixture),
         "--output-dir",str(skin_dir),"--assets-dir",str(png_dir)]
    subprocess.run(cmd+["--create"],check=True,capture_output=True,text=True)
    pack=skin_dir/"SourceSkinLayers55.json.gz"
    png=png_dir/"5555500000001.png"
    with gzip.open(pack,"rb") as f:
        data=json.load(f)
    assert data["synthetic"] is True
    assert len(data["skins"])==3
    assert data["skins"]["High/Gold"]["slots"][0]["layers"][0]["tileU"]==2
    assert data["skins"]["Ethereal/Spirit"]["slots"][0]["meshTextureId"]=="5555500000002"
    assert png.read_bytes().startswith(b"\x89PNG\r\n\x1a\n")
    assert subprocess.run(cmd+["--create"],capture_output=True).returncode!=0
    subprocess.run(cmd+["--clean"],check=True,capture_output=True,text=True)
    assert not pack.exists() and not png.exists()
    skin_dir.mkdir(parents=True,exist_ok=True)
    with gzip.open(pack,"wb") as file:
        file.write(b'{"synthetic":false,"format":"twr-source-skin-texture-layers-v1"}')
    assert subprocess.run(cmd+["--clean"],capture_output=True).returncode!=0
    assert pack.exists()


def test_pass55_native_ci_uses_all_three_fake_packs_and_no_private_upload():
    action=read(".github/workflows/windows-build.yml")
    app=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    source_profile=read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    assert "twr-pass55-source-skin-textures-and-shimmer" in action
    assert "--smoke-pass55" in action and "--smoke-pass55" in app
    assert "TWR_SMOKE_PASS55_LAYERS_OK" in action
    assert "TWR_SMOKE_PASS55_LAYERS_OK" in app
    assert "make_pass55_smoke_fixture.py --output-dir $skinDir --assets-dir $textureDir --create" in action
    assert "make_pass55_smoke_fixture.py --output-dir $skinDir --assets-dir $textureDir --clean" in action
    assert "make_pass54_smoke_fixture.py --output-dir $skinDir --create" in action
    assert "make_pass49_lobby_smoke_fixture.py --output $sourceLobby --create" in action
    assert "make_source_weapon_smoke_fixture.py --output $sourceTools" in action
    assert "pass55-smoke-profile.json" in source_profile
    assert "5555500000001.png" in action
    assert "SourceSkinLayers55.json.gz" in action
    assert 'Remove-Item -LiteralPath $sourceTools -Force' in action
    assert action.count("if: false && success()")>=14
    assert action.count("uses: actions/upload-artifact@v4")==action.count(
        "if: false && success()"
    )
    assert not (ROOT/"content/Skins/SourceSkinLayers55.json.gz").exists()
