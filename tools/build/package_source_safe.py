"""Package only public-source runtime binaries, never owner assets or QA fixtures.

Requires completed native regression receipts for the exact checked-out commit.
The package is executed from its own clean directory with SDK locations removed
from the environment. This is not a claim of a physically air-gapped test.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
import tempfile
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PRIVATE_SUFFIXES = (".rbxl", ".rbxlx", ".rbxm", ".rbxmx", ".scene.jsonl.gz",
                    ".terrainmesh.gz", ".nav25.gz", ".nav31.gz", ".nav41.gz",
                    ".nav42.gz", ".instances28.gz", ".collision26.gz")
RUNTIME_SUFFIXES = {".dll", ".exe", ".json", ".dat", ".bin", ".pdb", ".txt", ".xml", ".config"}


def private_path(path: Path) -> bool:
    name = path.name.lower()
    return name.endswith(PRIVATE_SUFFIXES) or "profile" in name or name.endswith(".obj")


def validate_embedded_inputs(project: Path) -> None:
    content = project / "Content"
    if not content.exists():
        raise ValueError("Compiled content is missing; run content validation first")
    for file in content.rglob("*"):
        if file.is_symlink() or getattr(file, "is_junction", lambda: False)():
            raise ValueError("Linked content is not allowed in source-safe exports")
        if file.is_file() and (private_path(file) or file.suffix.lower() != ".json"):
            raise ValueError("Unexpected non-metadata content before export: " + str(file.relative_to(project)))


def choose_files(output: Path) -> list[Path]:
    if not (output / "ThoseWhoRemainOffline.exe").is_file():
        raise ValueError("Missing exported Windows executable")
    # Disposable post-export tests place files here; any uncleaned file is a
    # release blocker, not something silently swept into a wildcard archive.
    external = output / "Content"
    if external.exists() and any(p.is_file() for p in external.rglob("*")):
        raise ValueError("External Content is not empty: private assets or test fixtures remain")
    selected = []
    for path in output.iterdir():
        if path.is_symlink() or getattr(path, "is_junction", lambda: False)():
            raise ValueError("Linked export output is prohibited")
        if path.is_file() and (path.suffix.lower() in {".exe", ".dll", ".pck"}):
            if private_path(path): raise ValueError("Private-named export file")
            selected.append(path)
        elif path.is_dir() and path.name.startswith("data_"):
            for file in path.rglob("*"):
                if file.is_symlink() or getattr(file, "is_junction", lambda: False)():
                    raise ValueError("Linked runtime dependency is prohibited")
                if not file.is_file(): continue
                if private_path(file) or file.suffix.lower() not in RUNTIME_SUFFIXES:
                    raise ValueError("Unexpected runtime dependency: " + str(file.relative_to(output)))
                selected.append(file)
    if not any(p.name.lower() == "coreclr.dll" for p in selected):
        raise ValueError("Self-contained .NET runtime (coreclr.dll) is missing")
    if not any(p.name == "Twr.Godot.dll" for p in selected):
        raise ValueError("Managed game assembly is missing")
    return sorted(selected)


def verify_receipt(path: Path, commit: str, expected: int | None = None) -> dict:
    data = json.loads(path.read_text(encoding="utf-8"))
    if data.get("tested_commit") != commit or data.get("failed") != 0:
        raise ValueError("Receipt does not verify current source: " + path.name)
    if expected is not None and data.get("passed", 0) < expected:
        raise ValueError("Incomplete acceptance matrix: " + path.name)
    return data


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--check-inputs", action="store_true")
    ap.add_argument("--output", type=Path, default=ROOT / "build/output")
    ap.add_argument("--destination", type=Path, default=ROOT / "build/deliverables")
    args = ap.parse_args()
    validate_embedded_inputs(ROOT / "src/Twr.Godot")
    if args.check_inputs:
        print("SOURCE_SAFE_EXPORT_INPUTS_OK metadata_only=true")
        return
    commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
    receipts = {
        "full": verify_receipt(ROOT / "build/runtime-validation/full.json", commit, 34),
        "focus": verify_receipt(ROOT / "build/runtime-validation/focus.json", commit, 9),
        "targeted": verify_receipt(ROOT / "build/source-fidelity-validation/targeted.json", commit, 2),
        "graphics": verify_receipt(ROOT / "build/graphics-validation/graphics.json", commit, 2),
    }
    files = choose_files(args.output)
    args.destination.mkdir(parents=True, exist_ok=True)
    package_name = "TWR-SourceSafe-Windows-x64-" + commit[:12]
    with tempfile.TemporaryDirectory(prefix="twr-source-safe-") as temp:
        stage = Path(temp) / package_name
        stage.mkdir()
        for file in files:
            destination = stage / file.relative_to(args.output)
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(file, destination)
        env = dict(os.environ)
        env["DOTNET_ROOT"] = str(Path(temp) / "no-system-dotnet")
        env["DOTNET_ROOT_X64"] = env["DOTNET_ROOT"]
        env["DOTNET_MULTILEVEL_LOOKUP"] = "0"
        env["PATH"] = os.path.join(env.get("SystemRoot", "C:\\Windows"), "System32")
        command = [str(stage / "ThoseWhoRemainOffline.exe"), "--headless", "--verbose",
                   "--quit-after", "3000", "--", "--smoke-runtime-readiness"]
        run = subprocess.run(command, cwd=stage, env=env, stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT, text=True, encoding="utf-8", errors="replace", timeout=180)
        if run.returncode != 0 or "TWR_SMOKE_RUNTIME_READINESS_OK" not in run.stdout:
            print(run.stdout[-20000:])
            raise ValueError("Clean-folder packaged executable failed: " + str(run.returncode))
        if re.search(r"(^|\n).*?(ERROR:|Fatal error|AccessViolationException)", run.stdout):
            raise ValueError("Packaged executable emitted a native error")
        readme = (
            "TWR OFFLINE - SOURCE-SAFE DEVELOPMENT BUILD\n\n"
            "Tested source commit: " + commit + "\n\n"
            "Extract the entire folder. Double-click ThoseWhoRemainOffline.exe.\n"
            "Keep the data_* directory and every DLL beside the executable.\n"
            "The .NET runtime is included. No Roblox, Studio, or separate SDK is needed.\n"
            "This package excludes all owner-private map/mesh/texture/archive files.\n"
            "It uses the existing reconstructed maps and locally generated visuals.\n"
            "It is NOT a completed 1:1 recreation or an original-asset release.\n\n"
            "Controls: W/A/S/D move, mouse look, left click fire, right click aim,\n"
            "R reload, Space jump, Shift sprint, E interact, Escape pause.\n"
            "F10 diagnostics, F11 save local diagnostics, F12 capture current view.\n"
            "Save data is stored in the game's Godot user-data directory, not this folder.\n\n"
            "Verification: clean-folder native launch, targeted importer checks,\n"
            "repeated exit checks, and inherited automated gameplay matrix.\n"
            "These tests do not establish original-map fidelity or real-time 15-wave survival.\n"
        )
        (stage / "START_HERE.txt").write_text(readme, encoding="utf-8")
        hashes = {}
        for file in sorted(stage.rglob("*")):
            if file.is_file():
                hashes[file.relative_to(stage).as_posix()] = hashlib.sha256(file.read_bytes()).hexdigest()
        manifest = {"format": "twr-source-safe-release-v1", "tested_commit": commit,
            "private_original_assets_included": False, "clean_folder_exit_code": run.returncode,
            "bundled_dotnet_runtime": True, "file_sha256": hashes,
            "verification_counts": {name: data["passed"] for name, data in receipts.items()}}
        (stage / "BUILD_MANIFEST.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
        archive = args.destination / (package_name + ".zip")
        with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as z:
            for file in sorted(stage.rglob("*")):
                if file.is_file(): z.write(file, file.relative_to(stage.parent).as_posix())
        with zipfile.ZipFile(archive) as z:
            if z.testzip() is not None: raise ValueError("Package archive integrity failure")
        digest = hashlib.sha256(archive.read_bytes()).hexdigest()
        result = {"archive": archive.name, "sha256": digest, "size_bytes": archive.stat().st_size,
            "tested_commit": commit, "file_count": len(hashes) + 1, "clean_launch_passed": True,
            "original_assets_included": False}
        (args.destination / "release-receipt.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
        print("SOURCE_SAFE_WINDOWS_PACKAGE_OK " + json.dumps(result), flush=True)


if __name__ == "__main__":
    main()
