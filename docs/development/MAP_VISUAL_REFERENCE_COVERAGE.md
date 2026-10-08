# Map visual reference coverage (owner-provided images.zip)

Reference images were inspected from the owner's Project source archive.
The counts below come from **filename prefix matches**, so they are only a
catalog of likely reference images, not verified coverage of every room.

| Map | Matched reference images |
| --- | ---: |
| Ranch | 28 |
| Mill | 14 |
| Bypass | 26 |
| Cabin | 14 |
| Cargo | 12 |
| District | 31 |
| Expressway | 6 |
| Prison | 39 |
| Laboratory | 45 |
| Manor | 26 |

The archive additionally contains ungrouped screenshots and many weapon/UI
references. Original videos in videos of game.zip (seven MP4 clips) mostly
show a test arena and sample lobby rather than a complete playthrough of
each release map.

Rebuild a private, machine-readable catalog with:

    python tools/visual/index_reference_archive.py --archive "images.zip" --output "private-reference-index.json"

Each reference record holds the original ZIP member name and SHA-256, not
the copyrighted image data. Keep the original screenshots, contact sheets,
and archive outside public GitHub history. Use the catalog to choose matching
vantage points for real Godot gameplay screenshots, then measure discrepancies.

**A successful image catalog, source geometry extraction, or headless
Windows build is not evidence of visual fidelity.**
