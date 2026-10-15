"""Retain public-only Linux QA tools, compiled game and bundled .NET runtime.

This is an engineering harness, not the Windows release. A fresh public CI
checkout contains no owner's source assets. Preserves dependency licenses.
"""
from pathlib import Path
import json
import os
import shutil
import subprocess
import zipfile

ROOT=Path(__file__).resolve().parents[2]

def main():
    engine=Path((ROOT/'build/graphics-validation/engine-location.txt').read_text().strip())
    dotnet=Path(shutil.which('dotnet')).resolve().parent
    versions=sorted((dotnet/'shared/Microsoft.NETCore.App').glob('8.*'),
        key=lambda p:tuple(int(x) for x in p.name.split('.')))
    if not versions:raise RuntimeError('Expected bundled .NET 8 runtime')
    runtime=versions[-1]
    host=dotnet/'host/fxr'/runtime.name
    if not host.is_dir():raise RuntimeError('Matching .NET hostfxr is unavailable')
    destination=ROOT/'build/qa-harness'
    destination.mkdir(parents=True,exist_ok=True)
    archive=destination/'TWR-Linux-QA-Harness.zip'
    files=[]
    for p in engine.parent.rglob('*'):
        if p.is_file() and p.suffix.lower() not in {'.zip','.ttf','.otf','.woff','.woff2'}:
            files.append((p,'engine/'+p.relative_to(engine.parent).as_posix()))
    project=ROOT/'src/Twr.Godot'
    for p in project.rglob('*'):
        if p.is_file() and p.suffix.lower() not in {'.ttf','.otf','.woff','.woff2'}:
            files.append((p,'project/'+p.relative_to(project).as_posix()))
    for folder in (runtime,host):
        for p in folder.rglob('*'):
            if p.is_file():files.append((p,'dotnet/'+p.relative_to(dotnet).as_posix()))
    for name in ('dotnet','LICENSE.txt','ThirdPartyNotices.txt'):
        p=dotnet/name
        if p.is_file():files.append((p,'dotnet/'+name))
    with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=1) as z:
        for path,member in files:z.write(path,member)
        z.writestr('HARNESS.json',json.dumps({
            'tested_commit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),
            'engine':'engine/'+engine.name,'project':'project','dotnet_runtime':runtime.name,
            'owner_assets_included':False,'windows_release':False}))
    print(str(archive),archive.stat().st_size,flush=True)

if __name__=='__main__':main()
