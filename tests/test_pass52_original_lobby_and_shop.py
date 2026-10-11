"""Pass52: recovered LoadoutOffset transforms, source camera and read-only cases."""
from __future__ import annotations

import json
import re
from pathlib import Path

from conftest import ROOT


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def test_all_original_source_cframe_offsets_are_parsed_from_inert_catalog():
    catalog = json.loads(read("content/weapons/catalog.json"))
    values = {}
    half_turn_names = set()
    for item in catalog["weapons"]:
        doc = json.loads(read("content/weapons/" + item["path"]))
        field = doc["stats"].get("LoadoutOffset", {})
        expression = field.get("expression", "")
        if not expression:
            continue
        # The authored RPG launchers add exactly this 180-degree Y turn
        # to their otherwise literal CFrame. No Luau needs to execute.
        suffix = " * CFrame.Angles(0, math.pi, 0)"
        if expression.endswith(suffix):
            expression = expression[:-len(suffix)]
            half_turn_names.add(item["name"])
        assert re.fullmatch(
            r"CFrame[.]new[(][0-9eE+., -]+[)]", expression
        ), (item["name"], expression)
        parts = [float(part.strip())
                 for part in expression[len("CFrame.new("):-1].split(",")]
        assert len(parts) == 12
        assert all(abs(number) <= 1.1001 for number in parts[3:])
        values[doc["name"]] = parts
    assert 75 <= len(values) <= 91
    assert half_turn_names == {"Festive RPG-7", "RPG-7"}
    glock = values["Glock 17"]
    assert abs(glock[0] - .0393884182) < .000001
    assert abs(glock[1] - .0704264641) < .000001
    assert abs(glock[2] - .163572311) < .000001


def test_godot_source_offset_parser_is_bounded_and_does_not_execute_luau():
    source = read("src/Twr.Godot/Scripts/Pass52SourceLoadoutOffsets.cs")
    assert 'SourceOffsetCount' in source
    assert 'GetProperty("LoadoutOffset"' in source
    assert 'GetProperty("weapons").EnumerateArray()' in source or (
        '.GetProperty("weapons").EnumerateArray()' in source)
    assert 'TryParse(string? expression, out Transform3D frame)' in source
    assert 'expression.Length > 384' in source
    assert 'HalfTurn = " * CFrame.Angles(0, math.pi, 0)"' in source
    assert 'basis *= new Basis(Vector3.Up, Mathf.Pi)' in source
    assert 'values.Length != 12' in source
    assert 'float.IsFinite' in source
    assert 'Math.Abs(basis.Determinant()) < .3f' in source
    assert 'new Vector3(n[0], n[1], -n[2]) * Stud' in source
    assert 'Eval(' not in source and 'Process.Start' not in source


def test_source_lobby_uses_three_second_50_fov_camera_with_deterministic_easing():
    source = read("src/Twr.Godot/Scripts/Pass49OriginalLobbyRuntime.cs")
    assert "public const float OriginalCameraMoveSeconds = 3f" in source
    assert "public const float OriginalLobbyFovDegrees = 50f" in source
    assert "public void AdvanceCameraTransition(float elapsedSeconds)" in source
    assert "MathF.Pow(2f,-10f*t)" in source
    assert "_cameraFrom.InterpolateWith(_cameraTo,eased)" in source
    assert "public bool IsCameraAtSourceAnchor" in source
    assert "if (ActiveCameraName.Length == 0)" in source
    assert 'SourceLoadoutPointCount' in source
    assert 'SourceCameraCount' in source


def test_showroom_applies_original_offset_and_bounded_mouse_pitch():
    stage = read("src/Twr.Godot/Scripts/Pass51OriginalLoadoutDisplayRuntime.cs")
    boot = read("src/Twr.Godot/Scripts/Bootstrap.cs")
    assert "Pass52SourceLoadoutOffsets.TryGet(spec.Name, out var sourceOffset)" in stage
    assert "model.Transform = sourceOffset;" in stage
    assert 'slot != "View"' in stage
    assert "public bool RotatePreview(float deltaX, float deltaY)" in stage
    assert "Math.Clamp(deltaY, -600f, 600f)" in stage
    assert "Math.Clamp(PreviewPitchDegrees +" in stage
    assert "-25f, 25f" in stage
    assert "Input.IsMouseButtonPressed(MouseButton.Right)" in boot
    assert "preview?.RotatePreview(motion.Relative.X,motion.Relative.Y)" in boot


def test_original_case_prices_are_display_only_and_do_not_mutate_profile():
    catalog = read("src/Twr.Godot/Scripts/Pass52SourceShopCatalog.cs")
    boot = read("src/Twr.Godot/Scripts/Bootstrap.cs")
    prices = {
        "Low": 6000, "Mid": 20000, "High": 50000,
        "Neon": 120000, "Tactical": 12000,
        "Ethereal": 1000000, "Hallows": 200000
    }
    assert len(re.findall(r'new[("]',catalog)) >= 7
    for name, price in prices.items():
        assert f'new("{name}",{price})' in catalog
    assert "4df17a84418b830e3a26f9246057a38b08234597344f62d2335a6a661e98e9db" in catalog
    assert "SHOP / CASES" in boot
    assert 'EnsureOriginalLobby("Shop")' in boot
    assert "Pass52SourceShopCatalog.All" in boot
    assert "not implemented offline" in boot
    assert "case_purchase_disabled=true" in boot
    assert "read_only_credit_balance=true" in boot
    assert not re.search(r'\b(?:PurchaseWeapon|Purchase|GrantItem|GrantSkin|Charge)\s*[(]',catalog)


def test_exported_windows_smoke_uses_only_fabricated_lobby_and_tools():
    workflow = read(".github/workflows/windows-build.yml")
    boot = read("src/Twr.Godot/Scripts/Bootstrap.cs")
    for path in (
        "Pass49OriginalLobbyRuntime.cs",
        "Pass50SourceLobbySignsRuntime.cs",
        "OriginalWeaponSourceRuntime.cs"
    ):
        source = read("src/Twr.Godot/Scripts/" + path)
        assert '--smoke-pass52' in source
    assert "twr-pass52-source-loadout-and-shop" in workflow
    assert "--smoke-pass52" in workflow and "--smoke-pass52" in boot
    assert "TWR_SMOKE_PASS52_LOBBY_SHOP_OK" in workflow
    assert "TWR_SMOKE_PASS52_LOBBY_SHOP_OK" in boot
    assert "make_pass49_lobby_smoke_fixture.py --output $sourceLobby --create" in workflow
    assert "make_pass50_bulletin_smoke_fixture.py --output $sourceSigns --create" in workflow
    assert "make_source_weapon_smoke_fixture.py --output $sourceTools" in workflow
    assert "Remove-Item -LiteralPath $sourceTools -Force" in workflow
    assert "make_pass49_lobby_smoke_fixture.py --output $sourceLobby --clean" in workflow
    assert "make_pass50_bulletin_smoke_fixture.py --output $sourceSigns --clean" in workflow
    assert workflow.count("if: false && success()") >= 14
    assert workflow.count("uses: actions/upload-artifact@v4") == workflow.count(
        "if: false && success()"
    )
