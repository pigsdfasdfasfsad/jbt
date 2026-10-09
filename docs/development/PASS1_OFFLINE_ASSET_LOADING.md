# PASS 1 — Actual offline rendering resources (2026-10-08)

**Problem fixed:** previous map/weapon/zombie renderers searched only for
res:// paths packaged inside the Godot application. The private recovery ZIP
instead instructs the owner to copy Content beside the exported Windows EXE.
An unimported PNG or .res supplied AFTER export cannot automatically appear
inside res://. As a result, missing meshes had always remained box proxies.

Now place authorized locally converted image/mesh assets under the executable:

    ThoseWhoRemainOffline.exe
    Content/Assets/Textures/<numeric Roblox asset ID>.png
    Content/Assets/Meshes/<numeric Roblox asset ID>.obj

The shared OfflineAssetResolver can load those private files at runtime
without internet, Roblox, Studio or Godot editor import steps. The OBJ format
supports vertex/UV/face lists, triangulates polygons and computes normals.
Imported Wavefront geometry MUST be centered in local space and unit-normalized
to the Roblox MeshPart dimensions in the supplied source pack. There is a
strict file-size and triangle cap to protect runtime memory. Valid Godot
binary .res meshes remain supported when included in the PCK at build time,
not merely copied alongside the EXE. External raw .res files are NOT
automatically loadable after export.

A procedural, deterministic microtexture improves wood, concrete, brick,
grass, sand, rough stone and certain metal fallback surfaces whenever
an original texture isn't installed. This is approximation, not original art.

Unknown or unavailable original meshes continue to display dimensioned
part proxies instead of claiming exact visual fidelity. The installer should
record unresolved IDs in a private asset report.
