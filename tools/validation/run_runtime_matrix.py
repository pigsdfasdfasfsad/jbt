"""Run the existing branch-selected Windows acceptance scripts unchanged.

Uses the last development branch as a matrix selector, not as source checkout.
Every result records the actual tested git commit. Unknown conditions fail closed.
No executable, private source archive, or generated asset is published.
"""
from __future__ import annotations
import argparse
import ast
import json
import os
from pathlib import Path
import re
import subprocess
import time

BASELINE = "twr-pass123-physical-objective-routes"
ROOT = Path(__file__).resolve().parents[2]


def condition(expression: str | bool | None, branch: str = BASELINE) -> bool:
    if expression is None:
        return True
    if isinstance(expression, bool):
        return expression
    value = expression.strip()
    if value.startswith("${{") and value.endswith("}}"):
        value = value[3:-2].strip()
    value = value.replace("github.ref_name", repr(branch))
    value = value.replace("success()", "True").replace("always()", "True")
    value = value.replace("&&", " and ").replace("||", " or ")
    value = re.sub(r"\btrue\b", "True", value)
    value = re.sub(r"\bfalse\b", "False", value)
    value = re.sub(r"!(?!=)", "not ", value)
    tree = ast.parse(value.strip(), mode="eval")

    def visit(node):
        if isinstance(node, ast.Expression):
            return visit(node.body)
        if isinstance(node, ast.Constant) and isinstance(node.value, (bool, str)):
            return node.value
        if isinstance(node, ast.BoolOp) and isinstance(node.op, (ast.And, ast.Or)):
            values = [bool(visit(v)) for v in node.values]
            return all(values) if isinstance(node.op, ast.And) else any(values)
        if isinstance(node, ast.UnaryOp) and isinstance(node.op, ast.Not):
            return not visit(node.operand)
        if isinstance(node, ast.Compare) and len(node.ops) == 1:
            left, right = visit(node.left), visit(node.comparators[0])
            if isinstance(node.ops[0], ast.Eq):
                return left == right
            if isinstance(node.ops[0], ast.NotEq):
                return left != right
        raise ValueError(f"Unsupported workflow condition: {expression!r}")

    return bool(visit(tree))


def select_steps(workflow: dict, focus: bool = False) -> list[dict]:
    steps = workflow["jobs"]["validate-and-export"]["steps"]
    after_export = False
    selected = []
    for step in steps:
        if step.get("name") == "Verify executable":
            after_export = True
            continue
        if not after_export or not condition(step.get("if")):
            continue
        if "uses" in step:
            raise ValueError(f"Unexpected active action in native matrix: {step['name']}")
        if "run" not in step or step.get("shell") != "pwsh":
            raise ValueError(f"Unsupported active step: {step['name']}")
        selected.append(step)
    required = ("Native Pass 119", "Native Pass 120", "Native Pass 121", "Native Pass 123",
                "Smoke Pass 90", "Smoke Pass 92", "Smoke Pass 94", "Verify no private map")
    if len(selected) < 30 or any(not any(s["name"].startswith(r) for s in selected) for r in required):
        raise ValueError("The inherited native matrix is incomplete")
    if focus:
        selected = [s for prefix in ("Smoke Pass 90", "Smoke Pass 92", "Smoke Pass 94")
                    for s in selected if s["name"].startswith(prefix)]
        if len(selected) != 3:
            raise ValueError("Missing focused Escort regression")
    return selected


def bounded_lines(text: str) -> list[str]:
    lines = re.sub(r"\x1b\[[0-9;]*m", "", text).splitlines()
    relevant = [s[:1800] for s in lines if re.search(
        r"TWR_.*(?:OK|FAILED)|error CS\d+|error MSB\d+|Fatal error|AccessViolation|Exception:|\d+ passed", s)]
    return relevant[-18:] + lines[-6:]


def invoke(name: str, command: list[str], cwd: Path, output: Path, timeout: int) -> dict:
    start = time.monotonic()
    env = os.environ.copy()
    env["GITHUB_REF_NAME"] = BASELINE
    path = output / (re.sub(r"[^A-Za-z0-9]+", "_", name)[:100] + ".log")
    try:
        with path.open("w", encoding="utf-8") as stream:
            result = subprocess.run(command, cwd=cwd, env=env, stdout=stream,
                                    stderr=subprocess.STDOUT, timeout=timeout, check=False)
        code = result.returncode
    except subprocess.TimeoutExpired:
        code = 124
    text = path.read_text(encoding="utf-8", errors="replace")
    row = {"name": name, "exit_code": code, "seconds": round(time.monotonic()-start, 2),
           "excerpt": bounded_lines(text)}
    print(json.dumps(row, ensure_ascii=True), flush=True)
    return row


def main() -> int:
    import yaml
    parser = argparse.ArgumentParser()
    parser.add_argument("--focus", action="store_true")
    parser.add_argument("--readiness", action="store_true")
    args = parser.parse_args()
    output = ROOT / "build" / "runtime-validation"
    output.mkdir(parents=True, exist_ok=True)
    sha = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
    print(json.dumps({"tested_commit": sha, "matrix_selector": BASELINE}), flush=True)
    workflow = yaml.safe_load((ROOT / ".github/workflows/windows-build.yml").read_text(encoding="utf-8"))
    rows = []
    if args.readiness:
        exe = (ROOT / "build/output/ThoseWhoRemainOffline.exe").resolve()
        row = invoke("Runtime readiness production scene", [str(exe), "--headless", "--verbose",
                     "--quit-after", "3000", "--", "--smoke-runtime-readiness"], ROOT, output, 180)
        raw = next(output.glob("Runtime_readiness*.log")).read_text(encoding="utf-8", errors="replace")
        if "TWR_SMOKE_RUNTIME_READINESS_OK" not in raw or "TWR_RUNTIME_PREQUIT_OK exit_code=0" not in raw:
            row["exit_code"] = row["exit_code"] or 1
        rows.append(row)
    else:
        selected = select_steps(workflow, args.focus)
        print(json.dumps({"selected_steps": [s["name"] for s in selected]}), flush=True)
        for repeat in range(3 if args.focus else 1):
            for index, step in enumerate(selected):
                script = output / f"matrix_{index}.ps1"
                script.write_text("$ErrorActionPreference = 'stop'\n" + step["run"] +
                                  "\nif (Test-Path variable:LASTEXITCODE) { exit $LASTEXITCODE }\n",
                                  encoding="utf-8")
                name = step["name"] + (f" [attempt {repeat+1}]" if args.focus else "")
                rows.append(invoke(name, ["pwsh", "-NoLogo", "-NoProfile", "-File", str(script)],
                                   ROOT, output, 1500))
    report = {"tested_commit": sha, "matrix_selector": BASELINE, "results": rows,
              "passed": sum(r["exit_code"] == 0 for r in rows),
              "failed": sum(r["exit_code"] != 0 for r in rows)}
    name = "readiness" if args.readiness else "focus" if args.focus else "full"
    (output / f"{name}.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    if os.environ.get("GITHUB_STEP_SUMMARY"):
        with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as summary:
            summary.write(f"\n### {name} - tested `{sha}`\n\n")
            summary.write("| Test | Exit | Seconds |\n|---|---:|---:|\n")
            for row in rows:
                summary.write(f"| {row['name']} | {row['exit_code']} | {row['seconds']} |\n")
    print(json.dumps({"runtime_matrix_result": name, "tested_commit": sha,
                      "passed": report["passed"], "failed": report["failed"]}), flush=True)
    return 1 if report["failed"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
