# Pass86 — Collision-supported loot and fortification markers

On the nine public approximate map blockouts, the source-derived original
marker positions are missing. Historical fallback coordinates could place
natural pickups in walls, below terrain, or beyond a room's traversable
floor. Pass86 uses world collision ray/box checks to position a natural
pickup at a clear .62m height above the actual supported surface.

A randomized scan considers the full finite pickup pool (not just four
blind picks), accepting up to four physically present items and two
fortifications. Rejected positions do not create uncollectable props;
respawns revalidate their earlier grounded locations. No invented owner
original source item positions are produced by this filter.

The private original Laboratory source map loader bypasses Pass86
entirely, preserving exact source marker positions. Wave rewards and
supply-drop payloads are deliberately not modified in this pass.

Python contract tests plus the new Pass88 native 10-map smoke are the
acceptance. Faithful original loot timing/locations remain to be measured.
