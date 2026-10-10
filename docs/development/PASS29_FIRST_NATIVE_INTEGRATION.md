# Pass 29 - First native Windows integration slice

This branch is based on the verified older `twr-offline-dev` Godot project, NOT
the unavailable Pass 23 full source tree. It does not falsely claim to have
merged every Pass 24-28 private source archive.

Integrated directly into the playable runtime:
- Sixteen occupied item types maximum for legacy dictionary inventory, with
  zero-count entries freeing slots and existing stacks remaining grantable.
- Read-only 4x4 inventory panel on `I`; `Escape` closes. No unsupported
  promise of reorder/split in the dictionary-based implementation.
- Swept Bloater projectile physics, wall-occluded splash damage.
- 0.5-second damage pulse clock for lingering gas, mask immunity, and no ticks
  after expiry or unbounded damage bursts from long frames.
- Native `--smoke-pass29` domain policy smoke in the exported Windows C# game.

The Windows CI job on branch `twr-pass29-integration` compiles, exports,
runs the pre-existing gameplay/release smokes, runs the Pass29 native smoke,
and uploads an executable ARTIFACT with no owner-held original game binaries.

The privately held Pass24-28 Laboratory scene/navigation/collision/geometry
packs and map/weapon artwork are NOT in this GitHub branch and are NOT
included in the CI artifact. All original-mesh fidelity and 10-map gameplay
validation remain incomplete.

## Follow-on optional source integrations (Passes 25-28)

Integrated C# source classes for the source-derived Laboratory navigation graph,
source-bound batched collider cache, native Part/WedgePart geometry streaming, and
map-card / weapon-icon presentation. All optional loaders fail closed to the
existing blockout/steering/per-Part/HUD presentation if private sidecars are missing.

The downloaded private content archive is installed locally **beside** the EXE.
It must not be added to public source or CI. The older Pass 29 CI artifact
from commit 86a8e669 is saved as the first verified executable checkpoint.
The follow-on source hooks are pending native CI verification on their own commit.
