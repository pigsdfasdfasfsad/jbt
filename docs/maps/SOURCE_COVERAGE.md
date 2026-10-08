# TWR Source Map Coverage — Evidence Audit

Source: privately supplied TestPlace.zip, two Roblox XML place snapshots.
No original XML, meshes, textures, sounds, or source asset IDs are committed.
Reproduce locally with:

    python tools/maps/audit_map_sources.py --archive TestPlace.zip --output coverage-current.json
    python tools/maps/audit_map_sources.py --archive TestPlace.zip --member TestPlace/TestPlace.prev.rbxlx --output coverage-previous.json

## Snapshot distinction

Both snapshots contain a loaded Workspace/Map with **34,268 renderable
instances** and Laboratory-specific props, including microscopes.
Only the newer TestPlace/TestPlace.rbxlx contains ReplicatedStorage/CMaps
with the ten stored-map branches. Do not interpret this as eleven full maps:
the loaded scene and stored ancillary map objects have different purposes.

## Recoverable stored map evidence (newer snapshot)

| Map | Renderable source objects under CMaps/Map | MeshPart | UnionOperation | Server-wall shapes |
| --- | ---: | ---: | ---: | ---: |
| Ranch | 152 | 56 | 67 | 1,266 |
| Mill | 267 | 164 | 70 | 1,055 |
| Bypass | 852 | 567 | 150 | 768 |
| Cabin | 36 | 29 | 2 | 871 |
| Cargo | 27 | 0 | 27 | 321 |
| District | 195 | 142 | 47 | 346 |
| Expressway | 22 | 3 | 19 | 274 |
| Prison | 372 | 178 | 104 | 1,234 |
| Laboratory | 1,705 | 1,674 | 31 | 1,517 |
| Manor | 1,633 | 24 | 1,602 | 785 |

All ten stored map branches contain a Lighting ModuleScript. Additional
serialized lighting effects may be present. Their presence does not prove
that corresponding scripts, effect presets, or original mesh binaries have
been recovered or ported.

The original CSG mesh bytes for UnionOperation records are not in the
current exported source pack. Manor's 1,602 union objects are therefore
a severe art-fidelity dependency, even though its coordinates and bounds
are recoverable. The same distinction applies to MeshPart references.

**Important:** The listed stored objects are fragmentary source evidence,
not ten complete decorated map environments. Do not silently replace
functional maps with these incomplete folders. Record source provenance,
missing binary dependencies, and gameplay validation before enabling a
new reconstructed map.
