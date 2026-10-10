# Pass 37 — Source-recovered CMaps Lighting Effects (10 release maps)

Original evidence: the owner-held `TestPlace/TestPlace.rbxlx` in `TestPlace.zip`, SHA-256
`272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2`.

The extractor at `tools/validation/recover_source_lighting37.py` reads
`ReplicatedStorage/CMaps/<Map>/Lighting Effects` without executing Lua.
It preserves each Sky, Atmosphere, BloomEffect, ColorCorrectionEffect and
SunRaysEffect source record in source order, including multiple distinct color
correction layers. It emits ten deterministic owner-private JSON packs and
SHA-256 manifest. These must be placed beside the privately distributed
executable under `Content/Lighting/<Map>.lighting37.json`. No source XML,
Roblox skybox image binaries or owner-private bundles are committed to Git.

Map record counts: Ranch 6, Mill 6, Bypass 5, Cabin 5, Cargo 4, District 3,
Expressway 4, Prison 5, Laboratory 3, Manor 4 (46 total).

## Runtime

`Pass37SourceLightingRuntime` accepts only packs for the ten release maps
whose SHA-256 matches the owner-derived, hard-coded digest, and validates
map identity, source provenance, effect types, count, numeric bounds and the
explicit absence of skybox textures. Map-load failures are non-fatal.

**F3** toggles an *approximate* Godot lighting preview; it defaults OFF and
restores the previous environment when disabled. Bloom, color corrections,
fog and sun-scattering use documented visual approximations, not Roblox
screen-space/postprocess parity. Skybox image asset IDs are not image bytes.
The original images, Roblox atmosphere scattering, renderer behavior and
dynamic lighting scripts remain absent. The HUD's F10/F11 diagnostics include
the source count and preview state.

The preview is intentionally optional: visual parity has not been measured
against controlled original gameplay captures. The ten-map original-geometry
gaps from Pass 36 remain open, as do full 15-wave gameplay sessions and
frame-time/performance measurements using all privately sourced assets.

## Testing

`tests/test_pass37_source_lighting.py` uses synthetic XML fixtures to
assert deterministic ten-map export, SHA integrity, private sidecar contract,
and the opt-in toggle wiring. The Windows CI branch compiles and exports the
Godot project; this test is a static/integration contract, **not** a pixel-diff
or real-original-lighting graphical verification.
