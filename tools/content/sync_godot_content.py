#!/usr/bin/env python3
from __future__ import annotations
import argparse, shutil
from pathlib import Path

def main():
    ap=argparse.ArgumentParser(); ap.add_argument('--repo-root',type=Path,default=Path(__file__).resolve().parents[2]); ns=ap.parse_args()
    root=ns.repo_root.resolve(); src=root/'content'; dst=root/'src/Twr.Godot/Content'
    if dst.exists(): shutil.rmtree(dst)
    dst.mkdir(parents=True)
    for p in src.rglob('*'):
        if p.is_file():
            q=dst/p.relative_to(src); q.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(p,q)
    print(f'synced runtime content to Godot project: {sum(1 for p in dst.rglob("*") if p.is_file())} files')
if __name__=='__main__': main()
