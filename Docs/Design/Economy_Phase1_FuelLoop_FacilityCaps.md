# Economy Phase 1: Antimatter Fuel Loop + Facility Caps

Status: draft for review. Numeric values below are strawman defaults meant to be tuned, not
final balance — everything with a number next to it should be treated as "designer can change
this in one place," not as a hard requirement.

This is Phase 1 of the larger star-system economy rework discussed in-thread. Later phases
(explicitly **out of scope here**, but designed around so they slot in cleanly):

- Combat bombardment choices: destroy facilities outright vs. suppress/disable defenses while
  preserving structures (Factories/Power Plants) for capture, vs. a targeted strike on the
  Antimatter Stockpile itself to render a system uninhabitable.
- Intel-driven sabotage of a specific facility (Power Plant and/or Factory) outside of combat.
- Ground invasion resolution (troops on transports vs. a system's Ground Forces) once its power
  is down. **This does not exist in the codebase today** — no facility currently has HP or a
  destroy-in-combat path except Orbital Battery (already modeled as a combat `ShipController`).

Everything in this doc is written so those phases become "wire into the field that already
exists" rather than a redesign.

---

## 1. Antimatter Fuel Loop

### 1.1 Concept

Dilithium remains the **capital cost** — a one-time resource locked into a Power Plant's reactor
matrix or a ship's drive core at construction time, recoverable by scrapping, lost on
destruction. That mechanic is unchanged.

Antimatter is the new **operating fuel** — produced continuously by Factories, consumed
continuously by Power Plants to sustain output. It is a per-system stockpile, same shape as
`StarSysData.DilithiumStockpile`.

The failure trigger the user specified is **destruction-based, not shortage-based**: losing all
of a system's Factories and/or all of its Power Plants is what cascades into blackout. The
stockpile's job is to be the *buffer* that turns "instant unrecoverable collapse" into "a grace
period the player can act inside of" — reinforcements, repairs, evacuation — rather than a second
way to accidentally starve yourself through mismanagement.

### 1.2 Data model additions (`StarSysData.cs`)

```csharp
[Header("Antimatter Fuel Loop")]
public int AntimatterStockpile;
public int AntimatterProductionRate; // banked per turn from active Factories
public int AntimatterConsumptionRate; // drawn per turn by active Power Plants

public bool HasAntimatter(int amount) => AntimatterStockpile >= amount;
public void DeductAntimatter(int amount) => AntimatterStockpile = Mathf.Max(0, AntimatterStockpile - amount);
```

Mirrors `HasDilithium`/`DeductDilithium` deliberately — same shape means the future "hit the
stockpile directly" bombardment option (Phase 2+) is just another call site for
`DeductAntimatter`, no new plumbing.

### 1.3 Production (Factories → Stockpile)

Each **active** (powered-on) Factory contributes a flat per-turn amount to
`AntimatterProductionRate`, tech-multiplied the same way Factory build speed already is:

```
AntimatterProductionRate = ActiveFactoryCount * BaseAntimatterPerFactory * TechManager.GetFactorySpeedMultiplier(techLevel)
```

Suggested `BaseAntimatterPerFactory = 2`. Computed once per stardate (same tick cadence as
`PopulationManager`/`TechManager`), added to `AntimatterStockpile`.

### 1.4 Consumption (Stockpile → Power Plants)

Each **active** Power Plant draws a flat per-turn amount:

```
AntimatterConsumptionRate = ActivePowerPlantCount * BaseAntimatterPerPlant
```

Suggested `BaseAntimatterPerPlant = 3` (deliberately higher than production-per-Factory, so a
1-Factory colony with 1 Power Plant runs a mild deficit and needs 2 Factories to break even —
this is what makes "how many Factories do I actually need" a real decision instead of "build one
and forget it").

If `AntimatterStockpile` can't cover `AntimatterConsumptionRate` this turn: **brownout, not
blackout**. Power output throttles proportionally to the shortfall (e.g. reserve covers 60% of
draw → system runs at 60% `TotalSysPowerOutput` this turn), which feeds into the existing
power-priority shutdown logic (§1.6) to decide what gets dropped first. Full collapse is still
reserved for the destruction trigger in §1.5 — running a temporary deficit degrades the system,
it doesn't zero it out.

### 1.5 The blackout trigger

```
if (ActivePowerPlantCount == 0 || ActiveFactoryCount == 0 && AntimatterStockpile <= 0)
    → all power off: Shipyard, ResearchCenter, ShieldGenerator, OrbitalBattery, (future: Ground Forces)
    → system flagged open to invasion (future phase)
```

Two distinct paths in:
- **Power Plants destroyed** → immediate, since there's nothing left to distribute stored fuel
  through regardless of reserve size.
- **Factories destroyed** → not immediate. The existing `AntimatterStockpile` keeps Power Plants
  running until it's drawn down to zero at `AntimatterConsumptionRate`/turn. This is the grace
  period — `AntimatterStockpile / AntimatterConsumptionRate` turns to react (repair, resupply,
  evacuate) before the system actually goes dark. Surface this as a turn-count ETA in the system
  UI once it's under threat, not just a raw number.

### 1.6 Bootstrapping (no chicken-and-egg) — "the Colony Kit"

New colonies need a starter reserve so the loop isn't dead on arrival. Resolved: the seed amount
is a **small flat constant, uniform across every civ and system** — not scaled by `QualityScore`
or system role the way the facility caps are. Every colonization transport carries the same
starter provisioning regardless of who's sending it.

This is also a naming fix. The transport cargo that seeds a new colony has been called
"Dilithium" (`transportDilithium` in `ColonizeWithTransport`/`ColonizeTimerCoroutine`), but a
colony ship is obviously carrying a lot more than a fuel crystal — colonists, equipment,
rations, everything else this sim doesn't model as discrete data. Going forward, refer to that
payload as the **Colony Kit**: Dilithium (player-loaded, variable, via the existing "Load
Dilithium" transport cargo UI) + Antimatter (new, small, fixed) + implicitly everything else
we don't track. Concretely:

- `AntimatterColonyKitSeed` — a new constant (suggested `10`, tunable in one place), granted
  automatically whenever `ColonizeTimerCoroutine` completes, independent of how much Dilithium
  the player chose to load. No new cargo-loading UI needed — this isn't player-adjustable cargo,
  it's assumed baseline provisioning bundled into every colony transport.
- `ColonizeTimerCoroutine` seeds both `starSysData.DilithiumStockpile = seedDilithium` (existing,
  player-controlled) and `starSysData.AntimatterStockpile = AntimatterColonyKitSeed` (new, fixed)
  in the same place — comment the pair as "the colony kit" so the intent reads together instead
  of as two unrelated seed values.
- Homeworlds remain separately authored with their own starting `AntimatterStockpile` on
  `StarSysSO` (same as `Dilithium` is today) — they aren't colonized via transport, so the Colony
  Kit constant doesn't apply to them; author a larger, per-civ value there instead.

### 1.7 AI integration

`StarSysAIManager`'s `EconomyPowerPriority` / `WarPowerPriority` / `DefencePowerPriority` arrays
already decide what gets powered off first when power is tight — extend the power-on/off check
(`TryPowerOnOneFacility` / `TryPowerOffOneFacility`) to also weigh `AntimatterStockpile` trend,
not just `TotalSysPowerOutput - TotalSysPowerLoad`. No new AI subsystem: this is one more number
those functions already read.

This is also where the user's War/Defence/Economy framing lands directly:
- **War** wants more active Shipyards → needs more sustained power → needs more Antimatter →
  AI should bias toward keeping Factory count above the break-even point, not just building
  Shipyards outright.
- **Defence** wants Shields/Batteries/Ground Forces powered → same reasoning.
- **Economy** wants balanced growth → this is naturally enforced once Antimatter deficit throttles
  output, since over-building consumers without matching Factories starts brownout-ing everything.

---

## 2. Facility Caps

Two limits, doing different jobs, both already partially present in the code:

| | Governs | Existing mechanism |
|---|---|---|
| **Power** | How many *built* facilities can be *active* right now | `TotalSysPowerOutput` vs `TotalSysPowerLoad`, per-facility on/off toggle |
| **Build ceiling** (new) | How many of each type can *exist at all* in this system, active or not | Currently a single flat `MaxFacilitiesPerType = 4` in `StarSysAIManager.cs` — same number for every system in the game |

The build ceiling is what's changing. Per the user's requirements, it needs to account for:
1. The owning civ's overall tech progress (a civ-wide unlock, not per-system).
2. The system's role — Major-civ homeworld gets a **fixed number per facility type**; every
   other system gets a **range**.
3. For non-Major systems, where in that range a given system lands is driven by the owning civ's
   in-canon power tier — reusing `CivSO.QualityScore` (already a 0–10 "weak/quantity ↔
   strong/quality" designer dial, already used throughout `ShipStatCalculator` for exactly this
   kind of civ-strength scaling) rather than inventing a parallel stat.

### 2.1 Formula sketch

```
EffectiveCap(facilityType, system) =
    IsMajorHomeworld(system)
        ? MajorHomeworldCap[facilityType] + TechBonus(civ.TechPoints)
        : Clamp(
              Lerp(RoleRange[role][facilityType].Min,
                   RoleRange[role][facilityType].Max,
                   owningCiv.QualityScore / 10f)
              + TechBonus(civ.TechPoints),
              RoleRange[role][facilityType].Min,
              MajorHomeworldCap[facilityType] - 1   // non-Majors never reach Major parity
          )

role ∈ { MinorHomeworld, Colony }   // IsHomeworld && !civ.Playable, vs. everything else
```

Resolved: `TechBonus` is keyed off raw `CivData.TechPoints`, not the 4 coarse `TechLevel` bands —
and per the brief, the trigger points should be **more numerous, and more closely spaced, the
higher TechPoints climbs** (each individual bump matters less by then, but they arrive more
often). This is the same shape `TechManager.fogSightRangeStages` already uses for fog-of-war
sight range — a `(TechPointsRequired, Value)[]` stage table, stepped rather than smooth. Mirror
that pattern exactly rather than inventing a second one:

```csharp
// Facility-cap tech bonus: more numerous, more closely-spaced stages as TechPoints grows —
// same "stepped breakthroughs" shape as TechManager.fogSightRangeStages, but its own
// independent set of trigger points. Deliberately NOT aligned with CivData.TechThresholds
// (0/100/300/600, i.e. where TechLevel itself advances) - those are already the "big moment"
// jumps for research/factory/power multipliers, and stacking a facility-cap bump on the exact
// same stardate would just double up that one moment instead of reading as its own separate
// progression. Runs up to the ~1000-point effective cap CivData.TechRating clamps to (see
// TechRating comment), so late-game engineering gains keep feeling earned instead of
// flatlining the moment SUPREME is reached.
private static readonly (int TechPointsRequired, int Bonus)[] facilityCapTechStages =
{
    (0,    0),
    (150,  1),
    (350,  2),
    (550,  3),
    (700,  4),
    (800,  5),
    (870,  6),
    (920,  7),
    (955,  8),
    (980,  9),
    (1000, 10),
};
```

Gaps run 150/200/200/150/100/70/50/35/25/20 — a light ramp-up then a steady tightening toward
the cap, none of them landing on 100/300/600. Looked up via the same linear scan
`GetFogSightRangeMultiplier` uses (walk the table, keep the last stage whose threshold is met).

### 2.2 Suggested starting values

| Facility | Major Homeworld (fixed) | Minor Homeworld (range, quality-lerped) | Uninhabited/Colony (range, **rolled per system**) |
|---|---|---|---|
| Power Plant | 2 *(matches existing `BasePowerPerPlant` comment: "two power plants for major home systems")* | 1–2 | 1 *(matches existing `MaxPowerPlants = 1` default)* |
| Factory | 6 | 2–4 | 2–4 |
| Shipyard | 4 | 1–3 | 1–3 |
| Research Center | 4 | 1–3 | 1–3 |
| Shield Generator | 3 | 1–2 | 1–2 |
| Orbital Battery | 4 | 1–3 | 1–3 |

Reads as: a Ferengi or Breen system (high `QualityScore`) lands near the top of its range; a
Pakled or Kazon system (low `QualityScore`) lands near the bottom — both still well under what
FED/ROM/KLING/CARD/DOM get on their own homeworld, matching the canon power gap you described.

**Revision:** the Colony/Uninhabited row is no longer a `QualityScore` lerp. Every system that
starts the game uninhabited is generated from one of the interchangeable `ZZUNINHABITEDx`
placeholder CivSOs, which all share the same default `QualityScore` (5) — lerping against that
would hand every uninhabited system in the galaxy an identical cap, defeating the point of a
range. Instead `StarSysManager.InitializeFacilityCaps` rolls each uninhabited system
independently within `UninhabitedFacilityCapRange` (`UnityEngine.Random.Range`, once, at galaxy
generation) so uninhabited systems come out genuinely varied instead of all being interchangeable
— still fixed forever at whatever it rolled, same permanence guarantee as every other role.

### 2.3 Where this is enforced

- `StarSysAIManager.PickNextEconomyFacility` / `PickNextDefenceFacility`: replace the flat
  `count >= MaxFacilitiesPerType` check with `count >= EffectiveCap(type, system)`.
- `StarSysBuildManager.QueueFacilityBuild`: same check needs to apply to the **player's**
  drag-and-drop build path too (currently only the 5-slot queue-length is checked there) — this
  is the actual enforcement point for humans, not just AI.
- Cap applies to **total built, active or inactive** — a powered-off facility still counts
  against the ceiling. This matches what's already implicitly true today (`MaxFacilitiesPerType`
  checks list count regardless of on/off state); the change is only that the number itself now
  varies by system instead of being a global constant.
- **The cap only ever gates new construction, never removes what already exists** — see §2.4 on
  conquest below. It's a ceiling on *building more*, not a live constraint the game enforces
  against a system's current facility list.

### 2.4 Conquest: captured facilities transfer intact

Resolved: when a system changes hands and its Factories/Power Plants/etc. were **not** destroyed
in the fighting, they are captured and reused by the new owner as-is — no rebuild, no forced
teardown, no freeze. This applies even if the captured count now exceeds what the new owner's
role/tech/`QualityScore` would normally allow them to build fresh (e.g. a minor conquering a
Major's homeworld inherits facility counts way above its own range).

Mechanically this is simple because §2.3's cap is a *build-time* gate only:
- On ownership change (`GameEvents.SystemOwnershipChanged`, already fired by
  `StarSysController.ClaimSystem`/`ColonizeWithTransport` and to be fired by the future combat
  conquest path), the captured facility lists (`StarSysData.Factories`, `.PowerPlants`, etc.)
  simply carry over untouched.
- Whether each captured facility is powered on or off from that point forward is ordinary
  post-conquest AI power management (`TryPowerOnOneFacility`/`TryPowerOffOneFacility`, §1.7) —
  the new owner's power budget (and, once §1 lands, Antimatter reserve) decides what it can
  actually run, exactly as it would for any other system. No special-cased "over cap" logic
  needed anywhere.
- The only place `EffectiveCap` is consulted is `QueueFacilityBuild` — an over-cap system simply
  can't queue *more* of a facility type it's already over on until attrition (combat losses,
  decommission) brings it back under. Nothing forces that to happen on any particular timeline.

### 2.5 Data model additions

`EffectiveCap` should be a **pure function, computed on demand** at the two call sites in §2.3 —
not cached on `StarSysData`. With the tech bonus now keyed off raw `TechPoints` across an 11-stage
table (§2.1) rather than 4 coarse `TechLevel` bands, a cached value would need re-validating on
every one of those stage crossings for every system the civ owns; a table lookup + one `Lerp` is
cheap enough to just recompute at the two places that actually check it, so there's nothing to
keep in sync and nothing that can go stale after a conquest hands a system to a new owner
mid-game.

```csharp
// StarSysManager or a small static helper — no new StarSysData field required.
public int EffectiveCap(StarSysFacilityType type, StarSysController sysCon) { ... }
```

Reads `sysCon.StarSysData.IsHomeworld`, `sysCon.StarSysData.CurrentOwnerCivEnum` →
`CivData.QualityScore`/`Playable`/`TechPoints` — all already available where §2.3's call sites
live. Follows the same `civSO.Playable && starSysSO.IsHomeworld` branch already used for ship
`tierCeiling` (`StarSysManager.cs:1247`), so it's reusing an existing classification, not adding
a new one.

---

## 3. Status

All three open questions from the previous draft are resolved (tech-bonus trigger shape → §2.1's
independent stage table; conquest behavior → §2.4; Colony Kit seed → §1.6). Nothing left blocking
implementation on the design side — ready to move to actual code changes when you want to start,
or to a further review pass first if you'd rather read through the whole doc end-to-end before
committing to it.
