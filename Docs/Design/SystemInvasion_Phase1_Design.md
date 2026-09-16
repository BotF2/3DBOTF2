# System Invasion Phase 1: Orbital Defense, Planetary Shields, and Conquest

Status: design draft; Invasion.3 (§11) is implemented code-side, pending Editor asset authoring.
Invasion.1/.2's earlier code shipped under the old (superseded) structure and needs partial rework —
see §9. Phase B (Invasion.4/.5/.6) is still design-only. Companion to the existing space-combat system
(`CLAUDE.md`'s Combat
System section, `Assets/TURN_BASED_COMBAT_GUIDE.md`) and to `FacilityCaps_Phase2_ResourceDriven.md`
(Shield Generator / Orbital Battery Power costs, which this doc doesn't touch). This is the next major
system after Tech Tree Phase II (`TechTree_Phase2_Design.md` §8 — only the II.5 balance pass remains
there, and that's playtesting-gated, not a coding task).

**Revision note (2026-09-11) — canon-accuracy pass, replaces the three-phase structure entirely.**
The previous draft treated Shields as a Phase A (space combat) participant protecting Orbital Batteries.
Directed design pass settled instead on a **ground vs. orbital split**, matching the Star Trek games
this project draws from: Shipyards and Orbital Batteries are in orbit and fight in space combat;
Shields are planetary, protecting everything actually on the ground (Power Plants, Factories, Research
Centers, the Shield Generators themselves, Population, Ground Forces). This collapses the old
three-phase structure (A: space combat, B: bombardment, C: invasion) into **two phases** — see §4.
Most of the previous draft's settled decisions (Total-Destruction-reverts-to-uninhabited, 20s timers)
carry forward; anything below superseding them says so explicitly.

## 1. What already exists (reuse, don't rebuild)

- `CombatType` (`GameEnums.cs:487`) already has four members: `FleetVsFleet`, `FleetVsSystem`,
  `SystemVsFleet`, and **`StarSystemInvasion`** — fully reserved (declared, never referenced). Still
  open whether Phase B's abstract resolution needs it as a real `CombatData.CombatType` — see §9.
- `CombatManager.RequestCombat` (`CombatManager.cs:298,309`) and `SceneController.cs:134-157` already
  route a fleet-vs-system encounter into `FleetVsSystem`/`SystemVsFleet` correctly depending on which
  side is the mover. Phase A keeps using this path unchanged.
- **Orbital batteries already fight as real combat participants.** `ShipManager.
  CreateOrbitalBatteryForSystem` spawns one as `ShipType.OrbitalBattery`, a normal `ShipController`
  with its own stat row (`ShipStatCalculator.cs:38`, `BaseStats(95, 90, 38, 26, 0f, 15, 5)` — high
  Shield/Hull, no Warp). It joins `CombatData.SideOneShipCons`/`SideTwoShipCons` and fights through the
  same `TurnBasedCombatResolver` turn loop as any ship. Its own Shield/Hull stat row is now the *only*
  protection it has (no more Planetary Shield screening it — see §4.1's line-of-sight mechanic instead).
- Transports that survive a `FleetVsSystem`/`SystemVsFleet` combat already drop their
  `ShipData.LoadedGroundForces` onto the system: `CombatController.ApplyTransportCargoConsequences`
  → `StarSysManager.AddGroundForceUnit` (`StarSysManager.cs:1862`).
- `StarSysData` already tracks the defender's own ground strength as real per-system state:
  `GroundForces` (`List<GameObject>`, count-based), `Population`/`MaxPopulation`/
  `PopulationGrowthAccumulator`/`MaxGroundForceUnits`, converted by the existing `PopulationManager`
  (`Assets/Script/Core/PopulationManager.cs`) at the shared `GroundForceData.PopulationPerUnit = 8`
  rate. Real, live data — nothing to invent here. All ground-side per §2, so all now sit *behind*
  Phase B's Shields rather than being exposed in Phase A.
- `TurnEventQueue` (`Assets/Script/Galaxy/TurnEventQueue.cs`) already does the "present a decision at
  the start of InterTurn, gray out Advance Turn until answered" pattern — the same mechanism
  `TurnEventType.DiplomacyEncounter` uses for the Fight/Withdraw panel. Still the right tool for the
  single Phase-A→B gate decision (§4), even though Phase B itself no longer needs recurring turn-gating
  (§4.2 — this is the big simplification from the old design).
- `DiplomacyController.Combat()` / `CombatController.RequestStartCombat` already own the "should this
  even become combat" decision (Fight vs. Withdraw) that starts Phase A. Also the natural place to gate
  a third party from opening fleet-vs-*system* combat against a system with a live Assault (§4.3).
- **Fleet contact/encounter machinery already exists and is reused, not rebuilt, for §4.3's
  interrupt cases.** `FleetController.OnTriggerEnter` (`FleetController.cs:1030-1087`) already
  distinguishes friendly-fleet contact from enemy-fleet contact and already routes hostile contact
  through `GalaxyEncounterQueue.EnqueueFleetVsFleet` → the same Diplomacy Fight/Withdraw panel. A
  third-party fleet "deciding to enter combat" with the besieging fleet is exactly this existing path -
  no new combat trigger needed, only new gating on top of it (§4.3).

## 2. Ground vs. orbital facility split (net-new framing)

Matches the reference games' canon: Shipyards orbit a planet like a space station; Orbital Batteries
are space-based defense platforms; everything else a system builds is on the planet's surface, under
whatever Shield coverage the planet has.

| Facility | Location | Exposed in Phase A? | Exposed in Phase B? |
|---|---|---|---|
| Shipyard | Orbital | Yes — the asset Phase A protects | No (already resolved by Phase A's outcome) |
| Orbital Battery | Orbital | Yes — the wall defending the Shipyard | No |
| Shield Generator | Ground (planetary) | No | Yes — what Phase B fires on first |
| Power Plant | Ground | No | No (never directly targeted; only a power source) |
| Factory | Ground | No | No |
| Research Center | Ground | No | No |
| Population / Ground Forces | Ground | No | Only via Phase B's Total Destruction / Target Troops branch |

This is why Shields move entirely out of Phase A: canonically a planetary shield has nothing to do with
defending a ship in orbit. Shipyards and Orbital Batteries get their own combat-unit stats (Shield+Hull
for OB, Hull-only for the new Shipyard unit — §3) instead.

## 3. Phase A — Fleet vs. System (space combat, `CombatScene`, mostly reshaped from existing code)

Attacker fleet vs. the system's own regular ships (if any) + Orbital Batteries + the new Shipyard unit,
through the existing `TurnBasedCombatResolver` turn loop — same scene, same UI, no new scene needed.
Planetary Shields, Power Plants, Factories, Research Centers, Population, and Ground Forces never
appear here.

### 3.1 The Shipyard becomes a combat unit

New `ShipType.Shipyard` (mirrors `ShipType.OrbitalBattery`'s "stationary system-defense platform"
model): `Warp = 0f`, never moves, spawned one-per-built-Shipyard-facility the same way
`EnsureOrbitalBatteryShipsForCombat` already does for OB (`EnsureShipyardShipsForCombat`, mirrored).
**Hull only, no Shield stat** — per your description, the Shipyard is a station, not a warship; it has
no shield of its own and relies entirely on the OB wall (below) for protection. Destroyed the same way
OB already is (`ShipController.DestroyShip` → new `RemoveShipyardFacility()`, mirrors
`RemoveOrbitalBatteryFacility`, decrementing `StarSysData.Shipyards` so a destroyed Shipyard is
permanently gone, not just marked dead).

**New per-civ-per-era `ShipSO`-like asset**, one per playable civ × TechLevel tier, named the same way
ship SOs already are (`Assets/SO/ShipSO/TERRAN_CRUISER_II.asset` is the existing pattern —
`{CIVSHORTNAME}_{TYPE}_{ROMAN_NUMERAL}`, all-caps with underscores). So concretely:
`TERRAN_SHIPYARD_I` / `TERRAN_SHIPYARD_II` / etc. for every playable civ's `CivShortName`. (Your example
used `Fed_Shipyard_I` mixed-case — flagging the mismatch since every existing ship SO in
`Assets/SO/ShipSO/` is upper-case; recommend matching the existing convention exactly rather than
introducing a second casing style, but say so if you want the mixed-case form instead.) Selected by
civ + TechLevel the same way `ShipStatCalculator`/`ShipDataInitializer` already resolve a ship's stat
row from those two inputs — no new resolution mechanism, just a new SO list to populate.

### 3.2 Orbital Batteries: their own Shield+Hull, wall formation, line-of-sight protection of the Shipyard

OB keeps its existing Shield+Hull stat row (`ShipStatCalculator.cs:38`) — that row now *is* its combat
protection, full stop, with no Planetary Shield screening layered on top (that mechanic is deleted, not
extended — see §9's rollback note).

**Formation**: OB defenders form a wall between the Shipyard and the attacking fleet, reusing the
existing Formation-order wall positioning (`CombatOrderStateMachine.cs:328-360`,
`CalculateFormationWallPosition` — the YZ-plane wall mechanic ships already use for the Formation
order). The system's own regular ships (§3.3) can also hold this wall and are expected to shift into any
gap the OB wall doesn't cover — "especially if there are few or no OB," per your description.

**Line-of-sight protection, not a targeting-priority flag.** This replaces the old "screen OB while any
Shield unit lives" rule (deleted along with Shields' Phase A presence). The Shipyard is not a valid
attack target while at least one OB is still alive and positioned in the wall, between the attacker and
the Shipyard, on that line of fire. This is a genuinely new mechanic with no existing precedent in
`CombatTargetingSystem` — the old rule was a pure alive/dead flag check
(`ScreenOrbitalBatteriesBehindShields`); this one needs an actual geometric check against ship
positions. **Open implementation question (§9):** true line-of-sight (a raycast/line-segment check from
attacker to Shipyard, blocked by any living OB collider in between) vs. a cheaper proxy (e.g. "any OB
still alive on this side counts as blocking, full stop — same flag-check shape as the old rule, just
renamed"). The proxy is far less work and may be indistinguishable in practice since the wall already
concentrates OB between the fleets; recommend starting with the proxy and only building real geometric
LOS if playtesting shows attackers exploiting positioning to snipe the Shipyard around a wall that
still has live OB in it.

### 3.3 The system's own defending ships

Any regular combat ships the system itself owns (not OB/Shipyard/Shields) take ordinary Combat Orders —
the same order set as fleet vs. fleet (`CombatOrders`: Engage, Rush, Retreat, Formation,
AttackTransports, Capture, Scuttle) — **defaulting to Formation** rather than requiring the player (or
AI) to pick one. `AttackTransports` behaves exactly as it already does in fleet-vs-fleet combat: those
ships try to flank around the attacker's combat ships to reach and destroy the attacker's Transports
before they can drop ground forces — no new logic needed there, this is existing
`CombatOrderStateMachine`/`CombatTargetingSystem` behavior applied unchanged to the defending side.

### 3.4 Power: priority cascade, not an exclusive lock

**Correction from an earlier draft's wording:** power isn't reserved exclusively for OB (or, in Phase B,
Shields) — it's a *priority cascade* down `ReallocatePowerForCombat`'s existing sequential
`budget -= PowerOnUpTo(...)` structure (`StarSysManager.cs:1720-1743`), just reordered per phase:

- **Phase A entry:** Orbital Battery first, then Shipyard, then (only if the Shipyard's already been
  destroyed) Factory, then Research Center. Shield Generator is left out of this reallocation
  entirely — it isn't a Phase A participant, so it doesn't compete for combat power at all here.
- **Phase B entry (§4):** Shield Generator first (OB and Shipyard are already gone by construction —
  Phase A only ends once both are destroyed, so there's nothing left to prioritize ahead of Shields),
  then Factory, then Research Center.

This is a straightforward reordering of existing `PowerOnUpTo` calls per phase, not new mechanism — the
cascade-if-still-alive behavior (an empty/destroyed facility list just contributes 0 spend and falls
through to the next tier) already falls out of `PowerOnUpTo` iterating an empty list and returning 0.

### 3.5 Phase A end state

Phase A is *won* (eligible to proceed to Phase B) only when the system's own regular ships, every OB,
and the Shipyard are **all** destroyed — Shipyard is now a required kill, not just OB/ships. Two
sub-cases, unchanged from the original draft:
- **Attacker clears Phase A:** `CombatController.EndCombat()` runs normally, control returns to the
  Galaxy scene, and the system is flagged as awaiting the Phase B gate decision (§4).
- **Attacker's fleet is destroyed or withdraws first:** normal combat loss/end, nothing further happens
  — unchanged from today's `FleetVsSystem`/`SystemVsFleet` behavior.

## 4. Phase B — Assault System (galaxy-layer, abstract resolution, no `CombatScene`)

Single phase, not two — this replaces the old design's separate Bombardment (B) and Invasion (C)
phases. Your own framing settled it: once the attacker is already inside the system's orbital defenses,
firing on the planet's Shields and then choosing Total Destruction/Target Troops/eventual invasion are
all *consequences of the same commitment*, not separate decisions to re-litigate turn after turn.

### 4.1 Entry gate

One decision, one panel, one 20-second timer — presented via `TurnEventQueue` the same way the old
design's siege decision was (mirrors `TurnEventType.DiplomacyEncounter`'s presentation). **Settled
(2026-09-11): the Total Destruction/Target Troops choice is folded into this same window**, not a
separate decision once Shields fall (§4.2/§9's original phrasing suggested a second decision point;
your answer collapsed it). Concretely, three effective outcomes:
- **Withdraw** — the default if the timer expires unanswered.
- **Assault System — Target Troops** — the default sub-choice if the player picks Assault System.
- **Assault System — Total Destruction** — requires actively selecting it over the Target Troops default.

Withdraw-by-default (rather than defaulting into an invasion) mirrors the same reasoning as the old
Total-Destruction/Target-Troops timer defaulting to the less-destructive option — doing nothing should
never commit an AFK/distracted player to an invasion, and if they do commit, it shouldn't default to
the most destructive option either.

### 4.2 Abstract resolution — no turn freeze, no CombatScene

**Settled (2026-09-11):** Phase B is fought as an abstract attrition resolution inside the Galaxy scene
— report entries / a Shield-strength readout ticking down over turns via the attacking fleet's
aggregate weapon stats vs. the system's Shield Generator Shield+Hull pool — not a second `CombatScene`
visit. (Real 3D combat against the Shields is a possible future enhancement, explicitly deferred.) Two
direct consequences:
- **No Advance-Turn freeze.** The old design's siege-freezes-the-fleet mechanic doesn't apply here —
  the attacking fleet isn't taking ship losses during Phase B (Shields don't shoot back), so there's no
  need to pause anyone's turn advance while it resolves. This drops almost all of the old §4 siege
  state machine's complexity (no more `SystemsUnderSiege` per-turn re-queue loop needed just to keep a
  decision alive across turns — see §9's rollback note on what Invasion.2 built that this supersedes).
- **Power reallocation** on Phase B entry now prioritizes Shield Generator per §3.4's cascade.

**Total Destruction vs. Target Troops is chosen up front at the §4.1 entry gate**, not as a second
decision once Shields fall (settled 2026-09-11 — see §4.1). Once Shields are destroyed (the attrition
pool hits zero), what happens next depends on which sub-choice was made at entry:

- **Total Destruction:** reverts the system to uninhabited via `GetFirstOwner()`, destroys every
  facility, zeroes population/ground forces (unchanged from the prior draft's §3.2 mechanics), then
  auto-claims the now-empty system for the attacker (§4.3). The attacker's transported troops land
  into this claimed, empty system — no defending troops left to fight, so this is a simple landing,
  not a fight.
- **Target Troops:** grinds `GroundForces` down with incidental facility damage (magnitude TBD in a
  balance pass), same abstract-attrition mechanism as the Shield stage. Once `GroundForces.Count`
  drops to ≤ the attacker's transported troop count, a **second attrition sub-stage** begins
  automatically (still inside this same Phase B, still no `CombatScene`): the attacker's Transports
  land their troops **with the besieging fleet's remaining ships providing support**, and it becomes a
  troops-vs-troops fight — both sides' counts can fall, not an instant numeric-advantage flip. Ship
  support gives a significant advantage but extends how long the fight takes to resolve — the
  tradeoff is speed vs. safety, not a free win once the numeric threshold is crossed. Ownership flips
  to the attacking civ once defending `GroundForces` hits zero.

**Revision (2026-09-14) — real-time bounded resolution replaces the per-InterTurn drip.** This
section's original framing ("no turn freeze... fought... over turns") is superseded: turn
advancement turned out to be 100% player-button-driven with no real-time auto-advance at all, so an
Assault could sit "in progress" across an arbitrarily long, unbounded stretch of real time depending
on how many turns the player took to click Advance Turn again — never converging on anything close
to Phase A combat's pacing. Settled instead: once an order is chosen at the §4.1 gate, Phase B now
resolves via a bounded real-time coroutine (`StarSysManager.BeginPhaseBRealtimeResolution`,
`Time.unscaledDeltaTime` via `WaitForSecondsRealtime`, same convention `CLAUDE.md` documents for
Phase A combat) that re-runs the existing, unmodified `ResolvePhaseBAtritionTick` on a fixed
real-time interval until a terminal outcome (repel/capture/mutual elimination) fires — instead of
once per InterTurn. Advance Turn is grayed out for the duration
(`StarSysManager.IsResolvingPhaseB`, read by `GameControlOverlay.SetControlsInteractable`), the same
way combat already freezes it. The existing gray-out progress sprites (`PhaseBProgressUI`) double as
the "in progress" feedback while this runs — no separate spinner widget was added. §4.1's 20-second
entry-gate countdown (previously unbuilt despite being settled 2026-09-11) shipped alongside this,
defaulting to Withdraw on expiry or on an early Close click, and Withdraw now visibly separates the
besieging fleet from the system on the galaxy map (`FleetController.ServerMoveAwayFromSystem`)
instead of leaving their colliders overlapping.

### 4.3 Interruptions — another fleet arrives at a system mid-Assault

Net-new requirement, no prior-draft precedent. Three cases, keyed on the arriving fleet's civ relative
to the besieging fleet and the system's (pre-Assault) owning civ:

1. **Same civ as the besieging fleet:** merges into the Assault — its ships' weapon stats join the
   aggregate the Phase B attrition math uses (§4.2). No combat, no decision needed.
2. **The system's own (defending) civ sends a relief fleet:** this triggers ordinary `FleetVsFleet`
   combat between the relief fleet and the besieging fleet — reuses existing contact/encounter
   machinery (§1's last bullet), no new combat type. **Settled (2026-09-11):** yes, this pauses Phase
   B's attrition tick while the relief fight resolves. Further: Phase B's accumulated
   damage/destruction so far is *retained* (Shield/troop attrition progress isn't lost), but if the
   besieging fleet survives the relief fight, Phase B doesn't silently resume — it **restarts at the
   §4.1 entry gate** (a fresh Withdraw vs. Assault System decision). Rationale: the fleet just took
   losses, so its aggregate attack strength changed — worth re-confirming the player still wants to
   continue, on top of keeping the progress already made.
3. **A third-party civ's fleet arrives:** if that civ chooses to fight the besieging fleet (existing
   Diplomacy Fight/Withdraw contact flow, §1), that's ordinary `FleetVsFleet` combat, same as case 2.
   But a third party **cannot** initiate a fleet-vs-*system* attack against this system — i.e. cannot
   open their own competing Assault — until the current Assault resolves (new owner installed, or the
   original owner retains it). This needs a new gate on `DiplomacyController.Combat()`/
   `CombatController.RequestStartCombat`'s system-encounter path: refuse to start a second
   `FleetVsSystem`/`SystemVsFleet` against a system that already has a Phase B Assault in progress.

**Total-Destruction aftermath and uninhabited systems:** when Total Destruction leaves a system
uninhabited (§4.2), it doesn't sit fully open for anyone to grab — the attacking civ gets first claim
automatically, the same "insignia planted, no facilities" state `StarSysController.ClaimSystem` already
produces for a manually-claimed uninhabited system. Concretely: `ResolveTotalDestruction` calls the
equivalent of `ClaimSystem(attackingCiv)` right after reverting ownership to `GetFirstOwner()`, rather
than leaving `CurrentOwnerCivEnum` at the bare uninhabited sentinel for whoever happens to arrive first.

## 5. Bugs to fix as part of this work (not new scope, just adjacent)

- `StarSysManager.AddGroundForceUnit`'s `CivEnum = sysData.CurrentOwnerCivEnum` mistagging
  (`StarSysManager.cs:1870`) — needs to take the actual owning/attacking civ as a parameter instead of
  inferring it from the system, since post-invasion the two are no longer guaranteed the same.

## 6. Balance considerations

Shipyard/OB/Shield/ground-force numbers should land through the same discipline
`FacilityCaps_Phase2_ResourceDriven.md` and `TechTree_Phase2_Design.md` §3 established: derive from
existing per-civ multiplier tables (`ShipStatCalculator`'s `CivFlavor` rows) rather than hand-authoring
a parallel balance table per civ. The Phase B attrition formula (§4.2) is explicitly a placeholder
pending this same discipline — don't hand-tune it in isolation.

## 7. Data & code architecture (sketch — refine as §9's open questions settle)

- `ShipType.Shipyard` (`GameEnums.cs`), one `ShipSO`-style asset per playable civ × TechLevel
  (`{CIVSHORTNAME}_SHIPYARD_{ROMAN}`, §3.1), a `ShipStatCalculator` base-stats row (Hull only, no
  Shield, `Warp=0`), `ShipManager.CreateShipyardUnitForSystem` (mirrors
  `CreateOrbitalBatteryForSystem`), `StarSysManager.EnsureShipyardShipsForCombat` (mirrors
  `EnsureOrbitalBatteryShipsForCombat`), `StarSysController.RemoveShipyardFacility` (mirrors
  `RemoveOrbitalBatteryFacility`) wired into `ShipController.DestroyShip`/`SelfDestruct`.
- `CombatTargetingSystem`: replace `ScreenOrbitalBatteriesBehindShields` with a Shipyard-behind-OB-wall
  check (§3.2) — starts as the same shape (any-alive-OB flag check) with a `// TODO` toward real LOS.
  Delete the Shield-related exclusions this same file currently carries (§9's rollback list).
  `TurnBasedCombatResolver`/`CombatOrderStateMachine`: apply the existing Formation wall math to OB
  the same way it already applies to ships (§3.2) — confirm whether OB units need their own
  `ShipPhaseTracker`/`CombatOrderStateMachine` component the way regular ships do, or whether a
  simpler static wall placement (they never move regardless of order) is enough.
- `StarSysManager.ReallocatePowerForCombat`: split into (or parameterize by) Phase A vs. Phase B
  priority order per §3.4 — needs to know which phase is being entered.
  `StarSysManager.GetCombatShipsForSystem`: extend the OB/Shield powered-on filtering (currently
  Shield+OB) to OB/Shipyard for Phase A; Phase B no longer calls into `CombatScene` at all so this
  method's Shield-specific case (§9's rollback) simply goes unused there.
- Phase B abstract resolution: new per-system attrition state (Shield pool remaining, ticked down per
  turn or per some other cadence — TBD) plus `StarSysController.ResolveTotalDestruction()` /
  `ResolveTargetTroopsBombardment()` / `ResolveSystemInvasion(FleetController attacker)`, all carried
  forward from the prior draft's sketch, now triggered by Phase B's attrition hitting zero rather than
  by winning Phase A directly.
- §4.3's interrupt handling: a same-system-same-civ-fleet-arrival merge hook, a gate on
  `DiplomacyController.Combat()`/`CombatController.RequestStartCombat` refusing a second system attack
  while a Phase B Assault is active, and `ResolveTotalDestruction`'s new auto-claim-for-attacker step.
- Report/log entries via the existing `ReportEntryUI.PushReport`/`GameLogger` pattern
  `ApplyTransportCargoConsequences` already uses.

## 8. Suggested phasing

1. **Invasion.1 — Shield combat integration (implemented, now partially superseded — see §9's
   rollback list).** What stays: `ShipStatCalculator`/`ShipDataInitializer` groundwork, the general
   "one combat unit per built facility" pattern (`EnsureXForCombat`, `RemoveXFacility`). What goes:
   `ShipType.PlanetaryShield`'s presence in Phase A combat and the OB-screening targeting rule — see §9.
2. **Invasion.2 — Siege state machine (implemented, now mostly superseded — see §9's rollback list).**
   What stays: the Break Off Siege UI pattern is still a useful precedent for *some* player-facing
   escape hatch if Phase B ever needs one (unclear it does now that it doesn't freeze the fleet — §9).
   What goes: `SystemsUnderSiege`/`EnqueueActiveSiegeEvents`'s per-turn re-queue loop, since Phase B no
   longer needs to re-present a decision every InterTurn.
3. **Invasion.3 — Shipyard as a combat unit (implemented, code-side, 2026-09-11).** See §11 for exactly
   what shipped and what's still Editor-only work.
4. **Invasion.4 — Phase B: Assault System gate + abstract Shield/troop attrition + Total
   Destruction/Target Troops.** The single merged phase from §4: entry gate panel (three effective
   outcomes per §4.1), the two-sub-stage attrition resolution (Shields, then troops-vs-troops under
   Target Troops per §4.2), the outcome methods, Phase B power-priority reorder.
5. **Invasion.5 — Interrupt handling (§4.3).** Same-civ merge, defender-civ relief combat + Phase B
   restart-at-entry-gate, third-party gating, Total-Destruction auto-claim.
6. **Invasion.6 — UI polish.** Visual distinction between an active Assault and a normal system view on
   the galaxy map, Shield-strength readout, report/log entries.

## 11. Invasion.3 implementation notes (2026-09-11, shipped)

What actually landed, for reference against §3/§7's sketch:

- `ShipType.Shipyard` (`GameEnums.cs`) — Hull-only `BaseStats(0, 130, 0, 0, 0f, 20, 6)`
  (`ShipStatCalculator.cs`), first-pass numbers pending §6's balance pass. `PlanetaryShield` kept as an
  enum value (unused as a spawned combat unit) per §9's rollback note — its fate is still Phase B's call.
- **Per-civ-per-era SO resolution, not a shared placeholder.** Unlike OB/Shield's single `ShipSO` field
  on `ShipManager`, `ShipManager.CreateShipyardUnitForSystem` resolves the Shipyard SO via
  `shipSOProvider.GetShipSOAtBestTechLevel(ShipType.Shipyard, civTechLevel, civEnum)` — the same lookup
  `BuildShipInSystem` already uses for real player-built ships — so it reads from each civ's existing
  `ShipSO` list (`FedShipSOList`/`TerranShipSOList`/etc.) rather than a new dedicated field. **Still
  needs doing in the Unity Editor:** author one `ShipSO` asset per playable civ × TechLevel tier named
  `{CIVSHORTNAME}_SHIPYARD_{ROMAN}` (e.g. `TERRAN_SHIPYARD_I`), `ShipType = Shipyard`, assign a 3D
  model (reusing an existing station/starbase model as a placeholder is fine to start), and add each to
  its civ's `ShipSOList` in `ShipManager`'s Inspector — nothing spawns until at least one exists per
  civ/era combination that reaches Phase A.
- `StarSysManager.EnsureShipyardShipsForCombat` (mirrors `EnsureOrbitalBatteryShipsForCombat`),
  `StarSysController.RemoveShipyardFacility` (mirrors `RemoveShieldGeneratorFacility`) wired into
  `ShipController.DestroyShip`/`SelfDestruct`.
- `CombatTargetingSystem.ScreenShipyardBehindOrbitalBatteries` replaces
  `ScreenOrbitalBatteriesBehindShields` — the any-alive-OB proxy per §3.2/§10's settled answer, not
  real geometric line-of-sight. Shipyard also added to the unarmed-attacker exclusion lists (previously
  PlanetaryShield's spot) and to `ShipController.ShipFireLoop`'s early-out guard.
- `StarSysManager.ReallocatePowerForCombat` reordered for Phase A: Orbital Battery → Shipyard → Factory
  → Research, with Shield Generator forced off and dropped from the cascade entirely (§3.4).
  `GetCombatShipsForSystem` now includes Shipyard unconditionally (not power-gated — it's the physical
  objective, not a discretionary defense toggle, per your description) and excludes any stray
  `PlanetaryShield` outright rather than power-gating it.
- `TurnBasedCombatResolver.PickAIOrder` now forces `CombatOrders.Formation` for whichever side is the
  star system in a `FleetVsSystem`/`SystemVsFleet` combat, instead of rolling a random AI order — the
  §3.3 default. (Only covers the AI-auto-submit path; a human player actively controlling the
  defending side's order-selection UI isn't specially defaulted to Formation yet — flagged as a
  follow-up, not currently in scope since a system's defenders are rarely a human sitting at the order
  panel.)
- **OB wall + Shipyard-behind-wall spawn positioning** (`ShipSetupManager.cs`) — the concrete resolution
  of §3.2's/§7's open "does OB need real Formation-order movement, or just a fixed spawn?" question:
  discovered that `CombatOrderStateMachine.ExecuteFormation`'s movement speed is
  `ShipData.maxWarpFactor * 0.65f`, which is always 0 for a `Warp=0` unit — so a Warp=0 ship assigned
  Formation would compute a wall target position but never actually move there via that code path.
  Given that, OB/Shipyard's wall placement is set directly at spawn instead: `SetupOrbitalBatteryWall`
  arranges every OB into an evenly-spaced grid (same `col%5`/`row/5` shape as
  `CalculateFormationWallPosition`) at the system's existing combat-line X (±200); `SetupShipyards`
  places the Shipyard the same way but at ±300 (between the ±200 combat line and the ±400 transport
  line) so OB is genuinely positioned between the enemy and the Shipyard along the depth axis, not just
  sharing its X. The system's other (mobile) defending ships are unaffected — they still reach their
  own Formation wall position at runtime via the existing movement code, since they have nonzero warp.

**Follow-up (2026-09-11): mid-combat Shipyard production joins the fight.** If a Shipyard completes a
queued ship (`StarSysBuildManager.BuildShipCoroutine` → `ShipManager.BuildShipInSystem`) while Phase A
combat is already active at that exact system (checked against `CombatData.StarSysCon`, not just civ,
so it can't misfire onto some other combat the civ happens to be fighting elsewhere), the new ship
launches straight into the fight instead of sitting idle until the next combat:
`CombatController.AddReinforcementShip` spawns it near a living Shipyard on its side (falling back to
the normal combat-line X if none is found) via `ShipSetupManager.SetupReinforcementShip`, adds it to
that side's `CombatData.SideXShipCons` so the very next `AssignTargetsToAllShips`/
`StartAllShipWeaponFire` pass picks it up like any other ship, and sets it to **Engage** so it moves
toward the enemy and fires as targets are located, per your description. Two implementation notes:
- **Discovered during this work:** a freshly-built ship's `ShipData.CurrentStarSysController` is set to
  the system by `BuildShipInSystem`, which is exactly the flag `CombatOrderStateMachine.isSystemOwned`
  checks to permanently freeze OB/Shipyard in place. `SetupReinforcementShip` clears it before setup so
  this ship is treated as a normal mobile combatant instead of inheriting that freeze.
- **Engage only guarantees the ship's first turn.** `CombatController.SetShipOrders` unconditionally
  overwrites every ship's individual order (this one included) to the side's collective order on the
  very next full order-resolution turn — same as it already does for every other ship. A reinforcement
  arriving mid-turn gets one turn of guaranteed Engage behavior, then falls in with whatever the
  defending side is collectively doing from that point on (often Formation, per §3.3's default).
- **Reachability caveat, not yet resolved:** ship-build completion is gated on
  `TimeManager.CurrentStarDate()` advancing, and combat runs on `Time.unscaledDeltaTime` while (per
  `CLAUDE.md`) game time is paused during combat resolution — meaning a build likely can't actually
  complete *during* Phase A combat today regardless of this wiring. Built anyway as correct,
  future-proof behavior for whenever that stops being true (a longer Phase A, a change to how time
  pauses during combat, etc.) rather than blocking on resolving that separately.

## 9. Rollback list — what Invasion.1/.2's shipped code needs undone or repointed

Both of these shipped under the old three-phase design and need explicit rework, not just extension:

- `GameEnums.cs`: `ShipType.PlanetaryShield` no longer participates in Phase A combat at all — either
  repurpose it as the Phase B abstract-resolution's internal representation of "how much Shield is
  left" (if that attrition math wants a real stat row to read from) or retire it in favor of a plain
  numeric pool. Decide once §4.2's attrition formula is designed.
- `CombatTargetingSystem.ScreenOrbitalBatteriesBehindShields` and its unarmed-attacker/`ShipFireLoop`
  guards for `PlanetaryShield` (§8 old Invasion.1 note) — delete; replaced by §3.2's
  Shipyard-behind-OB-wall check.
- `StarSysManager.EnsureShieldUnitsForCombat` and its call sites in `SceneController.LoadCombatScene`
  (`SceneController.cs:139,160`) — remove; Shields are never a Phase A combat participant.
- `StarSysManager.GetCombatShipsForSystem`'s Shield-powered-on filtering — remove the Shield case (OB
  stays); Shield's powered-on filtering instead matters for Phase B's abstract attrition math, not for
  building a `CombatScene` roster.
- `StarSysController.RemoveShieldGeneratorFacility` — still needed (a destroyed-in-Phase-B Shield
  Generator facility should still be removed), just no longer called from `ShipController.DestroyShip`
  since Shields never spawn as `ShipController`s in the new design; called instead from Phase B's
  attrition resolution directly against the facility list.
- `StarSysData.BesiegingFleet`/`BesiegingCivEnum`/`DefensesCleared`, `StarSysManager.
  SystemsUnderSiege`/`StartSiege`/`EndSiege`, `FleetController.syncedIsBesiegingSystem`/
  `IsBesiegingSystem`/`ServerSetBesiegingSystem`, `FleetData.BesiegedSystem`, the Break Off Siege
  button, and `TurnEventQueue`'s `SiegeDecision` re-queue loop (`EnqueueActiveSiegeEvents`) — all of
  this existed specifically to freeze a fleet and re-present a decision indefinitely across turns.
  §4.2 says Phase B doesn't freeze the fleet or need recurring re-presentation, so most of this
  machinery has no job left. Recommend keeping the *data shape* (a system knows which fleet/civ is
  assaulting it — still needed for §4.3's merge/gating checks) but dropping the freeze and the
  per-turn re-queue behavior built around it.
- The already-authored `ShieldGeneratorShipSO` asset (`Assets/SO/ShipSO/ShieldGeneratorShipSO.asset`)
  becomes unused as a combat-unit SO under this design — Shields no longer spawn as ships. Repurpose or
  delete once §4.2's attrition representation is settled.

## 10. Open questions before more coding starts

- ~~§3.2: real geometric line-of-sight vs. the cheaper any-OB-alive proxy?~~ Resolved (2026-09-11):
  proxy, as implemented in Invasion.3 (§11).
- ~~§3.1: Shipyard SO naming casing~~ Resolved (2026-09-11): matches the existing all-caps
  `TERRAN_CRUISER_II` convention exactly (`TERRAN_SHIPYARD_I`), not the mixed-case example.
- ~~§4.2: does Total Destruction/Target Troops get its own timer, or share the entry-gate window?~~
  Resolved (2026-09-11): **same window, same 20s timer.** The entry-gate panel is Withdraw (default)
  vs. Assault System, and choosing Assault System is itself a choice of *which* Assault - Target
  Troops (default) or Total Destruction - decided up front, not as a second decision once Shields
  fall. Simplifies §4.2/§7 considerably: no second decision point needed at all, one panel with
  effectively three outcomes (Withdraw / Assault-TargetTroops / Assault-TotalDestruction).
- §4.3: does a defending civ's relief fleet fight pause Phase B's attrition tick while it resolves?
  **Resolved, refined (2026-09-11):** yes, and further - Phase B's accumulated damage/destruction so
  far is *kept* (Shield attrition progress isn't lost), but once the relief fight resolves, if the
  besieging fleet still exists, Phase B **restarts at the entry-gate window** (a fresh Withdraw vs.
  Assault System - Target Troops/Total Destruction decision) rather than silently resuming. Rationale:
  the fleet composition changed (took losses), so the aggregate attack strength changed - worth
  re-asking whether to continue, on top of the retained progress.
- §3.3/§7: troop cost on a successful System Invasion. **Resolved (2026-09-11), and the eligibility
  mechanic is now real combat, not a numeric-advantage instant flip:** under the Target Troops branch,
  once `GroundForces.Count` drops to ≤ the attacker's transported troop count (via Phase B's ongoing
  attrition), the attacker's Transports land **with the besieging fleet's ship support**, and troops
  vs. troops actually fight it out (both sides' counts can fall) rather than resolving instantly.
  Ship support gives a significant combat advantage but extends how long the fight takes to flip
  ownership - a real tradeoff, not a free numeric shortcut. Under Total Destruction, there's no
  defending troops left to fight - the attacker's transported troops simply land into the
  now-uninhabited, auto-claimed system (§4.3) to seed whatever comes next (e.g. eventual
  recolonization). This lands entirely inside Phase B's abstract resolution (§4.2) - still no
  `CombatScene` visit, just a second attrition sub-stage (troops vs. troops) after the first
  (ships vs. Shields) - see §7 for the architecture implication.
- §1: is `CombatType.StarSystemInvasion` needed as a real `CombatData.CombatType` for anything now that
  Phase B is confirmed to never re-enter `CombatScene`, or does it stay permanently unused?
- §6: can an AI-controlled (non-human) civ conduct an Assault against a human or another AI's system?
  Needs its own decision logic (Assault vs. Withdraw, Total Destruction vs. Target Troops vs.
  invade-when-eligible), analogous to `DiplomacyController.DoAIDiplomacy`.
- §7: does the troops-vs-troops fight (above) need its own small attrition formula distinct from the
  ships-vs-Shields one, or can the same abstract-attrition mechanism be parameterized for both stages
  (different pools, different per-tick damage sources)? Recommend the latter - one mechanism, two
  configurations - unless the ship-support bonus turns out to need bespoke shape.

## 12. Ground Force manual training + power upkeep (2026-09, shipped, adjacent to this doc's scope)

Net-new civ-management feature discovered while designing Phase B's eventual troops-vs-troops ground
battle (§11's follow-up note) - not itself a siege mechanic, but the thing Phase B's ground combat will
read from, so recorded here rather than a separate doc.

**Manual training (shipped):** `TroopButtonAdd`/`TroopButtonSubtract` on the System UI let a player
train one ground force unit from civilian Population on demand (`StarSysManager.
TrainGroundForceUnit`, costing `GroundForceData.PopulationPerUnit`), shown desaturated
(`GroundForceIconUI.SetTraining`) until completed at the next Advance Turn
(`StarSysManager.ProcessGroundForceTrainingForAllCivs`, called from `TimeManager.ProcessTurnEvents`
alongside `PopulationManager.ProcessPopulationGrowthForAllCivs`). `TroopButtonSubtract`
(`StarSysManager.CancelGroundForceTraining`) cancels the most recent in-training unit if one exists,
otherwise disbands the most recently fielded real unit - both refund population in full.

**Important finding:** `PopulationManager.GrowSystem`'s automatic conversion has no per-turn throttle -
it jumps straight to `totalPopulation / PopulationPerUnit` (capped at `MaxGroundForceUnits`) every
turn. Since manual training only *moves* population between the civilian and military buckets without
changing their sum, it can never get ahead of what auto-growth would hand the player the next turn
anyway - its only real effect is timing (this turn vs. next), not a higher ceiling. This matters for
the decay item below.

**Power upkeep (shipped):** Ground Forces now draw power like any other facility, but as a single
always-on line item rather than a per-unit toggle (`GroundForceData.CurrentPowerLoad`,
`PeacetimePowerLoadPerUnit`/`CombatPowerLoadPerUnit` constants - no per-civ SO exists for ground
forces to author these from, so they're flat constants pending the balance pass below):
- **Peacetime:** deducted unconditionally in the system's normal (non-combat) power balance
  (`StarSysMenuUIController.UpdateSystemPowerBalance`) just for having troops on the roster, shown via
  the new `groundForceLoadText` (`LoadText (TMP)` in `SystemUI_Prefab`).
- **Combat ("full power"):** `StarSysManager.ReallocatePowerForCombat` deducts the peacetime portion
  off the top (before the whole Phase A cascade runs), then attempts to upgrade to the higher
  `CombatPowerLoadPerUnit` rate (`GroundForceData.OnCombatFooting`) right after Orbital Battery - troops
  go on alert the moment invasion begins, second priority behind what's actually shooting. All-or-
  nothing (no per-troop partial toggle); if the upgrade doesn't fit, troops stay at peacetime draw
  rather than losing power. `CombatController.EndCombat()` resets `OnCombatFooting` back to false once
  Phase A ends.

**Decay-to-population-target — backlog, phase 2.5 balance pass, not yet implemented.** Your own
proposed mechanic: if `GroundForces.Count` sits above the population-supported target
(`totalPopulation / PopulationPerUnit`) for a few consecutive turns, shrink it by 1/turn back toward
that target. Given the "important finding" above, **this can only ever actually trigger after
something else shrinks total population** (combat losses, a future bombardment/Total-Destruction-style
event, `MaxPopulation` dropping) — manual training alone can't create an overshoot state for it to pull
back from, since auto-growth already caps at the same ceiling instantly. Still worth building for that
case (troops naturally attrit down to what a wounded population can still support, rather than
instantly vanishing or sitting at an unsustainable number forever) — just not, on its own, an answer to
"why not always max out troops on a healthy population." The power-upkeep mechanic above is what
answers that question for now; decay is a complementary consequence for the population-loss case.
Needs: a per-system "turns above target" counter (`StarSysData`), a check + decrement step
(`PopulationManager.GrowSystem` is the natural home, right where the target is already computed),
real magnitudes (how many consecutive turns before decay starts, how fast it decays) - same
first-pass-numbers-now/real-numbers-later discipline as everything else pending a balance pass.

## 13. Shipyard combat-unit follow-ups (2026-09, shipped)

Settled while authoring the actual per-civ Shipyard `ShipSO` assets:

- **Minor-race fallback confirmed working.** One shared `ACAMARIAN_SHIPYARD_I` asset covers every
  minor race's Shipyard via `ShipSOProvider.GetAnyMinorShipSOAtBestTechLevel` (§11) - no per-minor-race
  asset needed, `civOverride` still stamps the actual owning minor civ onto the spawned unit.
- **Placeholder model, all 8 assets:** every playable civ's `{CIV}_SHIPYARD_I` plus
  `ACAMARIAN_SHIPYARD_I` now reference `FED_TRANSPORT_II`'s FBX (distinct from the Orbital Battery
  model, per your request that Shipyard not look identical to OB) until real per-civ Shipyard models
  are authored and imported. `shipSprite` fields are untouched - those already hold each civ's real
  Shipyard icon, used for UI display independent of the 3D placeholder.
- **No Dilithium/currency cost for the combat unit** - confirmed not applicable, since a Shipyard
  combat unit is never queued through the normal ship-build economy (one spawns automatically per
  built Shipyard *facility* - that facility's own build cost/power is `ShipyardSO`'s job, a completely
  separate, pre-existing class in `Assets/SO/StarSysShipyardSO/`, not to be confused with the
  `ShipSO`-type combat-unit assets in `Assets/SO/ShipSO/`). `ShipStatCalculator`'s Shipyard `BaseStats`
  row now zeroes `BuildDuration`/`DilithiumCost` accordingly instead of carrying stray placeholder
  numbers that were never actually load-bearing.
- **Shipyard count is capped by TechLevel/homeworld/major-vs-minor, not power/currency** -
  `StarSysManager.DetermineMaxShipyards`, layered on top of (not replacing) `GetFacilityCap`'s existing
  power-budget cap via `Mathf.Min` - whichever is more restrictive wins:

  | | EARLY | DEVELOPED | ADVANCED | SUPREME |
  |---|---|---|---|---|
  | Playable homeworld | 2 | 3 | 4 | 4 |
  | Playable non-homeworld (colonized/joined) | 1 | 2 | 3 | 3 |
  | Minor race (any) | 1 | 1 | 1 | 1 |

  First-pass numbers, same pending-balance-pass caveat as everything else in this doc.
- **Hull is computed live from a same-civ Destroyer's Hull at the civ's CURRENT TechLevel, not a new
  SO field.** You proposed a "hull strength" field on the Shipyard SO; the better fit turned out to be
  no new field at all. A static per-SO field can't represent "current TechLevel" - TechLevel changes
  over the course of a game, and every Shipyard SO authored so far is EARLY-tier regardless of what
  tech the owning civ has actually reached (`GetShipSOAtBestTechLevel` falls back to EARLY until
  higher-tier Shipyard SOs exist - §11). A field would freeze Hull at whatever the EARLY SO says
  forever. Instead, `ShipDataInitializer.InitializeShipData` special-cases `ShipType.Shipyard`: right
  after computing the SO's own base stats, it re-runs `ShipStatCalculator.Calculate` for
  `ShipType.Destroyer` at the civ's live `CurrentTechLevel` and substitutes that Hull value before the
  existing `HullMultiplier`/`StationHPMultiplier` tech-effect chain applies on top - so a Shipyard's
  toughness rises automatically as the owning civ researches, using the exact same Destroyer-hull
  numbers already balanced elsewhere, with nothing new to hand-author. Same "derive from existing
  tables" discipline as §6.

## 14. Target Power — third §4.1 assault choice, plus panel progress sprites (2026-09-13, shipped code-side)

**New `AssaultMode.TargetPower`**, sitting between Target Troops and Total Destruction in both the enum
and the SiegeDecisionUIController button row. Reuses the `PhaseBPowerPlantHP`/`PhaseBPowerPlantMaxHP`
pool that `InitializePhaseB` already computed but no mode previously drained (`ComputePowerPlantHP`
derives it from `ShipType.PlanetaryShield.HullMaxHealth`, same placeholder source `ComputeSGContribution`
uses for shields — see §9's still-open PlanetaryShield-repurposing question).

- **Sequencing, settled:** once shields fall, Target Power fires *exclusively* on power plants
  (`ResolvePhaseBAtritionTick`'s new power sub-stage) — real facilities removed one at a time via the
  already-existing `StarSysController.RemovePowerPlantFacility()` as the pool depletes, same shape as
  the shield phase's SG removal. Only once the pool (and the facility list) hits zero
  (`PhaseBPowerPlantsDown = true`) does the fleet turn to troops, falling into the *same* ground-phase
  code Target Troops uses (transport landing, troops-vs-troops) — not a separate implementation.
- **Firepower penalty, settled:** defending ground troops' attack power is scaled by
  `StarSysManager.GetDefenderFirepowerMultiplier` — `Lerp(TargetPowerFirepowerFloor, 1f, currentPP/maxPP)`,
  live during the power sub-stage (troops weaken as power drains, not just at the end) and pinned at the
  floor for the rest of the fight once power hits zero. **Floor = 30%** (`TargetPowerFirepowerFloor`),
  explicit first-pass placeholder pending §6's balance-pass discipline — deliberately not 0% so a Target
  Power win still leaves some resistance rather than a free mop-up.
- Every other mode leaves `PhaseBPowerPlantHP` untouched (never drained), so the multiplier no-ops
  (`return 1f`) for Target Troops/Total Destruction — matches the panel spec below ("if there is no
  targeting power plants, [the power sprite] remains normal").

**Panel progress sprites** (`PhaseBProgressUI`, shared by both `SiegeDecisionUIController` — the
attacker's entry-gate/status panel — and `SiegeDefenseUIController` — the defender's status panel):
each combat step (shield / power / defending-troop / landed-attacker-troop) gets an `Image` that lerps
white→gray continuously as that step's `current/max` HP pool drains, plus an owner-insignia `Image` that
always shows `StarSysData.CurrentCivController.CivData.InsigniaSprite` — swapping to the invader's
insignia the instant `AssimilateSystem`/`ResolveTotalDestruction` flips `CurrentOwnerCivEnum`, with no
extra state needed. The power sprite is forced back to normal color outside Target Power rather than
tracking a pool that's never drained. All Image fields are optional (`PhaseBProgressUI` no-ops on null)
so this compiles and runs before the step sprites exist — Editor wiring is still needed once art lands.

**Known gap — resolved in §15:** the one-shot-decision-panel gap described in the original version of
this section (the "Assault System" button hiding for the rest of the siege once a mode was chosen) is
fixed — see §15.

## 15. Assault-progress viewing, Infrastructure sprite, and the full per-mode step breakdown (2026-09-13)

### 15.1 "Assault System" button stays live for the whole siege

`FleetMenuUIController`'s button now shows for as long as `fleetCon.IsBesiegingSystem` is true, not just
while `AssaultMode == None`. Label switches from "Assault System (Name)" (no mode chosen — opens the
§4.1 decision gate) to "View Assault (Name)" (mode already chosen — reopens the same panel as a live
progress view). `SiegeDecisionUIController.OpenPanel` does the corresponding gating: once
`AssaultMode != AssaultMode.None`, the three mode-choice buttons (Target Troops/Target Power/Total
Destruction) are hidden — the choice is locked in for this siege — while Withdraw stays available,
relabeled "Break Off Siege" to match `FleetMenuUIController`'s dedicated button of the same name (both
call the same `RequestBreakOffSiege()`). Total Destruction still ends the siege the instant it resolves
(`ResolveTotalDestruction` → `EndSiege`), so the button disappears again then regardless of this fix.

**"View Assault" is the Fleet UI button, not a panel button — the panel needs its own Close instead.**
Since `SiegeDecisionUIController`'s panel fully covers the Fleet UI while open (confirmed against your
description of the flow), the Fleet UI button that reopens it isn't reachable again to close it — the
only other button left in the reopened (mode-already-chosen) view was Withdraw, which would have forced
the player to abandon the siege just to stop looking at it. Added a dedicated `closeButton`, shown only
when `modeChosen`, that calls `ClosePanel()` directly with no side effect on the siege. So: Fleet UI
button = open only; in-panel Close = dismiss the view; in-panel Withdraw/Break Off Siege = the
separate, consequential action of actually ending the siege.

**Revision (2026-09-14):** `closeButton` is now shown at the initial §4.1 gate too, alongside the
new 20s countdown — see §4.2's 2026-09-14 revision note. There, clicking it (before picking a mode)
implies Withdraw rather than a free no-op dismiss, so the gate still can't be left unanswered
indefinitely just because the panel is closeable — it's a second path to the same default the
countdown itself falls back to on expiry.

### 15.2 New Infrastructure sprite (Factories/Research Centers/Universities/Population)

`StarSysData.PhaseBInfrastructureHP`/`MaxHP`, computed at `InitializePhaseB` for every mode the same way
`PhaseBPowerPlantHP` is (`ComputeInfrastructureHP`, same `ShipType.PlanetaryShield` placeholder source —
pending §6's balance pass same as everything else). Unlike Power/Troop/Shield, nothing gradually drains
this pool — no mode does incidental per-tick infrastructure damage — so in practice it only ever moves
once, instantly, when Total Destruction resolves.

**Revision (2026-09-14):** this single Infrastructure `Image` split into two —
`facilitiesProgressImage` (Factories/Research Centers) and `populationProgressImage` (Population) —
on both `SiegeDecisionUIController` and `SiegeDefenseUIController`, both still fed by the same
`PhaseBInfrastructureHP`/`MaxHP` pool above (no data-model change) so they gray together. Real icon
art for the two still needs Editor authoring/import — same placeholder-now convention as the
Shipyard's placeholder FBX (§13).

### 15.3 Full per-mode step breakdown, sprite-by-sprite

All five progress sprites (Shield / Power / Infrastructure / Troop / Landed-Troop) start at normal color
when the §4.1 panel first opens. What follows depends on the chosen mode:

**Stage 1 — Shield bombardment (identical for every mode).** Fleet fires on `PhaseBShieldHP`; defending
troops counter-fire at the fleet throughout. Shield sprite grays white→gray as the pool drains. Power,
Infrastructure, Troop, and Landed-Troop sprites all stay normal — untouched until shields fall
(`PhaseBShieldsDown = true`).

**Stage 2 — Target Troops.** Fleet fires directly on `PhaseBTroopHP` (Troop sprite grays); Power and
Infrastructure sprites stay normal for the rest of the assault (never targeted this mode). Once
transports land (`PhaseBTroopsLanded = true`), the Landed-Troop sprite appears and grays as
`PhaseBAttackerTroopHP` drains from the troops-vs-troops fight. Ends in capture (`AssimilateSystem`),
mutual elimination, or the attacking fleet being repelled.

**Stage 2 — Target Power.** Fleet fires exclusively on `PhaseBPowerPlantHP` (Power sprite grays);
Infrastructure sprite stays normal (never targeted this mode). Troop sprite stays normal during this
sub-stage too — troops aren't fired on yet — but defenders' own counter-fire against the fleet is
already scaled down live by `GetDefenderFirepowerMultiplier` as power drains. Once power is fully
destroyed (`PhaseBPowerPlantsDown = true`, Power sprite fully gray), falls into the *same* Stage 2
Target Troops behavior above (Troop sprite starts graying, Landed-Troop sprite appears once transports
land) — except defender firepower is pinned at the 30% floor for the remainder.

**Stage 2 — Total Destruction.** Resolves in one instant step, the tick immediately after shields fall
(`ResolveTotalDestruction`) — not gradual like the other two modes. Power sprite and Infrastructure
sprite both snap straight to fully gray (`PhaseBPowerPlantHP`/`PhaseBInfrastructureHP` forced to 0);
Troop sprite also snaps fully gray (`PhaseBTroopHP` forced to 0 — real `GroundForces`/`Population` are
wiped for real in the same call). Landed-Troop sprite stays normal/unused for the whole assault — Total
Destruction has no troops-vs-troops fight; the attacker's transports land afterward into the
already-claimed, empty system per §4.2, which isn't a combat the sprite needs to represent.

**Owner insignia**, all modes: always shows `StarSysData.CurrentCivController.CivData.InsigniaSprite`,
so it swaps to the invader automatically the instant `CurrentOwnerCivEnum` flips
(`AssimilateSystem`/`ResolveTotalDestruction`) — no mode-specific handling needed.

### 15.4 `EndSiege` no longer zeroes the HP pools

Previously `EndSiege` reset every `PhaseBXxxHP`/`MaxHP` pair to 0/0 on *any* siege end (repelled,
withdrawn, or captured). That raced against the sprites above: `PhaseBProgressUI`'s ratio math treats a
0/0 pool as "normal" (nothing drained yet), so a panel left open at the moment of capture would flash
fully-gray for one frame and then immediately flip back to fully-normal once `ResolveTotalDestruction`'s
trailing `EndSiege` call zeroed `MaxHP` too. Fixed by having `EndSiege` reset only what actually needs a
clean slate for a *future* siege on this same system — `AssaultMode`, `PhaseBCollateralAccum`, and the
three stage-gating bools (`PhaseBShieldsDown`/`PhaseBPowerPlantsDown`/`PhaseBTroopsLanded`, since
`InitializePhaseB` doesn't reset the latter two itself) — and leaving every HP pool at whatever this
siege's last tick left it: fully drained on a Total Destruction/capture (the sprites correctly stay
gray), partially drained on a repel (the sprites now show real accumulated damage instead of magically
resetting to pristine). `InitializePhaseB` unconditionally recomputes every pool fresh the next time
Phase B starts on this system regardless, so nothing is lost by leaving stale values here between sieges.
