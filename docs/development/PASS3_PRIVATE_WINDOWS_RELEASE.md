# PASS 3 — Full ten-map PRIVATE Windows packaging

Once a Windows PC has compiled current twr-offline-dev source using
build/Windows/build.ps1, privately bundle the EXE and all original recovered
data without publishing binaries on GitHub:

powershell -ExecutionPolicy Bypass -File build/Windows/package_private.ps1 -PrivateSourceZip "C:\MyFiles\TWR-Original-Ten-Maps-Zombies-Weapons-Private-Pack-v3.zip" -Destination "C:\Users\me\Downloads\TWR-Offline-Private-Preview.zip"

Optional authorized local assets:
- PrivateAssetsFolder C:\MyAssets containing Meshes\<id>.obj and Textures\<id>.png
- AuthorizedAudioFolder C:\MySounds containing gunshot.wav, shotgun.wav, etc

The packaging script checks all ten original v2 scene map headers, refuses
synthetic CI fixtures and missing zombie/weapon assemblies, includes all
Content/Maps, Content/Terrain, Content/Enemies and Content/Weapons, and
creates a BUILD-MANIFEST.json with SHA-256 hashes and explicit limitations.
The destination must be outside the public repository.

It also keeps the legacy -LaboratoryPack argument, which packages a single
map and must NOT be advertised as a complete ten-map release.

This script is not a substitute for a human-controlled Windows gameplay
playtest. GitHub Windows CI has export+headless smoke only, and intentionally
does not publish an Actions EXE artifact.
