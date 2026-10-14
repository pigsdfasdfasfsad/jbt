# TWR Offline

Durable development repository for the standalone offline reconstruction of **Those Who Remain**.

## Latest development: Pass 117 — playable-format original Laboratory source restored privately

[Pass117 source branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass117-original-laboratory-private-installer) ·
[Pass117 engineering report](docs/development/PASS117_ORIGINAL_LABORATORY_PRIVATE_INSTALL.md) ·
[Pass117 Windows CI](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass117-original-laboratory-private-installer)

- **Source geometry rather than more approximation:** the owner-provided original `TestPlace/TestPlace.rbxlx` Laboratory map was decoded offline into a private Godot-ready `Content/Maps/Laboratory.scene.jsonl.gz` containing **34,268 original-positioned geometry records**, 836 original enabled lights and original player/infected, item and fortification markers. The matching `Content/Collision/Laboratory.col26.gz` caches **18,130 source-transform collision shapes across 180 tiles / 217,204 triangles**; **4,501** MeshPart/Union bounds are explicitly approximate because custom triangle data is absent. An additional source-SHA-pinned `Content/Geometry/Laboratory.native28.gz` batches **18,212 original Part/Wedge instances into 786 draw batches across 47 tiles**, preserving their source positions and authored color/material tokens. These are source placements, **not** a recovered high-fidelity mesh/texture library.
- The authenticated original **scene, collision and native drawing cache** are packaged only in the private conversation ZIP, never in the public repository or GitHub Actions. `tools/maps/install_private_laboratory117.py` verifies three exact SHA-256 identities and installs them beside an existing `ThoseWhoRemainOffline.exe`, backing up replaced files. `build/Windows/package_private.ps1` now supports the verified Lab v2 scene plus its optional exact-source collision and native Part caches, while retaining legacy v1 and full ten-map private packaging. `LaboratorySourceLoader` already gives original map imports priority over Pass115/116 approximated blockouts.
- Public Windows CI validates installer safety with **no owner-private asset bytes**, and retains all earlier weapon, map, objective, navigation and native ten-map regression stages. Actual **graphical Windows traversal of the owner-private original source**, missing Roblox cloud-hosted 3D mesh/texture binaries, other nine maps, navigation sidecars and a tested full 15-wave victory remain substantial fidelity gaps. The private source-pack download does **not** contain a game executable.

## Earlier verified development: Pass 116 — procedural Laboratory floors and luminous equipment

[Pass116 source branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass116-laboratory-surface-materials) ·
[Pass116 engineering report](docs/development/PASS116_LABORATORY_SURFACE_MATERIALS.md) ·
[Pass116 Windows CI](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass116-laboratory-surface-materials)

- The source-absent Laboratory's formerly uniform-color boxes now have **five fully offline, deterministic tile/concrete/vinyl/acoustic texture patterns** on the existing **13 floor, wall and roof mesh surfaces**. Their true static collision, map entrance, navigation and objectives do not move. Server LEDs, monitoring screens and fluorescent diffuser meshes now emit light visually, and ten thin marked parking-bay stripes improve the garage's appearance without adding physics collision.
- Pass116 uses locally authored small textures, **not original Roblox texture imports or owner screenshots**. Existing Pass115 reception, server-rack, research, ward, examination, parking and cafeteria objects stay in place. The owner-original Laboratory asset loader still takes precedence and these materials are only applied inside the approximation-only Laboratory fallback, never to other nine maps.
- Native Windows `--smoke-pass116` builds real first-person gameplay on all 10 maps, asserts 5 generated 64×64 textures, 13 repainted render meshes, at least 28 lit status/diffuser parts, 10 noncolliding garage stripes, intact 4 original-floor colliders, Wave2 transitions and normal lobby returns. The preceding Pass115+Pass72 markers and all earlier Windows regression stages remain mandatory. **The dedicated Pass116 exported-Windows native test passed** in [Actions 38503928944](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38503928944); the full regression suite and screenshot-based visual parity must be checked independently. This still does not establish 1:1 fidelity or a distributable private-assets executable.

## Earlier verified development: Pass 115 — screenshot-guided Laboratory interiors

[Pass115 source branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass115-laboratory-reference-dressing) ·
[Pass115 engineering report](docs/development/PASS115_LABORATORY_REFERENCE_DRESSING.md) ·
[Pass115 Windows CI](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass115-laboratory-reference-dressing)

- **A map-focused fidelity pass, rather than another tiny combat animation:** the formerly empty Laboratory blockout now has **seven recognizable interior areas** based on the owner's original source screenshots: ALYTH reception counter and blue scanner; server cabinets/indicator lamps; research workstations; examination cots; parked garage cars; emergency hospital-ward beds; and cafeteria tables/plants. Approximately 190 collision-free primitive meshes and 10 soft local fixture lights are authored entirely as standalone Godot 3D objects; original screenshot images themselves are not published.
- **No original-map modifications:** new decor exists *only* in the source-absent Laboratory fallback. If owner original Laboratory source geometry loads locally, it takes priority and the visual dressing is never built. No source model proxy is mislabelled original, and no player/enemy physics, objective actors, navigation blockers or original private art bytes are added. The other nine maps keep their existing scenes.
- **Pass115 native Windows acceptance:** `--smoke-pass115` extends the physically grounded ten-map Pass72 wave 1/2 test. It verifies all seven render-only landmark zones in Lab, 140–260 mesh pieces, 8+ lights, no extra collision, no Lab props on any other map, correct reward and full lobby-return lifecycle. Historical Pass70–114 native and privacy regressions remain required. The new dedicated native gate and full workflow must pass before declaring the latest branch verified; screenshot parity, source geometry, full 15-wave manual playthrough and a distributable owner-private `.exe` remain outstanding.

## Earlier verified development: Pass 114 — slope-aligned fallen infected

[Pass114 branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass114-slope-aligned-corpse-settling) ·
[Pass114 engineering report](docs/development/PASS114_SLOPE_ALIGNED_CORPSES.md) ·
[Pass114 native Windows tests](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass114-slope-aligned-corpse-settling)

- Fallen R6 infected now ease toward the **actual nearby static terrain's surface normal** on ramps and hills, with no more than a 20° visual tilt. Rather than measuring only the lowest world-Y corner, the body lands using the signed distance of all actual visible, articulated mesh/AABB corners from the measured **inclined collision plane**. The old flat-ground correction remains in place when terrain is nearly horizontal or slope information is missing.
- The new `--smoke-pass114` fires the **real Glock** through the ordinary physics/camera/ammo/reward path, kills a real infected positioned above a temporary 16° **static test ramp**, and verifies four animated R6 limbs, collision normal, measured corpse tilt, at most **4.5cm support-plane clearance error**, and the same previous Pass70/111/112/113 gameplay success markers. The isolated source-lookalike gun fixture is removed after the test. All legacy ten-map, Load/Repair, Escort, infected, and privacy tests are still configured.
- **Windows native slope acceptance passed** in [Actions 38446320183](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38446320183), including the earlier Pass111–113 gun/death/ground tests. The remaining broad historical regressions were still executing at documentation time. This is still a visual approximation using one locally measured collision plane—not recovered original Motor6D/ball-socket ragdoll physics, multi-surface contact or rendered frame-by-frame fidelity. No public owner-private executable is distributed.

## Earlier development: Pass 113 — ground-settled infected death visuals

[Pass113 branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass113-ground-contact-corpse-settling) ·
[Pass113 engineering record](docs/development/PASS113_CORPSE_GROUND_CONTACT.md) ·
[Pass113 Windows tests](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass113-ground-contact-corpse-settling)

- Death bodies now probe the **real static collision floor once** in the next physics step, reject steep/missing/dynamic ground, and compute landing height using every visible mesh's current **R6 arm, leg and accessory poses**. The corpse eases vertically into contact at ~3.5cm clearance over the last portion of its existing fall, instead of always rigidly lowering its centre by 0.16m. Correction is bounded at 2.75m, with no new collisions, enemy damage, navigation or objective effects.
- The Pass113 exported-Windows real Glock death test verifies the four independent R6 limb pivots, actual static ground normal, a measurable adjustment, **≤4.5cm final body-to-floor error** and the unchanged shot/ammunition/reload/XP/wave cleanup markers. Existing Pass70/111/112 acceptance and all longer map, escort, interaction, and private asset exclusion regressions remain on this branch.
- **Windows native acceptance passed:** the actual Glock-kill, four-limb-fall and measured floor-contact gate passed in [Pass113 CI run 38443944987](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38443944987). The complete historical regression workflow is still being checked separately. This remains a render-only contact approximation rather than recovered original Roblox ragdoll physics; headless measurements do not establish screenshot parity or a player-tested full fifteen-wave match.

## Earlier verified development: Pass 112 — articulated R6 death silhouettes

[Pass112 branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass112-articulated-infected-death-pose) ·
[Pass112 engineering report](docs/development/PASS112_ARTICULATED_INFECTED_DEATH.md) ·
[Pass112 Windows CI](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass112-articulated-infected-death-pose)

- Builds on the real infected lethal hit and bounded, 5.5-second noncolliding corpse snapshot introduced in Pass111. Instead of **only tipping the whole dead body as a single solid piece**, the four recognizable original-source/fallback R6 shoulder and hip pivots are captured from the actual victim's death pose and animated independently, while maintaining the exact source material/mesh references in memory. Unsupported imported source anatomy stays a flat visual snapshot rather than fabricating recovered joints.
- The Pass112 **native Glock test** requires four correct R6 limb pivots, at least six real arm/leg meshes, more than 0.15 rad of visible independent motion across a normal R-key reload, and the earlier Pass111 single visual, camera ray, ammo, kill-credit, Wave2 and bounded cleanup checks. Temporary synthetic weapon fixtures are accepted **only** under explicitly named smoke flags and removed in CI. All preceding objective, map and private-asset exclusion gates remain.
- **Not yet source-exact:** The fall's curves are authored approximations; there are no recovered Roblox Motor6D or BallSocketConstraint dynamics, no per-joint rigid-body simulation or manual screenshot/image parity. Do not call this an authentic physics ragdoll or a 1:1 zombie animation. Pass112 must pass its own exported Windows workflow before being claimed native-verified.

## Earlier development: Pass 111 — temporary infected death bodies

[Pass111 branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass111-bounded-infected-corpse-fall) ·
[Pass111 engineering record](docs/development/PASS111_BOUNDED_INFECTED_CORPSE_FALL.md) ·
[Pass111 Windows validation](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass111-bounded-infected-corpse-fall)

- Real infected kills no longer need to make their 3D bodies vanish instantly: at the moment the normal kill callback fires, **visible mesh/material parts are snapshotted** from the already-rendered infected and tilted down into a short-lived, noncolliding fallen silhouette. The original owner-provided gameplay clip establishes that bodies appear on the ground; the 0.62s fall and 5.5s lifetime are source-unverified approximations, not a skeletal original Roblox ragdoll.
- The presentation budget is bounded to **16 simultaneous bodies / 64 mesh leaves each**, culls kills beyond 70 metres and clears on genuine wave/map transitions. Corpse nodes are not enemies, combat colliders, navigation blockers or reward actors. Lethal hit rules, ammo, kill credits, XP, special Burster effects and the previous original/source-fallback infected live rigs are untouched.
- `--smoke-pass111` reuses the **actual exported-Windows Glock camera ray + mouse-click** kill test, checks a noncolliding corpse is rendered from that death, observes the fall through a real reload, and verifies cleanup at wave end while retaining all existing Pass70 and Pass103–110 regression gates. **The new branch needs Windows CI confirmation** before Pass111 is called native-verified. Full source ragdoll physics, pixel-level similarity and distributable owner-private binaries remain incomplete.

## Earlier verified development: Pass 110 — visible vehicle rebuild during Repair

[Pass110 branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass110-repair-vehicle-assembly) ·
[Pass110 engineering report](docs/development/PASS110_REPAIR_VEHICLE_ASSEMBLY.md) ·
[Pass110 Windows build/checks](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass110-repair-vehicle-assembly)

- The fallback **Repair** depot now has a 3D pickup truck instead of only the generic green collection box. Two distinct approximate source-NAME-informed scales represent small Silverado (10 repair items) and large Tundra (12). Direct render-only Godot primitives form the body, windows, bumpers, head/brake lights, hood, bed, grille and wheels; no source meshes, private screenshots or collision bodies are added.
- During actual E-key Repair deposits, four engine parts, the two missing rear wheels and a progressive fuel meter assemble on the displayed vehicle. A completion indicator lights only after the final legitimate part; none of these visuals grant rewards or change the authoritative objective state. Owner-original map scene imports are not altered.
- The new `--smoke-pass110` uses real W/E player input to physically complete Wave1 Load and Wave2 Repair on Ranch/Manor and checks **each deposited part's visible state**, both final trucks, ADS/carry/frustum, generator, Repair HUD, reward and persistence behavior. **The first dedicated native Pass110 gate passed** in [Windows run 38356193184](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38356193184). A later revision whitelists exact part names and follows actual E-collected pickup order; its [Windows compilation and tests](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38356914570) must pass fully before calling the latest branch native-verified. Pixel-level TWR art fidelity, long human survival and a distributable owner-private `.exe` remain incomplete.

## Earlier development: Pass 109 — reference-guided generator and live Repair badge

[Pass109 source branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass109-reference-generator-repair-icon) ·
[Pass109 engineering record](docs/development/PASS109_REFERENCE_GENERATOR_REPAIR_ICON.md) ·
[Live Windows verification](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass109-reference-generator-repair-icon)

- **Actual rendered model improvement:** the earlier five-part objective Generator cuboid now has a pale industrial enclosure, dark skid feet, exhausts, service door, front intake vents and working-looking colored status indicators, reconstructed as 23 collision-free primitive meshes from the owner-supplied `Generator-Market.png` visual reference. The screenshot does **not** provide original Roblox mesh triangles, scale or source dimensions.
- **Actual HUD improvement:** a newly drawn round, cream-and-ink Repair wrench icon based on `RepairObjective.png` appears when the *real* Repair objective actors spawn and clears on their completion/wave transition. This uses gameplay objective activity, not whichever of two simultaneous objective text updates happened most recently. Neither owner-private screenshot is committed.
- **Native Pass109 acceptance passed** in [Windows run 38354422921](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38354422921): the exported Godot/.NET executable physically completes Wave1 Load and Wave2 Repair on Ranch/Manor, verifies two Generator world meshes, two genuinely camera-carried models, two active Repair-badge show/clear cycles, 40+ actual item pickups/deposits, and the earlier Pass108 ADS/4-viewport checks. The older broad regression tests in this workflow were still in progress when recorded. Source screenshots were *not* bundled, and rendered-pixel human playtests plus exact 1:1 art are not certified.

## Earlier verified development: Pass 108 — complete carried-object framing and native ADS stow checks

[Pass108 source branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass108-native-carry-ads-frustum) ·
[Pass108 Windows validation](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass108-native-carry-ads-frustum) ·
[Engineering record](docs/development/PASS108_NATIVE_CARRY_ADS_FRUSTUM.md)

- Pass106 already **passed 757 Python regressions and native exported-Windows 40-item carry/deposit checks** in [run 38348938021](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38348938021). Pass107 added camera-relative hip item placement and ADS-only visual stow, preserving authority; its [native Windows run 38351553766](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38351553766) completed successfully with **762 Python tests** and the full exported Windows regression checks.
- Pass108 now audits the **entire 3D model's primitive corners** inside the camera field of view, not merely its anchor, and bounds optional per-viewport visual scaling. Its exported native Windows test walks and deposits the real Wave1 Load and Wave2 Repair materials on Ranch/Manor, verifies **four** real synthetic-input RMB ADS stow/reappear transitions without dropping the material, and evaluates every carried 3D model at **4:3, 16:9, 21:9 and ultrawide** mathematical viewport ratios.
- The Pass108 exported-Windows executable and **dedicated Pass108 RMB/whole-mesh viewport gate passed** in [Windows Actions 38352406878](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38352406878): four right-button stows, at least 160 carried-mesh/viewport evaluations and 40+ W/E material pickups/deposits with matching counts. Earlier full Load/Repair and live-infected gates passed in the same run. The overall workflow's remaining regression steps were still in progress when recorded. Analytical/headless geometry and genuine gameplay input are **not** pixel-rendered screenshot or 1:1 source model certifications. Private owner content is excluded from public Git and compiled public .exe distribution remains disabled.

## Earlier native-verified checkpoint: Pass 104 — full physical Load and Repair across two waves

[Pass104 code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass104-full-physical-repair) ·
[Successfully exported Windows CI 38338396152](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38338396152) ·
[Detailed Pass104 report](docs/development/PASS104_FULL_PHYSICAL_REPAIR.md)

- **748 Python tests passed (one inherited warning), native Windows Godot/.NET export succeeded**, and all enabled material, stair, combat-adjacent, escort, navigation, wave-replay/persistence and private-source-exclusion gates passed. Verified gameplay code SHA: `840acf516025f3293e30734d08e1d90fd2e65e8f`.
- The real first-person player physically completed every material pickup and deposit for production **Wave1 Load (10 items/map)** and **Wave2 Repair (12 items/map)**, advancing through the production wave state machine without test teleports. **Ranch: 72.5m Load + 97.0m Repair; Manor: 90.5m Load + 76.4m Repair. Total 336.4m traveled, 44 materials carried and deposited, exactly-once mission rewards and persisted match results.**
- **Scope:** enemy spawning was frozen during each material mission after an accelerated wave start/transition, so this verifies collision-backed objective interactions, *not* a continuously human-survived real-time two-wave match. The ten public maps remain approximate reconstructed blockouts where original scene/model/collision data is unavailable. The retail game's complete art, audio, animations, behavior and a publicly distributed executable are not certified.

## Earlier verified checkpoint: Pass 103 — full production Load completed by walking

[Pass103 code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass103-full-physical-load) ·
[Verified exported Windows CI 38335724427](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38335724427) ·
[Detailed Pass103 engineering report](docs/development/PASS103_FULL_PHYSICAL_LOAD.md)

- **746 Python tests passed**, full Godot/.NET Windows export, native physics/objectives/combat/escort/persistence checks and private-asset exclusion passed. The final code SHA was `439c8c46ffae48756802cd88b7381681cc923293`.
- New exported-Windows `--smoke-pass103` completed both **real Wave1 Ranch and Manor Load objectives**. The player walked from the actual map spawn to every material and back using normal W/E gameplay input: **20 pickups, 20 deposits, 163.0 metres physically traveled, zero teleports**, each completed objective saved with exactly-once domain rewards. Previous full material tests used controlled repositioning.
- This is an isolated headless mission test with an accelerated wave start and enemy spawning held during item collection. It does **not** prove a human-survived real-time match or 1:1 original Roblox geometry, sounds, models or animation. Public executable downloads remain deliberately disabled.

## Earlier verified checkpoint: Pass 102 — all ten physical objective ingress routes

[Pass102 source](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass102-remaining-spawn-ingress) ·
[Successful exported Windows CI 38334803204](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38334803204) ·
[Pass102 engineering notes](docs/development/PASS102_ALL_MAP_SPAWN_INGRESS.md)

- Normal first-person player input and real collision now reach the first Load depot **from natural spawn across all ten public fallback maps**. The first five were accepted in Pass101; Pass102 completed **Cabin, Cargo, Laboratory, Expressway and upstairs Manor**. The same player then physically collected and deposited one material for each Load and Repair mission family on all ten fallback maps.
- Real Windows native regression gates passed Pass78 Manor stair ascent/descent twice, Pass95 Escort, Pass96 15-wave offline simulation/replay persistence, Pass98 full material objectives with test-controlled positioning, plus the ten-map physics and privacy gates. **Pass103 and Pass104 separately verify full physical Wave1 Load and Wave2 Repair on Ranch and Manor, with both checks now passing native Windows CI.
- This project remains a **development reconstruction**, not a release-equivalent 1:1 original. Original Roblox map geometry/art/audio/animations, real-time human-operated survival, and a downloadable standalone .exe are not certified by these headless CI tests. Owner-private source assets and compiled executables are intentionally not publicly uploaded.

## Earlier verified branch: Pass 92 — Expressway physically supported Escort

[Pass92 source branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass92-expressway-escort) ·
[Successful Windows CI 38296791093](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38296791093) ·
[Pass92 native verification report](docs/development/PASS92_EXPRESSWAY_ESCORT.md)

- **692 Python tests passed, zero Windows compiler errors; all prior Pass78/83/88/89/90/91 native regressions passed**, plus the new Expressway Wave6 Escort scenario.
- A separate real-collision Pass73 graph on the fallback **Expressway** enables its physical Escort survivor to navigate around authored road obstacles without replacing the existing infected `HighwayNavigation` graph. Windows measured **22.52m of NPC movement and 193 physical movement steps**, a single authoritative **4,000-credit/3,500-XP** objective reward and safe no-op replay.
- This completes **nine public reconstructed map Escort scenarios** verified in native Windows: Ranch, Mill, Bypass, Cabin, Cargo, District, Prison, fallback Laboratory, and Expressway.
- Owner-original scene packs remain private/unpublished. Exact Roblox meshes/collision/animation/audio, Manor multi-floor Escort, and real-time 15-wave manual survival are **not** claimed verified. Public Windows .exe artifact upload remains disabled by repository policy.

## Historical verified branch: Pass 91 — Eight-map collision-safe Escort completion

[Pass91 source branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass91-multimap-escort) ·
[Successful native Windows CI (38295592996)](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38295592996) ·
[Pass91 engineering report](docs/development/PASS91_MULTIMAP_ESCORT.md)

- **688 Python tests passed; 0 Windows compiler errors.** Native gameplay acceptance for Pass78/83/88/89/90 and all eight Pass91 Escort map runs succeeded.
- Wave5/6 **real physics Escort and exactly-once reward** verified on Ranch, Mill, Bypass, Cabin, Cargo, District, Prison and fallback Laboratory. Map-specific walk distances were 18.55–24.50m, without NPC teleporting.
- Pass91 floor/capsule-safe Escort starts, independently capsule-audited AStar routes, obstacle-safe waypoint following and corrected unsupported fallback doorway lips. Prior zombie navigation and recovered owner map content remain separate.
- **Not a retail 1:1 release:** Expressway and upper Manor still need physical Escort coverage; exact original map meshes, source NPC animation, original audio, 15-wave human survival, and bundled owner-private executable are not proven.

## Historical verified branch: Pass 90 — Real Escort arrival and reward verification

[Pass90 source branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass90-escort-completion-rewards) ·
[Successful native Windows build (38281900340)](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38281900340) ·
[Pass90 engineering report](docs/development/PASS90_ESCORT_COMPLETION_REWARDS.md)

- Fixes an Escort destination-height mismatch: approximate objective visual markers previously sat 1.1m above the grounded character's feet, making Pass89 physics navigation unable to validate the final destination.
- Five collision-floor probes and a survivor-capsule clearance query now ground candidate goals; final completion requires correct level, floor contact, nearby player and safe collision-supported arrival.
- **673 Python tests passed; zero Windows compiler errors; native Pass78/83/88/89/90 tests all passed.** The actual Ranch Wave5 Escort survivor traveled **22.69m (136 real physics steps)** and completed its objective. It awarded **4,000 credits and 3,500 XP**, with a separate **2,700-credit level-up bonus**, and a duplicate completion ID produced no second payout.
- Original source-map loading and the explicit no-public-artifact-release policy remain unchanged. Exact Roblox map meshes, authentic Escort animations, full-map Escort coverage and 15 real-time player-survived waves are not proven.

## Historical verified branch: Pass 89 — Collision-safe Escort survivor navigation

[Pass 89 source](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass89-escort-ground-navigation) ·
[Verified Windows CI run 38237636678](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38237636678) ·
[Pass 89 engineering record](docs/development/PASS89_ESCORT_GROUND_NAVIGATION.md)

- Public reconstructed ground-floor maps now give Escort survivors collision-verified Pass73 routing rather than blindly walking toward an unverified straight-line destination. Candidate waypoints and every next 55cm movement are audited against the active static physics world; invalid routes stop safely, while an earlier verified route remains usable through a transient failed re-anchor.
- A bounded set of alternative Escort destinations is used only if the original reconstructed local target is physically unreachable. Owner private source maps, Expressway and upper-floor Manor retain existing behavior until specific source-backed reconstructions exist.
- **666 Python tests passed; Godot/.NET Windows compilation/export completed with zero errors.** Native Windows Pass78, Pass83, Pass88 and **Pass89** acceptance checks passed in [run 38237636678](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38237636678).
- A real Ranch Escort survivor detoured **8.66m sideways / 17.25m forward** around an actual static wall without teleporting or clipping through it. The route was also correctly rejected when another wall was introduced after map navigation initialization.
- This does **not** certify original Roblox Escort meshes/animations/waypoints, complete Escort objective payouts on all maps, full 15-wave playthroughs or exact retail fidelity. Public Windows executable uploads remain deliberately disabled under repository policy.

## Historical verified branch: Pass 88 — Safer physical waves, loot and two-floor objectives

[Pass 88 source code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass88-physical-wave-hardening) ·
[Successful native Windows CI (run 38229534100)](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38229534100) ·
[Detailed Pass 88 acceptance record](docs/development/PASS88_TEN_MAP_PHYSICAL_WAVES.md)

- **Five cumulative passes (84–88)** harden reconstructed gameplay while preserving original owner-private source-pack geometry and marker loading. Pass84 prevents Manor upstairs Radio/Unpack secure progress from accumulating from the wrong floor. Pass85 requires collision-safe fallback infected spawns at least 11 metres from the player. Pass86 samples walkable surfaces and retries fallback natural loot markers. Pass87 cycles each other fallback map's objective positions over waves. Pass88 performs native ten-map Windows physics/wave acceptance.
- **660 Python regressions passed**, Windows Godot/.NET export succeeded with **zero compiler errors** (one inherited Pass36 warning), and native Pass78, Pass83 and Pass88 gameplay checks all succeeded in [run 38229534100](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38229534100).
- All ten **public reconstructed** maps instantiated an actual game scene, physically grounded player, six floor-supported natural pickups, and real infected on successive Wave 1/Wave 2 transitions. Manor Wave 3 Radio rejected downstairs progress and completed upstairs. These are controlled native smoke tests with accelerated wave clocks, not 15 human-played waves.
- **Fidelity boundary:** original terrain, full map meshes/collisions, weapon and infected animations, original audio, identical spawn curves, all-objective completion and packaged owner-private Windows distribution are not authenticated by this checkpoint. This is not a full 1:1 release.

## Historical verified branch: Pass 78 — Three Manor stair flights, physical ascent and descent

[Pass 78 source code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass78-manor-bidirectional-stairs) ·
[Verified Windows build and all 12 native stair movement checks](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38213401718) ·
[Pass 78 engineering record](docs/development/PASS78_MANOR_BIDIRECTIONAL_STAIRS.md)

- Fallback Manor navigation now allows physically verified **upstairs-to-downstairs infected pursuit** across each of the three reconstructed stair flights. Seven collision-floor samples and line-of-sight checks gate the downhill route; replanning can resume from the nearest downhill ramp segment rather than sending an enemy back upstairs.
- The original 1.58m flat-ground waypoint tolerance no longer permits skipping downhill route heights on Manor. An infected cannot perform a melee attack from another floor merely because the player's horizontal coordinates are nearby. Movement remains ordinary Godot `CharacterBody3D.MoveAndSlide`, never animation-triggered teleportation.
- **All 12 native Windows traversal tests passed** on a production reconstructed Manor `GameplayRoot`: real W-key player ascents, S-key descents, infected uphill and downhill movement on **West Wing, East Wing and Front Hall** stairs.
- Measured player rises/drops: West **2.78m/2.69m**, East **2.78m/2.69m**, Front **2.65m/2.55m**. Actual infected rises/drops: West **2.70m/2.55m**, East **2.70m/2.39m**, Front **2.62m/2.50m**. Each occurred with solid physics support, no forced enemy movement and zero source-jump attempts.
- **632 Python regressions passed**, Godot/.NET Windows export succeeded with **0 compiler errors**, the Pass78 native marker passed, domain-only ten-map progression remained passing, and owner-private content exclusion passed.
- **Fidelity boundary:** No retail-original Manor stair meshes or collisions are authenticated by this test. Routes through distant upstairs rooms and between wings, other-map multi-storey navigation, authentic original assets, 15 human-played waves and final packaged user-ready Windows EXE are still unfinished.

## Historical verified branch: Pass 77 — Manor stair and balcony physical traversal

[Pass 77 source code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass77-manor-stair-traversal) ·
[Successful Windows CI: 627 Python tests and full native Front Hall ascent](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38212138620) ·
[Pass 77 engineering record](docs/development/PASS77_MANOR_STAIR_TRAVERSAL.md)

- Three **approximate fallback** Manor stair flights now have continuous sloped Godot collision ramps, eight decorative (noncolliding) treads each, and widened upper-deck connectors. All three staircase collider assemblies were created in a production `GameplayRoot`, but the physical traversal test specifically drove the **Front Hall** route.
- First-person player reached Front Hall's actual upstairs collision landing using real W-key movement and normal physics; Windows acceptance logged **2.68m vertical ascent** and a grounded upper landing without teleporting.
- The real `InfectedAgent` followed collision-audited uphill waypoints and reached the upper landing with **2.62m vertical ascent**, without jump attempts, physics bypass, or position assignment during motion.
- Native debugging caught several real collision and AI bugs: a flat landing lip, a coplanar overlapping connector seam, early route reset and overly permissive 1.58m flat-ground waypoint acceptance. The verified route now requires close **0.35m horizontal / 0.15m vertical** arrival before switching to balcony pursuit.
- **627 Python regressions passed; zero Godot/.NET compiler errors; native player/infected upper-landing markers and owner-private asset exclusion passed**. The ten-map 150-wave domain-only smoke also remains green, but is not a human-played 15-wave survival run.
- **Fidelity boundary:** These are reconstructed fallback collision surfaces, not recovered retail Manor stair meshes. West/East stair full player/enemy traversal, upstairs descent, authentic UV/artwork, original collision layouts, 15-wave manual survival and final private offline EXE packaging remain unverified.

## Historical verified branch: Pass 76 — Objective, Pickup, Supply and Fortification Physics

[Pass 76 source code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass76-interaction-objective-fortification) ·
[Successful Windows export, 622 regressions and two native interaction smokes](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38208475688) ·
[Pass 76 engineering record](docs/development/PASS76_INTERACTION_OBJECTIVES_FORTIFICATIONS.md)

- **E-key pickups** are now collision-occluded and resolve to one nearest visible item per held E press, including when the first pickup is queued for deletion in the same frame. This fixes multi-looting of clustered helicopter/supply drops and wall-through pickup exploits.
- **Load/Repair materials** require a world-visible grab/deposit path. The native Ranch test collected materials and deposited exactly **1/10 items**, while a wall prevented collection.
- **Hammer placement** requires a real walkable floor and free placement footprint; a blocked attempt does not consume inventory. Direct LMB begins the first swing without waiting for held-input polling, and the original hammer cooldown still prevents double swings.
- **Clap Bomb/Jack** radius is scaled from recovered **25/35 Roblox studs** to Godot metres and filtered by world collision; Barbed Wire does not hit through walls. A native Ranch Clap Bomb killed a visible infected and granted real credits while preserving an occluded zombie and tanker beyond the 25-stud radius.
- **Mounted 50 Cal** now directly hits layer-2 infected bodies with a first-click input handler plus the existing automatic firing cadence; it grants kill credits and dismounts on death. **Supply pallet** landing samples actual walkable geometry and keeps a single landing callback, confirmed on a 54m-high synthetic test floor.
- **622 Python regressions passed, zero Godot/.NET compiler errors**, both Ranch/Manor exported Windows acceptance markers passed, ten-map domain-only `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` passed, and owner-private content exclusion passed.
- **Fidelity boundary:** All native scenario obstacles/floors are synthetic; this does **not** establish source-authentic map collision, complete multi-floor traversal, original item/fortification art, a human-played 15-wave session, or a private complete EXE release.

## Historical verified branch: Pass 75 — Collision-safe weapon blasts and crosshair-first firing

[Pass 75 source code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass75-blast-line-of-sight) ·
[Successful Windows export, 613 tests and native rocket/Frag acceptance](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38206918852) ·
[Pass 75 engineering record](docs/development/PASS75_WEAPON_BLAST_OCCLUSION.md)

- Launchers now sweep against **Godot layer 2 infected colliders** as well as world/source collision. Previously the default layer-1-only ray could fly through zombies.
- Launcher and Frag splash now require physical line-of-sight to infected and damage-objective centres; explosions no longer damage targets through solid reconstructed walls. Recovered **300 Frag damage / 30-stud blast radius / 3-second fuse** remain unchanged.
- Physics Frags now rest on a second valid **upward-facing ground contact** instead of sliding into/tunnelling through a floor as the source fuse continues. The native test exposed this actual defect and validated the correction.
- All first-shot bullet/shotgun/launcher directions are determined **before** the current shot's camera recoil; recoil still changes subsequent aim and visual weapon behavior. Domain ammo, kill credits and XP authority are unchanged.
- **613 Python regressions passed**, Godot/.NET Windows export finished with **zero compiler errors**, and two real `GameplayRoot` scene smokes passed: on Laboratory, actual rocket direct infected hits and visible objective damage; on Manor, a **resting live Frag fuse**, occlusion-safe kill rewards and untouched out-of-range objectives. Owner-private original assets were excluded.
- These checks used **clearly synthetic test floors/walls**, not recovered retail map interiors. Native full 15-wave player sessions, original mesh/animation parity and a downloadable privately assembled Windows EXE remain unfinished.

## Historical verified branch: Pass 74 — Special-infected collision safety and real native combat

[Pass 74 source code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass74-special-infected-safety) ·
[Successful Windows export, 607 Python tests and native ability smoke](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38205313363) ·
[Pass 74 engineering record](docs/development/PASS74_SPECIAL_INFECTED_SAFETY.md)

- **Burster** instant detonation now respects solid-wall collision rather than dealing 27.5 damage through walls. Its original 20-stud radius stays converted to Godot metres; gas masks protect against the **spore cloud**, not the direct blast.
- **Bolter** leap attempts now require actual physical floor support at the target. Its approximate 28m/s leap retains original-style fixed direction and recovery, with travel duration bounded to 0.24–0.55s by target distance rather than overshooting close players.
- Native exported Windows **Ranch** verified one real Bolter contact inflicting **15.00 damage**, wall-blocked launch and ground-supported landing, plus **all eight infected types' smoke multipliers**, Riot melee resilience, and Bloater fire vulnerability. Real **Manor** projectiles and hazards verified a wall blocked Burster's initial blast, gas mask prevented cloud ticks, a clear blast inflicted **27.50 damage**, and Bloater couldn't fire through a wall but could fire a real damaging projectile in clear line of sight.
- **607 Python regressions passed**, Windows Godot/.NET compiled with **zero errors**, both native special-attack markers passed, scripted domain ten-map completion remained passing, and owner-private data exclusion passed. CI does **not** publish private original assets or an executable.
- This is safety/fidelity improvement to **reconstructed** specials and **synthetic test walls**, not original enemy animation/mesh parity, exact original map pathfinding, or a human-controlled 15-wave completion.

## Historical verified branch: Pass 73 — Collision-sampled fallback navigation and real-gameplay smoke

[Pass 73 source code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass73-fullmap-ground-navigation) ·
[Successful exported Windows build, 601 regressions and native acceptance](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38202837479) ·
[Pass 73 engineering record](docs/development/PASS73_FALLBACK_GROUND_NAVIGATION.md)

- Nine **approximate** maps now construct bounded, physics-sampled **ground-floor AStar3D paths** during real Godot gameplay. Source-recovered Laboratory navigation and Expressway's existing road graph remain authoritative where available; fallback maps do **not** gain recovered Roblox-original navigation or higher-floor stairs.
- Native Windows verified 799 reachable Ranch graph nodes / 2,718 edges. An actual infected **walked around a synthetic 12m-wide blocking wall** (7.75m lateral detour, 8.90m net pursuit gain), rather than teleporting or ignoring solid collision.
- The same exported Windows game accepted **real simulated Godot W/Shift/Space/E input**: 1.35m walking, 1.82m sprinting in equal test intervals, physical jump/landing, Medkit healing and Radio objective rewards through the authoritative local domain. Dead-player item and objective grants were prevented.
- **All ten maps** passed their native per-map readiness checks. Nine fallback maps produced 595–1,075 collision-backed ground nodes; Expressway retained 204 road-graph nodes. **601 Python regressions passed**, Godot/.NET Windows export succeeded with **0 errors**, and the private-asset exclusion gate passed.
- No owner-original source packs, private profiles, custom mesh/texture bytes or publicly downloadable Windows executable were included. This is **not** ten 1:1 retail maps, a source-authentic navmesh, full multi-floor navigation or manually survived 15-wave matches.

## Historical verified checkpoint: Pass 58 — Source-guided weapon silhouettes

[Pass 58 source code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass58-original-weapon-silhouettes) ·
[Successful Windows build and native gun silhouette test](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38117871532) ·
[Pass 58 engineering and private geometry audit](docs/development/PASS58_SOURCE_WEAPON_SILHOUETTES.md)

- Expanded actual first-person and Armory 3D gun geometry with bounded procedural **magazine, stock, grip, receiver, open trigger-guard and optic-glass silhouettes**, all based on source object names, sizes and original part transforms
- **Authentic locally installed OBJ mesh geometry always takes precedence**; these silhouettes are geometry approximations, not recovered Roblox custom MeshPart triangles
- Owner source audit of **98 authored tool models / 1,484 original part records** identified **390 recognizable visible shapes** eligible for silhouette/tube reconstruction and **525 remaining unclassified MeshParts** that retain conservative bounds; all **1,137 original unique mesh IDs** still lack embedded triangle streams
- Armory now displays mesh origin/quality counters (genuine local OBJ, rebuilt silhouettes, lens disks, tubes and bounding proxies)
- **525 Python regressions passed** and Godot/.NET Windows export succeeded with **0 compiler errors and 1 inherited Pass36 warning**, including a native test with 12 visible synthetic parts and a genuine handmade local OBJ file; baseline simulated progression and private asset exclusion also passed
- An owner-only 19 MB ZIP adds per-weapon source geometry gap metadata to the Pass57 private asset kit; the public repository does not upload the private original asset files or Windows EXE
- Original real-time player-controlled 15-wave sessions, complete original mesh triangles and map/weapon visual 1:1 parity remain unfinished

## Historical verified checkpoint: Pass 50 — Original 3D lobby bulletin board

[Pass 50 source code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass50-original-bulletin-board) ·
[Successful Windows build and native bulletin test](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38102627857) ·
[Original Source SurfaceGui recovery notes](docs/development/PASS50_ORIGINAL_LOBBY_BULLETIN.md)

- Restored the original 3D lobby's **static, source-authored bulletin signage**: nine physical panels and **34 original heading/category labels**, located using each Roblox SurfaceGui's actual Adornee CFrame, CanvasSize, nested UDim2 coordinates, source colors and font sizes
- The source contained **26 SurfaceGui panels and 872 text labels**, including **838 historical leaderboard rankings, player names and old scores**. These historical values are not presented as live online rankings and are excluded from the private sign pack
- Adds a real **BULLETIN BOARD** action on the map-selection menu, switching the recovered original **Leaderboards camera**. Four restored global ranking category headings (Highest Levels, Most Kills, Waves Survived, Amount Donated) rotate on an actionable NEXT BOARD PAGE button; BACK restores the original Start camera and ten map options
- All 91 original-armory selection buttons, perks, Laboratory source world, private 3D weapon assemblies, infected variants, light/audio packs, and existing standalone 15-wave systems remain operational. When entering gameplay, the lobby and sign nodes are fully removed
- The owner-only Windows Pass50 ZIP combines the freshly compiled executable with the unchanged prior source packs and the new SHA-pinned Content/Lobby/LobbySigns50.json.gz file; private original XML and sign data are not uploaded to the public CI artifact
- **471 Python regressions plus the real Godot Windows exported executable smoke passed**, including nine fabricated 3D sign panels, 34 Godot Label3D instances, camera/page/back-button interaction, unchanged 91-row armory and match start
- This is **not** the full Roblox SurfaceGui system. Fonts/UI shaders/online score updates remain approximated or missing. Custom MeshPart/CSG triangles, nine complete original worlds, animations/audio parity and a human-controlled 15-wave acceptance run are still outstanding

## Previous verified source branch: Pass 49 — Original 3D source lobby

[Pass49 code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass49-original-lobby-3d) ·
[Passed Windows build and 3D menu acceptance test](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38101644595) ·
[Source lobby camera, geometry and limitations](docs/development/PASS49_ORIGINAL_3D_LOBBY.md)

- Recover **818 original serialized lobby geometry records**, of which **774 are visible**, from the user's original `Workspace/Lobby` place hierarchy. This was a real missing 3D scene, not a synthetic map invented from TWR screenshots
- Restore **8 exact source camera CFrames** (Start, Shop, Loadout, Perks, Options, Gifts, Leaderboards, WeaponNode), **5 original loadout placement points**, and **18 original light emitters**, all source-relative to the original Start camera for stable Godot positioning
- The owner's offline Windows ZIP includes SHA-bound `Content/Lobby/SourceLobby49.json.gz` and provenance manifest, alongside all previous Laboratory, weapon, infected, map, UI art, and audio content. This real owner-only pack stays outside public GitHub
- The original 3D lobby renders behind the current actionable 2D map menu. The original **Loadout camera** is selected when opening the 91-row armory and the original **Perks camera** for the perks screen. Source lobby geometry/cameras are removed from the active wave scene. Without a valid private pack, the previous 2D menu still works
- **505** visible Lobby MeshPart/CSG parts still require actual-size geometric proxies because their externally hosted original mesh triangle data is missing. Original Roblox `SurfaceGui` panels, clothing/animations, textures and postprocessing are **not claimed recovered**
- **466 Python regressions and real exported Windows integration passed** with an entirely fabricated 8-part lobby scene, proving actual camera switching, intact ten-map selection/91-weapon armory and game transition. This is not a screenshot-verified retail lobby comparison
- Remaining full-game fidelity gates: original missing mesh triangle binaries/terrain, the other nine complete world maps, original animations/sound/skybox fidelity and a real-time player-controlled 15-wave match

## Previous verified source branch: Pass 48 — Original infected AI accessories

[Pass48 source code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass48-infected-source-asset-kit) ·
[Verified Windows build and native smoke](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38100839832) ·
[Source asset evidence and limitations](docs/development/PASS48_INFECTED_SOURCE_ASSET_KIT.md)

- Recovered the previously unused **ReplicatedStorage/Assets/AI/Infected** source kit from the owner's TestPlace.xml; extracted **35 original visible accessory components from 22 genuine source asset groups**, covering all **eight playable infected categories**
- Assembled **15 explicitly reconstructed source-and-proxy visual combinations** using original Bloater/Burster head/torso/arm pieces, Hazmat hood/respirators, Military and Riot helmets, and Generic hair/head details
- Each model has **six reconstructed standard R6 body proxy parts**. Missing custom MeshPart triangles, UVs, clothing and original animation tracks are **not** claimed as recovered. The 15 combinations are not the verified original retail spawn distribution
- The owner-only Windows ZIP adds a compressed **3 KB** original-bound source kit `Content/Enemies/InfectedSourceVariants.json.gz` with SHA-256 `9ef29ce243992722dbab0ea5f5f78872ea4a6b5da4718c925662a34cfb743d64`. Original Laboratory, weapon source assemblies, UI art and other previously recovered assets remain present
- The Godot source kit reader verifies original place identity and exact pack SHA, has compressed/decompressed budgets, excludes invisible marker geometry, and uses the previous procedural fallback when the private pack is unavailable
- F10/F11 now reports the number of live source-kit infected, actual source-accessory parts, reconstructed R6 proxy parts and missing external mesh proxies
- **461 Python regression tests and native exported Windows acceptance passed**, including all eight entirely fabricated infected types and an actual Godot infected actor. Private original packs remain outside the public GitHub CI artifact
- **Not yet 1:1:** nine remaining full original map worlds, missing cloud mesh triangle files/terrain, original zombie animation/clothing, retail media and a full human-controlled 15-wave acceptance run remain incomplete

## Previous verified source branch: Pass 47 — Original 3D source tool assemblies

[Pass 47 development branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass47-original-weapon-assemblies) ·
[Successful Windows build and native tests](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38099854679)

- Recovered the user's exact Roblox-place ReplicatedStorage/Models/Tools original **98 model assemblies**. The owner-only Windows ZIP now includes the previously missing 54 KB source-derived 3D tool pack, representing **1,484 authored 3D parts** with original CFrame transforms, dimensions, RGB and material tokens.
- **All 91 selectable catalog weapons** match original tool model assemblies after two documented spelling aliases. Pack contains 100 total lookup keys including original names and two aliases; Molotov, Frag and other tool models also remain available.
- Skip **548 original invisible helper/locator parts** that should never have been rendered as bounding cubes; **936 originally visible parts** retain source colors and geometry envelopes. 1,282 MeshPart and 16 UnionOperation references still lack original external triangle bytes: bounding proxies are explicitly identified, not misrepresented as originals.
- The Godot loader now requires exact SHA-256 binding to the original XML and original derived weapon pack, rejects synthetic fixture packs during normal gameplay, and retains previous generic model fallback when an original/private source pack is unavailable.
- **F10/F11** reports source-verified current first-person weapon model, visible original source components, and missing custom-mesh proxies. Pass46 source-map artwork, Pass45 proxy culling, Pass42 navigation and other earlier progress remain present.
- **457 Python regressions passed** and the real exported Godot Windows executable passed a fabricated six-model native source-assembly test covering five guns, throwable, hidden markers, native sphere, original-coordinate relative transforms and unavailable MeshPart fallbacks.
- **Still not 1:1:** missing externally hosted original weapon mesh polygons/UVs, original animations and 3D textures; nine full original-world maps, terrain, retail audio fidelity, full enemy traversal and a player-controlled real-time 15-wave match remain outstanding.

## Previous verified source branch: Pass 46 — Owner offline reference art

[Pass 46 code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass46-offline-original-art) ·
[Successful Windows native QA](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38098969880) ·
[Art provenance and limitations](docs/development/PASS46_OWNER_REFERENCE_ART.md)

- Supplied owner-side archive **images.zip** is now integrated into the **offline Windows map menu, armory/loadout screen and existing live-ammunition weapon HUD**. The previous Windows pack lacked the Content/Art folder even though the runtime already supported loading it
- Source images provide **10 map reference screenshot cards and 88 of the 91 weapon reference silhouettes**. Ruger 10-22, Sawn Off Shotgun and Spiked Baseball Bat retain safe text/silhouette fallback instead of borrowing incorrect source images
- All 98 images are packaged in the **owner-only** Content/Art folder under a validated source-art manifest with per-image SHA-256 checksums and a matching input-image provenance report. The generator works offline and does not fetch Roblox/Wiki assets
- The public repository contains no user image data. Public GitHub Windows CI injects entirely synthetic 2x2 PNGs, tests actual menu cards, **all 91 functional armory buttons**, the images on three test weapon rows and a live weapon-dial icon, then removes the synthetic assets before uploading the executable
- **453 Python regression tests and the exported Windows native integration smoke passed**. The compiled owner-side ZIP combines this new executable with the preserved Pass45 original Laboratory map/collision/navigation, earlier native source geometry/light/audio and private 2D image references
- **Not original geometry:** these source pictures do not reconstruct the original 3D first-person guns or missing custom mesh triangles, terrain, map props and animations. The other nine complete original loaded maps, full real-world FPS QA and an actual human-controlled 15-wave match remain unfinished

## Previous verified source branch: Pass 45 — Laboratory fallback proxy spatial culling

[Pass 45 source code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass45-lab-proxy-visibility) ·
[Successful Windows build / native QA](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38097949393) ·
[Original source geometry audit and technical notes](docs/development/PASS45_SOURCE_PROXY_STREAMING.md)

- This pass adds distance culling to **15,678 original-positioned visible MeshPart/UnionOperation/SpecialMesh fallback proxy records** that Pass44's ordinary native-Part pack does not contain.
- Uses a 256-source-stud spatial tile grid and shares material/mesh resources across tiles, rather than keeping geographically distant proxies in world-wide appearance batches. The original loaded Laboratory source produces **669 proxy batches**, with estimated **455 initially visible** (145m radius) at source spawn #1.
- F1 switches *only the proxy culling* ON/OFF for same-match performance comparisons, default **ON**. F10/F11 reports active source proxies and real p50/p95/p99 frame times; no physics, enemy spawns or source navigation are changed.
- Original native 18,213 ordinary source Parts, collision, maps, fallback meshes and Godot offline runtime remain present. If the original native28 pack is absent the existing global source batching remains supported.
- No original custom MeshPart/CSG triangle data is reconstructed; improved FPS is **not claimed** until player-recorded F1 ON/OFF performance reports confirm it. The exported Windows build passed **448 Python regressions and the real Godot synthetic proxy-visibility smoke test**. Public CI artifacts contain no original source data.
- Outstanding completion gates remain the other nine missing full original map scenes, authentic custom meshes/terrain/recordings/animations, full enemy traversal and an actual human-controlled 15-wave match.

## Previous verified source branch: Pass 44 (October 10, 2026)

[Development code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass44-native-lab-streaming) · [Successful Windows CI](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38097138396) · [Technical notes](docs/development/PASS44_NATIVE_LAB_RENDER_STREAMING.md)

- **Missing content fixed:** Earlier owner Windows ZIPs lacked the SHA-bound `Content/Geometry/Laboratory.native28.gz` native source renderer pack. The Pass 44 owner package includes it.
- Recovered **18,213 source-positioned, size/rotation/material-preserving native Part and Wedge instances**, grouped into **787 spatial draw batches across 47 tiles** with bounded distance culling.
- Exact source spawn #1 geometry audit: 510/787 batches within 145m; 277 off-range batches and 6,228 instances are eligible for hiding. This is **static geometry culling**, not measured GPU FPS.
- The source loader now preserves all 337 `SpecialMesh` legacy visual fallbacks (instead of inadvertently hiding type=6) and retains the original collision cache.
- F10/F11 reports source-stream instance/batch/visibility counts. **443 Python tests and the exported Windows native smoke passed**, including near/far culling, source collision and special-mesh preservation.
- Owner source files and derived private Part pack **remain outside GitHub Actions artifacts**. The game is playable offline but still lacks nine full original map scenes, original custom mesh triangle data, terrain and retail media parity. A real 15-wave private-source FPS playthrough remains unverified.

## Previous verified source branch: Pass 43 (October 10, 2026)

[Pass 43 development branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass43-physical-jump-verification) ·
[Successful Windows native acceptance](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38094133273) ·
[Pass 43 engineering and playtest notes](docs/development/PASS43_PHYSICAL_TRAVERSAL_ACCEPTANCE.md)

- Fix the former **false-positive infected jump landing** check. Source-linked jumps now only count as successful when Godot's physical infected body makes grounded contact **near the actual destination waypoint**, not by hitting any floor
- Count separate launches, confirmed landings, failed jumps and active jumps; **preserve totals across enemy deaths and wave cleanup** for meaningful match QA
- Add bounded **2,048-frame rolling p50/p95/p99 frametime** collection, peak infected count and F10 display; F11 now writes timestamped JSON archives as well as the backwards-compatible latest snapshot
- Introduce a native Windows **two-platform physics-gap acceptance test**: synthetic solid floors are separated by a real 4.2-stud pit; a real Godot infected is required to **physically cross and land** without teleporting
- Add an actual **blocking wall** on the same test jump. Ground contact or timeout on the takeoff side must register **failure**, not a landing
- Native CI also exercises real Godot actor cleanup and **15 accelerated waves**. **439 Python regression tests** and exported Windows traversal tests passed. This is not a full original-map human-controlled match
- Preserve the owner-private Pass 39 Laboratory geometry, Pass 40/41/42 navigation packs, original-positioned wall/prop fragments, and ten-map lighting and sound metadata in the owner-only Windows ZIP. Public Actions artifacts remain private-source-free
- Still incomplete: all nine remaining full original map scenes, missing custom MeshPart/CSG triangle bytes, original terrain, source animations/audio and human-performed 15-wave traversal and frame-time benchmarking

## Previous source-development branch: Pass 42 (October 10, 2026)

[Pass 42 source](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass42-jump-navigation) ·
[Pass 42 source and limitations](docs/development/PASS42_LABORATORY_JUMP_NAVIGATION.md) ·
[Windows CI and native test](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38093325817)

- Recover original **Pathfind** settings: `AgentCanJump=true`, `AgentCanClimb=false`, `WaypointSpacing=4` studs. The previous all-walking graph could not represent these actions
- Create a **SHA-bound TWRNAV42** graph with **9 source-supported jump actions**. Preserve all **14,726** prior waypoints, all **43,937** walk connections, and add only 9 typed jumps
- Decrease Laboratory's graph disconnects from **117 to 108**; the player-start area's potentially route-connected waypoints expand from **291 to 1,681**. These edges are approximate and still governed by actual Godot collisions
- Infected in Laboratory can follow bounded ballistic jump actions. Existing source-based **F9 entry assistance remains OFF by default**, and earlier nav41/nav31 packs remain available as fallbacks
- The owner-private three-floor **F4 -> N** map displays cyan jump links, gold native bridge repairs, connected green regions, and disconnected purple regions. F10/F11 reports loaded jump actions and observed actor jumps
- Windows Actions compiles and exports the **actual Windows executable**, runs all Python checks, and tests a **fabricated** source-bound jump route plus accelerated real Godot actor/round transitions through **15 waves**; it excludes every original and synthetic private navigation file from the public artifact
- **Not complete:** an accelerated 15-wave runtime smoke is not a full player-controlled match. Original terrain voxels, custom mesh triangles, nine fully reconstructed maps, retail animations/recordings and full graphical/physics traversal QA are missing

## Previous source-development branch: Pass 41 (October 10, 2026)

[Pass 41 source](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass41-source-native-bridges) ·
[Lab bridge repair details](docs/development/PASS41_SOURCE_NATIVE_BRIDGE_REPAIR.md)

- **Laboratory** now has a SHA-pinned TWRNAV41 v3 graph preserving the original reconstructed 14,726 source-sampled nodes and 43,920 previous graph edges; only **17 new short native-Part-floor-validated bridges** were added, with nine collision/floor checks per bridge, for **43,937 total edges**
- Existing **133 disconnected** map graph regions become **117**; the 291-node original player-start region still has no verified path to the Lab interior. This is **not** original Roblox navigation mesh parity
- **F4 / N** displays three new owner-private source connectivity diagrams; 17 new bridges appear in gold. F4 switches the map view, Left/Right selects floors. F10/F11 records the number of loaded bridges and remaining disconnected regions
- Existing optional **F9** accessible infected-spawn fallback remains OFF by default, retains collision checks and minimum 24/14/10m candidate distances. It does not claim to reproduce the original horde spawn behavior
- New public-surface validation: deterministic conversion tests, unsupported floor/wall/proxy rejection, SHA checks, and actual exported Windows synthetic-fixture navigation/F4/F9 smoke. Original owner source packs remain **private** and out of GitHub Actions artifacts
- **Still incomplete:** Nine fully reconstructed map scenes, Laboratory terrain/custom mesh binaries and player-to-interior routes, full source-faithful enemy navigation and animations, original audio, visual pixel-match and a player-controlled 15-wave Windows performance pass

## Previous verified branch: Pass 40 (October 10, 2026)

[Pass 40 source](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass40-grounded-lab-navigation) ·
[Windows compile and native tests](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38085199252) (423 Python tests passed, exported Windows game verified with synthetic original-scene fixtures)

- Adds an owner-private SHA-bound **14,726-node / 43,920-edge** grounded navigation graph from **18,130** original-positioned Laboratory collision bounds. This is an **approximation**, not the missing Roblox navigation mesh
- Source floor geometry remains physically incomplete: 133 graph components, all 8 original player markers anchored, only 3 of 15 original infected markers near any waypoint and **no exact infected source spawn connected to the player's initial region**
- **F9** is OFF by default and optionally uses physics-checked connected entry positions at 24m, then 14m or 10m if no distant candidate exists. This is an explicit accessibility approximation, not original retail spawn behavior
- **F4** opens original Lab floor plans; **N** switches to three owner-only, checksum-verified floor navigation connectivity diagrams; **Left/Right** changes floor height band
- F10/F11 records graph integrity and adaptive F9 placements. Source navigators and diagnostic PNGs remain owner-private and are removed from public GitHub Actions artifacts
- Windows smoke in the exported Godot executable tests real floor-capsule collision checks, opt-in F9 wave spawn, and F4/N switching using **entirely synthetic** source data. A human-controlled 15-wave original map playtest is still outstanding
- Owner-side downloadable ZIP combines the verified Windows executable with Pass 39 Laboratory scene and collision data, nine smaller source map fragments, Pass 37 lighting, Pass 38 original-positioned sound references and generated replacement audio, and the Pass 40 graph

Source/provenance details: [Pass 40 reconstruction notes](docs/development/PASS40_GROUNDED_LABORATORY_NAVIGATION.md).

## Previous verified branch: Pass 39 (October 10, 2026)

[Source branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass39-original-laboratory) ·
[Windows verification](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38082626002)

- Full loaded owner-source **Laboratory** scene from TestPlace: **34,268** authored geometry records, 836 lights, eight player and fifteen infected spawn markers, 174 pickup/fortification anchors
- Owner-private SHA-bound **Laboratory.col26.gz** cache: **18,130** original-size collision shapes in **180** spatial tiles, of which 4,501 use openly labeled approximate bounds
- Three offline 2D floor plans rendered from Laboratory original source coordinates, accessed in game with **F4** and **Left/Right**; maps are read-only, SHA-bound to private original scene pack
- The other nine release maps retain playable **approximate blockouts** while owner-private, source-verified CMaps wall/prop fragments and F4 bird's-eye source footprints are available separately
- Original MeshPart/UnionOperation triangle binaries, textures, navigation parity, animations and original audio still missing. This is **not** a complete 1:1 game
- The GitHub Actions job runs Python contracts, compiles Godot/.NET, exports Windows, exercises an actual exported native `--smoke-pass39` scene with synthetic test geometry/PNGs, and removes all synthetic fixtures before uploading the public-safe executable. Real owner-private full-scene playthroughs remain unverified

The **private owner Windows ZIP** adds ten-map source outlines, original Laboratory geometry and 180-tile collision cache to the executable, plus Pass 37 lighting and Pass 38 synthetic substitute soundscapes. This private content is never committed to GitHub or added to a public CI artifact. See [Pass 39 development notes](docs/development/PASS39_ORIGINAL_LABORATORY.md).

## Locked target

- Windows standalone game using Godot 4 + C#/.NET.
- No Roblox, Roblox Studio, Roblox account, Roblox server, or Internet connection at runtime.
- Local-authoritative single-player simulation with command/event boundaries suitable for later LAN/co-op work.
- Older-style **Regular-only, 15-wave** release profile. Wave 15 completes the map; there is no wave 16.
- Hardcore, Classic, Endless, enemy Juggernauts, Juggernaut waves, and ordinary-hit movement stun are disabled.
- Completion reward is one-time and is persisted before results/lobby transition.
- Release maps: Ranch, Mill, Bypass, Cabin, Cargo, District, Expressway, Prison, Laboratory, Manor.

## Verified development checkpoint: Pass 58 (October 11, 2026)

The original owner weapon-model source contains **98 authored models, 1,484 parts, 936 visible source parts and 1,137 distinct mesh references**. Pass58 builds bounded, source-guided detail silhouettes for **390** recognized visible shapes (140 chassis, 78 barrels/tubes, 60 grips, 50 magazines, 43 stocks, 10 guards, 9 optic lenses); unknown meshes still use conservative bounds. Authentic locally installed owner meshes override every fallback. Five reconstructed part profiles and one optic disk were verified in a native Windows gameplay/Armory smoke.

**CI verified:** [run 38117871532](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38117871532) — **525 Python tests passed**, Windows Godot/.NET build **0 errors, 1 inherited Pass36 warning**, native `TWR_SMOKE_PASS58_DETAIL_GEOMETRY_OK` passed, baseline 10-map/150-wave simulated progression passed and private/synthetic asset leakage checks passed. No public EXE upload. The separate private owner pack adds an audit of unclassified weapon source geometry. See [Pass58 technical record](docs/development/PASS58_SOURCE_WEAPON_SILHOUETTES.md).

## Verified development checkpoint: Pass 57 (October 11, 2026)

Pass57 improves **the rendered 3D weapon models**, not just the wiki gallery. The source TestPlace contains **98 original tool models, 1,484 original part records and 1,137 unique remote mesh references** but no embedded triangle streams for those custom meshes. The existing owner-private source tool assembly data keeps original relative CFrames/colors/sizes; the renderer now resolves source-literal `rbxassetid://123` to locally installed `Content/Assets/Meshes/123.obj` (or `Content/Assets/Textures/123.png`) with strict numeric validation. It uses true native sphere/cylinder/wedge shapes where possible, carefully constrained 24-sided tubular stand-ins where only the source bounds and clearly tubular part names are available, and visible conservative bounds for the rest. The Armory explicitly identifies loaded triangle meshes versus approximations.

**Verified [Windows Actions run 38115844881](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38115844881):** **518 Python tests passed**, Windows Godot/.NET export succeeded (**zero errors, one existing Pass36 CS0108 warning**), native `TWR_SMOKE_PASS57_GEOMETRY_OK` passed for one synthetic local OBJ, two unresolved source IDs, four native primitives, one circular mesh proxy, two conservative bounding proxies, Armory diagnostics and unchanged credit/skin state. The 10-map × 15-wave simulated baseline and owner-private asset exclusion also passed. Full actual remote mesh triangles, all-map visual fidelity and a downloadable final EXE remain unfinished. [Pass57 technical report](docs/development/PASS57_ORIGINAL_WEAPON_GEOMETRY.md).

## Verified development checkpoint: Pass 56 (October 10, 2026)

Pass56 links the game's 91 normalized weapons and 118 known source skin identifiers to **owner-supplied wiki reference images** in a private, locally installed visual gallery. From image-docs metadata it verified **2,033 distinct images**, **90 default weapon screenshots**, **1,563 exact weapon/skin comparison images**, **270 pose/UI screenshots** and **110 skin icons**. Users can open them in Armory or Skin Inventory, with checksum-bound provenance, exact-match/default-only fallback notices, working Back navigation and no economy/gameplay side effects. These are **wiki screenshots, NOT original Roblox UV/normal texture files**. An owner-only install kit ZIP is provided separately and remains outside public Git.

**Windows native CI verified:** [run 38109511432](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38109511432) — 512 Python regressions passed, Godot/.NET Windows build succeeded with **zero errors** (one inherited Pass36 CS0108 warning), native Pass56 synthetic PNG/gallery test passed, baseline 10-map × 15-wave simulated domain progression passed and private source-asset exclusion audit passed. No publicly distributed Windows EXE was uploaded. [Pass56 implementation and owner installation details](docs/development/PASS56_WIKI_REFERENCE_GALLERY.md).

## Verified development checkpoint: Pass 55 (October 10, 2026)

**Branch:** `twr-pass55-source-skin-textures-and-shimmer`. The owner original skin source identifies **118 skin icon IDs, 94 Texture/Texture1 layers and 33 MeshPart TextureID assignments**; optional private metadata enables local PNG loading by numeric asset ID with original layer tints and UV parameters, deterministic and explicitly labeled procedural fallbacks when images are absent, and bounded reconstructed shimmer for source Neon/ForceField. Neither original images nor animation timings are falsely represented as recovered.

**Native Windows CI verified:** [run 38108741473](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38108741473) passed **506 Python tests**, Godot/.NET Windows export (zero errors, one existing Pass36 warning), source-like synthetic PNG/UV/shimmer/material-reset smoke, 10-map × 15-wave simulated domain baseline, and private source-asset exclusion audit. No public EXE was uploaded. Owner-only skin metadata ZIP is delivered separately from source Git. See [Pass55 engineering/evidence](docs/development/PASS55_SOURCE_SKIN_TEXTURES_AND_SHIMMER.md).

## Verified development checkpoint: Pass 54 (October 10, 2026)

The Pass54 branch replaces arbitrary hashed cosmetic colors with the **source-authored RGB and Roblox material fields from 118 skins (590 values)** when optional authenticated owner-private sidecars are installed. The original TestPlace contains **728 original Value1–Value5 skin tags on 1,484 tool parts in 98 models**; the runtime maps tags by source part index, preserving untagged parts. This pass implements local Godot material approximations, not original shader/texture animation parity. Original skin palette and part maps remain private; no raw source archives are published.

**Verified Windows Actions [run 38106666088](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38106666088):** 498 Python tests passed; Godot/.NET Windows build had zero errors (one old Pass36 warning); native Pass54 synthetic-only 3D source RGB/mapped part-tag, switch-and-reset, player-credit isolation and map lifecycle smoke passed; the baseline simulated 10-map/150-wave check and original-asset exclusion passed. No downloadable public EXE is uploaded. See the [Pass54 source materials implementation and owner pack setup](docs/development/PASS54_ORIGINAL_SKIN_MATERIALS.md).

## Verified development checkpoint: Pass 53 (October 10, 2026)

**Branch:** `twr-pass53-offline-skin-cases` — original-source case names/prices, offline case credit transactions and case completion, persistent owned skins, skin selection/resale, and visibly approximate 3D cosmetic finishes in the 91-weapon loadout and first-person gameplay. Original random odds and original material textures are not recovered. High's nine event skins and off-sale Hallows are never sold via ordinary cases.

**Verified [Windows CI run 38105813760](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38105813760):** 490 Python tests passed; Godot/.NET Windows executable built (zero errors, one older Pass36 CS0108 warning); native Pass53 smoke verified real credit debit, JSON persistence/reload, 3D tint assignment, resale, exclusive/duplicate protection and lobby cleanup; baseline 10-map × 15-wave simulated progression and private-asset exclusion passed. No downloadable public Windows executable was uploaded. Full art/map/skin visual parity and manually played 15-wave matches are not certified. [Pass53 implementation report](docs/development/PASS53_OFFLINE_SKIN_CASES.md).

## Verified development checkpoint: Pass 52 (2026-10-10)

The Pass52 development branch adds original per-weapon 3D loadout CFrame offsets, the source three-second easing function and 50-degree lobby camera field of view, right-mouse preview tilt and a read-only source-priced seven-case Shop. Owner-derived original art is not included in public Git. **Verified Windows CI:** [Actions run 38105034356](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38105034356): 485 Python tests passed, Windows C# export built (zero errors; one older Pass36 CS0108 warning), the 10-map 15-wave domain simulation, native Pass52 smoke and privacy audit all passed. No publicly downloadable Windows executable was uploaded. Gameplay, art, and original Shop functionality are still incomplete; read the [Pass52 source evidence and acceptance report](docs/development/PASS52_SOURCE_LOBBY_MOTION_AND_CASES.md).

## Verified development checkpoint: Pass 51 (2026-10-10)

The Pass 51 branch adds a source-CFrame-anchored 3D weapon showcase to the original offline lobby's working 91-weapon armory. The five actual loadout marker transforms are preserved, weapon-hover previews are presentational only, and source model assets remain owner-private. Windows CI run [38104204978](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38104204978) passed **477 Python regressions**, native Godot/.NET Windows export, the 10-map/150-wave domain smoke, the new 3D armory native smoke and the private source asset exclusion gate. This is **not** proof of original 3D mesh/pixel fidelity, human 15-wave matches, or an installable private release. See [Pass 51 verification notes](docs/development/PASS51_ORIGINAL_LOADOUT_PREVIEW.md).

## Recovery status

This branch contains the latest recoverable development state assembled from surviving engineering artifacts and immutable source evidence. The previous full working tree did not survive byte-for-byte. Files restored from surviving artifacts, files deterministically regenerated from recovered normalized evidence, and newly reconstructed implementation files are distinguished in `recovery/PROVENANCE.json` and `docs/RECOVERY_STATUS.md`.

The historical engineering report records **82/82 tests**, **78 C# files**, **21 acceptance gates**, ten converted source maps, and **14,363 normalized RemoteSpy calls**. Those are historical checkpoint facts, not automatically current claims. Run `python tools/validation/run_all.py` for current repository truth.

## Evidence policy

Original uploaded evidence remains immutable and outside ordinary public Git history. This repository stores hashes, manifests, normalized data, generated reconstruction outputs, tests, source code, and documentation. Raw `.rbxlx`/`.rbxl`, raw evidence ZIPs, raw recordings, and release binaries are intentionally excluded.

## Validation

```text
python -m pip install -e .[test]
python tools/validation/run_all.py
```

## Windows output

The pinned workflow targets:

`build/output/ThoseWhoRemainOffline.exe`

A successful executable build is only claimed after the Windows workflow actually produces that file.

## Windows executable distribution

The Windows CI workflow validates, exports, and smoke-tests the game in its
temporary runner workspace. A successful Pass 38 branch build uploads a
**public-safe, no-private-assets** Windows ZIP as a temporary GitHub Actions
artifact; this is not a complete original-content game release. The owner-only
source soundscapes, terrain, models, and sound recordings remain out of Git and
are distributed in a separate owner-held content pack. Executables are not
published as GitHub Releases.

Pass 38 adds 206 source-indexed map sound emitters across the ten release maps
(197 positioned, 9 environmental). Their original SoundId fields are blank in
the available TestPlace source, so the optional F2 audio preview requires
locally installed substitute/authorized PCM16 WAVs. F2 is OFF by default,
and source 3D positions require the real recovered map rather than approximate
blockouts. See docs/development/PASS38_SOURCE_MAP_SOUNDSCAPES.md.

## Laboratory source-pack runtime verification

Laboratory can load an external, owner-held `Content/Maps/Laboratory.scene.jsonl.gz`
beside the Windows executable. Original Roblox geometry bytes remain out of Git.

The Windows workflow now generates a **synthetic**, explicitly marked Laboratory
scene with 30,000 dummy render instances, 1,000 server-wall colliders plus 16,600 source-collidable render objects, 801 dummy lights,
15 infected spawn markers, and 8 player spawn markers. It loads that pack through
the **exported Windows executable** and requires both `TWR_LAB_SOURCE_LOADED`
and `TWR_SMOKE_LAB_SOURCE_OK` in the process log. The fixture is deleted at
the end of the smoke step and is never distributed. This validates loader
execution and pack discovery, **not** fidelity to the original Laboratory.

## Laboratory light streaming

The recovered Laboratory pack includes source light positions, colors, angles,
and ranges. Instead of fixing the 128 nearest lights at the initial spawn,
the scene now reselects up to 128 nearest emitters when the player moves.
Other source lights remain in the pack and are eligible for activation.
Roblox SurfaceLight is approximated with a directed Godot spotlight until
an exact surface-light shader exists. Per-map skybox textures remain unavailable.

## Prepared offline Laboratory textures

The original Laboratory scene references 59 different per-MeshPart texture
asset IDs. Mesh textures can be included **before** the Godot export at
`src/Twr.Godot/Content/Assets/Textures/<numeric asset id>.png` and are
loaded locally through `res://Content/Assets/Textures/`. If a texture is not
present, the source-color proxy remains. The same approach supports prepared
unit-normalized Godot mesh resources in `Content/Assets/Meshes/` before export.

**No original mesh or texture bytes are currently committed to public Git.**
Source IDs are not texture images, and this loader does not fetch resources
over the network.
