"""Apply the reviewed tanker/audio source update without private asset files.

Digests, exact allowed paths and git's forward/reverse checks prevent stale
source from being overwritten. A marker permits later coherent development.
"""
from pathlib import Path
import gzip
import hashlib
import io
import json
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
DIGEST = '901d2b2becb9eb048c59fea67183f7051b0adadf288a32fee3e8598200979dd4'
COMPRESSED_DIGEST = 'fb1159b0f30e2db2f739bfb4d4d7cc26fb7447a4715b897e13a3511f61c37bb7'
COMPRESSED_BYTES = 15514
PATCH_BYTES = 54891
PAYLOADS = [f"tools/dev/tanker_audio/part{i:02}.gz" for i in range(1,5)]
MARKER = "tools/dev/tanker_audio/integrated.json"
ALLOWED = {
    'src/Twr.Godot/Scripts/AudioLifecycleAcceptance.cs',
    'src/Twr.Godot/Scripts/Bootstrap.cs',
    'src/Twr.Godot/Scripts/DamageObjectiveTarget.cs',
    'src/Twr.Godot/Scripts/FirstPersonPlayer.cs',
    'src/Twr.Godot/Scripts/GameplayRoot.cs',
    'src/Twr.Godot/Scripts/LocalSessionNode.cs',
    'src/Twr.Godot/Scripts/ObjectiveGuidanceTargets.cs',
    'src/Twr.Godot/Scripts/ObjectiveRuntime.cs',
    'src/Twr.Godot/Scripts/OfflineAudioRuntime.cs',
    'src/Twr.Godot/Scripts/Pass38SourceSoundscapeRuntime.cs',
    'src/Twr.Godot/Scripts/ReconstructedTankerVisual.cs',
    'src/Twr.Godot/Scripts/SourceDamageAcceptance.cs',
    'src/Twr.Godot/Scripts/SourceDamageLayout.cs',
    'tests/test_objective_playability_release.py',
    'tests/test_tanker_audio_completion.py',
    'tools/build/package_source_safe.py',
    'tools/dev/write_release_handoff.py',
    'tools/validation/recover_source_client_walls34.py',
    'tools/validation/run_graphics_validation.py',
    'tools/validation/run_source_fidelity.py',
}


def apply(root: Path = ROOT) -> None:
    payload_paths = [root / name for name in PAYLOADS]
    if any(path.is_symlink() or not path.is_file() for path in payload_paths):
        raise ValueError("Missing or linked reviewed tanker/audio payload")
    payload = b"".join(path.read_bytes() for path in payload_paths)
    if len(payload) != COMPRESSED_BYTES or hashlib.sha256(payload).hexdigest() != COMPRESSED_DIGEST:
        raise ValueError("Compressed tanker/audio payload digest mismatch")
    with gzip.GzipFile(fileobj=io.BytesIO(payload)) as source:
        patch = source.read(PATCH_BYTES + 1)
    if len(patch) != PATCH_BYTES or hashlib.sha256(patch).hexdigest() != DIGEST:
        raise ValueError("Reviewed tanker/audio patch digest mismatch")
    entries = re.findall(rb"^diff --git a/(.+) b/(.+)$", patch, re.M)
    if len(entries) != len(ALLOWED) or any(a != b for a,b in entries):
        raise ValueError("Unexpected source rename or repeated file in patch")
    if {a.decode("utf8") for a,b in entries} != ALLOWED:
        raise ValueError("Unreviewed file in tanker/audio patch")
    for name in ALLOWED | {MARKER}:
        target = root / name
        for part in [target, *target.parents]:
            if part == root: break
            if part.is_symlink():
                raise ValueError("Linked integration destination")
    marker = root / MARKER
    if marker.exists():
        if json.loads(marker.read_text(encoding="utf8")).get("patch_sha256") != DIGEST:
            raise ValueError("Invalid tanker/audio integration marker")
        print("TANKER_AUDIO_ALREADY_INTEGRATED source_only=true")
        return
    reverse = subprocess.run(["git","apply","--reverse","--check","-"],
        input=patch,cwd=root,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if reverse.returncode != 0:
        subprocess.run(["git","apply","--check","-"],input=patch,cwd=root,check=True)
        subprocess.run(["git","apply","-"],input=patch,cwd=root,check=True)
    marker.parent.mkdir(parents=True,exist_ok=True)
    marker.write_text(json.dumps({"patch_sha256":DIGEST,
        "source_baseline":"da06ce99e5e6f4a4ad11e7675ba822895958c97d",
        "private_assets_included":False},indent=2)+"\n",encoding="utf8")
    print("TANKER_AUDIO_APPLIED source_only=true")


if __name__ == "__main__":
    apply()
