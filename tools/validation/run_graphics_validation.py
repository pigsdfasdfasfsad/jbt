"""Real OpenGL importer assertions and rendered screenshots on pinned Godot.

Godot's dummy headless renderer returns identity for MultiMesh readback, so
these unchanged geometry assertions REQUIRE an actual graphics backend.
This Linux graphics verification supplements native Windows gameplay tests.
"""
from __future__ import annotations
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'build/graphics-validation'


def invoke(name: str, command: list[str], timeout: int = 240, marker: str | None = None) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    result = subprocess.run(command, cwd=ROOT, stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT, text=True, encoding='utf8', errors='replace', timeout=timeout)
    (OUT / (name + '.log')).write_text(result.stdout, encoding='utf8')
    print(result.stdout[-12000:], flush=True)
    if result.returncode != 0:
        raise RuntimeError(f'{name} exited {result.returncode}')
    if marker is not None:
        if marker not in result.stdout or re.search(r'(^|\n).*?(ERROR:|Fatal error|AccessViolationException)', result.stdout):
            raise RuntimeError(f'{name}: marker missing or engine reported an error')


def get_engine() -> Path:
    version = re.search(r'Godot.NET.Sdk/([^\"]+)',
        (ROOT / 'src/Twr.Godot/Twr.Godot.csproj').read_text()).group(1)
    asset_name = f'Godot_v{version}-stable_mono_linux_x86_64.zip'
    request = urllib.request.Request(
        f'https://api.github.com/repos/godotengine/godot/releases/tags/{version}-stable',
        headers={'User-Agent':'TWR-build-verification','Accept':'application/vnd.github+json'})
    with urllib.request.urlopen(request, timeout=60) as response:
        release=json.load(response)
    asset=next(x for x in release['assets'] if x['name']==asset_name)
    expected=asset.get('digest','')
    if not re.fullmatch(r'sha256:[0-9a-f]{64}',expected):
        raise RuntimeError('Official release did not provide a valid asset checksum')
    folder=Path(os.environ.get('RUNNER_TEMP','/tmp'))/'twr-pinned-graphics-engine'
    folder.mkdir(parents=True,exist_ok=True)
    archive=folder/asset_name
    urllib.request.urlretrieve(asset['browser_download_url'],archive)
    if hashlib.sha256(archive.read_bytes()).hexdigest()!=expected.split(':')[1]:
        raise RuntimeError('Pinned engine checksum mismatch')
    with zipfile.ZipFile(archive) as z:
        for member in z.infolist():
            destination=(folder/member.filename).resolve()
            if not destination.is_relative_to(folder.resolve()):
                raise RuntimeError('Unsafe engine archive member')
        z.extractall(folder)
    executable=next(p for p in folder.rglob('Godot*') if p.is_file() and p.name.endswith('.x86_64'))
    executable.chmod(0o755)
    print(json.dumps({'engine_version':version,'official_asset':asset_name,'sha256':expected}),flush=True)
    return executable


def main() -> None:
    OUT.mkdir(parents=True,exist_ok=True)
    sha=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
    report={'tested_commit':sha,'passed':0,'failed':0,'platform':'Linux',
        'graphics_backend':'Mesa OpenGL via Xvfb (not headless dummy)',
        'original_private_assets_loaded':False,'original_visual_parity_verified':False,'cases':[]}
    try:
        engine=get_engine()
        invoke('content-build',['python','tools/content/build_content.py'])
        invoke('content-sync',['python','tools/content/sync_godot_content.py'])
        invoke('privacy-audit',['python','tools/build/package_source_safe.py','--check-inputs'])
        invoke('managed-build',['dotnet','build','src/Twr.Godot/Twr.Godot.csproj','-c','Debug'],300)
        project=ROOT/'src/Twr.Godot'
        invoke('resource-import',[str(engine),'--headless','--editor','--import','--path',str(project)],300)
        common=['xvfb-run','-a','-s','-screen 0 1280x720x24',str(engine),
            '--path',str(project),'--rendering-method','gl_compatibility',
            '--rendering-driver','opengl3','--display-driver','x11',
            '--audio-driver','Dummy','--verbose','--max-fps','60','--quit-after','12000','--']
        def case(name, arguments, marker):
            invoke(name, common + arguments, 180, marker)
            report['cases'].append({'name': name, 'exit_code': 0,
                'required_marker': marker, 'marker_seen': True, 'passed': True})
            report['passed'] += 1
        case('source-mesh-real-renderer',['--smoke-source-mesh'],
            'TWR_SOURCE_MESH_PRESENTATION_OK')
        if 'TWR_SOURCE_RENDER_PARTITION_OK' not in (OUT/'source-mesh-real-renderer.log').read_text(encoding='utf8'):
            raise RuntimeError('Spatial source instance/culling assertions did not execute')
        case('actual-gameplay-capture',['--capture-presentation',f'--capture-output={OUT}'],
            'TWR_PRESENTATION_CAPTURE_OK')
        case('guided-load-real-renderer',['--smoke-objective-guidance',
            f'--guidance-capture={OUT}/guided-load-actual-engine.png'],
            'TWR_OBJECTIVE_GUIDANCE_OK')
        case('tanker-lifecycle-real-renderer',['--smoke-source-tanker',
            f'--tanker-capture={OUT}/tanker-actual-engine.png'],
            'TWR_SOURCE_DAMAGE_OK')
        case('held-clearance-real-renderer',['--smoke-held-clearance',
            f'--clearance-captures={OUT}'], 'TWR_HELD_MODEL_CLEARANCE_OK')
        from PIL import Image, ImageStat
        images=[]
        for name in ('laboratory-fallback-first-person.png','deployed-fortifications-engine-render.png',
                     'guided-load-actual-engine.png', 'tanker-actual-engine.png',
                     'held-weapon-clear-of-wall.png', 'held-weapon-near-wall.png',
                     'held-shotgun-near-wall.png', 'held-shotgun-aim-near-wall.png'):
            image=Image.open(OUT/name).convert('RGB')
            if image.width<640 or image.height<360 or max(ImageStat.Stat(image).stddev)<4:
                raise RuntimeError('Empty or blank engine screenshot: '+name)
            images.append({'name':name,'size':list(image.size),
                'sha256':hashlib.sha256((OUT/name).read_bytes()).hexdigest()})
        report['captures']=images
        (OUT/'engine-location.txt').write_text(str(engine),encoding='utf8')
    except Exception as error:
        report['failed']+=1
        report['error']=str(error)
        raise
    finally:
        (OUT/'graphics.json').write_text(json.dumps(report,indent=2),encoding='utf8')
        print(json.dumps(report),flush=True)


if __name__=='__main__':
    main()
