#!/usr/bin/env python3
"""Audit authored runtime sources before OR after a native build.

MSBuild's implicit global-usings files mention networking namespaces even
when no runtime networking is used. Generated build directories are not
runtime source inputs and must not make an offline project fail its audit.
The forbidden APIs are still checked in every authored runtime source file.
"""
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
GENERATED = {'.godot', '.import', 'bin', 'obj'}
SUFFIXES = {'.cs', '.gd', '.tscn', '.godot', '.cfg', '.csproj'}
FORBIDDEN = ('system.net.http', 'httpclient', 'webrequest', 'websocket',
             'roblox.com', 'rbxapi', 'fireserver', 'invokeserver')


def source_files(root: Path):
    for base in (root/'src/Twr.Domain', root/'src/Twr.Godot'):
        for path in base.rglob('*'):
            relative = path.relative_to(base)
            if any(part.lower() in GENERATED for part in relative.parts[:-1]):
                continue
            if path.is_file() and path.suffix.lower() in SUFFIXES:
                yield path


def violations(root: Path = ROOT):
    return [(path.relative_to(root).as_posix(), token)
            for path in source_files(root)
            for token in FORBIDDEN
            if token in path.read_text(encoding='utf8', errors='replace').lower()]


if __name__ == '__main__':
    errors = violations()
    if errors:
        raise SystemExit('Offline runtime violations: ' + repr(errors))
    print('offline runtime audit ok (authored sources, generated outputs excluded)')
