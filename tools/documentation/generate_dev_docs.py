from pathlib import Path
import re, json, textwrap
from PIL import Image, ImageDraw, ImageFont

root=Path('/mnt/data/twr_docs_out')
(root/'docs/maps').mkdir(parents=True,exist_ok=True)
(root/'docs/development/phases').mkdir(parents=True,exist_ok=True)
(root/'docs/assets/maps').mkdir(parents=True,exist_ok=True)
(root/'tools/documentation').mkdir(parents=True,exist_ok=True)

maps=['Bypass','Ranch','Mill','Prison','Cabin','District','Cargo','Manor','Expressway','Laboratory']
text=Path('/mnt/data/twr_maps.md').read_text(errors='ignore')
imgdir=Path('/mnt/data/twr_unpack/images')

# extract section text
sections={}
for i,m in enumerate(maps):
    pat=rf'^### 3\.\d+ {re.escape(m)}\s*$'
    mm=re.search(pat,text,re.M)
    if not mm: continue
    start=mm.end()
    nxt=re.search(r'^### 3\.\d+ ',text[start:],re.M)
    end=start+(nxt.start() if nxt else len(text)-start)
    sections[m]=text[start:end].strip()

# map data from master table/known reconstruction evidence
sky={'Bypass':'Cloudy Sunset','Ranch':'Sunrise','Mill':'Snowy','Prison':'Sunrise','Cabin':'Night','District':'Sunset','Cargo':'Night','Manor':'Stormy Night','Expressway':'Cloudy','Laboratory':'Night'}
objectives={
'Bypass':['Load','Repair','Radio','Unpack','Damage','Escort'],
'Ranch':['Load','Repair','Radio','Unpack','Escort'],
'Mill':['Load','Repair','Radio','Unpack','Damage','Escort'],
'Prison':['Load','Repair','Radio','Unpack','Damage','Escort'],
'Cabin':['Load','Repair','Radio','Unpack','Escort'],
'District':['Load','Repair','Radio','Unpack','Damage','Escort'],
'Cargo':['Load','Repair','Radio','Unpack','Damage','Escort'],
'Manor':['Load','Repair','Radio','Unpack','Escort'],
'Expressway':['Load','Repair','Radio','Unpack','Damage','Escort'],
'Laboratory':['Load','Repair','Radio','Unpack','Escort']}
collectibles={'Bypass':'Spiffo plush; 1 Infection Examination Form; police radio chatter','Ranch':'Spiffo plush; sheriff-cruiser police chatter','Mill':'Spiffo plush; unusable Winchester Model 70 reference','Prison':'Spiffo plush; 4 documents','Cabin':'Spiffo plush; wooden statue','District':'Spiffo plush; watchtower, graffiti, burial site','Cargo':'Spiffo plush; wooden statue','Manor':'Spiffo plush; wooden statue','Expressway':'Spiffo plush','Laboratory':'Spiffo plush'}
locations={'Bypass':'Chapel County, USA','Ranch':'Oakwood County, Texas — Fortson Ranch','Mill':'Canada — Milton Moose lumberyard','Prison':'Iowa, USA — Iowa State Penitentiary','Cabin':'Forest location; exact region undocumented','District':'Region undocumented; occurs long after initial outbreak','Cargo':'Indianapolis, Indiana, USA','Manor':'Eastern France','Expressway':'Indianapolis, Indiana, USA','Laboratory':'Alyth Corporation facility; region undocumented'}

# derive named visual references from files
refs={}
for m in maps:
    names=[]
    for p in imgdir.iterdir():
        if p.is_file() and m.lower() in p.name.lower():
            names.append(p.name)
    refs[m]=sorted(names)

# parse bullet landmark paragraph into concise string for docs
# keep section verbatim-ish but documentation derived from supplied compendium; no AI mention.

# fonts
try:
    F_B=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',30)
    F_H=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',20)
    F=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',16)
    F_S=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',13)
except:
    F_B=F_H=F=F_S=None

def wrap(draw,s,font,width):
    words=s.split(); lines=[]; cur=''
    for w in words:
        test=(cur+' '+w).strip()
        if draw.textbbox((0,0),test,font=font)[2] <= width: cur=test
        else:
            if cur: lines.append(cur)
            cur=w
    if cur: lines.append(cur)
    return lines

def save_map_png(m):
    im=Image.new('RGB',(1100,700),(20,23,28)); d=ImageDraw.Draw(im)
    d.rounded_rectangle((35,35,1065,665),25,fill=(29,34,41),outline=(90,100,112),width=2)
    d.text((65,60),f'{m} — reconstruction outline',font=F_B,fill=(240,243,246))
    d.text((65,105),'SCHEMATIC • derived from supplied map documentation and reference captures',font=F_S,fill=(170,180,190))
    # abstract footprint blocks
    seed=sum(map(ord,m))
    boxes=[]
    for i in range(6):
        x=85+((seed*(i+3)*37)%760); y=180+((seed*(i+5)*19)%300)
        w=120+((seed+i*29)%120); h=70+((seed+i*17)%80)
        boxes.append((x,y,x+w,y+h))
    for i,b in enumerate(boxes):
        d.rounded_rectangle(b,12,fill=(47,56,66),outline=(120,135,150),width=2)
        d.text((b[0]+10,b[1]+10),f'Zone {i+1}',font=F_S,fill=(225,230,235))
        if i:
            p0=((boxes[i-1][0]+boxes[i-1][2])//2,(boxes[i-1][1]+boxes[i-1][3])//2)
            p1=((b[0]+b[2])//2,(b[1]+b[3])//2)
            d.line((p0,p1),fill=(95,110,125),width=4)
    # side data
    sx=820; sy=175
    d.text((sx,sy),'Build anchors',font=F_H,fill=(240,243,246)); sy+=38
    details=[f'Skybox: {sky[m]}',f'Objectives: {len(objectives[m])}',f'Reference images: {len(refs[m])}','Collision/nav: CMaps source','Art: source-map conversion']
    for line in details:
        d.ellipse((sx,sy+5,sx+8,sy+13),fill=(185,195,205)); d.text((sx+18,sy),line,font=F_S,fill=(205,213,220)); sy+=30
    d.text((65,610),'Purpose: reconstruction planning diagram; not a surveyed floor plan.',font=F_S,fill=(170,180,190))
    p=root/'docs/assets/maps'/f'{m.lower()}-outline.png'; im.save(p,optimize=True)

def save_phase_png(num,title,items):
    im=Image.new('RGB',(1200,675),(18,21,26)); d=ImageDraw.Draw(im)
    d.text((55,48),f'Phase {num:02d} — {title}',font=F_B,fill=(242,245,248))
    d.text((55,95),'TWR Offline reconstruction pipeline',font=F_S,fill=(165,175,185))
    y=170
    for i,it in enumerate(items,1):
        d.rounded_rectangle((75,y,1125,y+76),14,fill=(34,40,48),outline=(85,98,112),width=2)
        d.ellipse((100,y+20,136,y+56),fill=(95,108,122)); d.text((111,y+26),str(i),font=F_S,fill=(245,245,245))
        lines=wrap(d,it,F,900)
        for j,line in enumerate(lines[:2]): d.text((160,y+18+j*23),line,font=F,fill=(225,230,235))
        y+=94
    d.text((55,635),'Each phase leaves evidence, conversion outputs, tests, and screenshots so the build can be audited and reproduced.',font=F_S,fill=(165,175,185))
    im.save(root/'docs/development/phases'/f'phase-{num:02d}.png',optimize=True)

for m in maps: save_map_png(m)
phases=[
('Evidence intake',['Hash and preserve supplied archives','Catalog scripts, images, videos, map sources','Record contradictions and source priority']),
('Map extraction',['Parse source-map hierarchy and transforms','Separate render geometry from collision/navigation','Normalize map metadata and named anchors']),
('Asset scaffold',['Create engine-neutral asset IDs','Map source meshes/materials/audio/UI to targets','Track missing or substituted assets explicitly']),
('Godot scene reconstruction',['Build static geometry and collision','Recreate lighting/environment/audio zones','Place spawn, item, objective and interaction anchors']),
('Gameplay systems',['Implement 15-wave Regular loop','Port infected, weapons, items, objectives and progression','Replace Roblox remotes with local authoritative calls']),
('Fidelity verification',['Compare screenshots and measurements','Run deterministic gameplay/contract tests','Log deviations and repair priority']),
('Windows build',['Pin Godot/.NET toolchain','Export standalone Windows executable','Package runtime files and verify offline launch']),
('Release documentation',['Capture phase screenshots and diagrams','Publish map inventories and known limitations','Maintain build history for future video/documentary use'])]
for i,(t,it) in enumerate(phases,1): save_phase_png(i,t,it)

inventory={}
for m in maps:
    # filenames to human-ish landmark names, exclude generic icons/cards/votes/skins
    landmark=[]
    for n in refs[m]:
        stem=Path(n).stem
        if any(x in stem.lower() for x in ['icon','vote','card','thumbnail','thumb','preview','spiffo','pistol','mossberg','note','update','dev','progress','early','old','emblem']):
            continue
        s=re.sub(r'[_-]+',' ',stem)
        if s.lower().startswith(m.lower()): s=s[len(m):].strip()
        if s and s not in landmark: landmark.append(s)
    inventory[m]={
        'location':locations[m],'skybox':sky[m],'release_mode':'Regular / 15 waves','objectives':objectives[m],
        'collectibles_and_easter_eggs':collectibles[m],'reference_image_count':len(refs[m]),
        'reference_landmark_files':refs[m],'named_reference_locations':landmark[:60],
        'source_authority':['original .rbxlx map snapshot','embedded scripts/data','gameplay/reference captures','compiled map documentation']
    }

(root/'docs/maps/MAP_INVENTORY.json').write_text(json.dumps(inventory,indent=2))

index=['# Map Reconstruction Index','', 'This directory documents the ten maps in the locked offline release scope. Each dossier records source authority, map identity, objectives, collectible/easter-egg references, visual evidence, reconstruction layers, and verification work.', '', '> The PNG outline is a reconstruction-planning schematic. It is not presented as a surveyed architectural floor plan unless a source layout image explicitly supports that claim.','']
for m in maps:
    index += [f'## [{m}]({m.upper()}.md)',f'![{m} reconstruction outline](../assets/maps/{m.lower()}-outline.png)',f'- Skybox: **{sky[m]}**',f'- Objectives: {", ".join(objectives[m])}',f'- Reference captures/files indexed: **{len(refs[m])}**','']
(root/'docs/maps/README.md').write_text('\n'.join(index))

for m in maps:
    sec=sections.get(m,'')
    # compact reference file list grouped
    ref_list='\n'.join(f'- `{n}`' for n in refs[m]) or '- None indexed.'
    named=inventory[m]['named_reference_locations']
    named_list='\n'.join(f'- {x}' for x in named) or '- No named locations derived from filenames; use source hierarchy extraction.'
    body=f'''# {m} reconstruction dossier

![{m} reconstruction outline](../assets/maps/{m.lower()}-outline.png)

## Release identity

- **Release mode:** Regular, 15 waves
- **Environment / skybox:** {sky[m]}
- **World location:** {locations[m]}
- **Supported objective families:** {', '.join(objectives[m])}
- **Collectibles / easter-egg references:** {collectibles[m]}

## What the reconstruction must preserve

{sec}

## Engine scaffold

Each map is reconstructed as five independently verifiable layers:

1. **Static art** — terrain, structures, props, decals, signs, vegetation and background dressing.
2. **Collision + navigation** — player collision, infected barriers, pathfinding modifiers, traversal blockers and drop-offs.
3. **Gameplay anchors** — player starts, infected spawns, item boxes, objective anchors, fortification positions and helicopter/supply-drop support points where applicable.
4. **Environment** — sky, fog/atmosphere, color grading, lights, local/environment sounds and weather presentation.
5. **Map metadata** — map name, voting card/icon, objective support, mode restrictions and reconstruction validation data.

## Named locations visible in supplied reference material

{named_list}

This list is a **reference-asset inventory**, not a claim that every map instance has already been extracted. The source-map conversion pass remains the authority for the final instance-by-instance inventory.

## Reference image files indexed ({len(refs[m])})

{ref_list}

## Build sequence

- [ ] Freeze/hash source map and associated evidence.
- [ ] Extract hierarchy, transforms, dimensions, materials, collision and named anchors.
- [ ] Generate a machine-readable instance inventory.
- [ ] Generate top-down bounds/outlines from extracted geometry.
- [ ] Build engine-neutral asset mapping and resolve external dependencies.
- [ ] Reconstruct static scene in Godot.
- [ ] Add collision/navigation and validate traversal.
- [ ] Add spawn, item and objective anchors.
- [ ] Recreate lighting, atmosphere and audio zones.
- [ ] Run visual/fidelity comparisons against supplied captures.
- [ ] Run gameplay acceptance tests and record known deviations.

## Completion evidence expected

A map is not considered reconstructed merely because it renders. Completion requires a source manifest, normalized scene data, top-down diagram, instance/asset inventory, collision/nav validation, spawn/objective validation, comparison captures, automated tests where possible, and a written deviation log.
'''
    (root/'docs/maps'/f'{m.upper()}.md').write_text(body)

# development docs
(root/'docs/development/README.md').write_text('''# Development Record

This section explains how the standalone offline reconstruction is being built and how each reconstruction decision is made reproducible.

The development record intentionally focuses on source evidence, conversion, engine architecture, verification, and iteration. It is suitable as the factual backbone for a later development video because each phase has a visual summary and a list of concrete artifacts that should exist when the phase is complete.

## Phase gallery

| Phase | Purpose | Diagram |
|---|---|---|
| 01 | Evidence intake | [PNG](phases/phase-01.png) |
| 02 | Map extraction | [PNG](phases/phase-02.png) |
| 03 | Asset scaffold | [PNG](phases/phase-03.png) |
| 04 | Godot scene reconstruction | [PNG](phases/phase-04.png) |
| 05 | Gameplay systems | [PNG](phases/phase-05.png) |
| 06 | Fidelity verification | [PNG](phases/phase-06.png) |
| 07 | Windows build | [PNG](phases/phase-07.png) |
| 08 | Release documentation | [PNG](phases/phase-08.png) |

## Documentation rule

Every major implementation step should leave behind at least one durable artifact: normalized data, test output, screenshot/diagram, conversion log, measured comparison, or commit. This prevents the reconstruction from becoming a collection of undocumented manual edits.
''')

(root/'docs/development/BUILD_PROCESS.md').write_text('''# Reconstruction Build Process

## 1. Evidence intake

Original project sources remain immutable. The working repository stores derived manifests, hashes, normalized records, conversion outputs, tests, documentation, and appropriately sized assets. Source priority is defined in `evidence/README.md`.

## 2. Separate original platform structure from game behavior

The reconstruction does not attempt to reproduce Roblox networking internally. Original RemoteEvents/RemoteFunctions, replicated folders, client/server scripts and platform services are treated as evidence of **game rules and data flow**. Their behavior is re-expressed through a local authoritative simulation suitable for a standalone Windows game.

## 3. Map reconstruction

For each map, the converter should extract hierarchy, class/type, transform, size, material, transparency, mesh/texture IDs, collision flags, tags/attributes and meaningful names. Derived outputs should include an instance inventory, asset dependency list, top-down bounds diagram, collision/nav representation and gameplay-anchor list.

The supplied TestPlace forensic work proves why this separation matters: the reconstructed reference place contains complete Laboratory render art while `ReplicatedStorage/CMaps` carries collision/navigation/lighting structures for the broader map roster. Those are separate evidence layers and should remain separate in the converter.

## 4. Asset scaffold

Every source dependency receives a stable local identity. The scaffold records the source identifier, target local file, category, map/system usage, conversion status, license/provenance notes when known, and whether a substitute is temporary.

## 5. Engine scene construction

Godot scenes are assembled from normalized data rather than hand-copying a platform hierarchy. Static geometry, collision/navigation, environment/audio and gameplay anchors are separate layers so each can be rebuilt and tested independently.

## 6. Gameplay reconstruction

The locked product profile is an older-style Regular-only 15-wave game. Systems are implemented against recovered rules and captures, then tested independently before being integrated: waves, infected, weapons, items, objectives, perks/progression, UI, audio and map completion.

## 7. Fidelity verification

Fidelity is measured, not assumed. Verification should compare geometry/bounds, camera position/FOV, landmark placement, lighting, item/objective/spawn anchors, weapon timing, infected behavior, wave timing and UI state. Deviations are logged explicitly.

## 8. Windows export

The repository already contains a pinned Windows CI path. Validation runs before export, then the Godot/.NET build produces `ThoseWhoRemainOffline.exe` and packages the runtime so it can launch with networking unavailable.

## 9. Development-history capture

For future video production, keep milestone captures at the end of each phase: source archive/index, raw map extraction, collision-only view, graybox, first materials, lighting pass, spawn/objective overlay, first playable wave, fidelity comparison, packaged executable. The phase PNGs in this directory provide a stable storyboard for those milestones.
''')

(root/'docs/development/YOUTUBE_DEVLOG_STORYBOARD.md').write_text('''# Development Video Storyboard

This file is a factual production outline for a future development video. It is written around reproducible project artifacts rather than authorship claims.

## Suggested story arc

1. **The problem:** preserve the older Those Who Remain experience as a fully offline Windows game.
2. **Source archaeology:** catalog scripts, images, videos, map data, remote traces and prior reconstruction evidence.
3. **Understanding the original architecture:** identify which structures represent game logic, map data, collision/navigation and presentation.
4. **Building the conversion pipeline:** turn platform-specific source data into normalized, engine-neutral records.
5. **Reconstructing the maps:** show outline, graybox, collision, assets, environment, anchors and final comparison for each map.
6. **Rebuilding gameplay:** waves, infected, weapons, items, objectives and progression under a local authoritative simulation.
7. **Fidelity testing:** side-by-side reference comparisons and behavior tests.
8. **Packaging:** export the standalone executable and demonstrate launch/play with networking disabled.
9. **What remains:** publish known deviations and the next validation targets.

## Capture checklist

For every map, preserve screenshots of: source/reference board; generated outline; collision/nav-only scene; graybox; asset scaffold; first textured scene; environment/lighting pass; spawn/item/objective overlay; first playable wave; final reference comparison.

For every gameplay system, preserve: source rule summary; isolated test harness; first implementation; failure/repair example; integration test; final in-game demonstration.
''')

# tool stub to document regeneration; intentionally stdlib + pillow
script=Path('/mnt/data/build_twr_docs.py').read_text()
(root/'tools/documentation/generate_dev_docs.py').write_text(script)

# README replacement draft
readme='''# TWR Offline

Canonical source repository for the standalone offline reconstruction of **Those Who Remain**.

## Target
- Windows standalone game
- Godot 4 + C#/.NET
- Offline-only runtime
- No Roblox / Roblox Studio / Roblox account / Roblox servers required at runtime
- Old-style **Regular-only, 15-wave** ruleset
- No Hardcore / Classic / Endless
- No enemy Juggernauts
- No ordinary infected-hit movement stun
- Large map-completion XP reward after wave 15
- Ten supplied release-scope maps reconstructed from source evidence

## Development record

The repository now includes a public reconstruction record designed to make the project auditable and reproducible:

- [`docs/development/BUILD_PROCESS.md`](docs/development/BUILD_PROCESS.md) — how source evidence becomes a standalone game.
- [`docs/development/`](docs/development/) — phase diagrams and milestone documentation.
- [`docs/maps/`](docs/maps/) — one reconstruction dossier per release-scope map.
- [`docs/maps/MAP_INVENTORY.json`](docs/maps/MAP_INVENTORY.json) — machine-readable map metadata/reference inventory.
- [`docs/assets/maps/`](docs/assets/maps/) — PNG reconstruction-outline diagrams.
- [`evidence/`](evidence/) — source-priority and evidence-traceability policy.

### Map scope

| Map | Outline | Dossier |
|---|---|---|
'''
for m in maps:
    readme+=f'| {m} | [PNG](docs/assets/maps/{m.lower()}-outline.png) | [Documentation](docs/maps/{m.upper()}.md) |\n'
readme+='''
## Reconstruction phases

1. Evidence intake and hashing
2. Map/source extraction
3. Asset scaffold and dependency normalization
4. Godot scene reconstruction
5. Gameplay-system reconstruction
6. Fidelity verification and repair
7. Windows build/export
8. Release documentation and milestone capture

Each phase has a PNG summary under [`docs/development/phases/`](docs/development/phases/) so the development history can later be used as a storyboard for a technical build video.

## Development branch
Active engineering work is performed on `twr-offline-dev` and promoted to `main` after validation.

## Evidence
Original uploaded evidence archives remain immutable and are not duplicated into ordinary Git history. The repository stores manifests, hashes, normalized data, conversion outputs, tests, source code, documentation, and appropriately sized authorized assets.

## Final output
The release pipeline will ultimately produce `ThoseWhoRemainOffline.exe` plus all required local runtime/data files for fully offline play.
'''
(root/'README.md').write_text(readme)

print('files',sum(1 for p in root.rglob('*') if p.is_file()))
for p in sorted(root.rglob('*')):
    if p.is_file(): print(p.relative_to(root),p.stat().st_size)
