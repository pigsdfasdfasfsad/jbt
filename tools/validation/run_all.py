#!/usr/bin/env python3
from __future__ import annotations
import subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
commands=[
 [sys.executable,'tools/evidence/bootstrap.py'],
 [sys.executable,'tools/content/build_content.py'],
 [sys.executable,'tools/content/sync_godot_content.py'],
 [sys.executable,'-m','pytest','-q'],
 [sys.executable,'tools/validation/static_csharp.py'],
 [sys.executable,'tools/validation/offline_audit.py'],
 [sys.executable,'tools/validation/project_audit.py'],
 [sys.executable,'tools/validation/gate_audit.py'],
]
for cmd in commands:
 print('+',' '.join(cmd),flush=True); subprocess.run(cmd,cwd=ROOT,check=True)
print('ALL VALIDATION CHECKS PASSED')
