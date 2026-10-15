"""Retain public-only Linux QA tools, compiled game and optional compiler.

This is an engineering harness, not the Windows release. A fresh public CI
checkout contains no owner's source assets. Preserves dependency licenses.
The compiler option enables offline iteration without downloading packages.
"""
from pathlib import Path
import argparse
import json
import shutil
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[2]
FONT_SUFFIXES = {'.ttf', '.otf', '.woff', '.woff2', '.ttc', '.eot'}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--include-sdk', action='store_true')
    args = parser.parse_args()
    engine = Path((ROOT/'build/graphics-validation/engine-location.txt').read_text().strip())
    dotnet = Path(shutil.which('dotnet')).resolve().parent
    versions = sorted((dotnet/'shared/Microsoft.NETCore.App').glob('8.*'),
        key=lambda p: tuple(int(x) for x in p.name.split('.')))
    if not versions:
        raise RuntimeError('Expected bundled .NET 8 runtime')
    runtime = versions[-1]
    host = dotnet/'host/fxr'/runtime.name
    if not host.is_dir():
        raise RuntimeError('Matching .NET hostfxr is unavailable')
    destination = ROOT/'build/qa-harness'
    destination.mkdir(parents=True, exist_ok=True)
    archive = destination/'TWR-Linux-QA-Harness.zip'
    files = {}

    def add_tree(folder, prefix):
        for path in folder.rglob('*'):
            if path.is_symlink():
                continue
            if path.is_file() and path.suffix.lower() not in FONT_SUFFIXES:
                files[prefix + path.relative_to(folder).as_posix()] = path

    add_tree(engine.parent, 'engine/')
    files = {name: path for name, path in files.items() if path.suffix.lower() != '.zip'}
    add_tree(ROOT/'src/Twr.Godot', 'project/')
    add_tree(ROOT/'src/Twr.Domain', 'Twr.Domain/')
    for folder in (runtime, host):
        add_tree(folder, 'dotnet/'+folder.relative_to(dotnet).as_posix()+'/')
    for name in ('dotnet', 'LICENSE.txt', 'ThirdPartyNotices.txt'):
        path = dotnet/name
        if path.is_file():
            files['dotnet/'+name] = path
    sdk_version = None
    if args.include_sdk:
        sdk_version = subprocess.check_output(
            [str(dotnet/'dotnet'), '--version'], cwd=ROOT, text=True).strip()
        sdk = dotnet/'sdk'/sdk_version
        if not sdk.is_dir() or not sdk_version.startswith('8.'):
            raise RuntimeError('Pinned .NET 8 SDK was not available for QA')
        add_tree(sdk, 'dotnet/sdk/'+sdk_version+'/')
        add_tree(dotnet/'packs', 'dotnet/packs/')
        # Only resolved packages from the clean CI checkout, not credentials
        # or NuGet configuration; package licenses are retained.
        packages = Path.home()/'.nuget/packages'
        if not packages.is_dir():
            raise RuntimeError('Offline dependency cache is missing')
        add_tree(packages, 'nuget/')
    with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED, compresslevel=1) as z:
        for member, path in sorted(files.items()):
            z.write(path, member)
        z.writestr('HARNESS.json', json.dumps({
            'tested_commit': subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),
            'engine': 'engine/'+engine.name,
            'project': 'project',
            'dotnet_runtime': runtime.name,
            'dotnet_sdk': sdk_version,
            'offline_nuget_cache': bool(args.include_sdk),
            'owner_assets_included': False,
            'windows_release': False,
        }))
        if args.include_sdk:
            z.writestr('NuGet.Config', '<configuration><packageSources><clear /></packageSources></configuration>')
    print(str(archive), archive.stat().st_size, flush=True)


if __name__ == '__main__':
    main()
