#!/usr/bin/env python3
"""Mirror generated metadata without deleting an installed private asset pack.

Only files owned by the previous hash manifest may be retired. An edited
metadata file is backed up outside the Godot export before replacement.
Unknown files (including private source scenes) are never deleted. Public
release packaging must still reject them; this is not an export allowlist.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import tempfile
from pathlib import Path, PurePosixPath

STATE = Path("build/content-sync/state.json")
BACKUPS = Path("private-evidence/content-sync-backups")


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def regular_path(root: Path, relative: str) -> Path:
    parts = PurePosixPath(relative)
    if (not relative or parts.is_absolute() or ".." in parts.parts or
            "\\" in relative or ":" in relative or relative != parts.as_posix()):
        raise ValueError("Unsafe content manifest path")
    path = root
    for part in parts.parts:
        path = path / part
        if path.is_symlink() or getattr(path, "is_junction", lambda: False)():
            raise ValueError("Linked content is not followed")
    if path.exists() and not path.is_file():
        raise ValueError("A content file conflicts with an existing directory")
    return path


def atomic_write(path: Path, data: bytes) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    fd, temporary = tempfile.mkstemp(prefix=".twr-sync-", suffix=".tmp", dir=path.parent)
    try:
        with os.fdopen(fd, "wb") as stream:
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, path)
    finally:
        Path(temporary).unlink(missing_ok=True)


def sync(root: Path) -> dict[str, int]:
    root = root.resolve()
    source = root / "content"
    target = root / "src/Twr.Godot/Content"
    # Validate roots before any reads or mutations. Check ancestor links too,
    # including a replaced src/Twr.Godot directory on Windows.
    for directory in (source, target, root / BACKUPS, (root / STATE).parent):
        for ancestor in [directory, *directory.parents]:
            if ancestor == root:
                break
            if ancestor.is_symlink() or getattr(ancestor, "is_junction", lambda: False)():
                raise ValueError("Linked content directory is prohibited")
    if not source.is_dir():
        raise ValueError("Canonical content directory is missing")
    state_path = regular_path(root, STATE.as_posix())
    previous: dict[str, str] = {}
    if state_path.exists():
        state = json.loads(state_path.read_text(encoding="utf-8"))
        if state.get("version") != 1 or not isinstance(state.get("owned"), dict):
            raise ValueError("Unsupported content synchronization manifest")
        previous = state["owned"]
        for name, checksum in previous.items():
            regular_path(target, name)
            if not isinstance(checksum, str) or not re.fullmatch(r"[a-f0-9]{64}", checksum):
                raise ValueError("Invalid owned-content checksum")
    canonical: dict[str, bytes] = {}
    for item in sorted(source.rglob("*")):
        if item.is_symlink() or getattr(item, "is_junction", lambda: False)():
            raise ValueError("Linked canonical input is prohibited")
        if item.is_file():
            name = item.relative_to(source).as_posix()
            regular_path(target, name)
            canonical[name] = item.read_bytes()
    if not canonical:
        raise ValueError("Refusing to clear a mirror for empty canonical content")
    # Preflight every affected destination and possible backup before writes.
    changes: list[tuple[Path, bytes, bytes | None, Path | None]] = []
    for name, data in canonical.items():
        destination = regular_path(target, name)
        existing = destination.read_bytes() if destination.exists() else None
        backup = None
        if existing is not None and existing != data and digest(existing) != previous.get(name):
            backup = regular_path(root / BACKUPS, digest(existing) + "/" + name)
            if backup.exists() and backup.read_bytes() != existing:
                raise ValueError("Backup checksum collision or altered backup")
        changes.append((destination, data, existing, backup))
    retired = []
    preserved = 0
    for name, checksum in previous.items():
        if name in canonical:
            continue
        destination = regular_path(target, name)
        if destination.exists():
            data = destination.read_bytes()
            if digest(data) == checksum:
                retired.append((destination, data))
            else:
                preserved += 1
    updated = backed_up = 0
    for path, data, existing, backup in changes:
        if existing == data:
            continue
        # Refuse a concurrently edited destination rather than clobber it.
        now = path.read_bytes() if path.exists() else None
        if now != existing:
            raise RuntimeError("Content changed concurrently during synchronization")
        if backup is not None and not backup.exists():
            atomic_write(backup, existing)
            backed_up += 1
        atomic_write(path, data)
        updated += 1
    removed = 0
    for path, expected in retired:
        if path.read_bytes() == expected:
            path.unlink()
            removed += 1
        else:
            preserved += 1
    manifest = {"version": 1, "owned": {name: digest(data) for name, data in canonical.items()}}
    atomic_write(state_path, (json.dumps(manifest, indent=2, sort_keys=True) + "\n").encode())
    return {"canonical": len(canonical), "updated": updated, "retired": removed,
            "backups": backed_up, "modified_stale_preserved": preserved}


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    print("Godot content synchronized; unknown/private assets preserved: " + json.dumps(sync(args.repo_root)))


if __name__ == "__main__":
    main()
