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
