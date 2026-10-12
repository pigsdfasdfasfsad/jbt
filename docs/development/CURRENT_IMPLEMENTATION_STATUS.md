# PASS 78 — THREE MANOR STAIR FLIGHTS AND DOWNSTAIRS INFECTED NAVIGATION (WINDOWS CI PENDING)

**Branch:** `twr-pass78-manor-bidirectional-stairs`; based on [verified Pass77 exported Windows native gameplay](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38212138620).

Pass78 extends the approximate fallback Manor collision and route functionality rather than importing or inventing original Roblox stair meshes. It introduces physical **upstairs-to-downstairs** paths only when a living player is on the lower floor and a stair-accessible infected is above them. The Pass73 graph checks all seven physical ramp support samples before descending, checks solid-world visibility to the lower target and from any top-floor approach, and resumes at the nearest downhill segment when the enemy is already on the slope. Native `InfectedAgent` respects strict downhill waypoint height/position acceptance, never treats a lower-floor player as in melee range while remaining upstairs, and uses ordinary `MoveAndSlide` body collision with no teleportation.

A new exported Windows `--smoke-pass78` scenario iterates the reconstructed **FrontHallUpper, WestWingUpper, and EastWingUpper** ramps. It requires 12 distinct native acceptance markers: first-person W-key ascent, first-person S-key descent, infected body ascent to the top, and the **same infected** descending under authentic physics to the lower target for each of the three. No per-frame enemy position assignment or original asset pack is involved. The test uses isolated `user://pass78-manor-bidirectional-smoke-profile.json`, all target/player placements occur only before a traverse, and the usual owner-private asset exclusion remains required.

**Verification pending:** Python suite, Godot/.NET Windows export, native physical traversal of all 3 staircases in both directions, privacy gate. Pass78 does not imply original retail geometry, fully navigable all-map interiors, 15-wave human completion or public downloadable private-asset EXE.

[Pass78 engineering record](PASS78_MANOR_BIDIRECTIONAL_STAIRS.md)

---

# PASS 77 — VERIFIED WINDOWS MANOR FRONT-HALL PHYSICAL UPPER-STORY ASCENT

**Development branch:** `twr-pass77-manor-stair-traversal` based on verified [Pass76 Windows gameplay CI](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38208475688).

This pass corrects a concrete remaining environmental gap: the reconstructed Manor's three eight-tread stair flights previously used separate, roughly 32cm-high collision boxes without a reliable climbing surface. Godot's ordinary CharacterBody movement cannot ascend those as a continuous staircase without jumping, and the split balcony decks had an approximately four-metre-wide centre gap without a connecting landing.

- All three **approximate fallback Manor stair flights** now use an actual angled `StaticBody3D` collision ramp, eight visible decorative treads (no duplicate tread collision) and an upper cross-landing that overlaps both side platforms. The slope is bounded below 35°; the original part recovery claims remain explicitly approximate.
- `RuntimeMapLayout.ApproximateStairs77` records bottom/top collision-foot coordinates only for the reconstructed Manor fallback. The original Lab loaded-source and source-nav paths, nine other maps, and the Expressway road graph are unchanged.
- A conservative Pass73 graph supplement can issue **uphill** waypoints along one of three existing Manor ramps only after sampling real walkable Godot colliders along the ramp, verifying collision-free corridor travel and actual upper-floor target alignment. Stair down-routes, remote upstairs wings and all owner-original retail map nav parity are deliberately unsupported/unverified.
- A dedicated exported Windows native smoke starts real reconstructed Manor gameplay, validates three ramp colliders and upper landings, uses actual W keyboard input to physically climb the front stair without teleporting, computes a real collision-audited lower-to-upper zombie route, and exercises a genuine infected physics body climbing after the player.

**Verified:** [GitHub Actions Windows run 38212138620](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38212138620) from gameplay commit `61c8d24ad49ef83f116ea0b9b6b82d5af079a24a`: **627 Python regressions passed**, Godot/.NET standalone Windows export compiled with **zero errors**, native real-player `TWR_PASS77_REAL_PLAYER_STAIRS_OK` and real-infected `TWR_PASS77_REAL_INFECTED_STAIRS_OK` markers passed, completion `TWR_SMOKE_PASS77_MANOR_STAIRS_OK` passed, and owner-private content exclusion succeeded.

**Observed real physics:** First-person W-key player climb: **2.68m**, grounded upper landing; infected Body3D climb **2.62m**, confirmed upper landing, zero source jump attempts. No instant position updates were applied during either climb. The original 1.58m flat-route waypoint acceptance threshold was corrected specifically for verified uphill Manor routes so infected do not skip their final rise and turn into the side balcony.

**Remaining:** Only the Front Hall flight had full player/enemy ascent acceptance. West/East flights are instantiated and collider-checked but not yet walked end-to-end. Descent, authentic owner original Manor geometry, source match topology and completed player-driven 15-wave survival remain unverified. The private Roblox originals, original meshes and user save data are not committed.

[Pass77 technical record](PASS77_MANOR_STAIR_TRAVERSAL.md)

---

# PASS 76 — VERIFIED EXPORTED WINDOWS OBJECTIVE, PICKUP, SUPPLY AND FORTIFICATION INTEGRATION

**Development branch:** `twr-pass76-interaction-objective-fortification`. Parent: [verified Pass75 exported Windows acceptance](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38206918852) (613 Python tests, zero compiler errors, native launcher + grounded Frag smoke).

Pass76 makes actionable gameplay corrections instead of only increasing test coverage:

- **Single-item E-key pickup arbitration:** When several pickup actors overlap, only the nearest visible pickup handles one key press; a solid wall occludes item collection. A subsequent E press can collect the next item.
- **Load/Repair objective visibility:** Carrying materials and depositing into the objective target now require a physical unobstructed E-key path. Source-original objective payloads, 10/12-item variants and domain rewards remain unchanged.
- **Hammer fortifications:** Dead players cannot build. Placement now requires a walkable Godot floor and free collision footprint; invalid placement does NOT consume inventory. Build swings, perk timing and authoritative consumption are retained.
- **Mounted .50 Cal:** Swept hitscan collision includes real infected layer 2 and optional source-server wall colliders. Mounted guns stop functioning and dismount immediately when the player dies.
- **Clap Bomb and Jack:** Recovered original 25/35-stud radii are now correctly converted to Godot metres and obstacle-occluded like RPG/Frag. Barbed Wire can no longer damage an infected through a solid wall.
- **Supply helicopter pallet:** The descending pallet uses an actual walkable Godot collision contact where present, rather than always forcing Y=0.3; a source-player collider is excluded from the landing ray. Its configured Y fallback is preserved when geometry is missing.

New native Windows acceptance exercises E-key pickup contention, blocked/unblocked Load items, hammer invalid/valid placement, Clap Bomb wall/radius damage and rewards, mounted 50 Cal real infected damage, and landing a supply pallet on an elevated floor. The test uses Ranch/Manor production gameplay roots with clearly synthetic floors/walls and an isolated profile. It does not prove full original retail interiors, exact objective cinematic presentation or 15 wave human completion.

**Verified exported Windows CI:** [run 38208475688](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38208475688) from source commit `4c0ef8223a581bd91156a235edfd18c760d23ff8` — **622 Python tests passed**, one pre-existing XML FutureWarning, Godot/.NET Windows build succeeded with **0 compiler errors**, both real-gameplay `TWR_PASS76_RANCH_INTERACTIONS_OK` and `TWR_PASS76_MANOR_EQUIPMENT_OK` markers passed, and owner-private asset exclusion passed. The previous scripted domain-only ten-map, 150-wave completion marker also passed but is **not** a genuine human playthrough.

**Additional production input fixes from native testing:** A per-player E-press budget prevents additional pickups disappearing in the same held-key frame; first LMB sends an immediate hammer swing and mounted .50 Cal fire event, with the existing repeat cooldown retained.

**Remaining work:** Original map source geometry, authentic item/fortification visual models, complete all-map objective route validation, human-played 15-wave survival, and final user-ready private Windows packaging.

[Pass76 engineering report](PASS76_INTERACTION_OBJECTIVES_FORTIFICATIONS.md)

---

# PASS 75 — VERIFIED EXPORTED WINDOWS ROCKET, FRAG AND RECOIL COMBAT

**Source branch:** `twr-pass75-blast-line-of-sight` based on [verified Pass74 Windows native gameplay](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38205313363).

Pass75 fixes three concrete gameplay issues: **rocket swept collision rays did not include live infected layer 2**, **rocket and Frag splash ignored occluding world walls**, and **recoil was applied before a trigger pull's bullet/launcher direction was resolved**. Godot now raycasts launcher projectiles against world + infected + optional source-server collision, applies blast damage only where solid-world visibility permits (including damage objectives), and resolves each first shot against the pre-recoil crosshair. Ammo, source weapon statistics, kill rewards, explosive falloff and other combat authority remain unchanged.

The dedicated native Windows smoke creates production Laboratory and Manor gameplay roots with actual wave-spawned infected, a clearly synthetic raised arena floor and temporary wall, real rocket sweep/impact and 3-second Frag fuse physics, and native damage-objective actors. It tests blocked/unblocked infected and tanker splash, out-of-radius preservation, and authoritative kill credits. The standalone smoke uses an isolated local profile and never requires online services or publishes owner-original data.

**Verified native Windows CI:** [run 38206918852](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38206918852) on gameplay source SHA `e0eac465b43b7b28786c56ada02a6c1d3c5510f7` — **613 Python regressions passed, zero Godot/.NET compiler errors, real Laboratory rocket and Manor Frag native smoke markers passed, owner-private source exclusion passed**. One inherited XML FutureWarning remains. The real Frag was specifically verified as resting at the temporary physics floor with a completed 3-second fuse, physically damaging visible infected/objectives, paying normal domain kill rewards, and preserving occluded and out-of-radius targets.

**Fidelity limitations:** These tests do **not** demonstrate exact original weapon ballistics for all 91 weapons, authentic map meshes/collisions, complete 15-wave human survival or distribution-ready EXE. Test geometry is explicitly synthetic.

[Pass75 engineering report](PASS75_WEAPON_BLAST_OCCLUSION.md)

---

# PASS 74 — VERIFIED EXPORTED WINDOWS SPECIAL INFECTED ATTACK SAFETY

**Branch:** `twr-pass74-special-infected-safety` based on verified [Pass73 CI](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38202837479).

Pass74 fixes two concrete gameplay problems: **Burster's instant detonation could damage through solid walls** (while the lingering spore cloud already used occlusion); **Bolter leaps lacked target-floor support validation** and used a fixed 0.55-second horizontal flight time even at very close ranges, risking overshoot. Both now use physical collision/landing checks. Bolter maintains source-described fixed-direction jump and recovery, while its exact numerical velocities/cooldowns remain labelled reconstruction approximations.

The Windows-native smoke uses real Godot gameplay roots on Ranch and Manor, a clearly synthetic raised collision lane, the genuine wave-spawned infected actor, source-type smoke/melee/fire multipliers for all eight variants, Bolter launch/contact behavior, Burster wall-occluded and unblocked damage, gas mask immunity, Bloater line-of-sight gate, and solid-world spore projectile collision. Native test actions use an isolated profile and never publish owner-original packs.

**Verified:** [successful Pass74 exported Windows CI](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38205313363) — **607 Python tests passed**, native Godot/.NET Windows export completed with **zero compiler errors**, all Pass74 Bolter/resistance and Burster/Bloater/hazard markers passed, existing ten-map scripted domain completion passed, and the owner-private asset exclusion succeeded. The live Bolter contact inflicted **15.00 HP**; unblocked Burster blast inflicted **27.50 HP**; intact walls prevented the direct Burster blast and blocked projectile splash; gas mask blocked lingering clouds.

**Limitations:** These are isolated synthetic-wall combat tests, **not** proof of exact original infected creature models/animations, ten-map source geometry, all-map advanced navigation, or a human 15-wave survival run.

[Pass74 engineering record](PASS74_SPECIAL_INFECTED_SAFETY.md)

---

# PASS 73 — VERIFIED NATIVE GROUND-ROUTE FALLBACK + FULL-MAP INTERACTIONS

**Branch:** `twr-pass73-fullmap-ground-navigation`. Builds on [verified Pass72 Windows CI](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38201737219), which passed 596 Python regressions and ten actual Godot map scene/Wave 2 tests.

Pass73 adds incremental Godot physics-sampled A* floor navigation for approximate fallback maps (nine release maps, excluding Expressway), with explicit floor/height/collision checks, short-corridor direct chase, shared bounded route cache and no modification of recovered Laboratory source graph. The game now tracks fallback navigation nodes/edges in F10 diagnostics.

The successful Windows-native smoke checked grounded actual scene entry on all ten maps, public Godot input-based walking, sprinting and jumping, a real enemy body moving around a clearly fabricated wall on Ranch, actual Medkit and Radio domain interactions, and preventing item/objective grants after death. Short jump-buffer expiry also prevents old airborne Space presses from causing unexpected later auto-jumps.

**Verified exported Windows CI:** [Run 38202837479](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38202837479) — **601 Python tests passed, Godot/.NET Windows export succeeded with 0 errors, all ten native per-map navigation checks passed, Ranch movement/obstacle/pickup/objective/death checks passed, and owner-private asset exclusion passed**. On the fabricated Ranch wall test, genuine Godot input produced 1.35m walking and 1.82m sprinting; the infected body detoured 7.75m sideways and gained 8.90m toward its target.

**Limitations:** This is ground-floor-only, collision-sampled reconstruction navigation, not the original Roblox navmesh. Ten retail-quality original maps, multi-floor behavior, extended human-controlled 15-wave playthroughs and a packaged private Windows distribution remain unfinished.

[Pass73 engineering record](PASS73_FALLBACK_GROUND_NAVIGATION.md)

---

# PASS 72 — NATIVE ALL-TEN-MAP ENTRY VERIFIED

**Branch:** `twr-pass72-native-ten-map-entry`. The previous Pass71 exported Windows build and native two-map enemy/damage test [passed](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38201351136).

Pass72 adds actual Godot gameplay-root instantiation on every release map; native first-person physics grounding, HUD/pickup construction, real infected spawn, accelerated Wave 1 to Wave 2 scene transitions, isolated Results-save teardown and runtime lobby roundtrip on all ten maps. **Verified in [successful Windows CI run 38201737219](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38201737219): 596 Python tests passed, exported Godot/.NET Windows executable accepted, all ten native per-map markers and final Pass72 marker passed, and owner-private asset exclusion passed.** These tests are not manual fifteen-wave matches or visual/floorplan parity with original Roblox.

[Pass72 engineering report](PASS72_NATIVE_TEN_MAP_ENTRY.md)

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 70

**Date:** October 11, 2026 (US Central). **Branch:** `twr-pass70-live-player-combat-loop`.

**Successful exported Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38198146639

- **588 Python regression tests passed** with one inherited XML FutureWarning; **Godot/.NET Windows export: 0 errors**, one inherited Pass36 CS0108 warning.
- Pass70 bridges the previous domain-state-only 10-map scripted completion loop to actual native game interaction: the exported Windows game constructs a real Laboratory `GameplayRoot`, first-person `FirstPersonPlayer` camera/collision body, HUD, Wave 1 spawner and native physics `InfectedAgent`. A deliberately artificial platform high above the map and a controlled, nonlethally pre-damaged infected ensure reproducible collision; these **are tests, not claimed original map changes**.
- Native `TWR_SMOKE_PASS70_LIVE_COMBAT_OK` passed: an actual camera physics ray contacted the infected; a virtual left mouse event invoked production `TryUseWeapon`, spending **exactly one** cartridge and killing the target through `InfectedAgent.ApplyDamage`. The original death callback awarded XP/credits via the authoritative domain command bus. A normal R-key event waited out the player reload timer and transferred ammunition from reserve while conserving total rounds. The actual GameplayRoot end-of-wave and intermission logic then transitioned into Wave 2 and spawned a new living infected actor.
- Godot headless Windows does not report true cursor capture; an exact **`--smoke-pass70` flag-only** exception skips *only* that UI prerequisite so the virtual click reaches the untouched player combat controls. **Normal player mouse behavior has not been relaxed**.
- Pass59–69 weapon, hand, ADS and optics native smokes and `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` still passed. The prior completion test is scripted domain-only; **Pass70 verifies one controlled scene/one actual kill/reload/two waves, not ten manually completed 15-wave games**.
- A separate `user://pass70-live-combat-smoke-profile.json` prevents native test activity from modifying the real offline profile. Synthetic owner-shaped weapon/hierarchy fixtures were cleaned before private asset exclusion; the owner-provided originals and exported Windows executable **remain unpublished publicly**.

**Outstanding high-priority work:** owner-private Windows EXE distribution, real multi-map movement/collision/AI/gameplay acceptance and stability across full 15-wave matches, original source MeshPart/CSG triangle and UV/texture recovery, original rig/camera and optics fidelity, exact map interiors/fortifications/objectives on all ten maps, screenshot comparisons, audio, and manual player playtests. **Pass70 is verified, but the full game is not 1:1 finished.**

[Pass70 real player click-to-kill, ammo/credit/XP and Wave 2 engineering record](PASS70_LIVE_COMBAT_INTEGRATION.md)

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 69

**Date:** October 11, 2026 (US Central). **Branch:** `twr-pass69-source-optical-reticles`.

**Successful exported Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38196014637

- **583 Python regression tests passed** with one inherited Pass34 XML FutureWarning; **Godot/.NET Windows export: zero errors**, one inherited Pass36 CS0108 warning.
- Pass69 uses the exact surviving inert original `ScopeDiameter`, `ScopeRadius` and `FOV` stats to restore eight source-sized scoped/reflex weapon profiles: **AUG (0.106 stud diameter, FOV 40°), Barrett M82A1 (0.205, 10°), CBJ-MS (0.0555 stud radius = 0.111 diameter, FOV 60°), M110 SASS (0.188, 10°), M14 (0.179, 10°), SVD (0.210, 10°), VSS (0.210, 10°), and Winchester Model 70 (0.184, 10°)**.
- A new Godot `Pass69SourceScopeOptics` screen-space Control overlays six **procedurally reconstructed** reticle style families. The user-supplied, privately retained original `*-ADS.png` stills provided visual references: the **AUG/CBJ-MS angular optic bezel**, Barrett circular ring, and M110/M70 lower markings were inspected and approximated with primitive drawing. Original reticle textures/bitmap pixels **were neither recovered nor published**.
- Screen-space lens scale uses source aperture size × `RobloxUnits.MetersPerStud`, current camera FOV, screen resolution and bounded eye-relief depth. Existing Pass67/68 owner-verified `AimPart` can supply sight depth; unavailable owner geometry uses a clearly **approximated 0.43m default**. Invalid float/FOV/depth data and unscoped weapons do not trigger misleading overlays.
- `FirstPersonPlayer` drives visual scope blend from existing aim-permission/reload/equip/FOV timings. Overlay resets on death, weapon switches, throwables and mounted/hammer modes. `GameplayHud` hides only its generic "+" while a scope is actually drawn. **No damage, shot-ray, recoil, FOV authority, ammunition, zombie logic, perk, map or economy systems changed.**
- Native exported Windows `TWR_SMOKE_PASS69_SOURCE_OPTICS_OK` verified eight profile fields, meaningful projected lens sizes, reflex and magnified modes, valid fallback eye relief, correct visibility and crosshair transitions, plus unchanged Pass68 source-backed Winchester sight centering. Earlier Pass59–68 Windows-native weapon regression smokes passed. `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` remains a **scripted domain test, not player-controlled full survival matches**.
- Temporary fake owner-style SourceWeaponModels/hierarchy packs were removed, and CI's **private-asset exclusion check passed**. No original owner archive or public downloadable Windows executable was uploaded.

**Still required for a full offline 1:1 game:** original scope bitmap/mesh and optical postprocessing, exact Roblox avatar and equipped weapon Motor6D/camera rig, original external MeshPart triangles/UVs, a completed physics/visual reconstruction of all ten maps, screenshot- and interaction-level reference matching, actual 15-wave playthrough validation on every map, and an owner-authorized privately packaged Windows game. **Pass69 is verified, but the remake as a whole is not complete.**

[Pass69 source-optic sizes, reconstructed reticle styles, screenshot-guided geometry and verified native Windows acceptance](PASS69_SOURCE_SCOPE_OPTICS.md)

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 68

**Date:** October 11, 2026 (US Central). **Branch:** `twr-pass68-original-aimcf`.

**Successful exported Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38192674647

- **576 Python regressions passed** with one inherited Pass34 XML FutureWarning. **Godot/.NET Windows export: 0 errors**, one inherited Pass36 CS0108 warning.
- All **73** surviving original `ModuleScript` AimCF formulas were classified from the owner-supplied `scripts.zip` as inert source-family metadata. Their original algebra was recovered without Lua execution: **38** AimPart-relative Handle formulas with base offset inside, **9** with the base offset outside, **24** inverted Handle/sight formulas with GripCF inverse first, **1** unusual SPAS-12 formula, and **1** numeric Flamethrower constant. Source-specific MP7 0.45-stud marker shift and Mosin/Winchester/Obrez post-depth terms are preserved. Raw source Lua is **not published**.
- New `Pass68OriginalAimFormulas` evaluates these exact source-family transform orders with the original normalized per-weapon `GripCF`, SHA-gated original owner `AimPart` and **primary** Tool `Handle`. The full original AimCF transform is exposed separately from the approximate final Godot visual placement. No source Luau is executed.
- Native exported Windows `TWR_SMOKE_PASS68_ORIGINAL_AIMCF_OK` validated noncommuting source matrix multiplication, original SPAS-12 `CFrame.new(0,0,-0.6):toObjectSpace(...)` operator precedence, MP7 0.45-stud marker offset, Winchester -0.2-stud post-offset, Flamethrower numeric CFrame, and five entirely synthetic gun models' aiming axis alignment. Source SPAS-12 can require excessive original-camera-independent eye relief; **only visual depth** is clamped to a bounded 0.12–1.20m interval, while the exact source formula result is retained unchanged.
- Previous Pass59–67 exported-game weapon smokes passed, as did `TWR_SMOKE_COMPLETE_OK maps=10 waves=150`. The latter remains **scripted domain-state progression**, not ten real, player-controlled 15-wave matches.
- All synthetic owner-style weapon packs were deleted before the private-asset exclusion check. **No owner-private original pack or Windows EXE was published publicly.**

**Remaining 1:1 blockers:** the exact original Roblox avatar/weapon welds and character-camera attachments, native optics/scope/reticle meshes and texture layers, many missing original external MeshPart triangles/UVs, complete collision/decorated map reconstruction for all ten maps, true end-to-end manual survival playthroughs, screenshot parity, and authorized private Windows packaging. **The overall game is unfinished.**

[Pass68 original source AimCF formula families, visual clamping and successful native Windows tests](PASS68_ORIGINAL_AIMCF_FORMULAS.md)

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 67

**Date:** October 11, 2026 (US Central). **Branch:** `twr-pass67-source-sight-alignment`.

**Successful Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38191379736

- **569 Python regression tests passed** with one inherited Pass34 FutureWarning; **Godot/.NET Windows export: 0 errors**, one inherited Pass36 CS0108 warning.
- Pass67 now reads the original invisible `AimPart` CFrame directly from the separately SHA-verified owner-only Pass47 `SourceWeaponModels.json.gz` (exact original 100-model pack, of which 73 original/alias models have an unambiguous sight marker). It respects the existing original importer rotation/alignment and refuses missing/ambiguous/nonfinite source marker poses. Source packs are not committed or published.
- The original weapon modules compute `module.Stats.AimCF` dynamically from `AimPart`, `Handle`, and `GripCF` (not a literal numeric serialized AimCF). The implementation **does not evaluate Luau** and does **not claim exact source AimCF geometry**. Instead, SHA-grounded sight parts are centered on the camera aiming axis at an explicitly approximate **0.43m eye relief**, with the original inert `Aim`/`UnAim` **0.50s/0.25s** timing and approximate `SlowDown2`/`SlowDown` easing.
- `FirstPersonPlayer.UpdateAim` prefers original AimPart-guided ADS where owner pack/sight data is valid, keeps previous approximate ADS when unavailable, and leaves existing aim permission, perk/FOV timing, projectile ray direction, damage, ammo and recoil gameplay authority unchanged. Pass66 articulated camera-space shoulders now counter-transform whole-viewmodel ADS movement, retaining camera-anchored shoulders as original hands follow source welds.
- Native `TWR_SMOKE_PASS67_SOURCE_SIGHT_OK` passed: synthetic source Glock 17 and Mossberg sights centered, original Glock timing and easing records interpreted, fallback demonstrated on synthetic Mosin lacking AimPart, camera-space shoulder location preserved, invalid poses refused, and no owner pack falsely verified.
- Exported Windows Pass59–66 source animation regression smokes and scripted `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` passed. The latter is **domain simulation, not 10 complete playable 15-wave matches**.
- Synthetic owner-style packs were cleaned before owner-private asset exclusion. **The original packs and Windows EXE remain unpublished by public GitHub CI**.

**Unfinished:** exact per-weapon original dynamic AimCF formulas (at least two main expression families plus special SPAS-12 offset), real scope/reticle meshes and UV textures, original avatar/weapon Motor6D joints, missing external MeshPart triangle geometry, full interior/physics fidelity for all ten maps, screenshot-level comparisons, genuine manual survival tests and final owner-private offline Windows distribution. **The game is still not a complete 1:1 recreation.**

[Pass67 source sight marker engineering report, native Windows acceptance and exact visual limitations](PASS67_SOURCE_SIGHT_ALIGNMENT.md)

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 66

**Date:** October 11, 2026 (US Central). **Branch:** `twr-pass66-articulated-source-arms`.

**Successful native Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38173001546

- **563 Python regression tests passed**, one inherited Pass34 XML FutureWarning.
- **Godot/.NET Windows export: 0 errors** and one inherited Pass36 CS0108 warning.
- Supplied original `TestPlace.rbxlx` has R6-shaped **1 × 2 × 1 stud** arms attached to a source Supply Drop Survivor model. This supports an approximate nominal 2-stud limb length **but is not evidence of the original first-person player avatar**, its Motor6D C0/C1 joints, or missing textures.
- New `Pass66ArmKinematics` provides a finite, deterministic camera-local two-bone shoulder/elbow/wrist solver with original-scale total 2-stud reach, bounded 1.65× extension, and degenerate-input safeguards. Eight visibly **procedural upper/forearm and shoulder/elbow pieces** follow the existing Pass65 source-authoritative left/right hand/wrist welds, not a recovered player arm mesh.
- The `ArmRig66` root remains a sibling of the recoiling first-person `WeaponBody`, keeping camera-local shoulder anchors stable. Native `TWR_SMOKE_PASS66_ARTICULATED_ARMS_OK` verifies whole Glock reload arm action, Mossberg left pump support arm, Mosin right bolt support arm, bent segment bounds, returning to authored hand rest (within expected millimetre-scale existing idle sway on elbows), hide in static armory previews, and cleanup on weapon switches.
- All Pass59–65 original weapon-smoke regressions passed. `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` is still **scripted domain state progression**, not genuine player-controlled full 15-wave survival runs on all ten maps.
- Public owner-private source exclusions and temporary synthetic pack deletion passed. GitHub CI **does not publish an EXE or any owner-private map/weapon data**.

**Major unfinished parity:** original player arm/body/finger models and rig joints, true source Motor6D offsets, remote original MeshPart/CSG geometry and UV/texture binaries, precise dynamic AimCF sight alignment, full decorated and physics-faithful ten-map reconstruction, first-person screenshot comparisons, complete manually played matches, and an owner-private playable offline Windows build. **Pass66 is verified; the entire 1:1 game is not finished.**

[Pass66 camera-anchored source-hand-linked arm implementation, native acceptance and remaining limitations](PASS66_CAMERA_ANCHORED_SOURCE_ARMS.md)

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 65

**Date:** October 11, 2026 (US Central). **Branch:** `twr-pass65-source-hand-grips`.

**Successful exported Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38168111351

- **558 Python regression tests passed** with one inherited Pass34 XML FutureWarning.
- **Godot/.NET Windows export: 0 errors**, one inherited Pass36 CS0108 compiler warning.
- `Pass65SourceHands.cs` recovers numeric original weapon `LeftCF`/`RightCF` grip CFrames and inert `AV.LeftWeld`/`AV.RightWeld` original keyframes for `Equip`, `Reload`, and `Sliding`. Source symbols are restricted to each weapon's exact left/right CFrame and a small explicit whitelist of original handle-inverse grip conventions; **no Lua/Luau execution** occurs.
- Two **approximate procedural glove models** (palm, wrist, thumb and four fingers per hand) now follow these original grips in first-person. Pass62 source-indexed owner hierarchy supplies current moving subassembly Handle transforms so the left glove follows moving Glock magazines/Mossberg pumps and the right follows Mosin's Bolt Handle. Unsupported original avatar Base welds are skipped; showroom previews hide these glove placeholders. These are **not original Roblox character meshes or recovered skeletal IK**.
- Exported Windows `TWR_SMOKE_PASS65_SOURCE_HAND_GRIPS_OK` verifies source Glock grip/weld cue counts, animated equip and reload, two complete glove proxy rigs, owner-synthetic magazine support, moving Mossberg left support grip, moving Mosin right bolt-action grip, end-of-clip reset, and rejection of source code-like expressions.
- All Pass59–64 original weapon-smoke regressions and `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` passed. The latter remains **automated domain simulation**, not full manual ten-map end-to-end survival acceptance.
- Synthetic weapon/hierarchy fixtures were removed before the private-asset exclusion scan. **No owner-private data or Windows EXE was publicly published**.

**Outstanding:** true source character arms/hands, Motor6D C0/C1 avatar joints and dynamic AimCF; external original MeshPart/CSG triangle and texture binaries; 10 fully decorated and collision-faithful original maps; true 15-wave player-controlled smoke/playthroughs, screenshot parity, and an authorized privately packaged offline Windows release. **Still not a full 1:1 copy.**

[Pass65 source hand grips, weld animation engineering record and native Windows acceptance](PASS65_SOURCE_HAND_WELDS.md)

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 64

**Date:** October 11, 2026 (US Central). **Branch:** `twr-pass64-source-shell-and-bolt`.

**Verified native Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38163615096

- **553 Python regressions passed**; one inherited Pass34 FutureWarning.
- Godot/.NET Windows export compiled with **0 errors**, one inherited Pass36 CS0108 warning.
- Recovered original per-gun `Sliding` and `Reload` keyframes now power `Pass64SourceAction` for original `BoltRel` translation and exact nested Mosin Nagant / Winchester Model 70 bolt-handle rotations. Pass62's optional SHA-verified original source hierarchy now binds nested `Bolt/Bolt Handle` Model groups, composing parent bolt movement with child-local Handle pivot rotation. Original Motor6D C0/C1 offsets were **not** recovered.
- Original `Shell` marker timing, source direction, casing name and source velocity drive **bounded local visual shell proxies** for supported weapons such as Mossberg 500, SPAS-12, and Mosin Nagant. Their actual casing geometry, ejector port and collisions remain approximations; no Roblox dependencies or network calls were added.
- Source-normalized incremental shotgun reloads continue to animate Pass63 original `Mag` shell-feeding poses. A **one-frame reload pose leak** was fixed by decrementing the visual reload timer before pose sampling; native acceptance verifies it returns to rest on completion.
- `TWR_SMOKE_PASS64_SOURCE_BOLT_SHELL_OK` passed in an exported Windows game: original source cue counts, ninety-degree Mosin handle articulation before bolt motion, whole nested group movement/reset, source shotgun casing emission/deletion, incremental shell pose completion, and synthetically isolated owner pack handling.
- Pass59–63 weapon native smoke tests all passed; `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` remains **scripted simulation**, not real manual ten-map 15-wave victory.
- Synthetic source files were cleaned and public CI private-asset exclusion passed. **No public Windows EXE or private original assets were uploaded.**

**Remaining:** original external MeshPart/CSG geometry triangles and UVs, exact Motor6D C0/C1 joint pivots, authentic casing shapes/port/physics/sounds, hand/arm IK, dynamic source AimCF, original complete ten-map visual/physical parity, player-controlled 15-wave acceptance, and authorized private Windows executable packaging. **Not a complete 1:1 remake.**

[Pass64 shell and bolt original-source implementation and Windows CI verification](PASS64_SOURCE_SHOTGUN_AND_BOLT_ACTION.md)

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 63

**Date:** October 11, 2026 (US Central). **Branch:** `twr-pass63-source-magazine-reload`.

**Successful exported Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38161375690

- **548 Python regressions passed**, one inherited Pass34 XML FutureWarning.
- Godot/.NET Windows export succeeded with **zero errors**, one inherited Pass36 CS0108 compiler warning.
- `Pass63SourceMagazineReload` now consumes inert, normalized original `Reload.Sequence.ModelMotor.Mag` keyframes for supported guns, including explicit source `Lerp`/`Time`/numeric CFrames and exact `module.Stats.Rels.MagRel` symbolic references. The latter is safely derived from the **separate Pass62 SHA-verified original Mag.Handle hierarchy**, without evaluating source Luau. Original manufacturer model geometry files are not newly recovered.
- Original **Glock 17, AK-47, M4A1, P90 and Mossberg 500** magazine cue counts of **4, 5, 4, 8 and 3** are loaded and validated. Original magazine removal/insertion poses and rotations apply rigidly to all visible members of the owner-source top-level `Mag` subassembly, then reset to original rest poses. Existing generic magazine-drop fallback remains without the owner source pack.
- Native exported-Windows `TWR_SMOKE_PASS63_MAGAZINE_OK` passed: exact cue counts, source magazine CFrame parsing, original-relative and absolute motion, two-piece Glock magazine motion and group-spacing preservation, return to rest, blocked executable expressions and unchanged ammo authority.
- Pass59–62 weapon smokes and `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` passed. The latter remains **scripted domain progression**, not a full human-played 15-wave run on ten maps.
- CI synthetic magazine pack creation and cleanup passed; private owner asset exclusion passed, and **no Windows EXE or owner-private source pack was published as a public artifact**.

**Outstanding:** original Motor6D C0/C1 joints, detachable magazine meshes/shell-by-shell loading, original character hands/IK, texture UVs and missing MeshPart triangle bytes, original dynamic AimCF, complete visual and physical restoration across all ten maps, full player-controlled round tests, and private final Windows distribution. **Not yet a fully accurate 1:1 game.**

[Pass63 original source magazine reload implementation and Windows CI details](PASS63_SOURCE_MAGAZINE_RELOAD.md)

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 62

**Date:** October 11, 2026 (US Central). **Branch:** `twr-pass62-source-subassembly-hierarchy`.

**Verified exported Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38125821580

- **543 Python regressions passed**; one pre-existing Pass34 XML FutureWarning.
- **Godot/.NET Windows export succeeded with zero errors**, one pre-existing Pass36 CS0108 warning.
- Original `TestPlace.rbxlx` analysis recovered authored ancestry and real nested Handle CFrames for **259** unambiguous groups across **78** source weapon models, from the existing verified **98 models/1,484 parts**. **Motor6D C0/C1 joint data and remote mesh triangles were not recovered**; moving group pivots are source Handle CFrames rather than exact motor joints.
- New optional owner-only `SourceWeaponMotorHierarchy62.json.gz` is SHA-pinned to the exact `TestPlace.rbxlx` and unchanged Pass47 model-assembly pack. Original grouped Slide and Pump visible parts now move in their Handle-local axes and reset to source rest transforms where Pass61 has eligible source animations. Fallback remains unchanged without the sidecar.
- Native `TWR_SMOKE_PASS62_SOURCE_HIERARCHY_OK` verifies **two visible Glock slide group children** and **two visible Mossberg pump group children**, including rotated/off-axis Handle pivots, group-local movement and return-to-rest. It uses only synthetic CI fixtures created and cleaned during the test.
- Previous Pass59–61 first-person source-weapon smokes passed. `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` remains a **scripted domain progression test**, not manual end-to-end gameplay.
- All synthetic source pack cleanup/private-asset exclusion checks passed; public CI still **does not publish the Windows EXE or private original source packs**. A separate owner-only ZIP contains exact SHA-verified original Pass47 assembly and new Pass62 hierarchy data for offline local installation.

**Remaining:** actual cloud source MeshPart/CSG triangles and UV/texture binaries, original Motor6D C0/C1 joints, magazine and shotgun shell manipulation, source character hand/arm IK, dynamic AimCF, all-map interior/lighting and collision parity, ten full human-played 15-wave acceptance runs, and complete authorized private Windows EXE release. **Not a finished 1:1 game.**

[Pass62 original nested weapon subassembly/owner-only pack engineering record](PASS62_SOURCE_WEAPON_SUBASSEMBLIES.md)

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 61

**Date:** October 11, 2026 (US Central). **Branch:** `twr-pass61-source-weapon-cycling`.

**Successful exported Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38124940483

- **538 Python regressions passed**, with one inherited Pass34 FutureWarning.
- **Windows Godot/.NET export: zero errors**, one inherited Pass36 CS0108 warning.
- Original-normalized `Equip.AV.Main` first-person weapon CFrame keyframes now play using existing equip-authority duration; source-authored `ModelMotor.SlideSliding` and `ModelMotor.Pump` offsets drive actual visual pistol slides, rifle bolts and shotgun pumps where matching meshes exist. Missing or dynamic motor expressions are refused.
- Native Pass61 smoke `TWR_SMOKE_PASS61_CYCLING_OK`: source Glock 17 slide moves **0.063m**, source Mossberg 500 pump moves **0.084m**, source reload pump timeline recognized, original equip CFrames interpolated, real Godot viewmodel mesh positions change and reset, no ammo/economy contract changes.
- Previous Pass59/60 native weapon motion smokes and `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` continue to pass. The latter is still **scripted domain progression**, not a full 10-map human-controlled game completion.
- Private asset exclusion remains active; source packs and the Windows executable are **not uploaded publicly**. No player-downloadable fully populated Windows game is delivered.

**Major unfinished fidelity:** original mesh/UnionOperation triangle files and textures, exact source Motor6D per-subassembly hierarchy, all magazine/hand/shell animations, dynamically built AimCF, a truly faithful visual/physical reconstruction of ten maps, real 15-wave manual playtests and private complete Windows distribution. **Not 1:1 complete.**

[Pass61 original handling implementation and CI record](PASS61_ORIGINAL_WEAPON_HANDLING.md)

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 60

**Date:** October 11, 2026 (US Central). **Branch:** `twr-pass60-source-weapon-animations`.

**Successful exported Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38124413516

- **534 Python regression tests passed** with one inherited Pass34 FutureWarning.
- **Godot/.NET Windows export succeeded: 0 errors**, one inherited CS0108 warning.
- Native `TWR_SMOKE_PASS59_SOURCE_MOTION_OK` verifies source hip/aimed Glock 17 firing travel, weapon timing and safe inert-source parsing.
- Native `TWR_SMOKE_PASS60_ANIMATION_OK` verifies 3D weapon transforms built from original numeric CFrame matrix and angle records; distinct source hip/aimed recoil translations and rotations; source sprint position/rotation; reload `AV.Main` keyframe playback; unchanged reload duration authority; blocked invalid source expressions.
- Pre-existing domain smoke `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` passes, but remains **scripted progression only**, not a real complete-wave manual gameplay playthrough.
- Public CI private-asset exclusion passed. No owner-private source media or downloadable EXE is published to GitHub.

**Remaining:** original Roblox remote mesh triangles/CSG and UV/texture assets, dynamic sight AimCF, exact source arm/hand/finger animations, magazine/shotgun shell handling, source timing/easing fidelity, all-map decorated interior reconstruction, first-person screenshot comparisons, full interactive 15-wave game tests on ten maps and private Windows release packaging. **The project is not 1:1 complete.**

[Pass60 original first-person animation implementation and CI details](PASS60_SOURCE_WEAPON_ANIMATIONS.md)

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 58

**Date:** October 11, 2026 (America/New_York). **Branch:** `twr-pass58-original-weapon-silhouettes`.

**Successful exported Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38117871532

- **525 Python regression tests passed**, with one existing Pass34 XML FutureWarning.
- **Windows Godot/.NET export succeeded: 0 errors**, one pre-existing Pass36 CS0108 compile warning.
- Native `TWR_SMOKE_PASS58_DETAIL_GEOMETRY_OK`: synthetic original-style Glock tool, 12 visible parts, 1 local handmade OBJ, 8 unavailable source MeshIds, 5 reconstructed detail profile meshes, 1 optic lens disk, 1 narrow barrel tube, 1 conservative bounds object, 3 original-style primitives; smoke verified cached mesh geometry, invalid-size guards, source CFrame transform path, Armory fidelity counters, unchanged credits/skin state and scene cleanup upon entering Manor.
- Owner-private original source inventory: **98 authored models, 1,484 source parts (548 invisible helpers, 936 visible)**; exactly **390** visible original parts eligible for geometry profile/tube approximation (140 chassis, 78 tube, 60 grips, 50 magazines, 43 stocks, 10 guards, 9 lenses). **525 MeshParts remain unclassified bounds**, **21 other visible native/Union parts**. **1,137 referenced source MeshIds do not include original triangle bytes in TestPlace**. Approximation is NOT complete recovery.
- Verified previous baseline `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` remains a **domain simulation**, not human-controlled end-to-end gameplay.
- Private Pass58 ZIP contains previous source tool/skin/wiki packs plus a per-weapon geometry opportunity audit. Owner-only asset ZIP and local original mesh files are not added to public Git; CI's artifact upload steps remain disabled.

**Still missing:** actual custom Roblox MeshPart/CSG triangles, original shader animations and image bytes, exact 3D weapon silhouettes, map interiors and image-quality references across all 10 maps, manual full 15-wave match validation, and an installable private complete Windows release.

[Pass58 engineering record](PASS58_SOURCE_WEAPON_SILHOUETTES.md)

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 57

**Date:** October 11, 2026 (America/New_York). **Branch:** `twr-pass57-offline-source-weapon-geometry`.

**Successful Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38115844881

- **518 Python regression tests passed**; one inherited Pass34 XML FutureWarning.
- **Windows Godot/.NET export succeeded with zero errors** and one pre-existing Pass36 CS0108 warning.
- Native `TWR_SMOKE_PASS57_GEOMETRY_OK`: synthetic nine-part Glock tool with **8 visible** authored-style parts; **1 installed OBJ**, **2 unavailable source MeshIds**, **1 circular MeshPart stand-in**, **2 conservative box stand-ins**, **4 native Part/Wedge primitives**, strict source `rbxassetid://` parser and traversal guards, working Armory provenance, intact credits/owned skins, and proper game-entry cleanup.
- The verified TestPlace owner source metadata has **98 original tool models/1,484 parts/1,137 unique mesh IDs**. The owner-only Pass47 pack has **100 model names** after two documented aliases; it covers **all 91 active catalog weapons**. Missing original mesh triangle data remains unavailable and must not be counted as reconstructed.
- Previously verified domain progression smoke: `TWR_SMOKE_COMPLETE_OK maps=10 waves=150`, covering a simulated progression fixture only—not actual player-controlled full 15-wave matches.
- Private geometry and art reference kit remains separate from GitHub; all synthetic source assets are removed before privacy audit. No public EXE is uploaded by CI.

**Still pending:** actual original MeshPart/UnionOperation triangle files, richer original character/weapon animations, exact texture visual parity, all ten maps' interiors and lighting, manually played complete-wave acceptance, and distribution of a private playable Windows EXE with complete local assets.

[Pass57 source geometry/CI evidence](PASS57_ORIGINAL_WEAPON_GEOMETRY.md)

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 56

**Date:** October 10, 2026 (US Central). **Branch:** `twr-pass56-source-wiki-gallery`.

**Successful Windows Actions:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38109511432

- **512 Python regression tests passed**, with one pre-existing Pass34 XML FutureWarning.
- **Windows Godot/.NET build succeeded with 0 errors**, one inherited Pass36 CS0108 compile warning.
- Native `TWR_SMOKE_PASS56_WIKI_REFERENCE_OK` passed for synthetic owner-style wiki reference PNGs, exact Glock 17/Neon Ego lookup, default-only fallback disclosure, Armory and Inventory gallery navigation, Back restore, no credits/ownership mutation, and gallery teardown when a Manor match starts.
- The owner-supplied `images.zip` / `image-docs.zip` archives were matched against 91 normalized weapons and 118 known skin IDs. The separate private kit contains **2,033 integrity-checked reference images** (90 default weapon pictures, 1,563 weapon/skin variants, 270 pose/UI references, 110 icons) plus Pass54–55 metadata. Original source archive SHA256s and private reference pack SHA256 are pinned. **These wiki pictures are not original Roblox 3D texture/normal/UV resources**.
- Baseline `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` covers a domain simulation, not 150 real-time human-played waves.
- Public Windows Actions artifacts/EXE upload actions remain disabled; the private wiki ZIP and skin packs remain owner-local.

**Remaining:** original UV textures/normal maps, original source mesh/UnionOperation triangles, actual animated finishes, map interior/light/sound pixel fidelity, fully player-controlled 15-wave gameplay validation, and a private Windows release with executable and assets.

[Pass 56 development record](PASS56_WIKI_REFERENCE_GALLERY.md)

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 55

**Date:** October 10, 2026 (US Central). **Branch:** `twr-pass55-source-skin-textures-and-shimmer`.

**Verified Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38108741473

- **506 Python regressions passed**; one inherited Pass34 XML FutureWarning.
- **Native Godot/.NET Windows build succeeded** with zero errors and one existing Pass36 CS0108 warning.
- Native `TWR_SMOKE_PASS55_LAYERS_OK`: validates synthetic original-style skin layer IDs, locally installed PNG, source UV tiling/offsets, a clearly labelled missing-image procedural proxy, deterministic reconstructed neon/ForceField shimmer, safe material reset, and unchanged player economy.
- Source scripts enumerate **118 skin image icon IDs, 94 face texture layers and 33 MeshPart texture references**. Optional owner-only SourceSkinLayers55.json.gz contains inert source references and verified provenance; it contains no image bytes. Pass54 packs remain optional and can be installed together with Pass55 metadata next to the private game executable.
- Previous domain baseline: `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` (simulated progression, not a human-controlled match); owner/synthetic private assets are absent from public Windows CI output. All public EXE upload steps remain disabled.

**Unfinished:** authentic original source diffuse/normal image bytes, precise Roblox UV six-face projection and layered effects, original animated shader/timing, full original geometry for all maps, human-played 15-wave gameplay QA, and a privately distributed final Windows EXE.

[Pass55 implementation, provenance and installation record](PASS55_SOURCE_SKIN_TEXTURES_AND_SHIMMER.md)

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 54

**Date:** October 10, 2026 (US Central). **Branch:** twr-pass54-original-skin-materials.

**Successful Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38106666088

- **498 Python regressions passed**, with one older Pass34 XML FutureWarning.
- **Native Windows Godot/.NET build succeeded: zero errors, one existing Pass36 CS0108 warning.**
- Native TWR_SMOKE_PASS54_SOURCE_SKINS_OK confirmed synthetic source-like skin RGB, exact Value1..Value5 part tags in a four-part weapon, safe switching and removal of cosmetic overlays, unchanged player credits and no source lobby objects leaking into Manor gameplay.
- Baseline TWR_SMOKE_COMPLETE_OK maps=10 waves=150 is simulation coverage, not manually played real-time wave completion.
- Owner scripts.zip records **118 source skins / 590 material slots**; owner TestPlace.zip records **728 tagged parts across 98 source tool models with 1,484 parts**. Two SHA-pinned private gzip sidecars can be installed beside the game to enable source-authored RGB and per-part tags. All original skin image/animation/mesh bytes are still incompletely recovered. Sidecars are never uploaded by public CI.

**Still pending:** pixel-perfect material/animated texture recovery, full original map geometry, all map gameplay playthroughs, final offline installer and privately distributed Windows EXE. [Pass54 technical record](PASS54_ORIGINAL_SKIN_MATERIALS.md).

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 53

**Date:** October 10, 2026 (US Central). **Branch:** `twr-pass53-offline-skin-cases`.

**Successful Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38105813760

- **490 Python tests passed**, one existing Pass34 `FutureWarning`; exported Godot/.NET Windows build succeeded with **0 errors** and **1 pre-existing Pass36 compiler warning (CS0108)**.
- Native `TWR_SMOKE_PASS53_SKINS_OK` exercised original-source 118 skin catalog (104 ordinary + 14 event/exclusive), seven cases, real local credit purchases, deliberate offline uniform weighting, source-offsale Hallows rejection, duplicate guard, 50% resale, safe exclusive guard, JSON profile save and reload, original-positioned 3D showroom skin proxy, skin reversion after selling, and clearing shop/lobby scene on Manor match entry.
- Existing simulated progression acceptance: `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` — **not** player-controlled real-time playthrough certification.
- No publicly downloadable `.exe` is uploaded from this branch; the private source assets are excluded in the CI security test.

**Remaining:** original skin/animated textures, precise original drop weight source, final offline paid-item policy, every map's mesh geometry, original interface fidelity, long-form player gameplay QA, and completed private Windows delivery. Details: [Pass53 source and acceptance record](PASS53_OFFLINE_SKIN_CASES.md).

---

# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 52

**Date:** October 10, 2026 (US Central). **Branch:** `twr-pass52-source-loadout-and-shop`.

**Successful Windows Actions run:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38105034356

**485 Python tests passed**, exported Windows C# Godot build **0 errors / 1 old Pass36 compiler warning**, baseline **10-map × 15-wave domain smoke** passed, Pass52 native `TWR_SMOKE_PASS52_LOBBY_SHOP_OK` passed and public/private asset audit passed. Original loaded lobby cameras now interpolate over three seconds with the recovered 50° FOV, all **91** bundled normalized per-weapon `LoadoutOffset` CFrames parsed (including source RPG Y half-turn), user-facing loadout preview tilt is limited to ±25°, and the original Shop camera and seven credit-price case tiers are available in read-only mode. Case purchases, skins, owner mesh binaries, real 15-wave playthroughs and a privately distributed EXE remain unresolved. See [Pass52 evidence and limitations](PASS52_SOURCE_LOBBY_MOTION_AND_CASES.md).

---

## Latest verified update: Pass 51 (October 10, 2026, US Central)

Development branch: `twr-pass51-original-loadout-preview`. Successful native Windows CI: https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38104204978. **477 Python tests passed**, Godot/.NET Windows executable compiled with **0 build errors**, baseline 10-map/150-wave smoke passed, new Pass51 showroom native smoke returned `TWR_SMOKE_PASS51_LOADOUT_OK`, and owner/private/synthetic file exclusion passed. The exact five source loadout CFrames are used for a non-authoritative weapon showroom and the 91-weapon menu. Source mesh ownership and rendering remain explicitly distinguished from approximate presentation. This is tested on fabricated CI source packs; no original-asset 3D side-by-side comparison, human-driven 15-wave match or private distribution EXE was produced in this pass. [Detailed Pass51 record](PASS51_ORIGINAL_LOADOUT_PREVIEW.md).

# PASS 4 — Verified source terrain and improved infected/weapon visuals (October 2026)

**Source branch:** twr-offline-dev. **Latest pass 4 successful Windows
integration build:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/37875134264
(358 Python tests passed, zero C# compiler warnings, native Windows Godot
export, synthetic terrain/water/collision smoke, six source-weapon assemblies,
six source-zombie assemblies, ballistic effects, and all ten map source
loader smoke tests, and the audio classification tests).

## Newly implemented

- Original owner-held Terrain.SmoothGrid v1 bytes for all ten named release
  map snapshots are successfully decoded with a bounded native RLE decoder.
  4-stud source voxel positions, original 23-slot MaterialColors palettes
  and water-vs-solid classification are preserved.
- tools/terrain/convert_smoothgrid.py produces deterministic compressed
  .terrainmesh.gz greedy-surface files. Merging original voxel faces reduces
  the content enough for chunked offline rendering/collision.
- RecoveredTerrainRuntime integrates solid voxel meshes as original-colored
  chunk objects, creates static ConcavePolygonShape3D collision geometry
  for solids, and draws non-collidable water separately.
- The optional source file path is Content/Terrain/<Map>.terrainmesh.gz,
  physically located beside the private exported Windows EXE. Fallback maps
  continue to launch if a source terrain file is absent.
- Source firearm part geometries now align the barrel direction toward the
  Godot first-person camera and derive muzzle-flash coordinates from the
  source model's barrel position instead of fixed category guesswork.
  Magazines and bolts/slides move independently during basic firing/reload
  animations.
- Visible short-lived bullet tracers and solid-vs-infected impact flashes
  are generated offline. They are approximations of the real game.
- Infected track sustained movement failure and try alternate detour angles;
  recovered zombie assembly presentation now includes lightweight hit flinch
  and type-specific movement gait. This is NOT complete cross-floor pathfinding.
- The 190 owner-held original SoundIds have an optional heuristic
  classification utility. No restricted audio was downloaded.

## Private source delivery

TWR-Pass4-Original-Terrain-Private-Pack-v5.zip contains all ten original
place scene packs, converted original terrain surface files, source weapons,
source infected variants, and private audio reference indexes. The
Build-Private-Windows.ps1 wrapper can locally compile the matching current
GitHub source with Godot/.NET and package the Content folder to a Windows
ZIP outside GitHub.

The source chunk converter uses NumPy at offline conversion time only;
the exported game does not depend on Python, Roblox, internet or Godot
editor to read the already converted terrain files.

## Integrity and limitations

- Ten real private terrain files passed binary-face and archive integrity
  checks. The exported Windows Godot smoke verified a generated SYNTHETIC
  terrain file: one chunk, solid collision, separate water material.
  No actual owner-held ten-terrain graphical Windows test has been run.
- Terrain representation uses rectangular exposed voxel faces (greedy mesh),
  not Roblox's original surface-nets smoothing and interpolation.
- MeshPart and UnionOperation render binaries, original sound/animation
  binaries, skyboxes, UI texture artwork and some character variants remain
  incomplete.
- A domain-only 15-wave simulation does not equal ten complete human-
  controlled playable 15-wave matches.
- Original objectives, zombie multi-floor navigation, rendering performance,
  real collision clearances and graphical fidelity still need testing.
- No newly compiled EXE with this pass has been delivered privately in chat.

---

# LATEST VERIFIED THREE-PASS IMPLEMENTATION UPDATE — OCTOBER 2026

**New owner-requested iteration series on twr-offline-dev.** The historical
checkpoint below remains for provenance; this section records what was
actually implemented and tested in the newest three passes.

**Latest fully verified Windows build:** Actions run
https://github.com/pigsdfasdfasfsad/jbt/actions/runs/37871159241
(346 Python tests passed, 0 compiler warnings, native Godot Windows export
passed; all ten synthetic original-source map loaders, offline OBJ+PNG and
PCM16 WAV file loading, original-source zombies/weapons and Expressway
navigation smoke tests passed). The new muzzle-flash regression and private
packaging script PowerShell parser check also passed.

## Pass 1 — Map rendering and privately installed art

- OfflineAssetResolver loads valid authorized unit-normalized .obj meshes
  and .png files directly from Content/Assets beside exported Windows EXE,
  with caps and resource caches; original asset IDs are never downloaded
  at runtime.
- Existing Godot res:// mesh resources remain supported at build time.
- Loader is wired into all ten source maps, recovered infected variants and
  original weapon part assemblies; verified by a genuine exported Windows
  smoke using a tiny synthetic external OBJ and a valid external PNG.
- RobloxMaterialSurface adds non-original deterministic detail to source
  material colors for wood, concrete, brick, grass and related surfaces.
  Source mesh binaries are still missing for many geometry records.

## Pass 2 — Zombies, combat and interior clearance

- All 44 available source-derived active infected R6 variants lacked a
  complete ordinary Head part. Missing heads are now reconstructed at source
  R6 scale without inventing cloud mesh IDs, and accessory handles without
  their original mesh are no longer drawn as large solid blocks.
- Melee attack now requires floor-aware vertical proximity and an unobstructed
  physics ray, preventing through-floor and through-wall ghost hits; Bloater
  spore attacks also check for direct world obstruction.
- Player and infected collision radii were decreased toward Roblox R6
  world scale. Source infected spawns were recentered to the new capsule
  height to better support narrow rooms and doorways.
- A complete multi-floor navigation mesh and original animation tracks have
  NOT been verified. This is an improvement, not end-to-end combat fidelity.

## Pass 3 — Audible offline presentation, firing effects, release packaging

- OfflineAudioRuntime plays synthesized, original replacement sound cues
  for gunfire, reload, melee, infected deaths, player damage and wave
  transitions, or uses owner-provided valid PCM16 mono/stereo .wav files
  beside the exported EXE in Content/Audio.
- A brief emissive first-person muzzle flash was added for firearms.
- build/Windows/package_private.ps1 now accepts -PrivateSourceZip and checks
  each of the ten original source-map headers, refuses synthetic fixtures,
  bundles recovered enemy/tool source blueprints and terrain data, optionally
  copies authorized mesh/texture/sound files, and writes a manifest with hashes
  and explicit warnings. Private ZIP destination must remain outside GitHub.
- CI checks the Windows packaging script's PowerShell syntax without
  uploading or installing user-owned original art in public Actions.
- Owner-held chat download TWR-Three-Pass-Private-Source-Pack-v4.zip includes
  the actual ten-map recovery data and a private Windows build wrapper. It is
  NOT an EXE; the latest code must be built with Godot/.NET on Windows.

## What remains objectively incomplete

1. Original mesh/union/terrain visuals, textures, skyboxes, animation clips
   and many original audio binaries must still be recovered or recreated.
2. Original map-scale collision and AI navigation must be interactively tested
   on all ten authentic source packs, not just synthetic smoke fixtures.
3. Real objective placements, pickup accessibility, all weapon behaviors,
   lighting and performance must be visually calibrated in actual gameplay.
4. Full human-controlled 15-wave sessions, benchmarked performance,
   automated crash recovery and signed/private Windows executable release
   are not complete.
5. The old 72 MB preview ZIP is NOT upgraded by this source code commit.
   GitHub intentionally hosts no private executable artifact.

---

# CURRENT SOURCE RECOVERY UPDATE — 2026-10-08

**New complete original-place evidence supersedes the older source-blocker
sections below.** New owner-supplied `twr places(1).zip` includes the loaded
Workspace/Map scene for every one of the ten release maps. Original full
source-positioned scenes have been privately extracted and linked in the
conversation alongside 100 original weapon assembly hierarchies, source R6
infected variants, original Lighting metadata and 190 sound IDs.

**Canonical current update:**
[docs/development/SOURCE_PLACE_RECOVERY_2026-10-08.md](SOURCE_PLACE_RECOVERY_2026-10-08.md)

Old assertions below that the nine maps have *only* incomplete CMaps
fragments were accurate before the new source ZIP was provided but are now
historical. Real original mesh/CSG assets and terrain voxel conversion remain
unresolved; complete Windows graphical playtests are still pending.

---

# TWR Offline — Current Implementation Status
Updated: 2026-10-08. Canonical checkpoint for continuing the existing Godot/C# game.

## Target release and privacy
- Windows 11 fully offline single-player in Godot 4 C#/.NET.
- Ten source release maps, Regular mode, exactly 15 waves, no wave 16.
- GitHub source branch: pigsdfasdfasfsad/jbt, twr-offline-dev.
- No executable archive, original Roblox place bytes, or restricted asset
  binaries are published to GitHub Releases or Actions artifacts.

## Most recent independently verified Windows build
- Latest independently verified code commit: 5b9b16d58ee724529c3fe20772f83f344bf8507c.
- Successful Windows Actions run:
  https://github.com/pigsdfasdfasfsad/jbt/actions/runs/37840538113
- **315 Python tests passed**, all validators passed, and the Godot
  C# Windows export succeeded with **zero compiler warnings**.
- Expressway scene smoke constructed 140 bridge objects, 14 traffic
  assemblies, 61 screening-zone props and 12 street-lamp assemblies.
- Expressway navigation smoke verified **204 reachable AStar3D waypoint
  nodes** and a connected **29-point route** along the bridge. This is
  path-construction evidence, not evidence that every enemy navigates reliably
  in actual human-controlled wave gameplay.
- Exported executable smoke markers:
  TWR_SMOKE_COMPLETE_OK maps=10 waves=150;
  TWR_SMOKE_WEAPON_VISUALS_OK categories=6 throwables=1;
  TWR_SMOKE_INFECTED_VISUALS_OK types=8;
  TWR_SMOKE_FIDELITY_METADATA_OK;
  TWR_SMOKE_LAB_SOURCE_OK infected_spawns=15 item_markers=127 fortification_markers=47.
- Synthetic Laboratory CI fixture: 30,000 geometry items, 17,600 collision
  shapes, 801 source lights, original-count spawn groups. Asset caches correctly
  report 3 synthetic mesh IDs and 2 synthetic texture IDs unresolved.
- **Critical distinction**: 150 simulated wave state transitions are not a
  human playthrough, and a synthetic scene is NOT an original map visual test.

## Implementation passes added after initial Laboratory reconstruction
1. 6744312a / 75ad58fd: source wedge geometry and convex wedge colliders,
   corrected prepared-mesh material overrides, source-part material handling,
   and rebuilt tests. Previous failures were repaired in later green commits.
2. 75ad58fd: infected capsules are centered 0.9m above exact source foot
   markers, retaining original horizontal coordinates.
3. 462fdd16 / a634a46d: separate source pools of 127 ItemPickupBox coordinates
   and 47 FortificationPickupBox coordinates; runtime samples unique marker
   positions instead of placing forts beside approximate item anchors.
4. 5ae5c3dd: reproducible, source-hash-validated private marker extractor and
   synthetic tests. The owner-held private pack v2 was generated locally and
   verified. Its raw scene records are NOT in Git.
5. d941ed06: SpecialMesh.Scale now affects visuals independently from the tiny
   physical carrier Part.Size (important for remote Laboratory mountains).
6. 99ce37ea / de7bc11b: category-specific animated, multi-part first-person
   weapon fallbacks replace the single rectangular gun, with a prepared original
   weapon scene resource hook. All 6 representative types pass Windows smoke.
7. ff263c68: Roblox authored speed and weapon Distance/Range values are
   converted from studs to Godot metres using 0.28m/stud.
8. c5409705 / 46cb7b2a: F12 owner-local screenshot + camera-transform JSON
   metadata capture for reference comparisons. CI validates metadata only.
9. 2469d1bb: source grenade/fire/gas/spore area radii now use consistent
   stud-to-metre conversion; projectile speeds and unproven falloff remain
   explicitly approximate.
10. e7f1b2ec: private asset installation audit; verifies owner-provided .res/.png
    headers and records original source IDs missing without fetching assets.
11. b57ac978: source-mesh and prepared-texture lookup caches reduce repeated
    resource checking, and logs now count loaded/unresolved IDs.

## Owner-held source artifact packages — private conversation downloads
- TWR-Laboratory-Source-Pack-v2.zip:
  Content/Maps/Laboratory.scene.jsonl.gz with 34,268 geometry records,
  1,517 server-wall records, 836 lights, 15 infected spawn locations,
  8 player spawn locations, and NEW original 127/47 pickup spawn markers.
  Standalone original model/texture/UnionOperation binaries NOT included.
- TWR-Infected-Assembly-Evidence.zip: 520 inert instance/attachment/mesh
  references from infected asset hierarchies, no original mesh binaries.
- TWR-All-Maps-Partial-Source-Evidence.zip: partial stored source fragments and
  collision data for ten maps. NOT ten complete source-rendered maps.
- TWR-Map-Visual-Reference-Set.zip: private gameplay reference images.
- Original source archives currently accessible:
  TestPlace.zip, scripts.zip, other scripts and information.zip,
  images.zip, image-docs.zip, videos of game.zip.

## Critical source availability
- TestPlace.zip newer snapshot has a complete loaded Laboratory source scene
  in Workspace/Map plus separate stored CMaps fragments for ten maps.
- The separate previously referenced twr places.zip is NOT currently available
  in this Project's visible files, Library search, or working container.
  A complete original export for the remaining nine maps is NOT recovered.
- A separate SouthernMansion_Forensic_v2.rbxlx was found in Library, but it
  does not identify itself as original TWR Manor. Do not silently promote it
  to authentic source map data.
- 490 distinct Laboratory original MeshId references and 59 distinct
  MeshPart TextureID references are present in the manifest. Asset references
  are NOT binary meshes/textures. Decal/Texture face dependencies add more.
- At least 3,013 Laboratory UnionOperation render binaries remain unrecovered.
  Original CSG mesh data cannot be inferred from its part bounds alone.

## Per-map acceptance status

| Map | Available evidence | Integrated runtime | Source models/materials | Actual visual/playtest |
| --- | --- | --- | --- | --- |
| Laboratory | Loaded source geometry, 174 pickups, spawns, 836 lights | Optional owner-held private scene pack; fallback blockout | Missing many original mesh/CSG/texture files | NOT VERIFIED |
| Ranch | Partial stored fragment | Approximate blockout | NOT COMPLETE | NOT VERIFIED |
| Mill | Partial stored fragment | Approximate blockout | NOT COMPLETE | NOT VERIFIED |
| Bypass | Partial stored fragment | Approximate blockout | NOT COMPLETE | NOT VERIFIED |
| Cabin | Partial stored fragment | Approximate blockout | NOT COMPLETE | NOT VERIFIED |
| Cargo | Partial stored fragment | Approximate blockout | NOT COMPLETE | NOT VERIFIED |
| District | Partial stored fragment | Approximate blockout | NOT COMPLETE | NOT VERIFIED |
| Expressway | Partial stored source; 6 original screenshots | Reference-guided elevated bridge/checkpoint, vehicles, procedural material/sky and 204-point interim AI routing | Original source mesh/CSG/texture binaries NOT COMPLETE | Windows headless scene/nav smoke PASS; graphical playtest NOT VERIFIED |
| Prison | Partial stored fragment | Approximate blockout | NOT COMPLETE | NOT VERIFIED |
| Manor | Partial stored fragment, 1,602 unresolved unions | Approximate blockout | NOT COMPLETE | NOT VERIFIED |

## Other incomplete first-release systems
- Infected: 8 distinct procedural animated humanoid proxies, but still no
  exact original meshes, animations, multi-floor navigation or ragdolls.
- Weapons: 91 mechanical catalog entries; category-model fallbacks exist, but
  original weapon rigs, animations, sound, muzzle effects, and visual parity
  are NOT complete.
- Objectives/fortifications: functional baseline, but exact original per-map
  objective positions and fort emplacement zones unavailable.
- UI/audio/VFX: functional menu/HUD shell, but original-style visuals, full
  audio and animations, sky/weather/post-process not complete.
- Real gameplay: no full interactive 15-wave human-controlled original
  Laboratory playthrough, nor any interactive testing of the other nine maps.
- No up-to-date compiled Windows executable has been delivered in this chat.
  CI intentionally discards its executable rather than making it downloadable.

## Next implementation passes / acceptance gates
1. Privately install the real owner-held Laboratory Source Pack v2 and execute
   a graphical Windows build: verify loaded-source marker counts, geometry,
   movement/scale, floor and ramp collisions, 127/47 pickup pools and
   navigation under actual gameplay (not synthetic smoke).
2. Use F12 to capture position/FOV-matched Godot screenshots; compare original
   Laboratory image references room by room. Repair geometry, materials, missing
   props, light behavior, reflections, shadow budget and atmospheric effects.
3. Resolve or legally recreate actual mesh/texture/CSG/sound/animation assets.
   Use tools/assets/private_asset_inventory.py and original source manifest to
   track dependencies. Do not claim art fidelity from asset ID strings.
4. Obtain full authorized original exports of the other nine maps if possible;
   otherwise reconstruct missing geometry with explicit visual references,
   tracked deviations and practical gameplay tests. Never treat CMaps fragments
   as the full original decorated maps.
5. Source-guide infected assembly and AI navigation across all map floors,
   original weapon handling and appearance, objectives, pickups, fortifications,
   UI, sounds, weather and progression calibration.
6. Play entire Regular 15-wave sessions on each of ten maps, verify FPS/memory,
   save/restart, objective accessibility, offline startup, and crash recovery.
7. Privately package the final Windows executable with complete authorized
   local data via build/Windows/package_private.ps1. Do not publish it on GitHub.

## Useful developer files
- src/Twr.Godot/Scripts/LaboratorySourceLoader.cs
- src/Twr.Godot/Scripts/LaboratoryLightStreamer.cs
- src/Twr.Godot/Scripts/RobloxPrimitiveGeometry.cs
- src/Twr.Godot/Scripts/InfectedVisualAssembler.cs
- src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs
- src/Twr.Godot/Scripts/FidelityCaptureRuntime.cs
- src/Twr.Godot/Scripts/RobloxUnits.cs
- tools/maps/augment_laboratory_pickups.py
- tools/assets/private_asset_inventory.py
- docs/development/LABORATORY_PICKUP_MARKERS.md
- docs/development/LOCAL_FIDELITY_CAPTURES.md
- docs/development/ROBLOX_WORLD_UNITS.md
- build/Windows/build.ps1 and build/Windows/package_private.ps1

Complete means the playable game and graphics meet acceptance criteria, not
merely that C# compiles or automated wave state progression passes.

## Visual reconstruction pass: Expressway and HUD (2026-10-08)

- Source-guided Expressway raised bridge and checkpoint implemented through
  ExpresswaySceneBuilder (commit 4b3b2a8).
- The exported Windows scene smoke created 140 bridge nodes, 14 traffic nodes,
  61 checkpoint objects and 12 street lamp assemblies.
- The procedural scene is an **approximation** using original TWR Expressway
  screenshots, not an import of the complete original map assets.
- The HUD now includes anchored survival/wave panels, a segmented armor bar,
  a health bar, centered sight and a circular weapon/ammo gauge inspired by the
  owner-provided WeaponHUD.png. The weapon silhouette is temporary vector art.
- Real interactive camera screenshot comparison is still required. Automated
  headless runtime construction cannot certify the appearance or playability.

## Expressway first full visual implementation pass — latest (2026-10-08)

**Original screenshot references used:** images.zip entries
Expressway.png, ExpresswayPreview.png, ExpresswayPreview2.png,
ExpresswayIcon.png, Card-expressway.png and Vote-expressway.png.

**Actual code and CI evidence:**

1. 1c9bff43 / 4b3b2a8b: Created 925-line ExpresswaySceneBuilder
   and replaced the visible Expressway box arena with an elevated freeway
   structure, multi-lane asphalt, bridge beams/pylons, concrete parapets,
   skyline, abandoned traffic, two military Humvees, screening tents,
   barricades, fencing, stop signs, sandbags, traffic cones, crates,
   checkpoint signage, stationary helicopter and street lamps.
2. 65bbd6b4 / 1a16c776 / 4cc5bec9: Original-inspired circular
   ammunition HUD and responsive health/armor/wave panels. The first
   attempt left two obsolete source-string assertions; the next Windows
   run passed after verifying real behavior remained intact.
3. 4d32b719 / 68f529a3 / 92f67ee2 / 5b9b16d5: Dedicated shared AStar3D
   static-lane route network for infected on Expressway, with blockers for
   vehicles, Humvees, sandbags and screening walls. Two C# binding
   mismatches involving AStar3D.GetPointCount were fixed. The final
   compiled Windows scene smoke passed with 204 nodes and a 29-point path.
4. 663b288d: Deterministic locally generated surface-grain textures for
   Expressway asphalt and concrete to avoid the earlier entirely flat gray
   surfaces. No network or proprietary mesh/texture data is used.

**Gaps that prevent calling this map "complete":**

- Every road dimension and vehicle placement remains reconstructed from
  reference pictures, not original instance-level source coordinates.
- Original Expressway vehicle meshes, signs, checkpoint textures, cloud sky,
  CSG, vegetation, original spawn markers, sound and animations are missing.
- Real rendering, camera-matched fidelity comparisons, Godot frame time,
  player collision, AI movement under live waves and checkpoint traversal
  have not been interactively tested.
- The shared AStar grid approximates clearance around static obstacles but
  does not dynamically rebuild when fortifications are deployed.
- Existing original-looking model preview art is not proof of original assets.
- No latest privately downloadable Windows executable has been produced.
  CI exports executables only transiently and does not publish binary artifacts.

**Next concrete implementation passes:**

1. Graphical Windows playtest of Expressway on actual hardware with screenshot
   captures at several camera bearings using F12; repair noticeable geometry,
   lighting, vehicle, barricade placement and HUD layout discrepancies.
2. Traverse the bridge and complete multiple actual waves; catch AI pathing
   stalls, static collision blocking, line-of-sight problems, draw-call/frame
   time spikes, and unreachable pickup/objective anchors.
3. Obtain authorized original Expressway map scene/mesh/texture files if
   available; replace approximate prop meshes and material references.
4. Reuse the Expressway visual/AI engineering methods to reconstruct one
   further map to a demonstrable standard before spreading more placeholders.
5. Integrate source-based infected rig/animation, weapon/audio and effect
   packages as additional asset evidence becomes available.

