"""Pass 51: source-anchored showroom, independent of purchase/gameplay authority."""
from __future__ import annotations

import re
from pathlib import Path

from conftest import ROOT


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def test_source_loadout_anchor_set_is_exact_and_source_bound():
    stage = read("src/Twr.Godot/Scripts/Pass51OriginalLoadoutDisplayRuntime.cs")
    lobby = read("src/Twr.Godot/Scripts/Pass49OriginalLobbyRuntime.cs")
    assert '["Primary", "Secondary", "Melee", "Utility", "View"]' in stage
    assert '["Primary", "Secondary", "Melee", "Utility"]' in stage
    assert 'lobby.TryGetLoadoutPoint(slot, out var frame)' in stage
    assert 'Transform = frame' in stage
    assert 'OwnerLobbyVerified = lobby.OwnerSourceVerified' in stage
    assert 'Pass51OriginalLoadoutDisplayRuntime.TryBuild(stage)' in lobby
    assert 'public bool TryGetLoadoutPoint(string name, out Transform3D frame)' in lobby
    assert 'SourceLoadoutPointCount' in lobby
    assert 'OriginalLoadoutAnchor_' in stage


def test_preview_is_model_only_and_provenance_is_never_hidden():
    stage = read("src/Twr.Godot/Scripts/Pass51OriginalLoadoutDisplayRuntime.cs")
    assert 'OriginalWeaponSourceRuntime.OwnerSourcePackVerified' in stage
    assert 'RECOVERED ORIGINAL TOOL ASSEMBLY' in stage
    assert 'SYNTHETIC SOURCE FIXTURE (TEST ONLY)' in stage
    assert 'APPROXIMATE WEAPON SHAPE - SOURCE MESH NOT INSTALLED' in stage
    assert 'LOCALLY PREPARED WEAPON MODEL' in stage
    assert 'UNRESOLVED SOURCE MESHES' in stage
    assert 'public bool Preview(string? weaponName)' in stage
    assert 'RuntimeWeaponCatalog.All().FirstOrDefault' in stage
    assert 'model.SetWeapon(spec)' in stage
    assert 'model.SetProcess(false)' in stage
    assert 'public void HideDisplay() => Visible = false' in stage
    assert 'spec.IsMelee' not in stage
    # No player economy, unlocks or ammunition may be mutated by a hover.
    assert not re.search(r'\b(?:PurchaseWeapon|SetLoadout|GrantAmmo|SpendAmmo|AwardKill)\s*\(', stage)
    assert 'SourceMissingMeshProxies' in stage


def test_armory_shows_existing_ninety_one_rows_and_updates_preview_by_hover():
    boot = read("src/Twr.Godot/Scripts/Bootstrap.cs")
    assert 'var showroom=_sourceLobby?.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>' in boot
    assert 'showroom?.ShowForProfile(_runtime.Profile)' in boot
    assert 'button.MouseEntered += () =>' in boot
    assert 'showroom?.Preview(selected.Name)' in boot
    assert 'showroom.PreviewStatus' in boot
    assert 'button.Pressed += () => HandleArmoryWeapon(selected)' in boot
    assert 'foreach (var spec in RuntimeWeaponCatalog.All())' in boot
    assert 'stage.VisibleWeaponModels < 1' in boot
    assert 'rows.Length != 91' in boot
    assert 'stage.Preview("UNAVAILABLE_51")' in boot
    assert 'preview_does_not_purchase=true' in boot
    assert 'TWR_SMOKE_PASS51_LOADOUT_OK' in boot
    assert 'OriginalLoadoutAnchor_View' in boot


def test_source_lobby_is_removed_from_board_perk_and_match():
    boot = read("src/Twr.Godot/Scripts/Bootstrap.cs")
    assert boot.count('Pass51OriginalLoadoutDisplayRuntime.StageName') >= 4
    assert 'showroom?.ShowForProfile(_runtime.Profile)' in boot
    assert 'HideDisplay();' in boot
    assert 'lobby.ActiveCameraName != "Leaderboards"' in boot
    assert 'StartGame("Manor")' in boot
    assert 'match_scene_has_no_lobby=true' in boot
    assert 'StartGame' in boot and '_sourceLobby?.QueueFree();' in boot


def test_windows_action_runs_native_preview_smoke_and_does_not_ship_private_assets():
    action = read(".github/workflows/windows-build.yml")
    weapon = read("src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs")
    lobby = read("src/Twr.Godot/Scripts/Pass49OriginalLobbyRuntime.cs")
    boards = read("src/Twr.Godot/Scripts/Pass50SourceLobbySignsRuntime.cs")
    assert 'twr-pass51-original-loadout-preview' in action
    assert 'make_source_weapon_smoke_fixture.py --output $sourceTools' in action
    assert 'make_pass49_lobby_smoke_fixture.py --output $sourceLobby --create' in action
    assert 'make_pass50_bulletin_smoke_fixture.py --output $sourceSigns --create' in action
    assert '--smoke-pass51' in action and '--smoke-pass51' in weapon
    assert '--smoke-pass51' in lobby and '--smoke-pass51' in boards
    assert 'TWR_SMOKE_PASS51_LOADOUT_OK' in action
    assert 'Remove-Item -LiteralPath $sourceTools -Force' in action
    assert 'make_pass49_lobby_smoke_fixture.py --output $sourceLobby --clean' in action
    assert 'make_pass50_bulletin_smoke_fixture.py --output $sourceSigns --clean' in action
    assert 'Refusing' not in action or 'refuses to overwrite' in action
    assert 'TWR-Pass51-Windows-x64-NoPrivateAssets' not in action
    # Do not publish even synthetic Windows executable artifacts from public CI.
    assert action.count('if: false && success()') >= 14
    assert action.count('uses: actions/upload-artifact@v4') == action.count(
        'if: false && success()')
