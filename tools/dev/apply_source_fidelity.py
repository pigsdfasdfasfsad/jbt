"""Idempotent inspected source edits; run by the dedicated release workflow.

No owner ZIP or original geometry is read or published by this script.
The private archive audit found 334 Brick SpecialMeshes in Laboratory,
207 visible, including glass panes, wire shelves and supports. Roblox's
MeshType value 6 is Brick, not Sphere, and its Scale multiplies Part.Size.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def change(path: str, old: str, new: str) -> None:
    file = ROOT / path
    text = file.read_text(encoding="utf-8")
    if new in text:
        return
    if text.count(old) != 1:
        raise RuntimeError(f"Inspected source anchor changed: {path}: {old[:100]!r}")
    file.write_text(text.replace(old, new), encoding="utf-8", newline="\n")


def apply() -> None:
    loader = "src/Twr.Godot/Scripts/LaboratorySourceLoader.cs"
    change(loader,
        '        var key = appearanceKey;',
        '        // Distinct built-in shapes must not share a visual batch.\n'
        '        appearanceKey += "|builtin:" + SourceMeshPresentation.Kind(r);\n'
        '        var key = appearanceKey;')
    change(loader,
        '                var mesh = prepared ?? ProxyMesh(cls, Str(r, "shape"));',
        '                var mesh = prepared ?? SourceMeshPresentation.BuiltinMesh(r)\n'
        '                    ?? ProxyMesh(cls, Str(r, "shape"));')
    change(loader,
        '            return new CylinderMesh { TopRadius = 0.5f, BottomRadius = 0.5f, Height = 1f };',
        '            return SourceMeshPresentation.CylinderAlongX();')
    change(loader,
        '    private static Vector3 VisualExtents(JsonElement r, Vector3 physicalPartSize)\n    {',
        '    private static Vector3 VisualExtents(JsonElement r, Vector3 physicalPartSize)\n    {\n'
        '        if (SourceMeshPresentation.TryBuiltinSize(r, physicalPartSize, out var builtinSize))\n'
        '            return builtinSize;\n'
        '        // FileMesh and unknown historical meshes retain their existing\n'
        '        // approximation below; original upload-space bounds are absent.\n')
    change(loader,
        '// implementation also hid SpecialMeshType=6 (sphere), even though',
        '// implementation also hid SpecialMeshType=6 (Brick), even though')
    change("src/Twr.Godot/Scripts/Bootstrap.cs",
        '        var args=OS.GetCmdlineUserArgs();\n',
        '        var args=OS.GetCmdlineUserArgs();\n'
        '        if(args.Contains("--smoke-source-mesh", StringComparer.Ordinal))\n'
        '        {\n'
        '            AddChild(new SourceMeshPresentationAcceptance { Name="SourceMeshPresentationAcceptance" });\n'
        '            return;\n'
        '        }\n')
    change("src/Twr.Godot/Scripts/LocalSessionNode.cs",
        '        var store = new JsonProfileStore(save);\n',
        '        var sourceMeshSmoke = OS.GetCmdlineUserArgs().Contains("--smoke-source-mesh", StringComparer.Ordinal);\n'
        '        if (sourceMeshSmoke) save = ProjectSettings.GlobalizePath("user://source-mesh-smoke-profile.json");\n'
        '        var store = new JsonProfileStore(save);\n'
        '        if (sourceMeshSmoke) store.Save(new Profile());\n')


if __name__ == "__main__":
    apply()
