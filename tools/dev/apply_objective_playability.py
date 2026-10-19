"""Integrate the reviewed source-only objective/playability patch once.

No original map, texture, model, credentials or profile is in the payload.
The digest and exact path set must match before any source file is changed.
"""
from pathlib import Path
import gzip
import hashlib
import io
import json
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
DIGEST = "1eed37dec7a8e6e24f95c8350296475f3aed86ea62c7b824f3de72a4f457aa81"
GZIP_DIGEST = "def13f252b26bfa88417c906e0b310a6087ab63a2168d5fdf75b41086f638d2c"
MARKER = "tools/dev/objective_playability/integrated.json"
ALLOWED = {
    ".gitignore", "README.md",
    "src/Twr.Godot/Scripts/AcceptanceKeyboardDriver.cs",
    "src/Twr.Godot/Scripts/BolterGroundingAcceptance.cs",
    "src/Twr.Godot/Scripts/Bootstrap.cs",
    "src/Twr.Godot/Scripts/GameplayRoot.cs",
    "src/Twr.Godot/Scripts/InfectedAgent.cs",
    "src/Twr.Godot/Scripts/LoadStagingVisual.cs",
    "src/Twr.Godot/Scripts/LocalSessionNode.cs",
    "src/Twr.Godot/Scripts/ObjectiveGuidanceAcceptance.cs",
    "src/Twr.Godot/Scripts/ObjectiveGuidanceTargets.cs",
    "src/Twr.Godot/Scripts/ObjectiveRouteGuidance.cs",
    "src/Twr.Godot/Scripts/ObjectiveRuntime.cs",
    "src/Twr.Godot/Scripts/PrivateSourceTraversalAcceptance.cs",
    "src/Twr.Godot/Scripts/SourceEscortAcceptance.cs",
    "src/Twr.Godot/Scripts/SourceEscortFollower.cs",
    "src/Twr.Godot/Scripts/SourceEscortLayout.cs",
    "src/Twr.Godot/Scripts/SourceTraversalRoutes.cs",
    "tests/test_content_sync_preservation.py",
    "tests/test_objective_playability_release.py",
    "tools/build/package_source_safe.py",
    "tools/content/sync_godot_content.py",
    "tools/dev/write_release_handoff.py",
    "tools/validation/run_graphics_validation.py",
    "tools/validation/run_source_fidelity.py",
}


def apply(root: Path = ROOT) -> None:
    folder = root / "tools/dev/objective_playability"
    parts = [folder / f"part{i:02}.gz" for i in range(1, 10)]
    if any(p.is_symlink() or not p.is_file() for p in parts):
        raise ValueError("Missing or linked objective source payload")
    payload = b"".join(p.read_bytes() for p in parts)
    if len(payload) != 24398 or hashlib.sha256(payload).hexdigest() != GZIP_DIGEST:
        raise ValueError("Objective compressed source digest mismatch")
    with gzip.GzipFile(fileobj=io.BytesIO(payload)) as stream:
        patch = stream.read(2_000_001)
    if len(patch) > 2_000_000 or hashlib.sha256(patch).hexdigest() != DIGEST:
        raise ValueError("Objective source patch digest mismatch")
    paths = re.findall(rb"^diff --git a/(.+) b/(.+)$", patch, re.M)
    if not paths or any(a != b for a, b in paths):
        raise ValueError("Unexpected source rename in objective patch")
    if {a.decode("utf8") for a, b in paths} != ALLOWED:
        raise ValueError("Unexpected objective patch file set")
    marker = root / MARKER
    if marker.exists():
        if marker.is_symlink() or json.loads(marker.read_text())["patch_sha256"] != DIGEST:
            raise ValueError("Invalid objective integration marker")
        print("OBJECTIVE_PLAYABILITY_PATCH_ALREADY_INTEGRATED")
        return
    # Also tolerate a previously applied payload that did not yet save its
    # marker. Do not replay old hunks over unrelated newer development.
    reverse = subprocess.run(["git", "apply", "--reverse", "--check", "-"],
        input=patch, cwd=root, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if reverse.returncode != 0:
        subprocess.run(["git", "apply", "--check", "-"], input=patch, cwd=root, check=True)
        subprocess.run(["git", "apply", "-"], input=patch, cwd=root, check=True)
    marker.write_text(json.dumps({"patch_sha256": DIGEST,
        "source_baseline": "19bcba9e5b05bc7b502bd062d8d83d3a72fd3dd4",
        "private_assets_included": False}, indent=2) + "\n", encoding="utf8")
    print("OBJECTIVE_PLAYABILITY_PATCH_APPLIED source_only=true")


if __name__ == "__main__":
    apply()
