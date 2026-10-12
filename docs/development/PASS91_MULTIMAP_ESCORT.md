# Pass91 — Physically safe Escort starts on 8 fallback maps

**Starting checkpoint:** Pass90, Windows CI #38281900340, 673 tests.

Pass90 established authentic game-authority completion of a Ranch Wave5
Escort with a real 136-step NPC walk. Other fallback maps still used
reconstructed marker positions that had not been checked for an NPC-sized
capsule, often 1m above the actual collision floor or close to a wall.

Pass91 collision-audits the Escort **spawn** with the same five floor
and capsule probes already used for its verified Pass90 destination.
It tries up to 17 deterministic nearby positions, all within 10m
of the authored approximate marker, rejecting unsupported,
wall-embedded, elevated/graph-incompatible or player-overlapping
sites. It emits verified, rejected and adjusted counters.

This is strictly limited to fallback maps with Pass73 ground-floor
navigation. Original source-packed retail markers are not moved.
Expressway has a separate road graph; Manor's upper rooms require a
source-based elevated Escort policy and are deliberately left out.

The native Windows acceptance targets the actual Wave5 or Wave6
Escort spawn, walks and idempotent reward for Ranch, Mill, Bypass,
Cabin, Cargo, District, Prison and fallback Laboratory. It uses
accelerated stage timers only to reach each wave; the survivor body
uses real 60Hz physical movement and normal domain reward commands.
Original Roblox NPC animations and maps remain unverified.

**Initial status:** pending native Windows validation.

## Native feedback and door correction

- Windows run [38294806561](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38294806561)
  passed Ranch, Mill, Bypass, Cabin, Cargo and District Escort completion,
  then observed the real Prison NPC at X=-23.32, Z=-24.59, 0.59m outside
  the south end of CellBlockA. The next nav target was at X=-24, Z=-24.
- The original fallback InteriorShell adds a 0.20m raised floor lip
  *directly behind* the doorway. This boundary is not authenticated
  retail collision. Pass91 already corrected the same defect in Bypass
  using `frontDoorRelief91`; apply this collision relief to **both**
  approximate Prison cell blocks, leaving the other buildings unchanged.
- Keep a real wall, full-sized Escort capsule and physics swept
  obstruction tests. Any future failure still fails the native CI.
