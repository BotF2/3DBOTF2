# Turn & Stardate Flow

Status: implemented. This documents the actual current behavior of `TimeManager` plus every
system that hooks into its turn boundary, after the Phase-1 economy pass
(`Economy_Phase1_FuelLoop_FacilityCaps.md`) and the population/tech/elimination/victory work
described below. Treat this as the one place that explains "when does X happen" for the whole
galaxy-scene simulation.

## 1. Two clocks, not one

The game runs on two nested units of time:

- **Stardate** — the fine-grained clock. `TimeManager.TimeProgression()` (server-only coroutine)
  increments `currentStardate` by 1 every `10f / currentTimeSpeed` real seconds while
  `timeRunning` is true. Only special-event rolls (`CheckSpecialEvents`) happen at this
  granularity today.
- **Turn** — the coarse-grained clock every economy/research/population/outcome system actually
  runs on. `StarDatesPerTurn` (10) stardates make one turn. `TimeProgression` detects the
  boundary (`syncedStardate % StarDatesPerTurn == 0`) and calls `ProcessTurnEvents()`.

A galaxy-scene "turn" is therefore always exactly `StarDatesPerTurn` stardates of real-time
ticking, not a player-facing "click to end turn" in the classic 4X sense — see §3 for how player
input actually gates it.

## 2. What runs at the turn boundary, and in what order

`TimeManager.ProcessTurnEvents()` is the single choke point. Everything here runs once per turn,
in this fixed order, for every civ in the game at once (not per-player) — this is deliberate: later
steps (e.g. victory) need to see this turn's ownership changes, not last turn's:

1. `TechManager.ProcessResearchForAllCivs()` — playable civs earn TechPoints from their own active
   Research Centers; minor races earn their own independent trickle (§4).
2. `StarSysManager.ProcessDilithiumMining()` — per-system dilithium stockpile += mining rate.
3. `StarSysManager.ProcessAntimatterFuelLoop()` — Factories bank Antimatter, Power Plants draw it,
   blackout triggers on Factory/Power-Plant destruction (see the Phase-1 economy doc).
4. `StarSysManager.ProcessRepairs()` — docked ship repair ticks.
5. `PopulationManager.ProcessPopulationGrowthForAllCivs()` — population growth and ground-force
   conversion (§5). This used to self-subscribe to `TimeManager.OnStardateChanged` and run 10x more
   often than everything else on this list; it's now an explicit call here like every other system,
   so "when does population grow" has one obvious answer instead of two different clocks to check.
6. `CivManager.CheckForEliminatedCivs()` — flags any playable civ with zero systems and zero fleets
   left (§6).
7. `CivManager.CheckForVictoryCondition()` — checks the galaxy-ownership victory threshold (§7).

Random events are the one deliberate gap: `CheckSpecialEvents()` still only rolls
`RandomEvents`/`StardateEvents` per-*stardate*, not per-turn, and there is no turn-boundary random
event pass yet. That remains a ToDo — out of scope for this pass.

After step 7, `SetTurnPhase(TurnPhase.InterTurn)` and `PauseTime()` run — the clock always pauses
at the end of a turn and waits for the next `AdvanceTurn()`.

## 3. Player-facing gating: `TurnPhase` and `ReadyCivs`

`TurnPhase` has two states: `InterTurn` (paused, players give orders) and `TurnProgression`
(unpaused, the stardate clock ticks toward the next turn boundary). `AdvanceTurn()` is the only way
to leave `InterTurn`, and it is reached one of two ways:

- **All-ready auto-advance**: every non-AI, non-eliminated civ in `PlayerManager.Roster` calls
  `RequestSetCivReady(civ, true)`; once every such civ is in `ReadyCivs`,
  `TryAutoAdvanceIfAllReady()` calls `AdvanceTurn()` itself.
- **Force Turn**: `RequestForceAdvanceTurn()` (testing aid, always available) skips the ready-check
  entirely.

AI civs are always treated as ready the instant a new `InterTurn` begins
(`BeginNewInterTurnReadyState`) — there is no galaxy-map AI order-planning yet, so they have
nothing to wait on. **Eliminated civs are treated exactly the same way** (see §6) — once
`CivData.IsEliminated` is true, `TimeManager` auto-adds that civ to `ReadyCivs` every `InterTurn`
and skips it entirely in `GetHumanCivsNotReady()`, so a defeated human player is never shown as
something the table is "waiting on" and can never block or slow the turn loop.

Once `CivManager.GameHasEnded` is true (§7), `AdvanceTurn()` refuses to run at all — the game is
over and the clock stops for good.

## 4. Minor-race research is simulated independently

Minor races are not driven by a player, so they can't "earn" TechPoints by choosing to prioritize
research the way majors implicitly do by building Research Centers. Their growth used to be tied
directly to whatever the *local human player* earned that stardate (`ApplyMinorRaceGrowth`) — every
minor's tech rose and fell in lockstep with the human's own pace, not their own economy. That
coupling is gone.

`TechManager.ProcessMinorRaceResearch()` now gives every minor race with warp its own turn's
research output, entirely independent of every other civ in the game:

```
techPointsGained = round(
    (BaseMinorTechPerTurn + activeResearchCenters * ratePerCenter * techLevelMultiplier)
    * QualityScaleFactor(minor.QualityScore)
)
```

- `BaseMinorTechPerTurn` (1) is a flat passive trickle every warp-capable minor always earns, so a
  minor race never stalls at 0 forever just because its AI-managed economy hasn't gotten around to
  building a Research Center yet (see `StarSysAIManager`'s `ResearchCenterGateFactories`/
  `ResearchCenterGateShipyards` gate — a fresh colony-tier system can go a long time before
  qualifying).
- The active-Research-Center term uses the exact same per-center rate and tech-level multiplier the
  majors use, since minor systems are AI-managed (`StarSysAIManager`) and can build Research Centers
  within their own facility cap like any other AI-run system.
- `QualityScaleFactor` is `ShipStatCalculator.GetQualityScaleFactor` — the same 0.70x–1.30x curve
  already used to scale that civ's ship stats off `CivSO.QualityScore`. A canonically advanced minor
  (Breen, Tholian) researches faster than a canonically primitive one (Pakled, Kazon) from turn one,
  before either has built a single facility.
- Pre-warp minors and uninhabited placeholders never progress (`HasWarp` gate, unchanged).

## 5. Population growth

`PopulationManager.ProcessPopulationGrowthForAllCivs()` grows every owned system's population by a
turn's worth of growth (base rate + a bonus per active Factory/Research Center), then converts
accumulated population into `GroundForceData`-capped ground-force units. The per-turn constants
(`baseGrowthPerTurn = 0.2`, `growthPerActiveFactory/ResearchCenter = 0.05`) are the old per-*stardate*
constants (`0.02`/`0.005`/`0.005`) multiplied by `StarDatesPerTurn` (10), so overall pacing is
unchanged — only the clock it runs on changed, from "every stardate" to "once per turn, alongside
everything else."

## 6. Elimination

A **playable** civ is eliminated once it owns zero star systems and has zero fleets anywhere in the
galaxy (`CivManager.CheckForEliminatedCivs`, checked every turn boundary). This sets
`CivData.IsEliminated = true` (never cleared — there's no recapture mechanic yet) and fires
`GameEvents.OnPlayableCivDefeated`. An eliminated civ's `CivController`/`CivData` are **not**
removed from the game (reports/UI/save data can still reference them) — the only behavioral effect
is that `TimeManager` never waits on them again (§3).

Minor races are never checked here: a minor's `CivData` is superseded in place when its home system
is annexed (`CivManager.AnnexMinorCiv`), not zeroed out and left running.

## 7. Victory

A playable civ wins the moment it owns `VictorySystemFraction` (1/3, rounded up) of every star
system in the galaxy — majors' and minors' homeworlds, existing colonies, and any not-yet-colonized
system all count toward the total (`StarSysManager.StarSysControllerList.Count`).
`CivManager.CheckForVictoryCondition()` runs this check every turn boundary, right after the
elimination check; the first civ found meeting the threshold wins immediately
(`GameHasEnded = true`, `VictoriousCivEnum` set, `GameEvents.OnGameVictory` fired), and no further
turns can be advanced (§3). Only one civ can ever win a given game.

There is currently no dedicated victory/defeat screen wired to these events — `OnGameVictory` /
`OnPlayableCivDefeated` are the intended hook points for that UI work.

## 8. Deliberately out of scope for this pass

- **Random events at the turn boundary** — still only rolled per-stardate (§2); a real turn-scoped
  pass is a ToDo.
- **Tech tree / research choices** — `TechPoints` is still a single accumulating scalar per civ with
  four fixed tiers (EARLY/DEVELOPED/ADVANCED/SUPREME); a branching tech tree is Phase II.
  work.
- **Ground-force combat / invasion, and facility destruction/capture nuance in combat** — see
  `Economy_Phase1_FuelLoop_FacilityCaps.md`; still not modeled.
- **The `Greedy` personality trait's economic meaning** — currently only feeds `DiplomaticAptitude`;
  a real economic effect is deferred until diplomacy is built out further.
- **Credits/currency** — removed entirely (see below). Dilithium, Antimatter, and Power
  supply/generation are the game's economy; there is no money layer on top of them.
