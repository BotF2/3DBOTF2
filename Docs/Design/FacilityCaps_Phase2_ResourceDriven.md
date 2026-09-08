# Facility Caps Phase 2: Resource-Driven Build Ceilings

Status: **approved, ready for implementation.** All open design questions are resolved (§8).
Supersedes the first draft of this doc: facilities do **not** hold Dilithium themselves — only
Power Plants do, and the build ceiling for the other five types is purely a Power headroom check.
Nothing here is implemented yet — this doc is the spec to build against.

One dependency before §3's actual numbers can ship: the Dilithium-rebaseline doc it calls for
(mining rates / starting stockpiles / `starSysSO.Dilithium` sizing, against the new Power Plant
costs below). §2, §4, §5, §6, and §7 don't depend on that rebaseline and can be implemented now;
§3's formula shape is final, but plugging it in before the rebaseline lands means starting
Dilithium will feel tight until that follow-up pass authors the new stockpile numbers.

---

## 1. What's changing and why

Today, `StarSysManager.GetFacilityCap()` (`StarSysManager.cs:994-1157`) gates all five queue-able
facility types — Factory, Shipyard, ResearchCenter, ShieldGenerator, OrbitalBattery — through one
shared mechanism: a flat fixed number per type for every playable civ's homeworld
(`MajorHomeworldFacilityCap`), a `QualityScore`-lerped range for minor homeworlds, a rolled range
for uninhabited systems, plus a shared `TechPoints`-staged bonus. The flat Major-homeworld number is
the piece that doesn't vary by civ nature — an arbitrary designer dial disconnected from the civ's
actual economy.

`PowerPlanet` already does the right thing and stays untouched: `DetermineMaxPowerPlants`
(`StarSysManager.cs:1391`) returns `starSysSO.Dilithium` for a playable homeworld — a per-civ
resource value, not a flat dictionary entry. This doc extends that shape to the other five types,
with one correction from the first draft: **facilities are not ships.** A ship carries its own
Dilithium-fueled drive core, so it makes sense for `ShipStatCalculator` to price one. A Factory or
Orbital Battery has no reactor of its own — it draws power over a wire from the system's Power
Plants. So the five non-PowerPlant types get **no Dilithium cost of their own** — Power is the only
thing that gates them, and Power itself is what Power Plants convert Dilithium into. This also
means none of the five ever needs a "scrap for Dilithium recovery" path (Orbital Battery included)
— there's nothing stored in them to recover.

## 2. The build-ceiling gate: Power only

Replace `MajorHomeworldFacilityCap` / `MinorHomeworldFacilityCapRange` /
`UninhabitedFacilityCapRange` / `facilityCapTechStages` / `InitializeFacilityCaps` /
`StarSysData.FacilityCapBase`/`FacilityCapTechBonus` with a single live check inside
`GetFacilityCap`, still called from the same three places it is today (`StarSysAIManager`'s
pickers, `StarSysBuildManager.QueueFacilityBuild`, `FactoryBuildItemDrag`'s UI-side check) —
nothing changes at those call sites, only `GetFacilityCap`'s internals:

```csharp
// Ceiling if this system maxed out its (Dilithium-capped) Power Plant slots — not just what's
// currently built, so queuing a Shipyard doesn't require building every Power Plant first.
float maxOutput = sysData.MaxPowerPlants * PowerOutputPerPlant(civEnum); // PowerPlantSO.PowerOutput

// Load already committed by every OTHER cap-gated facility, built or queued, powered on or off —
// mirrors today's "cap applies to total built, active or inactive" rule.
int reservedLoad = TotalLoadOfCapGatedFacilities(sysCon, excluding: type);

float headroom = Mathf.Max(0f, maxOutput - reservedLoad);
int   powerCap  = GetBuiltAndQueuedFacilityCount(sysCon, type)
                + Mathf.FloorToInt(headroom / PowerLoad(type, civEnum));
```

`PowerLoad(type, civEnum)` reads `OrbitalBatterySO.PowerLoad` for OB (per-civ, see §5) and the flat
`FactorySO/ShipyardSO/ResearchCenterSO/ShieldGeneratorSO.PowerLoad` for the other four
(civ-invariant — confirmed 8/8/2/4 across all 7 `CivInt` variants, so nothing new to author there;
starting counts keep reading straight from `starSysSO.Factories` etc. as before).

This makes the five types a genuine shared budget instead of five independent slot counts:
building one more Orbital Battery lowers every other type's `powerCap` on the next call, so the
player/AI is choosing a mix within one resource, not filling five unrelated buckets. The Science
tech effect that used to add flat bonus slots (`TechEffectHook.SightRangeStage_6_FacilityCap`,
`StarSysManager.cs:1152`) should be re-targeted at bonus `maxOutput` instead
(`xenoBonus × PowerOutputPerPlant(civEnum)`, "worth that many extra Power Plants") — still a
decision point, not resolved here.

## 3. Power Plant Dilithium cost: anchored to warship costs, not a hand-authored table

This is the one place Dilithium still applies, and per your ask it should be **far** higher than a
ship's drive core — a system reactor is a much bigger installation than anything that fits on a
hull. Today's cost is `ShipStatCalculator.PowerPlantLi2Cost`, a flat hand-authored per-civ number
(FED 25, ROM 22, KLING 28, CARD 15, DOM 38, BORG 55, TERRAN 25) with no TechLevel dependence at
all — a Power Plant costs the same whether it's the first one you build or the last.

**The tier→hull anchor, spelled out plainly** (this is the part you asked me to clarify): you asked
for "10× the best ship at a given TechLevel," so I looked up which hull is actually the best one
unlocked at each TechLevel rather than picking tiers myself. `ShipSOProvider.cs` gates hull classes
by TechLevel directly: Scout/Destroyer/Transport exist from EARLY; **Cruiser is new at DEVELOPED**;
**no new hull unlocks at ADVANCED** (same roster as DEVELOPED — Cruiser is still the best ship
there too); Cruiser then splits into LtCruiser/HvyCruiser at SUPREME, with **HvyCruiser new and the
strongest hull in the game**. So "the best ship at a given TechLevel" is:

| TechLevel | Best ship unlocked | Why |
|---|---|---|
| EARLY | Destroyer | Only Scout/Destroyer/Transport exist yet |
| DEVELOPED | Cruiser | Newly unlocked, strictly stronger than Destroyer |
| ADVANCED | Cruiser | No new hull unlocks here — Cruiser is still the best |
| SUPREME | Heavy Cruiser | Cruiser splits into LtCruiser/HvyCruiser; HvyCruiser is new and strongest |

That's exactly the Destroyer / Cruiser / Cruiser / Heavy Cruiser mapping from the first draft — it
wasn't a separate choice I made, it's just what "best ship per tech level" resolves to once you
look up the actual unlock table. Nothing to decide here; consider this question closed.

**On "a better way to balance across civs"** — you're right to push on this, and there is a real
issue with anchoring the per-civ multiplier to `QualBuild` (the same curve ship costs use).
`QualBuild` and `PowerOutputPerPlant` don't move at the same rate: CARD/BORG's `PowerOutput` ratio
is 10:40 (4×) but their `QualBuild` ratio is only 0.68:1.70 (2.5×). Piggybacking the plant's
*cost* on `QualBuild` while its *output* is set by the separate `PowerOutput` table means Dilithium
spent per unit of power isn't level across civs — Borg comes out **more** dilithium-efficient per
watt than everyone else (34 Li₂ for 40 power = 0.85/unit) while Cardassia comes out **less**
efficient (14 Li₂ for 10 power = 1.4/unit), on top of Cardassia already needing more total plants.
That's not an intentional design choice anywhere in the brief — it's just what falls out of reusing
`QualBuild` for something it wasn't built for. Two ways to fix it:

**Decided: anchor to the civ's own `PowerOutput`**, not to the ship `QualBuild` curve (that curve
was the first draft's approach — rejected for the efficiency skew described above). Dilithium-per-
unit-of-power is the same flat rate for every civ; only the plant's absolute cost scales, because a
bigger reactor costs proportionally more by producing proportionally more, full stop:

```csharp
// TierRatio is "10x the best ship's Dilithium cost at this tier, expressed per unit of the
// FED/ROM/KLING/TERRAN baseline PowerOutput (20)" - a flat Li2-per-power-point rate, not a
// per-civ multiplier. Every civ pays this same rate; only PowerOutputPerPlant(civ) varies.
private static readonly float[] TierRatio = { 1.0f, 1.8f, 2.1f, 3.2f }; // EARLY..SUPREME

public static int GetPowerPlantDilithiumCost(CivEnum civ, TechLevel tier) =>
    Mathf.RoundToInt(PowerOutputPerPlant(civ) * TierRatio[(int)tier]);
```

This drops the hand-authored `PowerPlantLi2Cost` dictionary entirely and replaces it with a
formula — no second table to hand-tune and keep in sync as ship balance shifts. No civ is
intrinsically "better" or "worse" at converting Dilithium into power — Cardassia and Borg differ
only in *how many* plants their layout needs and *how big* each one is (which was always the point
of `PowerOutput` varying per civ), not in a hidden efficiency tax borrowed from the ship curve. It
also decouples Power Plant economics from ship economics entirely: a `QualBuild` retune for ship
balance no longer silently retunes every civ's power infrastructure too.

Final numbers (`Mathf.RoundToInt` applied only to the final cost; FED/ROM/KLING/TERRAN share one
value per tier since they share `PowerOutput=20`):

| Tier | FED/ROM/KLING/TERRAN | CARD | DOM | BORG |
|---|---|---|---|---|
| EARLY | 20 | 10 | 30 | 40 |
| DEVELOPED | 36 | 18 | 54 | 72 |
| ADVANCED | 42 | 21 | 63 | 84 |
| SUPREME | 64 | 32 | 96 | 128 |

**This has a real knock-on consequence — confirmed, gets its own doc.** Every homeworld's authored
`starSysSO.Dilithium` (currently 2-5) and the derived starting `DilithiumStockpile`
(`sysData.MaxPowerPlants * 10`, `StarSysManager.cs:869`) were sized against the *old* ~15-55 flat
cost, not the new formula's numbers. You've confirmed the stockpile can go up to compensate, with
the explicit constraint that Dilithium has to stay a genuine limiting resource, not just get
inflated until this stops mattering — that's a real balance pass (mining rates, starting
stockpiles, ship costs relative to Power Plant costs, all considered together), not a number swap
on one table, so it's queued as its own follow-up doc rather than folded in here.

## 4. Facility headroom growing with TechLevel — decided

Confirmed: fix `TechManager.GetPowerEfficiencyMultiplier`'s wiring rather than author a new curve.
It already exists and already returns exactly the right curve (`1.00 / 0.90 / 0.80 / 0.70` —
"10/20/30% less power needed" per its own doc comment), but it's currently multiplied onto power
*output* in `StarSysData.CalculateTotalPower` instead of dividing down power *load* as its comment
promises — and `UpdateSystemPowerBalance` (the function that actually drives the UI and the on/off
gate) doesn't apply it at all. Wiring it correctly — as a multiplier on `reservedLoad`/`PowerLoad
(type)` in §2's formula, not on `maxOutput` — means every facility effectively gets cheaper to run
as the civ's tech advances, which is exactly "more facilities fit in the same power budget" without
a new curve to author or a `MaxPowerPlants` change to make. No open item left here.

## 5. Starting power state: OB and Shields off, everything else on

Per your ask: `AddSystemFacilities` sets `ShieldGenerator` and `OrbitalBattery` to their off state
at creation, `Factory`, `Shipyard`, and `ResearchCenter` to on — reusing the existing on/off toggle
mechanism (`FactoryButtonOnClicked`/`OffClicked` family already used by `StarSysAIManager`'s power
priority passes) rather than inventing a new state.

**Checked against every major home system's current committed data** (real per-facility loads —
Factory 8, Shipyard 8, Research 2 — against `Load_on = F×8 + SY×8 + RC×2` vs. today's committed
`PowerStations × PowerOutputPerPlant`, OB/Shield excluded since they start off):

| Civ | F/SY/RC | Load (on-set only) | Output | Margin |
|---|---|---|---|---|
| FED | 2/2/2 | 36 | 40 | +4 |
| ROM | 2/1/3 | 30 | 40 | +10 |
| KLING | 3/2/1 | 42 | 60 | +18 |
| **CARD** | **4/2/1** | **50** | 40 → **50** | **−10 → 0** |
| DOM | 1/1/3 | 22 | 60 | +38 |
| BORG | 1/1/4 | 24 | 40 | +16 |
| TERRAN | 2/2/2 | 36 | 40 | +4 |

Good news: starting OB/Shields off resolves the deficit found in the last pass for **six of seven**
majors outright, using today's numbers — this was the OB load (48 at full authored count) that was
actually driving the earlier "Klingon" finding, and it's now deferred to whenever the player
chooses to power OB/Shields on rather than being live from turn 1.

**Cardassia — decided:** `PowerStations` goes from 4 to 5, which exactly closes the gap (Load 50 =
Output 50, margin 0). One-line change in `CivBalanceCalculator`/the authored `StarSysSO` — folds
into the same pass that re-baselines `starSysSO.Dilithium` against §3's new Power Plant costs
(needs to cover 5 plants' worth of authored capacity instead of 4 either way).

## 6. OB and Shield combat/power differentiation

Checked what's already in place before proposing anything new:

- **Orbital Battery is already civ-differentiated on both axes you asked for**, with no new work
  needed. Combat power: `OrbitalBattery` is a real `ShipType` in `ShipStatCalculator.Base`, run
  through the same per-civ `Flavor` multipliers as every warship — Borg's Flavor
  (`1.40/1.28/1.60/1.60`) already makes Borg OB tankier and harder-hitting than Cardassian's
  (`0.95/1.02/1.05/1.25`). Power load: `OrbitalBatterySO.PowerLoad` is already per-civ (CARD 2,
  lowest; BORG 8, highest). "Less/more combat-powerful with less/more power load" is exactly
  today's data — this section is a confirmation, not a change.
- **Shield Generator has no per-civ combat effect to remove** — it isn't a `ShipType`, doesn't
  fight, and a repo-wide search turned up no code path that varies any shield effect by civ. Its
  `PowerLoad` is already uniform (4, all 7 `CivInt` variants). So "same across civs" is already
  true on both axes; the only actionable note is if/when a Shield Generator combat effect does get
  built (e.g. a system-wide regen bonus during a siege), author it civ-invariant by design rather
  than following OB's per-`CivInt` SO pattern.

## 7. Combat-triggered automatic power reallocation

New behavior, distinct from `StarSysAIManager`'s existing `DefencePowerPriority` (that's a
gradual, one-facility-per-turn AI economy setting a human can never see fire instantly — this needs
to happen the moment a fleet attacks). Hook it at the same point `StarSysManager
.EnsureOrbitalBatteryShipsForCombat` already fires (combat about to start for this system):

```csharp
public void ReallocatePowerForCombat(StarSysController sysCon)
{
    var data = sysCon.StarSysData;

    // 1. Everything else off first — frees every watt for defense.
    ForceOffAll(data, StarSysFacilityType.Shipyard);
    ForceOffAll(data, StarSysFacilityType.Factory);
    ForceOffAll(data, StarSysFacilityType.ResearchCenter);
    // Shield/OB start from off too (§5) or whatever the player had set — always recomputed here.
    ForceOffAll(data, StarSysFacilityType.ShieldGenerator);
    ForceOffAll(data, StarSysFacilityType.OrbitalBattery);

    // 2. Max theoretical output — not just currently-active plants — is what's available.
    float budget = data.MaxPowerPlants * PowerOutputPerPlant(data.CurrentOwnerCivEnum);

    // 3. Defense fills first. If budget falls short, some OB/Shields simply don't power on —
    //    that's the intended "shortfall means partial defense" outcome from the ask.
    budget -= PowerOnAsManyAsFit(data, StarSysFacilityType.ShieldGenerator, budget);
    budget -= PowerOnAsManyAsFit(data, StarSysFacilityType.OrbitalBattery,  budget);

    // 4. Only if defense left something over: Shipyard, then Factory, then Research, in that order.
    budget -= PowerOnAsManyAsFit(data, StarSysFacilityType.Shipyard,       budget);
    budget -= PowerOnAsManyAsFit(data, StarSysFacilityType.Factory,        budget);
    PowerOnAsManyAsFit(data, StarSysFacilityType.ResearchCenter, budget);
}
```

`PowerOnAsManyAsFit` is a small new helper (turn on built-and-currently-off instances of `type`
one at a time while `PowerLoad(type) <= remaining budget`, return total load consumed) — the same
shape as the existing `TryPowerOnOneFacility`, just looped to convergence in one call instead of
one-per-turn, since combat needs the final state immediately rather than ramping over several
turns. Order within step 3 (Shield before OB, matching `DefencePowerPriority`'s existing order) and
within step 4 (Shipyard, Factory, Research, matching `WarPowerPriority`'s existing order) both
reuse priority orderings that already exist in `StarSysAIManager` rather than inventing new ones.

## 8. Open questions — none remaining

All five items across both rounds are resolved:

- §4: fix `GetPowerEfficiencyMultiplier`'s wiring rather than author a new curve.
- §3's Dilithium rebaseline: its own doc, queued as follow-up work (§3's dependency note above).
- §3's tier→hull anchor: wasn't actually a choice — Destroyer/Cruiser/Cruiser/Heavy Cruiser *is*
  "best ship per TechLevel" once you read the real unlock table, nothing to pick.
- §5's Cardassia fix: `PowerStations` 4→5.
- §3's per-civ Dilithium-cost formula: anchor to the civ's own `PowerOutput` (flat Li₂-per-power-
  point rate for everyone), not the ship `QualBuild` curve.

This doc is ready for implementation, with the one sequencing note from the top: §2/§4/§5/§6/§7
have no dependency and can be built now; §3's formula is final but its actual numbers will feel
tight in play until the Dilithium-rebaseline doc authors matching stockpile/mining-rate values.
