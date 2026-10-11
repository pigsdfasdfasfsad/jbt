"""Pass53: source case pools, offline skin economy and native Windows GUI smoke."""
import re
from pathlib import Path
from conftest import ROOT


def source(path: str) -> str:
    return (ROOT/path).read_text(encoding="utf-8")


def test_source_shop_catalog_exactly_matches_118_inert_skin_entries():
    code=source("src/Twr.Domain/Model/SkinCaseCatalog.cs")
    pattern=r'new\("([^"]+)",(\d+),(true|false),\[(.*?)\],\[(.*?)\]\)'
    found=re.findall(pattern,code,re.S)
    assert len(found)==7
    expected={
        "Low":(6000,20,0,True),
        "Mid":(20000,19,0,True),
        "High":(50000,20,9,True),
        "Neon":(120000,15,0,True),
        "Tactical":(12000,15,0,True),
        "Ethereal":(1000000,15,0,True),
        "Hallows":(200000,0,5,False)
    }
    names=set()
    for case,price,on_sale,ordinary,exclusive in found:
        standard=re.findall(r'"([^"]+)"',ordinary)
        rare=re.findall(r'"([^"]+)"',exclusive)
        assert case in expected
        assert (int(price),len(standard),len(rare),on_sale=="true")==expected[case]
        for name in standard+rare:
            assert name not in names,(case,name)
            names.add(name)
    assert len(names)==118
    for exclusive in ["Christmas 2018","Christmas 2023","Candy Cane","Hexbrew","Specter"]:
        assert exclusive in names
    assert 'new("Hallows",200000,false,[]' in code


def test_authoritative_case_service_prevents_guessed_prices_and_duplicate_rewards():
    s=source("src/Twr.Domain/Services/SkinCaseService.cs")
    assert "RandomNumberGenerator.GetInt32" in s
    assert "Where(name=>!profile.OwnedSkins.Contains" in s
    assert "if(missing.Length==0)" in s
    assert "if(!definition.OnSale)" in s
    assert "if(player.Credits<definition.PriceCredits)" in s
    assert "player.Credits-=definition.PriceCredits" in s
    assert "profile.OwnedSkins.Add(rewardId)" in s
    assert "if(skin.Exclusive)" in s
    assert "var refund=sourceCase.PriceCredits/2" in s
    assert "if(player.Credits>int.MaxValue-refund)" in s
    assert "profile.EquippedWeaponSkins.Remove(name)" in s
    assert "profile.Unlocks.Contains(weaponName)" in s
    assert "uniform" in s.lower()
    assert "Roblox" in s
    # The original source does not document loot weights.
    assert "20%" not in s and "rarity" not in s.lower()


def test_domain_commands_go_through_authoritative_session_with_save_and_rollback():
    commands=source("src/Twr.Domain/Contracts/Commands/SkinCommands.cs")
    domain=source("src/Twr.Domain/Runtime/LocalSession.cs")
    node=source("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    profile=source("src/Twr.Domain/Model/Profile.cs")
    for command in ("OpenSkinCaseCommand","SellOwnedSkinCommand",
                    "ApplyWeaponSkinCommand"):
        assert command in commands and command in domain and command in node
    assert 'OwnedSkins {get;set;}' in profile
    assert 'EquippedWeaponSkins {get;set;}' in profile
    assert 'Profile.OwnedSkins ??=' in domain
    assert 'Profile.EquippedWeaponSkins ??=' in domain
    assert "State.Match.Phase!=MatchPhase.Lobby" in domain
    assert "PersistProfile(\"SkinCaseOpen:" in domain
    assert "PersistProfile(\"SkinSale:" in domain
    assert "PersistProfile(\"WeaponSkin:" in domain
    assert 'catch(Exception)' in domain
    assert 'Profile.Credits=oldCredits' in domain


def test_shop_ui_inventory_resale_and_equipped_skin_visual_proxy():
    boot=source("src/Twr.Godot/Scripts/Bootstrap.cs")
    weapon=source("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    first=source("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    lobby=source("src/Twr.Godot/Scripts/Pass51OriginalLoadoutDisplayRuntime.cs")
    for text in ("ShopOpenSelectedCase","ShopOwnedSkins","ApplySkinPrimary",
                 "ApplySkinSecondary","SellOwnedSkin","ClearSkinPrimary",
                 "ClearSkinSecondary","ShowSkinInventory","TWR_SMOKE_PASS53_SKINS_OK"):
        assert text in boot
    assert "Offline case odds: uniform among missing skins (RECONSTRUCTION)" in boot
    assert "source.OnSale" in boot
    assert "SkinCaseCatalog.Completed(profile,definition)" in boot
    assert "ApplyCosmeticOverlay(" in weapon and "ApplyCosmeticOverlay(" in first
    assert "ApplyCosmeticOverlay(" in lobby
    assert "OriginalSkin" not in weapon or "proxy" in weapon.lower()
    assert "MaterialOverlay" in weapon
    assert "BuildCosmeticProxyTint" in weapon
    assert "VisibleCosmeticProxyParts" in lobby
    assert "OriginalSourceSkinMaterial" not in weapon


def test_native_ci_isolation_and_source_pack_cleanup():
    action=source(".github/workflows/windows-build.yml")
    boot=source("src/Twr.Godot/Scripts/Bootstrap.cs")
    assert "twr-pass53-offline-skin-cases" in action
    assert "--smoke-pass53" in boot and "--smoke-pass53" in action
    assert "TWR_SMOKE_PASS53_SKINS_OK" in action
    for path in ("Pass49OriginalLobbyRuntime.cs","Pass50SourceLobbySignsRuntime.cs",
                 "OriginalWeaponSourceRuntime.cs"):
        assert "--smoke-pass53" in source("src/Twr.Godot/Scripts/"+path)
    assert "make_pass49_lobby_smoke_fixture.py --output $sourceLobby --create" in action
    assert "make_pass50_bulletin_smoke_fixture.py --output $sourceSigns --create" in action
    assert "make_source_weapon_smoke_fixture.py --output $sourceTools" in action
    assert "Remove-Item -LiteralPath $sourceTools -Force" in action
    assert "make_pass49_lobby_smoke_fixture.py --output $sourceLobby --clean" in action
    assert "make_pass50_bulletin_smoke_fixture.py --output $sourceSigns --clean" in action
    assert action.count("if: false && success()") >= 14
    assert action.count("uses: actions/upload-artifact@v4")==action.count(
        "if: false && success()")
