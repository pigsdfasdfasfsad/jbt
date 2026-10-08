# Private Windows Development Packaging

This project intentionally keeps Windows executables and owner-held source
geometry data off GitHub, including Releases and Actions artifacts.

After successfully building the game **locally on Windows** with the pinned
Godot/.NET tools, invoke:

    pwsh -File build/Windows/package_private.ps1 -LaboratoryPack "C:\path\to\Laboratory.scene.jsonl.gz" -Destination "$HOME\Downloads\TWR-Private-Dev.zip"

The packager rejects synthetic CI packs, requires a real exported executable,
adds Laboratory.scene.jsonl.gz alongside the executable at
Content/Maps/Laboratory.scene.jsonl.gz, and writes a SHA-256 manifest.

Private packages must be located outside the Git repository.

**This script does not produce the original Roblox mesh/texture binaries**;
it only packages assets you already have permission to use. The exported
Godot build must also have any locally prepared Godot mesh/texture resources
included at export time, if those were installed. A successful package is
not evidence that the game is visually complete or has been playtested.
