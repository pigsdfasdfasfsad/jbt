# Local-authoritative architecture

The standalone runtime preserves a strict authority boundary:

`Input -> Player Intent -> Local Authoritative Simulation -> Validated State Change -> Game State -> Presentation Event -> Godot rendering/audio/UI`

`Twr.Domain` owns gameplay state and rules. Godot nodes issue typed commands and consume events/read models. Godot presentation code must not directly assign authoritative XP, credits, health, ammo, wave, completion rewards, objective completion, or purchases.

The boundary intentionally resembles a client/server command contract without recreating Roblox networking. That keeps the single-player build fully offline while leaving a future LAN/co-op transport adapter possible.
