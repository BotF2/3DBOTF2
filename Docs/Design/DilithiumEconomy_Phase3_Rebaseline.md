# Dilithium Economy Phase 3: Rebaseline Against New Power Plant Costs

Status: draft for review. Follow-up to `FacilityCaps_Phase2_ResourceDriven.md` §3, which replaced
the flat per-civ `PowerPlantLi2Cost` (15-55) with a TechLevel-and-`PowerOutput`-driven formula
(10-128) — this doc checks what else in the Dilithium economy needs to move in response, per that
doc's instruction to keep Dilithium a genuine limiting resource rather than just inflating
stockpiles until it stops mattering.

## 1. The actual finding: starting balance doesn't need a stockpile bump — late-game income does

Ran the numbers before proposing anything. **The starting game is already fine, no stockpile
inflation needed there.** At EARLY tier, the new per-civ Power Plant costs (FED/ROM/KLING/TERRAN
20, CARD 10, DOM 30, BORG 40) are all *at or below* the old flat costs they replace (25/22/28/25,
15, 38, 55) — so `InitializeDilithiumStockpile`'s existing formula
(`ppLi2 + shipLi2 + buffer`, where `ppLi2 = GetPowerPlantDilithiumCost(civ) * CurrentPowerPlantCount`)
already comes out roughly flat to slightly *lower* once it's calling the new per-tier function
instead of the old flat one:

| Civ | Old starting stockpile | New starting stockpile |
|---|---|---|
| FED | 142 | 132 |
| ROM | 136 | 132 |
| KLING | 191 | 167 |
| CARD (4→5 plants) | 181 | 186 |
| DOM | 169 | 153 |
| BORG | 133 | 118 |
| TERRAN | 142 | 132 |

(`shipLi2` = starting Destroyer's Dilithium cost, unchanged by this doc; `buffer = 60 + 15 ×
PowerPlant count`, also unchanged — both terms are untouched, the only thing that moved is `ppLi2`
picking up the new per-tier cost.) Cardassia's number goes up slightly purely because it's now
funding a 5th plant's worth of buffer, not because of any Dilithium-specific change here.

**The real gap is late-game.** The new Power Plant cost climbs to **3.2× its EARLY value by
SUPREME** (FED 20→64, BORG 40→128 — see Phase 2 doc §3's table) because it's meant to represent a
system reactor becoming a much bigger, costlier installation as tech advances. But
`HomeworldMiningRate`/`ColonyMiningRate` (`StarSysManager.cs:1206-1228`) — the per-turn Dilithium
*income* every system generates — are **flat constants with no TechLevel scaling at all**, today
and always have been. Ship costs already scale with tech (`TierBuild`, up to 1.6× by SUPREME) so
this gap already existed for ships in a mild form; the new Power Plant formula makes it much more
pronounced (3.2× cost growth against 1.0× income growth) specifically for the resource this whole
exercise is about protecting.

## 2. The fix: scale mining income with TechLevel, not the stockpile

Add a TechLevel multiplier to Dilithium income, applied in `StarSysManager.ProcessDilithiumMining`
(the per-turn tick that adds `DilithiumMiningRate` to `DilithiumStockpile`) rather than to the
stored `DilithiumMiningRate` field itself, so the field keeps meaning "base rate as authored" and
UI/other readers of it aren't silently changed:

```csharp
// Same TierRatio Power Plant costs use (Phase 2 doc §3) - deliberately reused rather than a
// third curve. Power Plants are now the single biggest Dilithium expense and the reason this
// rebaseline exists, so matching income growth to that specific cost's growth keeps "how many
// turns of mining income does one new Power Plant cost" constant across the whole game -
// Dilithium stays exactly as scarce late as it is early, instead of getting relatively easier
// (income catching up) or harder (costs outrunning income) as TechLevel climbs.
public static int GetEffectiveDilithiumMiningRate(StarSysData sysData)
{
    var tech = sysData.CurrentCivController?.CivData?.CurrentTechLevel ?? TechLevel.EARLY;
    return Mathf.RoundToInt(sysData.DilithiumMiningRate * TierRatio[(int)tech]);
}
```

`ProcessDilithiumMining` calls this instead of reading `DilithiumMiningRate` directly; any UI that
displays a system's "Dilithium/turn" should call the same helper so what's shown matches what's
actually credited.

Reusing `TierRatio` (rather than `TierBuild`, or a new curve) is a deliberate choice, not a default
— ships get relatively *more* affordable over time under this (income grows 3.2× by SUPREME, ship
cost only 1.6×), which reads as an acceptable, arguably desirable side effect (bigger late-game
fleets) rather than something that needed protecting. Power Plants are the piece explicitly being
kept scarce, so income is pinned to their cost curve specifically.

Computed effective rates:

| Civ | Base Homeworld | EARLY | DEVELOPED | ADVANCED | SUPREME |
|---|---|---|---|---|---|
| FED | 6 | 6 | 11 | 13 | 19 |
| ROM | 5 | 5 | 9 | 11 | 16 |
| KLING | 7 | 7 | 13 | 15 | 22 |
| CARD | 8 | 8 | 14 | 17 | 26 |
| DOM | 8 | 8 | 14 | 17 | 26 |
| BORG | 12 | 12 | 22 | 25 | 38 |
| TERRAN | 6 | 6 | 11 | 13 | 19 |

| Civ | Base Colony | EARLY | DEVELOPED | ADVANCED | SUPREME |
|---|---|---|---|---|---|
| FED | 3 | 3 | 5 | 6 | 10 |
| ROM | 2 | 2 | 4 | 4 | 6 |
| KLING | 3 | 3 | 5 | 6 | 10 |
| CARD | 4 | 4 | 7 | 8 | 13 |
| DOM | 3 | 3 | 5 | 6 | 10 |
| BORG | 5 | 5 | 9 | 11 | 16 |
| TERRAN | 3 | 3 | 5 | 6 | 10 |

Sanity check against the design intent already written into `HomeworldMiningRate`'s comments
(`StarSysManager.cs:1195` area — "Borg: compensates for extreme ship costs"): Borg keeps the
highest rate at every tier under this scaling (38 vs. FED's 19 at SUPREME), so the existing
per-civ relative ordering — the thing those comments were actually protecting — is preserved
exactly; only the absolute numbers grow with tech.

## 3. `InitializeDilithiumStockpile` — one call-site fix, no redesign

`GetPowerPlantDilithiumCost` needs a `TechLevel` parameter now (Phase 2 doc §3). The one call site
in `InitializeDilithiumStockpile` (`StarSysManager.cs:878`) already has `tech` in scope one line
above it — just thread it through: `ShipStatCalculator.GetPowerPlantDilithiumCost(civ, tech) *
sysData.CurrentPowerPlantCount`. `shipLi2` and `buffer` are untouched (§1 showed they don't need
to move). Cardassia's stockpile automatically picks up its 5th plant once
`sysData.CurrentPowerPlantCount` reflects the Phase 2 doc's decided bump — no separate change
needed here.

## 4. `starSysSO.Dilithium` — naming note, one already-decided change

Worth flagging since it reads as a currency amount but isn't one: `starSysSO.Dilithium` is
consumed by `DetermineMaxPowerPlants` as a **Power Plant slot ceiling** for playable homeworlds
(`return starSysSO.Dilithium;`), not an amount of Dilithium anything actually holds — that's a
separate field (`DilithiumStockpile`). `CivBalanceCalculator` currently authors it as `pp + 1`
(one more than the computed `PowerStations` count) for every major. No civ's slot ceiling needs to
change as a result of this rebaseline beyond the already-decided Cardassia bump — `PowerStations`
4→5 means its authored `Dilithium` field goes from 5 to 6 to keep the `pp+1` convention, a one-line
`CivBalanceCalculator` change alongside the `PowerStations` bump itself. Not proposing a rename
here, just flagging the field's actual meaning so it isn't confused with the stockpile while
implementing.

## 5. Minor / non-playable civs

Two small, low-stakes numbers, flagged rather than deeply analyzed since minors rarely reach high
TechLevel tiers in practice and the stakes are lower than the majors' economy:

- `InitializeDilithiumStockpile`'s minor branch (`StarSysManager.cs:869`) uses a flat
  `MaxPowerPlants * 10` heuristic, predating any per-civ cost function — doesn't call
  `GetPowerPlantDilithiumCost` at all. Suggest bumping the flat `10` to roughly the new EARLY-tier
  baseline majors use (`20`, since minors fall back to the majors' `PowerOutput=20`/`TierRatio`
  baseline per `CivBalanceCalculator`'s `POWER_PER_PLANT_FALLBACK`), so a warp-capable minor's
  single starting plant is funded consistently with how a major's is. Purely a constant tweak.
- `GetEffectiveDilithiumMiningRate` (§2) applies uniformly regardless of `Playable` — a minor's
  `HasWarp` mining rate (`Mathf.Clamp(maxPowerPlants, 1, 3)`) and `ColonyMiningRate` fallback (`2`)
  both scale by the same `TierRatio` if that minor ever advances TechLevel, no special-casing
  needed.

## 6. Adjacent idea, not assumed in scope: Power Plant decommission-for-Dilithium

`ScrapShip` (`StarSysManager.cs:1299`) already returns a ship's locked Dilithium when a player
scraps it — intentional, distinct from combat destruction, which loses it permanently. Power Plants
now lock up a genuinely large sum (up to 128 for a Borg SUPREME reactor) and have no equivalent
decommission path today. Flagging this as a natural follow-on feature given the new stakes, not
assuming it's wanted — say if you'd like it folded into the implementation pass or left for later.

## 7. Concrete changes for implementation

- `ShipStatCalculator.GetPowerPlantDilithiumCost(CivEnum, TechLevel)` — new signature, per Phase 2
  doc §3's formula (`TierRatio` × `PowerOutputPerPlant(civ)`), replacing `PowerPlantLi2Cost`.
- `StarSysManager.InitializeDilithiumStockpile` — thread `tech` through the `ppLi2` call (§3).
- `StarSysManager.ProcessDilithiumMining` — read through a new `GetEffectiveDilithiumMiningRate`
  helper (§2) instead of `DilithiumMiningRate` directly; any UI display of per-turn Dilithium
  income does the same.
- `CivBalanceCalculator` — Cardassia `PowerStations` 4→5, `Dilithium` 5→6 (Phase 2 doc §5, carried
  through here for the slot-ceiling consequence, §4).
- Minor-civ starting stockpile heuristic (`StarSysManager.cs:869`) — `* 10` → `* 20` (§5).

## 8. Open questions

- **§6**: add Power Plant decommission-for-Dilithium now, or leave it for a later pass?
- Everything else in this doc reuses decisions already made in the Phase 2 doc (`TierRatio`, the
  Cardassia bump) rather than introducing new dials — flag if any of the computed rate tables in
  §2 look wrong for a specific civ before implementation.
