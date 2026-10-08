# Local fidelity capture workflow — F12

While playing in the real Windows build, press F12 to capture:
- a PNG screenshot of the current rendered game viewport;
- a JSON sidecar identifying the map, camera world position (Godot metres),
  camera orientation basis, and current FOV.

Files are stored in the game's Godot user://fidelity-captures folder. The
actual platform path appears in the TWR_FIDELITY_CAPTURE_OK log marker.
This path is local, not part of the public GitHub repository.

Use the original owner-provided map screenshot/video references and
docs/development/MAP_VISUAL_REFERENCE_COVERAGE.md to match original vantage
points, then compare geometry, materials, lighting and missing props.
Visual comparison must be done on real rendered screenshots, not headless
test output.

Windows CI verifies metadata JSON serialization. It does NOT capture or
judge actual rendered screenshots in headless mode.
