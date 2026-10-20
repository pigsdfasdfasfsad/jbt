"""Native checks for source-fidelity work. Success requires markers AND exit 0."""
from __future__ import annotations
import argparse
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FATAL = re.compile(r"(^|\n)(?:[^\n]*)(?:ERROR:|Fatal error|System.AccessViolationException|heap corruption|TWR_\w+_FAILED)", re.I)


def execute(exe: Path, flag: str, marker: str, report_dir: Path) -> dict:
    # The scripted walking test is a real-time physics loop. A bare
    # render-frame watchdog can quit a fast headless scene before it ends.
    frames = "12000" if flag in {"--smoke-source-traversal", "--smoke-source-escort", "--smoke-objective-guidance", "--smoke-source-tanker"} else "3000"
    command = [str(exe.resolve()), "--headless", "--verbose", "--max-fps", "60",
               "--quit-after", frames, "--", flag]
    try:
        result = subprocess.run(command, cwd=exe.parent, text=True,
            encoding="utf-8", errors="replace", stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT, timeout=180)
        output = result.stdout
        code = result.returncode
    except subprocess.TimeoutExpired as exc:
        output = str(exc.stdout or "") + "\nNATIVE_TEST_TIMED_OUT"
        code = -1
    report_dir.mkdir(parents=True, exist_ok=True)
    (report_dir / (flag.removeprefix("--") + ".log")).write_text(output, encoding="utf-8")
    ok = code == 0 and marker in output and not FATAL.search(output)
    outcome = {"flag": flag, "exit_code": code, "passed": bool(ok),
        "required_marker": marker, "marker_seen": marker in output,
        "warnings": [line for line in output.splitlines() if "WARNING:" in line]}
    print(json.dumps(outcome), flush=True)
    print("\n".join(output.splitlines()[-35:]), flush=True)
    return outcome


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=ROOT / "build/output/ThoseWhoRemainOffline.exe")
    parser.add_argument("--reports", type=Path, default=ROOT / "build/source-fidelity-validation")
    args = parser.parse_args()
    commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
    # MultiMesh visual assertions run unchanged in the real-renderer job.
    tests = [("--smoke-runtime-readiness", "TWR_SMOKE_RUNTIME_READINESS_OK"),
             ("--smoke-pass76", "TWR_SMOKE_PASS76_OBJECTIVE_FORTIFICATION_OK"),
             ("--smoke-source-traversal", "TWR_SMOKE_SOURCE_TRAVERSAL_OK"),
             ("--smoke-bolter-grounding", "TWR_BOLTER_GROUNDING_OK"),
             ("--smoke-source-escort", "TWR_SOURCE_ESCORT_OK"),
             ("--smoke-objective-guidance", "TWR_OBJECTIVE_GUIDANCE_OK"),
             ("--smoke-source-tanker", "TWR_SOURCE_DAMAGE_OK"),
             ("--smoke-audio-lifecycle", "TWR_AUDIO_LIFECYCLE_OK"),
             ("--smoke-projectile-sweep", "TWR_PROJECTILE_COLLISION_OK"),
             ("--smoke-startup-close", "TWR_RUNTIME_PREQUIT_OK exit_code=0"),
             ("--smoke-held-clearance", "TWR_HELD_MODEL_CLEARANCE_OK")]
    outcomes = [execute(args.exe, flag, marker, args.reports) for flag, marker in tests]
    report = {"tested_commit": commit, "passed": sum(x["passed"] for x in outcomes),
        "failed": sum(not x["passed"] for x in outcomes), "tests": outcomes,
        "original_private_map_visual_parity_verified": False}
    args.reports.mkdir(parents=True, exist_ok=True)
    (args.reports / "targeted.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    if report["failed"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
