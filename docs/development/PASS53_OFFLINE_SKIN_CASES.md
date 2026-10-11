# Pass 53 - Offline Skin Case Economy, Inventory, Resale and Cosmetic Proxy

## What is implemented

This pass builds on the verified Pass 52 Shop and original 3D lobby without replacing their working scene or gameplay. It introduces **actual local-credit skin case opening** and saved cosmetics. Original Roblox in-app purchases, Robux, and network services are not supported.

- An authoritative domain catalog lists **118 source script skins in seven cases** with the exact recovered Shop credit prices.
- **104 ordinary skins** can appear in case rolls: Low 20, Mid 19, High 20, Neon 15, Tactical 15, Ethereal 15. The nine High Christmas/event skins and five Hallows exclusives are catalogued but **never** awarded by a regular case purchase.
- Hallows case stays **off-sale**, as documented in the source/wiki reference. Exclusive/event skins in existing profiles are visible but cannot be sold.
- A case roll chooses an unowned ordinary skin within the selected case and deducts the **server-authoritative local** case price. The original game source did not expose drop weights; offline selection is **uniform among missing ordinary skins**, labelled **RECONSTRUCTION** in the UI. This is **not a verified recreation of retail drop probabilities**.
- Collection completion disables further purchases until a skin is sold. Duplicate awards and insufficient-credit or unauthorized purchases are rejected without spending credits.
- The profile gains backward-compatible \`OwnedSkins\` and \`EquippedWeaponSkins\` save fields. Each successful purchase, cosmetic assignment and sale saves immediately to the same atomic JSON persistence pipeline used for armory purchases and wave progress.
- The Shop has a separate **OPEN SELECTED CASE** action; selecting a case still only reads metadata. A scrollable **OWNED SKINS / APPLY / SELL** view supports applying skins to equipped Primary/Secondary weapons, removing finishes, and selling regular skins for half the case credit price.
- Selling a skin removes it from active weapon assignments, prevents double-selling, and awards only credits (not XP). Credits never overflow an integer.
- Authenticated, source-derived skin names and prices are authoritative. Texture files, animated finishes and per-part original color assignments are **not** present in the distributable executable, so the selected skin currently produces an **explicitly approximate deterministic translucent 3D tint** on lobby models and actual first-person weapon viewmodels. Gameplay ballistics, damage, recoil, projectile paths and model collision remain unchanged.

## Source evidence

- \`scripts/ModuleScript.Skins.Source.txt\` in owner-provided \`scripts.zip\`; SHA-256: \`aeff32781cc9eafdbbd5ae7bc12ff157cc32934e7dae6773c87d7bfba355a015\`; seven cases with 118 skin objects. Raw Luau is **not** evaluated at runtime.
- \`scripts/ModuleScript.Shop.Source.txt\`; SHA-256: \`4df17a84418b830e3a26f9246057a38b08234597344f62d2335a6a661e98e9db\`; seven static case credit prices.
- \`other scripts and information.zip\` -> \`09-Skins-Shop-Cosmetics.md\`; the wiki-derived record documents half-case-price sell value, collection completion, Hallows off-sale, and the fact that official drop weights **were not documented**. Two wiki entries, Spiffo and City Bus, list disputed sell prices; this offline pass implements the general half-price rule and **does not resolve the dispute as factual**.

## Validation requirements

The new Python regression suite validates exact 118/104/14 source skin catalog counts, case tiers and prices, profile/save wiring, read-only selection vs authorized purchases, source provenance and no public executable upload. Native Windows \`--smoke-pass53\` tests the actual exported Godot Shop UI with fabricated source lobby/weapon packs: case opening, credit debit, JSON reload, equipped skin 3D tint, half-price resale, no stale assignment, exclusive/offsale/duplicate rejection, and transition into active Manor gameplay without lingering shop objects. Test synthetic owner packs are explicitly cleaned before the privacy audit.

**Important remaining limitations:** Original mesh triangles, original skin texture/color material images, exact original loot weighting, actual Robux product functions, network services, final map fidelity, real player-controlled 15-wave playthroughs and a private downloadable Windows build are not complete. This is a functional **offline reconstruction pass**, not a verified 1:1 skin system.
