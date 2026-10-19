"""Apply the reviewed traversal integration, without touching private data.

Idempotence is checked using the complete reverse patch, not a loose token.
A changed baseline fails before any source is modified.
"""
from pathlib import Path
import subprocess
ROOT=Path(__file__).resolve().parents[2]
PATCH=ROOT/'tools/dev/source_traversal.patch'


def apply():
    forward=subprocess.run(['git','apply','--check',str(PATCH)],cwd=ROOT,
        capture_output=True,text=True)
    if forward.returncode==0:
        subprocess.run(['git','apply',str(PATCH)],cwd=ROOT,check=True)
        return
    reverse=subprocess.run(['git','apply','--reverse','--check',str(PATCH)],
        cwd=ROOT,capture_output=True,text=True)
    if reverse.returncode==0:
        return
    raise RuntimeError('Source traversal integration no longer matches baseline: '+forward.stderr)


if __name__=='__main__':
    apply()
