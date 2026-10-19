"""Move graphics assertions to a real renderer without weakening assertions.

Godot RendererDummy::_multimesh_instance_get_transform returns identity;
source geometry cannot be verified through that backend's readback API.
The identical source-import dimensions/shapes/collision assertions now run
under pinned Godot with Mesa OpenGL. Windows still executes all gameplay,
readiness, equipped/deployed interactions, exit and inherited tests.
"""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]

def replace(path,old,new):
    file=ROOT/path
    text=file.read_text(encoding='utf8')
    if new in text:return
    # Later traversal acceptance extends (not replaces) these native gates.
    # Recognize the complete known successor so reruns remain idempotentent.
    if path.endswith('run_source_fidelity.py'):
        extended = new[:-1] + ',\n             ("--smoke-source-traversal", "TWR_SMOKE_SOURCE_TRAVERSAL_OK")]'
        if extended in text:return
    if path.endswith('package_source_safe.py'):
        extended = new.replace('targeted.json", commit, 2)', 'targeted.json", commit, 3)')
        if extended in text:return
    if text.count(old)!=1:raise RuntimeError('Inspected integration anchor changed: '+path)
    file.write_text(text.replace(old,new),encoding='utf8',newline='\n')

def apply():
    replace('src/Twr.Godot/Scripts/SourceMeshPresentationAcceptance.cs',
        '    private async Task Verify()\n    {',
        '    private async Task Verify()\n    {\n'
        '        if (DisplayServer.GetName() == "headless")\n'
        '            throw new InvalidOperationException("Mesh instance readback requires a real renderer, not Godot dummy rendering");')
    replace('tools/validation/run_source_fidelity.py',
        'tests = [("--smoke-source-mesh", "TWR_SOURCE_MESH_PRESENTATION_OK"),\n'
        '             ("--smoke-runtime-readiness", "TWR_SMOKE_RUNTIME_READINESS_OK")]',
        '# MultiMesh visual assertions run unchanged in the real-renderer job.\n'
        '    tests = [("--smoke-runtime-readiness", "TWR_SMOKE_RUNTIME_READINESS_OK"),\n'
        '             ("--smoke-pass76", "TWR_SMOKE_PASS76_OBJECTIVE_FORTIFICATION_OK")]')
    replace('tools/build/package_source_safe.py',
        '        "targeted": verify_receipt(ROOT / "build/source-fidelity-validation/targeted.json", commit, 2),',
        '        "targeted": verify_receipt(ROOT / "build/source-fidelity-validation/targeted.json", commit, 2),\n'
        '        "graphics": verify_receipt(ROOT / "build/graphics-validation/graphics.json", commit, 2),')

if __name__=='__main__':apply()
