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
