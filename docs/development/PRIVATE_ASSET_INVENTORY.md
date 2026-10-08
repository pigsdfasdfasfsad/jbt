# Private original mesh and texture installation audit

Before exporting an owner-held build with recovered Laboratory art, place
authorized Godot mesh resources (binary .res) and PNG textures under:

    src/Twr.Godot/Content/Assets/Meshes/<numeric asset ID>.res
    src/Twr.Godot/Content/Assets/Textures/<numeric asset ID>.png

Use the owner-held Laboratory.asset-manifest.json from the original
Laboratory source evidence ZIP to audit installation:

    python tools/assets/private_asset_inventory.py \
      --manifest "Laboratory.asset-manifest.json" \
      --assets-root "src/Twr.Godot/Content/Assets" \
      --output "private-laboratory-asset-report.json"

The output lists installed and missing source IDs with file sizes and hashes.
It must remain outside public GitHub. Identifiers are not asset binaries and
this tool performs no downloads. An asset is only considered installed when
the local file is present, nonzero, and has a plausible Godot .res or PNG
signature. This check is necessary but cannot prove mesh suitability,
geometry fidelity, material quality, authorisation, or CSG completion.

The imported place file contains no decoded original UnionOperation render
binaries, so the full Laboratory art set is not currently resolvable from
its XML and asset references alone.
