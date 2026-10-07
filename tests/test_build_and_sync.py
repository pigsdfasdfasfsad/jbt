import pytest
from conftest import ROOT

def test_workflow_pins_python(): assert "python-version: '3.13'" in (ROOT/'.github/workflows/windows-build.yml').read_text()
def test_workflow_pins_dotnet(): assert "dotnet-version: '8.0.425'" in (ROOT/'.github/workflows/windows-build.yml').read_text()
def test_build_script_pins_godot(): assert '4.7.2' in (ROOT/'build/Windows/build.ps1').read_text()
def test_expected_executable_path(): assert 'ThoseWhoRemainOffline.exe' in (ROOT/'src/Twr.Godot/export_presets.cfg').read_text()
def test_project_has_no_network_runtime_api():
 text='\n'.join(p.read_text(errors='replace').lower() for p in (ROOT/'src').rglob('*') if p.is_file() and p.suffix in {'.cs','.godot','.tscn','.cfg','.csproj'})
 for tok in ['httpclient','system.net.http','webrequest','roblox.com','fireserver','invokeserver']: assert tok not in text
def test_content_sync_file_count_and_bytes():
 src=[p for p in (ROOT/'content').rglob('*') if p.is_file()]; dst=ROOT/'src/Twr.Godot/Content'; assert len(src)>300
 for p in src: assert (dst/p.relative_to(ROOT/'content')).read_bytes()==p.read_bytes()
