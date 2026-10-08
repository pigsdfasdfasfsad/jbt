# Original Laboratory item/fortification markers — source recovery

The owner-supplied TestPlace/TestPlace.rbxlx has a loaded Laboratory map in
Workspace/Map. Its separate Workspace/Ignore/Spawn Boxes folders contain:
- 127 ItemPickupBox parts inside Item Spawn Boxes
- 47 FortificationPickupBox parts inside Fortification Spawn Boxes

Those are source-authored locations, not placements guessed from screenshots.
The private Laboratory source pack can be augmented with their position data
using this deterministic local command (outside public Git):

    python tools/maps/augment_laboratory_pickups.py --archive "TestPlace.zip" \
      --source-pack "Laboratory.original.scene.jsonl.gz" \
      --output "Laboratory.scene.jsonl.gz"

The script rejects a different source snapshot by SHA-256, validates each
marker folder ancestry, confirms expected counts, and does not publish private
coordinate records to GitHub. The original pack format is backward compatible:
an unaugmented pack still loads, but continues using provisional pickup anchors.

Runtime logic picks up to four distinct original Item markers and two distinct
Fortification markers at the start of a map, avoiding duplicate selections;
the recovered 20-second natural item/fortification respawn cadence is retained.
Original item-type probabilities have NOT been verified. Original objective
markers and complete original map art are also still outstanding.

Synthetic CI tests validate the new parser and exported Windows loader with
synthetic marker coordinates. They do NOT verify a real human-controlled
Laboratory session.
