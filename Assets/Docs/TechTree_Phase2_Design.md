# Tech Tree — Phase II Design Plan

Status: design draft, not yet implemented. Companion to `TechSystem_Implementation_Guide.md` and
`Tech_System_Quick_Reference.md`, which document the **Phase I** flat `TechPoints → TechLevel`
system already in `TechManager`/`CivData`. Phase II does not replace that plumbing — it builds a
branching, player-chosen tech tree on top of it.

Two companion spreadsheets hold the same content in flat, sortable/filterable form (open in Excel
or Google Sheets) — use these day-to-day, use this document for the reasoning behind them:
- `TechTree_CommonBranches.csv` — all 35 shared techs (Branches A–E × 7 tiers), identical for every
  civ except display flavor.
- `TechTree_FactionUnique.csv` — all 49 Branch F entries (7 civs × 1 innate + 6 researched), with
  the unlock tier, backend tie-in, and canon rationale for each.

## 1. What Phase I already gives us (reuse, don't rebuild)

- `CivData.TechPoints` — empire-wide currency, earned from Research Centers + a minor-race trickle
  (`TechManager.ProcessResearchForAllCivs` / `ProcessMinorRaceResearch`).
- `CivData.TechThresholds` (0 / 100 / 300 / 600) → 4 `TechLevel`s (EARLY/DEVELOPED/ADVANCED/SUPREME),
  each with its own flat multiplier tables in `TechManager` (`GetTechMultiplier`,
  `GetPowerEfficiencyMultiplier`, `GetFactorySpeedMultiplier`, `GetShipyardSpeedMultiplier`,
  `GetResearchOutputMultiplier`).
- A **7-stage granular threshold ladder** already exists precedent for: `fogSightRangeStages` in
  `TechManager` steps sight range at 0/100/200/300/450/600/900 rather than jumping only at the 4
  level boundaries. `Tech_System_Quick_Reference.md` documents the same idea for ship unlocks
  (`MinTechPointsRequired` between level thresholds).
- `civ.CivData.Playable` already splits majors (human/AI-played, 7 of them: `FED, ROM, KLING, CARD,
  DOM, BORG, TERRAN`) from minors (AI-trivial, always-on trickle). **Phase II only adds the
  choice-driven tree to the 7 majors** — minors keep today's flat auto-multiplier path unchanged.
  This is the single biggest scope-limiter and it falls out of code that already exists.
- The "player selects one thing from a menu, it resolves over several turns, civ-wide effect on
  completion" pattern already exists twice: `IntelProject`/`IntelligenceData` and
  `DiplomacyProject`/`DiplomacyData`. The tech tree's research queue should be a third instance of
  this same pattern, not a new one.

**Conclusion:** Phase II reuses the existing 7-stage threshold ladder (0/100/200/300/450/600/900)
as the tree's tier gate, reuses `TechPoints` as the spend currency, reuses the Project pattern for
"currently researching X", and only adds new data (tech definitions) and new UI on top.

## 2. Core gameplay shift

Today, every civ at the same `TechPoints` total gets *identical* multipliers automatically — there
is no choice. Phase II's whole point is to make research an active decision:

- At each of the 7 tiers, new tech **options** unlock across all branches at once (menu grows).
- Research is **single-threaded, not parallel** (revised — see below): a civ researches exactly one
  tech at a time, regardless of how many Research Centers/Universities it has built. Research
  Centers still matter — they're what `TechManager.CountActiveResearchCenters` already feeds into
  the Phase I per-turn `TechPoints` income calc — but that count now only scales *how fast* the one
  active project completes, not *how many* can run side by side. A civ never gets to develop two
  branches at once; building a second Research Center just means the single queued project finishes
  in fewer turns.
- The player is never left with nothing queued: **a default project auto-starts** the moment a civ
  has no active project — at civ init, and again immediately after any project completes. The
  default is always that civ's own Branch A, Tier 1 tech (`Warp Core Stabilization` or its
  civ-flavored equivalent) — same rule for all 7 civs, cheapest/first tech in the tree, easy to
  explain in a tooltip. The player can swap to a different tech at any time via `StartResearch`
  (below); this isn't a one-time nudge, it's the fallback the civ always lands on when the queue
  goes empty and the player hasn't chosen anything else yet.
- **Switching projects never destroys progress.** Progress is tracked *per tech*, not just on
  whatever's currently active — `StartResearch(civ, techId)` always succeeds and simply changes
  which tech that turn's `TechPoints` income is credited toward; a tech that already had some
  `TechPoints` banked against it from an earlier stint as the active project resumes from that
  banked amount when re-selected, rather than restarting from zero. This makes switching a free
  tactical choice (e.g. rush a cheaper tech first, come back to the expensive one later) instead of
  a punishing one.
- Each project's total cost is a fixed per-tier `TechPoints` amount (the `TimeLine` column, §3.3,
  converted from turns at one dedicated Research Center's baseline rate); every `TechPoints` a civ
  earns in a turn now goes entirely to its one active project (no more splitting across concurrent
  projects, since there's only ever one) — more Research Centers still means faster completion of
  whichever project is currently queued, just never more than one in flight.
- Completing a tech applies **its own specific effect**, civ-wide, immediately — not a blanket
  level-up. A civ that rushes the Propulsion branch and neglects Tactical ends up faster but
  squishier than a rival at the same `TechPoints` total who chose the opposite — a real tradeoff.
- `TechLevel`/`OnTechAdvanced` keep firing exactly as today (legacy hooks — build speed, minor-race
  trickle scaling) so nothing existing breaks; the tree is additive. The one exception is fog sight
  range — see the Branch D note below, its automatic growth is superseded by an actual research
  choice.

## 3. Balance framework (the actual ask)

The risk with 7 hand-authored civ trees is drift: one civ ends up with more options, or with a
unique tech that's quietly stronger. Four rules eliminate that risk **by construction** instead of
by manual bookkeeping:

1. **Identical skeleton, every civ.** 5 shared branches + 1 faction-unique branch, **7 tiers each**,
   one tech per branch per tier. Every civ has exactly **42 techs** (35 shared + 7 unique). No civ
   has more choices or a longer/shorter path than any other.
2. **Shared-branch techs are ONE asset, not seven.** A shared tech (e.g. "+hull% at tier 3") is a
   single `TechDefSO` with one backend effect and one magnitude. It cannot drift between civs
   because there is only one number to look at. Only the *display name/description/icon* varies per
   civ, via a flavor-text override table — never the effect.
3. **Same cost per tier, regardless of branch or civ.** Tier-N research cost — turns to complete
   once queued into a project, not the `TechPoints` unlock threshold — is one fixed number shared by
   every branch and every civ. Pacing is identical; the only thing that varies is *which* tier-N tech
   a civ's player picked, not how long it took to unlock tier N itself. This is the `TimeLine` column
   in both CSVs (§4, §5): **T1 3 · T2 4 · T3 5 · T4 6 · T5 8 · T6 10 · T7 14 turns**, baseline for one
   dedicated, unshared Research Center's standard output — actual turns shift with the civ's total
   `TechPoints`/turn income, all of which now goes to the one active project (§2, revised — no more
   splitting across concurrent projects, since only one can ever run). Innate abilities (§5) have no
   `TimeLine` — they're never queued, so it's `0`.
4. **Unique techs are power-budgeted cumulatively, not lockstep per tier.** Earlier drafts of this
   doc forced every civ's 7 unique techs into one-per-tier slots so tier 7 was always a straight
   apples-to-apples comparison. That's been dropped (§5) — canon placement matters more than
   artificial symmetry, and Star Trek lore does not put every faction's signature ability on the
   same rung of the ladder (Romulans had working cloaks in the TOS era; Klingons only acquired the
   tech later; Borg transwarp is a mid-Advanced-era reveal, not a day-one ability). Instead, each
   civ's 7 unique entries (1 innate + 6 researched, §5) are plotted as a **cumulative power curve**
   against `TechPoints` (0→900): the total area under that curve — not any single tier's row — must
   land within a tunable band (default ±15%) of the other six civs' curves. A civ can front-load
   cheap-but-minor abilities (Federation's diplomacy) or back-load one huge reveal (Borg transwarp)
   as long as the running total stays comparable. An editor validator (§7) checks this automatically
   instead of relying on memory during future content adds.

This means balancing effort concentrates entirely on **7 unique techs per civ** (49 total) — the 35
shared techs are balanced automatically by being singular assets.

## 4. The five shared branches (identical for all 7 civs, flavor-renamed only)

Tiers use the existing threshold ladder: **T1=0, T2=100, T3=200, T4=300, T5=450, T6=600, T7=900.**

Suggested extended multiplier curve for shared "+stat%" techs (interpolates the existing
1.00/1.15/1.35/1.60 `GetTechMultiplier` curve at the T2/T4/T6 boundaries so old balance mostly
survives): **T1 1.00 → T2 1.08 → T3 1.16 → T4 1.25 → T5 1.35 → T6 1.48 → T7 1.65**, applied as *this
branch's own* multiplier once fully researched, not a global one.

**Branch A — Propulsion & Spatial Engineering** (galaxy-map speed / FTL / dilithium cost)
| Tier | Tech | Effect hook |
|---|---|---|
| 1 | Warp Core Stabilization | `PersistentWarpSpeed` — sustained warp speed |
| 2 | Warp Field Optimization | `WarpSpeedMultiplier` (sector move speed), −move cost |
| 3 | Warp Field Overlap | `WarpSpeedAverage` — shares warp fields between ships in a fleet |
| 4 | Wormhole Quantum Slipstream | `WormholeStabilizer` — improve stability of wormholes |
| 5 | Transwarp Access | `AccessTranswarpHub` — access Transwarp hub travel |
| 6 | Warp Beacon Network | Static fast-lane between two owned systems (non-Borg version of Transwarp) |
| 7 | Deep Space Rangefinding | Capstone `WarpSpeedMultiplier` cap increase |

**Tier-1 downside (new):** without `PersistentWarpSpeed` researched, a fleet's warp core can't hold
its cruise speed steady — after 1 turn in transit, galaxy-map warp speed drops 25% (`WarpDecayPenalty`,
applies to every fleet, every civ, every turn spent moving beyond the first) and stays reduced for the
rest of that move. Researching `Warp Core Stabilization` (Tier 1, 0 `TechPoints`, 3-turn project)
removes the decay entirely — it doesn't buff speed above baseline, it just stops the drop, hence
"sustained" rather than "increased." This makes Branch A's Tier 1 the one Common-Branch tech with a
until-researched penalty rather than a pure on-completion bonus; every other branch's Tier-1 (§4) is
additive-only.

**Tier-3 change (implemented):** `FleetController.UpdateMaxWarp()` sets a fleet's `MaxWarpFactor` to
the *minimum* of its ships' `ShipData.maxWarpFactor` by default — one slow transport caps the whole
fleet. `Warp Field Overlap` (`WarpSpeedAverage`) changes that formula, for a civ that has it, to the
*average* of its ships' `maxWarpFactor` instead of the minimum — mixed fleets stop being bottlenecked
down to their slowest hull. This is a formula swap inside the existing `UpdateMaxWarp()` recompute
(still runs on the same ship-added/removed/merged triggers), not a new multiplier stacked on top of
it — civs without it keep today's min-of-fleet behavior unchanged.

Implemented ahead of the rest of Phase II's per-tech tracking (§7's `TechDefSO`/`UnlockMode` doesn't
exist yet), so `UpdateMaxWarp()` currently gates on the civ's flat `CivData.TechPoints >= 200` — the
Tier-3 threshold from the existing ladder (line 104) — as a stand-in for "has researched Warp Field
Overlap." This should be swapped for a real per-tech researched check once Phase II's tracking ships;
until then every Tier-3 Propulsion tech (not just this one) would read as "unlocked" by the same
threshold, which is a known gap of using the threshold as a proxy.

**Tier-4 change (design-only, blocked on wormholes shipping):** wormholes as a galaxy-map object
don't exist yet — `GameEnums.cs` only has a comment reserving that "black holes and wormholes will
have their own class" (no implementation). `Wormhole Quantum Slipstream` (`WormholeStabilizer`) is
written now so the hook name is reserved ahead of that work. Once wormholes ship, a fleet entering an
unstabilized wormhole is meant to risk two separate failure modes: (1) the wormhole itself collapsing
and destroying the fleet inside it, and (2) the wormhole's exit point drifting between the fleet
entering and exiting, so it surfaces somewhere other than the destination the player expected.
Researching this tech reduces the odds of both. No implementation to point at yet — flag this section
for a rewrite once the wormhole object model and its galaxy-map traversal exist.

**Tier-5 change (design-only, blocked on Transwarp Hubs shipping):** Transwarp Hubs themselves don't
exist yet — they're the Borg's unique T5 `Transwarp Hub Network` (`TranswarpNetwork`, §5, built via
the future `TranswarpHubController`, §8 phase II.3) for instant point-to-point travel between
Borg-held systems. `Transwarp Access` (`AccessTranswarpHub`) is the non-Borg counterpart: any other
civ that researches it becomes able to *enter and use* a Transwarp Hub the Borg have built, whereas an
un-researched civ cannot use one at all even if it physically sits at a Borg system they've entered
(diplomatically, or by force). Deliberately placed at the same Tier 5 / 450-point threshold as the
Borg's own hub-network tech, so hub *access* for everyone else unlocks no earlier than hub
*construction* does for the Borg. No implementation to point at yet — needs `TranswarpHubController`
to exist first.

**Branch B — Tactical: Hull & Shields**
| Tier | Tech | Effect hook |
|---|---|---|
| 1 | Polarized Hull Plating | `HullMultiplier` |
| 2 | Deflector Shields & Harmonics | `ShieldMultiplier` |
| 3 | Ablative Hull Materials | `HullMultiplier`, −torpedo dmg taken |
| 4 | Quantum Capacitors | `CombatBurst` — combat burst (+shield or +weapon, temporary) |
| 5 | Regenerative Shield Matrices | Shield regen mid-combat (`ShieldRegenMidCombat`) |
| 6 | Metaphasic Shielding | Resist environmental/exotic damage |
| 7 | Quantum Shield Tuning | Capstone `ShieldMultiplier` vs. exotic weapons |

**Tier-4 change:** `CombatBurst` is automatic, not player-activated — once `Quantum Capacitors` is
researched, the code triggers the temporary +shield-or-+weapon burst on its own during combat, with no
menu, button, or order for the player to select. (Earlier text in this doc and the CSV called it
"player-activated"; that's stale — corrected here.)

**Branch C — Weapons & Ordnance** (weapon *name* is civ-flavored: Phasers/Disruptors/Polaron/Cutting
Beam — same backend `WeaponDamageMultiplier`/ordnance-unlock hook regardless of label)
| Tier | Tech | Effect hook |
|---|---|---|
| 1 | Beam Weapon Calibration | `WeaponDamageMultiplier` |
| 2 | Photon-class Torpedoes | Ordnance unlock |
| 3 | Fire Control Solutions | +accuracy/crit |
| 4 | Plasma-class Torpedoes | Ordnance unlock, DoT |
| 5 | Quantum-class Torpedoes | Ordnance unlock, high yield |
| 6 | Transphasic-class Torpedoes | Ordnance unlock, % shield-bypass |
| 7 | Directed Energy Overcharge | Capstone `WeaponDamageMultiplier` |

**Branch D — Sensors, Science & Infrastructure**

Scanner range is this branch's throughline, not a one-off T1 pick: **every** node in Branch D
additionally grants the next `fogSightRangeStages` stage on top of its listed effect, in tier order.
This replaces today's `TechManager.GetFogSightRangeMultiplier(techPoints)`, which currently grows
automatically from banked `TechPoints` alone — under Phase II the multiplier instead comes from
`GetFogSightRangeMultiplier(highestResearchedBranchDTier)`, so a civ that never researches past
Branch-D tier 3 stays at that stage's sight range even if its `TechPoints` total (spent chasing
other branches) has long since crossed tier 6 or 7. A player can grab the first stage or two early
and cheaply, or gamble on delaying the whole branch to rush Tactical/Ordnance instead — the fog
reveal is the visible cost of that choice, not a background number nobody interacts with.

| Tier | Tech | Effect hook |
|---|---|---|
| 1 | Long-Range Sensors | `SightRangeStage` 1 + reveals system facility caps on scan |
| 2 | Stellar Cartography | `SightRangeStage` 2 + reveals hidden system resources |
| 3 | Anomaly Detection | `SightRangeStage` 3 + anomalies seen on galaxy map |
| 4 | High-Density Energy Storage | `SightRangeStage` 4 + facility power buffer |
| 5 | Structural Integrity Fields | `SightRangeStage` 5 + station HP/`FacilityCapBonus` |
| 6 | Xenobiological Engineering | `SightRangeStage` 6 + pre-req flavor step toward terraforming |
| 7 | Terraforming Technology | `SightRangeStage` 7 (capstone) + `FacilityCapBonus` on low-tier worlds |

**Tier-3 change (design-only, blocked on anomalies shipping):** anomalies as a galaxy-map object
class don't exist yet (same gap as wormholes, §4 Branch A Tier 4 note — `GameEnums.cs` only reserves
the idea). `Anomaly Detection` (`SightRangeStage_3`) is meant to make anomalies — wormholes included —
visible on a civ's galaxy-map view once researched; before that, an unresearched civ's map simply
doesn't render them at all, they're not just hidden-but-detectable. No implementation to point at yet.

**Branch E — Intelligence & Espionage** (detection/decoys/counter-intel only — **no true cloak**;
cloaking is reserved for the Klingon/Romulan unique branch per the design brief)
| Tier | Tech | Effect hook |
|---|---|---|
| 1 | Subspace Echo Decoys | Fake fleet signatures |
| 2 | Quantum Masking Algorithms | Hides sub-light movement from long-range sensors |
| 3 | Deep Cover Networks | Partial intel panel reveal on target empire |
| 4 | Full Surveillance Net | Full intel panel reveal |
| 5 | Counter-Espionage Doctrine | Resist enemy sabotage |
| 6 | Tachyon Detection Grid | Detects cloaked fleets (Tier-4/5 cloaks only) within sensor range on the galaxy map — see §5b |
| 7 | Strategic Intelligence Mastery | Capstone empire-wide intel dashboard |

## 5. Branch F — Faction-Unique (1 innate ability + 6 researched techs, per civ)

**Two kinds of entry, not one.** Not every faction trait is a laboratory result — some are how a
society already operates on day one. Each civ's Branch F is now:

- **1 Innate Ability (Tier 0)** — active from turn one, costs no `TechPoints`, occupies no research
  slot, and never appears in the "available to research" menu. It's a flag read straight off
  `CivData`/`CivSO`, not a project. Federation diplomacy is the driving example: `CivData` already
  computes `DiplomaticAptitude` from the Warlike/Xenophobia/Ruthless/Greedy trait enums, and
  Federation's `CivSO` already scores favorably on all four — so the "innate ability" for Federation
  is mostly a matter of *surfacing* that existing number as a visible Tech-Tree card ("Federation
  Charter — active since founding") plus a small first-contact-outcome bonus, not inventing a new
  stat from nothing. Every other civ gets an equivalent zero-cost Tier-0 trait card of its own so no
  civ is short a slot.
- **6 Researched techs**, gated behind the same threshold ladder as Branches A–E and requiring a
  Research Center/University project slot like anything else — but their **tier placement is
  lore-driven, not forced one-per-tier**. Star Trek canon does not put every faction's signature
  ability on the same rung: Romulans fielded working cloaking technology as early as the TOS era
  (2266, *Balance of Terror*); Klingons only acquired the tech later via a Romulan technology
  exchange (2268, *The Enterprise Incident*) and fielded their own indigenous cloak-capable
  Birds-of-Prey generations afterward — so Romulan cloak research starts earlier on the ladder than
  Klingon's. Borg transwarp conduits/hubs are a mid-to-late *Advanced*-era reveal (Voyager-era,
  well after first Borg contact), not a day-one Borg ability — placed at tier 5 (450 points, past
  the 300-point Advanced threshold) rather than as an endgame tier-7 capstone. This means **total
  count per civ stays 1 + 6 = 7** (so the 42-techs-per-civ symmetry in §3.1 still holds), but the
  *ladder position* of each civ's 6 techs is no longer lockstep-matched to the others — see §3.4 for
  how balance is checked instead (cumulative power curve, not per-tier rows).

| Civ | T0 Innate (free, day one) | Researched techs (tier · name · why this tier) |
|---|---|---|
| **Federation** | Federation Charter — surfaces existing `DiplomaticAptitude`; small first-contact bonus | T1 First Contact Protocols (structured first-contact doctrine, present from Starfleet's founding mandate) · T2 Diplomatic Outreach Doctrine — **University-researched: directly raises the chance/speed of a minor civ agreeing to join the Federation**, the concrete ask, distinct from the passive T0 trait · T3 Minor-Civ Alliance Discount (Federation's historical growth-by-invitation, Centauri/Andor/Tellar-era pattern) · T4 Federation Science Exchange (exploration/research mandate maturing) · T5 Positronic Neural Network (Soong-type android tech, a rare mid-late-24th-century achievement) · T7 Federation Charter Mastery (capstone — post-Khitomer-Accords galactic diplomatic leadership) |
| **Klingon** | Warrior's Creed — small innate combat-morale trait | T1 Ionized Hull Plating (baseline Bird-of-Prey shipbuilding) · T2 Disruptor Overload Arrays (disruptors are baseline Klingon armament since TOS) · T3 Great Houses Fleet Coordination (House politics organizing fleets) · **T5 Battle Cloak — fleets become invisible on the galaxy map** (acquired-tech lag from the Romulans, indigenous by the Bird-of-Prey era — unlocks later than Romulan's own cloak, see §5b) · T6 Disruptor Subsystem Cripple (mature targeting systems) · **T7 Adaptive Battle Cloak Refinement (capstone) — defeats the shared Tachyon Detection Grid, unseen again** (see §5b) |
| **Romulan** | Culture of Secrecy — small innate detection-resistance trait (matches the existing Xenophobia trait) | T1 Cloak Field Theory (theoretical groundwork only — no invisibility yet) · T2 Tal Shiar Intelligence Matrix (Tal Shiar formalized by the TNG era; moved earlier for Romulan specifically since it doesn't depend on the cloak) · T3 Adaptive Cloak Harmonics (precursor shield/sensor tuning — still no invisibility) · **T4 Basic Cloaking Field — fleets become invisible on the galaxy map**, first operational cloak in the game, above the Developed threshold (TOS-era precedent, *Balance of Terror*, see §5b) · T5 Warbird Ambush Doctrine (first-strike combat bonus when decloaking to attack) · **T7 Near-Perfect Cloak (capstone) — defeats the shared Tachyon Detection Grid, unseen again** (Nemesis-era Scimitar-class-grade stealth, see §5b) |
| **Borg** | The Collective — small innate fleet-coordination/repair trait (Borg ships are already unnervingly capable at first contact) | T1 Nanite Regeneration Matrix I · T2 Adaptive Shield Modulation I · T3 Adaptive Shield Modulation II · T4 Nanite Regeneration Matrix II · **T5 Transwarp Hub Network — placed mid-Advanced (450, past the 300 threshold) per design direction, not as a tier-7 capstone: the Collective is still developing it, not opening the galaxy with it on day one** · T7 Assimilation Protocols (capstone — full ship/population capture; escalating species-conversion threat is the true endgame, arguably scarier than mobility alone) |
| **Cardassian** | Obedient Society — small innate unrest-reduction trait (authoritarian central control is inherent, not researched) | T1 Reinforced Duranium Hulls · T2 Cardassian Logistics Optimization · T3 Interrogation Algorithm Suites · T4 Obsidian Order Surveillance Net (the Order's influence is a DS9-era-occupation-scale story beat, mid-game) · T5 Occupation Efficiency Doctrine (decades-long Bajoran Occupation backdrop) · T7 Central Authority Infrastructure (capstone) |
| **Terran Empire (Mirror)** | Imperial Ambition — small innate aggression/expansion trait | **T1 Agonizer Discipline Regimen — University-researched: brutal authority and punishment directly boost facility/production output** (fear-driven productivity, the concrete ask; reuses a facility-output-multiplier hook, distinct from the T3 combat-morale tech below) · T2 Fear-Driven Command Protocols I · T3 Imperial Phaser Overcharge · T4 Fear-Driven Command Protocols II · T5 Terran Elite Strike Teams · T7 Flagship Domination Systems (capstone) |
| **Dominion** | Founders' Design — small innate obedience/discipline trait (Vorta/Jem'Hadar are bioengineered for it, not trained into it) | T1 Ketracel-White Optimization · T2 Vorta Command Algorithms · T3 Polaron Beam Enhancement · T4 Gamma Quadrant Supply Lattice · T5 Changeling Infiltration Units (the infiltration campaign is a mid-to-late DS9-arc reveal, not a day-one ability) · T7 Cloning Acceleration Chambers (capstone — Dominion War-era mass reinforcement) |

Every capstone (tier 7 above) sits in a different *domain* (diplomacy, total war, stealth, capture,
authoritarian control, command aura, mass production) and is checked against the others via the
cumulative-curve method in §3.4, not by being on the same row.

### 5b. The cloak arc (Branch E + Romulan/Klingon Branch F, tied together across four beats)

This is the one place a shared-branch tech and two civs' unique techs are deliberately sequenced
against each other as a single story, per design direction:

1. **Tier 4 (300 pts, above Developed) — Romulan `Basic Cloaking Field`.** Romulan fleets become
   invisible on the galaxy map. No other civ has an answer yet. Canon precedent: Romulans had a
   working cloak as early as the TOS era (*Balance of Terror*, 2266) — first among every stealth
   line in this tree, matching that head start.
2. **Tier 5 (450 pts) — Klingon `Battle Cloak`.** Klingons acquire the same galaxy-map invisibility,
   noticeably later than the Romulans — canon precedent: Klingons only received cloaking technology
   via a Romulan exchange in 2268 (*The Enterprise Incident*) and fielded their own indigenous
   cloak-capable Birds-of-Prey afterward.
3. **Tier 6 (600 pts) — shared `Tachyon Detection Grid` (Branch E, all seven civs).** The rest of
   the galaxy researches a countermeasure: any civ that completes it can detect a Romulan/Klingon
   fleet running Tier-4/5 cloak tech within sensor range on the galaxy map. Canon precedent: DS9-era
   Starfleet/Federation used tachyon detection grids specifically to spot cloaked Romulan ships.
   Deliberately shared, not Federation-exclusive — every non-cloak civ gets the same answer at the
   same tier.
4. **Tier 7 (900 pts) — Romulan `Near-Perfect Cloak` / Klingon `Adaptive Battle Cloak Refinement`.**
   Both cloak-owning civs research an upgrade that specifically defeats Tachyon Detection Grid,
   restoring invisibility — the "unseen again" beat. This is each civ's Branch-F capstone, so the
   arms race resolves right at the top of the ladder rather than leaving a dangling loose end.

Backend-wise, `Basic Cloaking Field`/`Battle Cloak`/their Tier-7 upgrades all share one new
`GalaxyMapCloak` effect hook (a flag a fleet carries, checked by whatever currently decides which
enemy fleets render on the galaxy map — likely alongside the existing fog-of-war/`FogRevealer`
visibility layer, not a parallel system); `Tachyon Detection Grid` is the corresponding
`CloakDetection` hook that suppresses that flag *only* for Tier-4/5-grade cloaks, explicitly not for
the Tier-7 upgrades. This file doesn't name the exact class that currently renders enemy fleet icons
on the galaxy map — confirm that during II.3 effect wiring before implementing `GalaxyMapCloak`.

### 5a. Concrete backend tie-ins (research a specific number in a system that already exists)

The instinct to avoid is inventing a disconnected `EspionageBonus`/`ProductionBonus` multiplier that
lives only inside `TechManager` and nowhere else. Every civ already has a real subsystem whose
numbers a Branch F tech can add to directly — found by grep, not assumed:

- **Romulan Tal Shiar Intelligence Matrix / Cardassian Obsidian Order Surveillance Net** — this is
  the case that prompted the check, and it already has an exact landing spot:
  `IntelligenceManager.GetCivSuccessModifier(CivEnum)` today returns a *purely trait-driven* value
  (`-((int)Ruthless + (int)Xenophobia) / 30f`) with no tech input at all, and feeds straight into
  every `IntelProject`'s `SuccessChance`/`DiscoveryChance` (`CreateIntelProject`,
  `CalculateSuccessChance`/`CalculateDiscoveryChance`). Researching the tech adds a flat bonus on
  top of that trait-driven value (and trims `CalculateDiscoveryChance`) for the researching civ only
  — i.e. **the same Research Centers that fund the tech tree are what make the Tal Shiar/Obsidian
  Order actually better at their job**, not a separate stat nobody can see.
- **Federation Diplomatic Outreach Doctrine** — lands in `DiplomacyController`: minor-civ
  `DiplomacyPointsOfCivs` drift toward `DiplomacyStatusEnum.Membership` (which triggers
  `AnnexMinorIntoMajor()`) is already scaled by a `majorFactor` derived from `DiplomaticAptitude`,
  and menu-gesture point gains already scale by `proposer.CivData.DiplomaticAptitude * 0.2f`
  (`ApplyGestureGain`). The tech adds a further flat multiplier to both once researched — a direct,
  measurable increase in how fast/likely a minor civ agrees to join, not a vague "diplomacy is
  better now" flag.
- **Terran Agonizer Discipline Regimen** — lands in the existing `TechManager.
  GetFactorySpeedMultiplier`/facility-output plumbing (already keyed by `TechLevel` for every civ):
  once researched, Terran gets an additional flat multiplier stacked on top of the shared curve,
  representing punishment-driven output the other six civs don't get access to at all.
- **Klingon Disruptor Overload Arrays / Subsystem Cripple** — lands in the same order-based
  damage-modifier step `CombatOrderHelper`/`TurnBasedCombatResolver` already use for Rush/Flanking
  advantages (per `CLAUDE.md`'s "damage multipliers via `CombatOrderHelper`") — adds a chance to
  apply an extra subsystem-disable effect on top of a normal hit, not a new parallel damage system.
- **Borg Nanite Regeneration Matrix / Assimilation Protocols** — Nanite Regen taps the existing
  per-turn hull-repair path ships already have between engagements (a flat bonus regen rate);
  Assimilation Protocols hooks the post-combat resolution step that currently only awards
  kills/wrecks, adding a chance to convert a destroyed-or-disabled enemy hull/crew into a Borg asset
  instead.
- **Dominion Changeling Infiltration Units** — lands in `IntelligenceManager` the same way as the
  Romulan/Cardassian techs, but unlocks a *new* `SecretActionsEnum` action (infiltration/
  destabilization) rather than just improving the odds on existing ones — the one Branch F tech
  across all 7 civs that adds a new verb to an existing enum instead of a new number to an existing
  formula.

The pattern to keep for every future unique tech: **before inventing a new multiplier field, grep
for whether the mechanic it's supposed to affect already has one** (`GetCivSuccessModifier`,
`DiplomaticAptitude`, `GetFactorySpeedMultiplier`, the order damage-modifier step, hull-repair,
post-combat resolution). Phase II's job is almost always to add a civ-gated bonus onto an existing
formula, not to build a parallel one.

## 6. Data & code architecture

New types (Core-layer-safe: pure data, no app-namespace imports in anything under `_Core/`):

- `TechFieldEnum { Propulsion, Tactical, Ordnance, Science, Intelligence, FactionUnique }`
- `TechUnlockMode { Researched, InnateFromStart }` — the Tier-0 innate ability per civ (§5) sets
  `InnateFromStart` and is applied once at civ init, skipping the research queue and
  `CountActiveResearchCenters` cap entirely; it never appears in the "available to research" list,
  only as an already-active card in the UI.
- `TechDefSO : ScriptableObject` — `Id`, `Field`, `Tier(0-7)`, `UnlockMode`, `ResearchCost`,
  `Prerequisites`, `EffectHookId`, `EffectMagnitude`, `UniqueToCiv` (null = shared).
- `TechEffectHook` enum dispatched from a single `TechManager.ApplyTechEffect(civ, techDef)` —
  reuses existing multiplier plumbing (`ShipStatCalculator`, `CombatOrderHelper`,
  `ResearchCenterData`, `StarSysData` facility caps, `IntelligenceManager.GetCivSuccessModifier`,
  `DiplomacyController`'s `DiplomaticAptitude`-scaled drift/gesture gain — see §5a for the full
  list) rather than adding parallel systems.
- `TechFlavorSO` (one per civ) — override table of `{TechId → DisplayName, Description, Icon}` for
  the 35 *shared* techs only; unique techs already carry their own name on the `TechDefSO`.
- `CivData` additions (revised — single-project model, not a list): `HashSet<string>
  ResearchedTechIds`, `Dictionary<string, int> BankedTechPointsByTechId` (every tech's progress,
  including ones not currently active — this is what makes switching lossless, §2), `string
  ActiveTechId` (the one tech currently receiving that turn's income; null only ever transiently,
  since a default always gets queued the instant it would otherwise go empty).
- `TechManager` additions: `StartResearch(civ, techId)` — always succeeds and simply reassigns
  `ActiveTechId`, no concurrency check against `CountActiveResearchCenters` anymore (Research
  Centers no longer gate *how many* projects run, only the income that fuels the one that is); a
  per-turn progress tick alongside `ProcessResearchForAllCivs` that credits that turn's entire
  `TechPoints` income to `BankedTechPointsByTechId[civ.ActiveTechId]`; `CompleteResearch` →
  `ApplyTechEffect` + new `OnTechResearched` event (additive to the existing `OnTechAdvanced` level
  event, which keeps firing unchanged for legacy hooks), then immediately calls `StartResearch` again
  with the default pick (Branch A Tier 1, §2) if the player hasn't already queued something else for
  after this one. `GetDefaultTechId(civ)` — the Branch A Tier 1 tech, flavor-resolved per civ — is
  what both civ init and post-completion auto-start call into, so the "always something queued" rule
  has one implementation, not two.

New UI: `TechTreeMenuUI` (tabs per branch, node list per tier, lock/available/in-progress/completed
states) follows the same construction pattern as the Diplomacy/Intelligence menus. Extend
`TechNotificationUI` to announce individual tech completions, not just level-ups. A `Menu.TechTree`
enum entry and a ribbon `TechTreeButtonPressed()` handler (mirroring `IntelButtonPressed`/
`EncyclopediaButtonPressed`) already exist in `GalaxyMenuUIController.cs`/`GalaxyUIStateManager.cs`
as a reserved placeholder — see the note at the end of §8.

## 7. Balance QA tooling

`TechTree_CommonBranches.csv` and `TechTree_FactionUnique.csv` (same folder as this doc) are the
source of truth for all 84 authored `TechDefSO`s (35 shared + 49 unique, i.e. 7 civs × 7 Branch-F
slots). An editor validator (`TechBalanceValidator`, same convention as the existing
"Ship Tech Setup Helper" tool) checks at edit time:
- every civ has exactly 1 `InnateFromStart` entry + 6 `Researched` entries in Branch F (§5);
- shared-branch tech counts match across civs (structurally guaranteed, but checked for missing
  `TechFlavorSO` overrides so no civ ships a shared tech under its raw internal name);
- per civ, plots cumulative unique-tech power score against `TechPoints` (0→900) and checks the
  **area under that curve** falls within a tunable band (default ±15%) of the other six civs' —
  the replacement for the old per-tier row check, since tier placement is now lore-driven (§3.4, §5)
  and no longer guaranteed to line up between civs.

## 8. Suggested phasing

1. **II.1 Data model** — enums, `TechDefSO`/`TechFlavorSO`, `CivData` fields (single `ActiveTechId` +
   `BankedTechPointsByTechId`, §6, revised), author all 84 `TechDefSO`s with placeholder numbers from
   §4/§5.
2. **II.2 Research flow** — `StartResearch`/progress tick/`CompleteResearch`/default-pick auto-start
   in `TechManager` (§2, §6, revised for the single-active-project model), reusing existing
   `TechPoints` income unchanged.
3. **II.3 Effect wiring** — hook `ApplyTechEffect` into `ShipStatCalculator`, `ShipMovementController`,
   `CombatOrderHelper`, `ResearchCenterData`, `StarSysData`; new `CloakingController` for
   Klingon/Romulan, new `TranswarpHubController` for Borg.
4. **II.4 UI** — `TechTreeMenuUI`, notification/HUD updates.
5. **II.5 Balance pass** — `TechBalanceValidator` tool + playtesting tuning of the 49 unique techs.

**UI stub already landed ahead of schedule:** `Menu.TechTree` (`GameEnums.cs`), the ribbon
`GalaxyMenuUIController.TechTreeButtonPressed()` handler, and matching `techTreeMenuView`/
`techTreeBackground` slots in `GalaxyMenuUIController`/`GalaxyUIStateManager` are already wired in
code — this only reserves the menu-system plumbing, it doesn't require II.1/II.2 to exist first. It
opens/closes an empty placeholder panel with no content. Two things still need doing **in the Unity
Editor**, not in code, before it's visible in-game: (1) add an actual ribbon Button GameObject next to
the existing Diplomacy/Intel/Encyclopedia buttons and point its OnClick() at
`GalaxyMenuUIController.TechTreeButtonPressed()`, the same way those buttons' OnClick()s are wired
directly in the Inspector today (not via code-side `AddListener`); (2) create a placeholder panel
GameObject (even just an empty panel with a "Tech Tree — Coming Soon" label) and assign it to
`GalaxyMenuUIController`'s new `Tech Tree Menu View` inspector field (`Tech Tree Background` is
optional). Swap the placeholder panel for the real `TechTreeMenuUI` once II.1–II.4 land — the enum,
button, and handler don't need to change again.

## 9. Decisions settled

- **UI**: node list (tabs per branch, rows per tier, lock/available/in-progress/completed states),
  not a visual node-graph — matches the existing Diplomacy/Intelligence menu conventions and is far
  cheaper to build.
- **Concurrency (revised):** only **one** research project active at a time, civ-wide, regardless of
  Research Center/University count (§2, §6). Building more centers still raises per-turn `TechPoints`
  income, which speeds up whichever single project is queued — it no longer unlocks a second
  concurrent project. A default project (Branch A Tier 1) auto-starts whenever the queue would
  otherwise be empty, and switching to a different tech never loses progress — each tech banks its
  own `TechPoints` independently and resumes where it left off when re-selected (§2).
- **Fog-of-war sight range**: driven by the highest Branch-D tier actually *researched*, not by raw
  banked `TechPoints` (§4, Branch D) — an active, delayable player choice instead of an automatic
  background stat.

## 10. Still open before coding starts

- Whether unique techs strictly require completing the same-tier shared techs first, or only the
  global threshold (simpler, recommended: threshold-only, no cross-branch prerequisites, to avoid
  soft-locking a civ out of its own signature ability by a slow shared branch pick).
