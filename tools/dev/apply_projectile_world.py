"""Integrate reviewed projectile/rendering changes without original asset bytes.

Both compressed and unpacked digests, exact source paths, and git apply checks
prevent stale or corrupted patches from overwriting unrelated development.
"""
from pathlib import Path
import gzip
import hashlib
import io
import json
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
DIGEST = 'c893fcd9c6164a2175b3430579797a437c367fe5ce6143992fbf29cc0c19f57a'
COMPRESSED_DIGEST = '4d63fd8abfeeb640570800031fd159337af82865a152496b2751eed4a9534e67'
COMPRESSED_BYTES = 15014
PATCH_BYTES = 53802
PAYLOADS = [f"tools/dev/projectile_world/part{i:02}.gz" for i in range(1, 5)]
MARKER = "tools/dev/projectile_world/integrated.json"
ALLOWED = {
    'src/Twr.Godot/Scripts/Bootstrap.cs',
    'src/Twr.Godot/Scripts/ExplosiveProjectileRuntime.cs',
    'src/Twr.Godot/Scripts/FirstPersonPlayer.cs',
    'src/Twr.Godot/Scripts/LaboratorySourceLoader.cs',
    'src/Twr.Godot/Scripts/LocalSessionNode.cs',
    'src/Twr.Godot/Scripts/ProjectileCollisionAcceptance.cs',
    'src/Twr.Godot/Scripts/ProjectileSweep.cs',
    'src/Twr.Godot/Scripts/RuntimeShutdownCoordinator.cs',
    'src/Twr.Godot/Scripts/SourceMeshPresentationAcceptance.cs',
    'src/Twr.Godot/Scripts/SourceRenderPartition.cs',
    'src/Twr.Godot/Scripts/SourceRenderPartitionAcceptance.cs',
    'src/Twr.Godot/Scripts/ThrowableProjectileRuntime.cs',
    'tests/test_pass75_weapon_blast.py',
    'tests/test_projectile_world_integration.py',
    'tests/test_tanker_audio_completion.py',
    'tools/build/package_source_safe.py',
    'tools/dev/write_release_handoff.py',
    'tools/validation/run_graphics_validation.py',
    'tools/validation/run_source_fidelity.py',
}


def apply(root: Path = ROOT) -> None:
    payload_paths = [root / name for name in PAYLOADS]
    if any(path.is_symlink() or not path.is_file() for path in payload_paths):
        raise ValueError("Missing or linked projectile/world payload")
    payload = b"".join(path.read_bytes() for path in payload_paths)
    if len(payload) != COMPRESSED_BYTES or hashlib.sha256(payload).hexdigest() != COMPRESSED_DIGEST:
        raise ValueError("Compressed projectile/world payload digest mismatch")
    with gzip.GzipFile(fileobj=io.BytesIO(payload)) as source:
        patch = source.read(PATCH_BYTES + 1)
    if len(patch) != PATCH_BYTES or hashlib.sha256(patch).hexdigest() != DIGEST:
        raise ValueError("Reviewed projectile/world patch digest mismatch")
    entries = re.findall(rb"^diff --git a/(.+) b/(.+)$", patch, re.M)
    if len(entries) != len(ALLOWED) or any(a != b for a, b in entries):
        raise ValueError("Unexpected source rename or repeated file in patch")
    if {a.decode("utf8") for a, b in entries} != ALLOWED:
        raise ValueError("Unreviewed file in projectile/world patch")
    for name in ALLOWED | {MARKER}:
        target = root / name
        for part in [target, *target.parents]:
            if part == root:
                break
            if part.is_symlink():
                raise ValueError("Linked integration destination")
    marker = root / MARKER
    if marker.exists():
        if json.loads(marker.read_text(encoding="utf8")).get("patch_sha256") != DIGEST:
            raise ValueError("Invalid projectile/world integration marker")
        print("PROJECTILE_WORLD_ALREADY_INTEGRATED source_only=true")
        return
    reverse = subprocess.run(["git", "apply", "--reverse", "--check", "-"],
        input=patch, cwd=root, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if reverse.returncode != 0:
        subprocess.run(["git", "apply", "--check", "-"], input=patch, cwd=root, check=True)
        subprocess.run(["git", "apply", "-"], input=patch, cwd=root, check=True)
    marker.parent.mkdir(parents=True, exist_ok=True)
    marker.write_text(json.dumps({"patch_sha256": DIGEST,
        "source_baseline": "88b436d9fcb364672ce46f5e4854435f7ac9a064",
        "private_assets_included": False}, indent=2) + "\n", encoding="utf8")
    print("PROJECTILE_WORLD_APPLIED source_only=true")


if __name__ == "__main__":
    apply()
