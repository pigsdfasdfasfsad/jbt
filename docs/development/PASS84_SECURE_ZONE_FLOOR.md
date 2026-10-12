# Pass84 — Reconstructed Manor objective floor isolation

The original GameplayRoot allowed a 60-second Radio or Unpack secure zone to
accumulate progress inside an 8-metre Y band, including while the player
stood on a physically separated floor directly below the objective.
The public approximation of Manor now uses a 1.35m same-story vertical
threshold for Radio/Unpack, while preserving the 14m horizontal radius and
60-second timer. This prevents collecting an upper-floor secure objective
from the lower floor without walking the stairs.

The extra gate only activates when fallback Manor creates the three
Pass77 stair links. Owner private reconstructed original map scenes, other
maps, existing Load/Repair/Escort objectives and command/reward authority
are deliberately untouched. Actual original retail secure-height behavior
is not authenticated by this change.

Verification: Python source contract plus cumulative Pass88 native session.
