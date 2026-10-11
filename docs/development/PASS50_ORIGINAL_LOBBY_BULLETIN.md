# Pass 50 - Original 3D Lobby Bulletin Board

This pass restores source-authored, static 3D bulletin-board lettering from the original user-provided Roblox place. Pass49 provided the lobby's world geometry but no in-world SurfaceGui text.

## Verified source inventory

Owner TestPlace/TestPlace.rbxlx SHA256:
272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2

Pass49 source lobby pack SHA256:
98a8bf71c0561be814b2de01831b48e413e349ed4925fab3c0b8a403d7845b49

Pass50 owner-only Content/Lobby/LobbySigns50.json.gz SHA256:
6858501862b3b3344e13bdb91b002979592225722073e52e1848a2abb9ace732

| Source metric | Value |
| --- | ---: |
| Original SurfaceGui panels inspected | 26 |
| Original TextLabel records inspected | 872 |
| Physical 3D panels containing selected headings | 9 |
| Recovered author-written headings/category labels | 34 |
| Old player names, scores, ranks, and dynamic snapshot labels excluded | 838 |
| Original global leaderboard title pages | 4 |

Examples of actual source static text include PERSONAL STATS:, INFECTED KILLED:, HIGHEST LEVELS, MOST KILLS, WAVES SURVIVED, WEEKLY LEADERBOARD:, LAST UPDATED and original combat statistic categories.

The 838 excluded labels contain historical rankings, names, and values from the user's source snapshot. Displaying them as current online leaderboard data would be incorrect. They are deliberately not imported or updated. The original Roblox networking-dependent leaderboard and UIListLayout/SurfaceGui renderer are not recreated.

## Original 3D attachment and typography

The extractor tools/maps/extract_original_lobby_signs50.py reads inert Roblox XML, follows each original SurfaceGui's Adornee referent to its actual physical Part, rebases its original CFrame against the same lobby Start camera used by Pass49, and reconstructs TextLabel positions from nested UDim2 layouts, CanvasSize, original text colors, and font sizes. Every selected label becomes a Godot Label3D on the recovered source board's original front face.

Four overlapping source global leaderboard headings are mutually exclusive. The stage initially displays the HIGHEST LEVELS page; NEXT BOARD PAGE rotates through MOST KILLS, WAVES SURVIVED and AMOUNT DONATED. Independent personal/weekly/updates headings remain visible.

This is source-authored static typography and placement. The original full GUI appearance, special fonts, baked graphics, live scroll lists, external Decal pictures and dynamic online user values remain incomplete.

## Offline menu and gameplay

The existing main menu gains a BULLETIN BOARD button. It switches the Pass49 original 3D lobby to its authentic Leaderboards camera CFrame. An unobtrusive overlay provides explanation, NEXT BOARD PAGE and BACK controls. Returning to Start keeps all ten map selection buttons. ARMORY / LOADOUT still contains all 91 functional weapon rows and the original Loadout camera remains active there; PERKS still operates.

Opening a 15-wave match destroys the lobby and board stage. No enemy AI, weapons, collision, navigation, or wave progression is changed. If the sign pack is absent, stale, corrupt or fails the owner SHA checks, the previous 3D lobby and fully functional menu remain available.

## Reproduce privately

From the source repository root, with the owner-supplied TestPlace.zip:

    python tools/maps/extract_original_lobby_signs50.py --archive TestPlace.zip --out Content/Lobby/LobbySigns50.json.gz

Output also includes SOURCE_LOBBY_SIGNS_MANIFEST50.json. The tool runs fully offline without Roblox Studio, online asset access or executing original Luau scripts. Owner-derived 3D and text packs are not committed to GitHub or public Actions artifacts.

## Native Windows acceptance

Windows Actions exports the actual Godot/.NET Windows game. Its smoke uses only fabricated 8-part lobby geometry plus a 9-panel/34-heading synthetic bulletin pack. The test checks source UI counts, owner source validation, 34 Godot Label3D nodes, 31 initially visible static headings, original Leaderboards/Start camera switches, four board page cycles, a functional BACK button, all 91 armory purchase rows, and destruction of the lobby after game start. Both synthetic sidecars are deleted before the public Windows executable is uploaded.

The native smoke does not prove pixel-perfect original lobby typography or user-played 15-wave completion.

## Outstanding fidelity gates

The user still needs complete original maps beyond Laboratory, original custom MeshPart/CSG triangles and UVs, terrain, full Roblox GUI and live leaderboard data, animations/audio, rendering screenshot comparisons and a full real-time 15-wave player-controlled acceptance run. Pass50 is a bounded, independently verified source-graphics addition, not full 1:1 completion.
