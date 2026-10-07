import pytest
from conftest import ROOT

def test_workflow_pins_python(): assert "python-version: '3.13'" in (ROOT/'.github/workflows/windows-build.yml').read_text()
def test_workflow_pins_dotnet():
 import json
 assert "dotnet-version: '8.0.425'" in (ROOT/'.github/workflows/windows-build.yml').read_text()
 assert json.loads((ROOT/'global.json').read_text())['sdk']['version']=='8.0.425'
def test_build_script_pins_godot():
 script=(ROOT/'build/Windows/build.ps1').read_text()
 assert '4.7.2' in script
 assert '_mono_win64_console.exe' in script
 assert 'Godot Windows export failed with exit code' in script
 assert 'Godot Windows export reported ERROR output.' in script
 assert '<ImplicitUsings>enable</ImplicitUsings>' in (ROOT/'src/Twr.Godot/Twr.Godot.csproj').read_text()

def test_godot_solution_is_tracked_and_references_runtime_projects():
 solution=(ROOT/'src/Twr.Godot/Those Who Remain Offline.sln').read_text()
 assert 'Twr.Godot.csproj' in solution
 assert '..\\Twr.Domain\\Twr.Domain.csproj' in solution
def test_expected_executable_path(): assert 'ThoseWhoRemainOffline.exe' in (ROOT/'src/Twr.Godot/export_presets.cfg').read_text()
def test_project_has_no_network_runtime_api():
 text='\n'.join(p.read_text(errors='replace').lower() for p in (ROOT/'src').rglob('*') if p.is_file() and p.suffix in {'.cs','.godot','.tscn','.cfg','.csproj'})
 for tok in ['httpclient','system.net.http','webrequest','roblox.com','fireserver','invokeserver']: assert tok not in text
def test_content_sync_file_count_and_bytes():
 src=[p for p in (ROOT/'content').rglob('*') if p.is_file()]; dst=ROOT/'src/Twr.Godot/Content'; assert len(src)>300
 for p in src: assert (dst/p.relative_to(ROOT/'content')).read_bytes()==p.read_bytes()


def test_godot_project_declares_csharp_assembly_name():
 text=(ROOT/'src/Twr.Godot/project.godot').read_text()
 assert '[dotnet]' in text
 assert 'project/assembly_name="Twr.Godot"' in text
 assert '"C#"' in text
