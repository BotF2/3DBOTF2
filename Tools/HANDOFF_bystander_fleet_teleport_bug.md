# Handoff: Bystander fleet "teleport" bug

**UPDATE 2026-08-07h: likely resolved — found by GitHub Copilot, verified present and correct by
Claude, pending a few more clean validation rounds given this bug's intermittent history.**

**Root cause:** `SceneController.HideAllFleets`/`ShowAllFleets` `SetActive`-cycle fleets on the
host/combatants during combat. That triggers `NetworkTransformReliable.OnEnable`/`OnDisable`, which
calls `base.OnEnable()` → `ResetState()`, zeroing that component's delta-compression baselines
(`lastSerializedPosition`, etc.) on the host. Bystander clients (Player 3) never `SetActive`-cycle
their own copies of those fleets — confirmed repeatedly in this investigation, bystanders never run
`HideAllFleets`/`ShowAllFleets` at all — so their own `lastDeserializedPosition` baseline stays at the
pre-combat value. The first delta-compressed position packet the server sends after combat ends is
computed against its just-zeroed (effectively absolute) baseline; the bystander then decodes it as
`stale_baseline + new_delta`, i.e. pre-combat-position **plus** current-position, summed — which reads
as exactly the "large negative Z" symptom, since two real negative-Z galaxy coordinates added together
produce a much larger negative number. This cleanly explains why it was bystander-only, why the
`ServerTeleport` fix attempted earlier made things *worse* (it raced an already-queued bad delta in the
same batch instead of fixing the baseline that produced it), and why no real disconnect was ever
involved.

**Fix:** `Assets/Mirror/Components/NetworkTransform/NetworkTransformReliable.cs` now overrides
`OnEnable()`/`OnDisable()` to save the delta-compression baselines, call `base.OnEnable()`/
`base.OnDisable()` (still correctly clears snapshot interpolation buffers), then restore the baselines
afterward.

**One loose end this doesn't obviously explain:** one earlier test captured Player 3's own
`LocalHumanPlayerController` (their own player object, not a fleet) going through a genuine
`ApplySpawnPayload`/`OnSpawn` call — spawn-message machinery, not position-delta machinery, and player
objects are never `SetActive`-cycled by combat in the first place. Doesn't contradict the fix being
right for the position symptom, but worth watching whether it recurs on future tests even with the
position bug gone.

**Secondary fix likely relevant to a separately-reported symptom:** `FleetManager.cs` now guards
against attaching a duplicate `csFogVisibilityAgent` to a fleet that already has one. A duplicate agent
with an unfiltered renderer list would force-enable a fleet's *real* insignia sprite whenever its tile
is fog-visible, regardless of contact status — matching a report of Player 3 seeing another civ's real
sprite through fog-of-war before any contact. Not explicitly re-tested in isolation yet.

**Everything below this point is the original, pre-fix investigation record — kept intact since it's
still useful context (what was ruled out and why, and the diagnostic instrumentation still live in the
code) until several more clean tests confirm this is fully resolved.**

---

**Status as of 2026-08-07 (pre-fix): unsolved, but heavily narrowed down.** This document is a complete,
self-contained summary of a long multiplayer debugging investigation — everything confirmed, everything
ruled out (with evidence, so it isn't re-chased), current diagnostic instrumentation left in the code,
and suggested next steps. Written so a fresh reader (human or AI) can pick this up without needing the
original conversation.

There's a parallel, more detailed technical log in Claude's own memory at
`project_bystander_fleet_duplication.md` and `project_turn_structure.md` if you're continuing with
Claude specifically — this document is the portable version.

---

## The bug

**Setup:** 3-player local-network multiplayer test. Host = Federation, Player 2 = Klingon, Player 3 =
Romulan. Federation and Klingon fight each other in combat; Romulan (Player 3) is not involved — a
bystander.

**Symptom:** After the Federation-vs-Klingon combat ends, **Player 3's client** shows all three
fleets — Federation's, Klingon's, and Romulan's own — relocated to a wrong position, generally
described as "large negative Z" / "off the map." The host and Player 2's windows are unaffected; both
show all fleets in their correct positions throughout.

- Confirmed via a debug hover-tooltip (reads `FleetData` directly, bypassing all UI/contact gating)
  that the relocated sprites really are Federation/Klingon/Romulan's real `FleetData` — not another
  civ mislabeled.
- Confirmed via camera rotation that the objects are genuinely rendering at the wrong position (not a
  hidden-renderer/visibility bug) — this is a real position corruption, not a rendering bug.
- Not reproducible without a full multiplayer test (3 separate windows/processes); can't be verified
  by reading code alone from this point.

---

## Confirmed facts (proven via diagnostics, not guessed)

1. **`NetworkServer.Spawn()` fires exactly once per fleet, ever**, at true game start
   (`FleetManager.BuildFirstFleetsNearSyst` → `InstantiateFleet`). Confirmed via a stack-trace
   diagnostic placed at Mirror's own internal `NetworkServer.SpawnObject` — the single choke point
   every public `Spawn(...)` overload funnels through, catching calls from project code *or* Mirror's
   own internals. There is no second spawn anywhere, for any fleet, in any test.
2. **The real netIds are known and match** (values are per-session, will differ next time): in one
   test, Federation Fleet 1 = netId 6, Romulan Fleet 1 = netId 8, Klingon Fleet 1 = netId 10.
3. **Player 3's client receives a fresh `SpawnMessage` for these exact same real netIds** — confirmed
   via `NetworkClient.FindOrSpawnObject` logging `keyExistedInSpawnedDict=False` for netId 6 (the real
   Federation Fleet 1 netId). Not a different object — the same one, being treated as never-before-seen.
4. **This is not fleet-specific — Player 3's own player object is affected too.** Player 3's own
   `LocalHumanPlayerController` (netId 5, `isOwned=True`) fired `OnPlayerCivChanged` again via
   `ApplySpawnPayload` (not a normal delta sync), with `oldCiv` resetting to the SyncVar's C# zero
   default (`FED`) before re-applying the real value (`ROM`). A player's own player object only ever
   spawns once, at connect. **This proves Player 3's entire Mirror client-side state — not just
   fleets — is being wiped and rebuilt from scratch, mid-game, using the original real netIds
   throughout.**
5. **Nothing is destroyed.** `FleetController.OnDestroy()` never fires on Player 3 during this event
   (diagnostic logs every destroy, for any cause; zero hits on Player 3 across multiple tests).
6. **No real network disconnect occurs.** `NetworkClient.OnTransportDisconnected()` — Mirror's one
   guaranteed entry point for *any* disconnect, transport-initiated or explicit — never fires
   (unconditional diagnostic placed before its own early-return guard). No "disconnect" / "reconnect"
   / "timeout" text has ever appeared anywhere in Player 3's console across many tests.

**The open question this leaves:** something is invalidating/clearing entries in Player 3's
`NetworkClient.spawned` dictionary (and causing full re-application of already-known SyncVar data),
without a destroy, without a disconnect, and without a domain reload. That mechanism has not been
found.

---

## Ruled out (with evidence — don't re-chase these)

| Theory | How it was ruled out |
|---|---|
| `ServerTeleport`-based position resync "fix" in `SceneController.ShowAllFleets` | Made it *worse* — displaced all fleets including the actual combatants, not just the bystander. Reverted. |
| Mirror's `NetworkServer.SpawnObjects()` (fires on any additive scene load, e.g. the raw `SceneManager.LoadSceneAsync` used for CombatScene) | Read the actual Mirror source: it only touches scene-*placed* NetworkIdentities (`Utils.IsSceneObject` requires `sceneId != 0`). Runtime-`Instantiate()`'d fleets always have `sceneId == 0`. Confirmed no-op. |
| Disconnect/reconnect blip on Player 3's connection | Checked console thoroughly, multiple times — no such messages ever found. |
| KCP transport timeout too short | Was 30000ms, suspiciously matching the game's 30-second combat-turn design. Bumped to 120000ms in `PersistentScene.unity`. Bug still reproduced identically. |
| The diagnostic logging itself (stack-trace extraction) causing a frame hitch that trips a false timeout | Stripped `ExtractStackTrace()` from all hot-path diagnostics. Bug still reproduced identically. |
| A second, real `NetworkServer.Spawn()` call somewhere in project code | Grepped the entire `Assets/Script` tree — only one call site exists (`FleetManager.InstantiateFleet`), and the choke-point diagnostic (see fact #1) confirms it only ever fires once per fleet. |
| "Duplicate" fleets are actually other (minor/AI) civs mislabeled | Built a debug hover-tooltip reading `FleetData` directly (bypasses contact-gating). Confirmed genuinely Federation/Klingon/Romulan in the test that mattered. *(Note: in one earlier, separate test, netIds 41-45 genuinely were other minor civs — XINDI/MALON/ARBAZAN/KRADIN/TAMARIANS — legitimately spawning very late on Player 3. That looks like an unrelated, likely benign, delayed-initial-sync issue — see "Also worth investigating" below.)* |
| Unity domain reload / Multiplayer Play Mode's known "additional player windows fail to reload domain in sync" bug ([Unity GitHub issue](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/2900)), triggered by live script edits during an active test session | Ruled out via a fully clean test: Editor rebooted, fresh 3-window session, **zero code edits made during the test**. Bug still reproduced identically. This is a real bug in the running game/network state, not an artifact of editing scripts mid-session. |

---

## Diagnostic instrumentation currently left in the codebase

All tagged with a distinct emoji prefix for easy console searching. None of these change behavior —
pure logging. Should be removed once root-caused (or could be kept longer-term as debug tooling if
useful).

- **`Assets/Mirror/Core/NetworkServer.cs`** — `SpawnObject()`: logs every spawn attempt, plus the
  actually-assigned netId. Tag: `🎯[ServerSpawnDiag]`.
- **`Assets/Mirror/Core/NetworkClient.cs`** — `FindOrSpawnObject()`: logs whenever a netId isn't found
  locally, including whether the dictionary key existed at all. Tag: `🔍[SpawnDupeDiag]`.
  `OnTransportDisconnected()`: logs unconditionally on every call. Tag: `💔[ClientDisconnectDiag]`.
- **`Assets/Script/Galaxy/Fleet/FleetController.cs`** —
  - `OnDestroy()`: logs on every fleet destroy, any cause. Tag: `🪦[FleetDestroyDiag]`.
  - `OnCivEnumChanged` hook: logs netId, `SyncedFleetInt`, `isFreshReconstruction`, and position on
    every fire.
  - **Debug-only hover tooltip** (`OnMouseEnter`/`OnMouseExit`/`OnGUI`): shows real `CivEnum`/
    `FleetName` on hover, regardless of contact status. Self-contained (uses `OnGUI`, touches no real
    UI). Marked `REMOVE BEFORE SHIPPING` in its comment — **not a real gameplay feature**, do not ship
    this.
- **`Assets/Script/Galaxy/Fleet/FleetManager.cs`** — `InstantiateFleet()`: logs the one spawn call.
- **`Assets/Script/Core/SceneController.cs`** — `HideAllFleets()`/`ShowAllFleets()`: logs each fleet's
  position before/after. Tag: `[FleetHideShowDiag]`.

There is also a helper script, **`Tools/close-test-instances.ps1`**, that closes leftover
Player-2/Player-3-titled Unity processes from a previous test session (useful if you hit "Only one
usage of each socket address" trying to host again).

---

## Suggested next steps

1. **Test without combat at all.** Let a game run through a couple of full turns
   (`InterTurn → TurnProgression → InterTurn`) with zero encounters. If the bystander's fleets still
   get relocated at some point, this was never actually about combat specifically — just about enough
   time/turns passing — which is a very different and more useful lead.
2. If it turns out to require combat specifically: instrument `CombatController.EndCombat`/
   `EndCombatCleanup` and `TurnBasedCombatResolver.ShowVictoryScreen` for anything that might touch
   `NetworkClient`'s internal state indirectly. Nothing suspicious found there so far, but it hasn't
   been exhaustively logged line-by-line the way the spawn/destroy/disconnect paths have.
3. Consider instrumenting Mirror's own snapshot/interpolation-buffer internals directly
   (`NetworkTransformReliable.ResetState()`/`RewriteHistory()`, or whatever manages
   `NetworkClient`'s snapshot buffers) for anything that could explain a partial state wipe that
   doesn't go through `OnTransportDisconnected`.
4. This symptom description — *"a client's own player object and several other already-tracked
   objects all get a fresh `ApplySpawnPayload` using their true original netIds, with
   `OnTransportDisconnected` never firing"* — is specific enough that it's worth searching/posting to
   the Mirror Networking Discord or GitHub issues. Someone familiar with Mirror's internals may
   recognize it immediately as a known edge case.

## Also worth investigating (separate, likely-unrelated issue)

In one test, netIds 41-45 on Player 3 were conclusively confirmed (via the host's own
`ServerSpawnDiag` netId-assignment log) to be other minor/AI civs — XINDI, MALON, ARBAZAN, KRADIN,
TAMARIANS — spawning on Player 3's client very late, well after game start, rather than immediately at
connect like they should. This looks like a real but separate throughput/delivery-delay issue specific
to Player 3's connection for lower-priority objects, not directly related to the main "teleport" bug —
but hasn't been independently investigated.

---

## Other bugs found and FIXED during this same investigation (confirmed working — not part of the open mystery)

These were all correctly diagnosed and fixed along the way. Listed here so they aren't confused with
the still-open bug above, and so their fixes aren't accidentally reverted.

1. **Dropline missing for a client's own fleet on first connect** — race condition where
   `FleetManager.SetUpDropLine`'s readiness check could fail once with no retry. Fixed:
   `FleetController.Update()` now self-heals by retrying while `DropLine` is null.
2. **Drag-and-drop target marker not billboarding** — had the wrong (combat-only, self-disabling)
   `BillboardCameraCombat` component. Fixed: swapped to the correct `Billboard` component.
3. **Orphaned `ServerPlayerTargetMarker_*` GameObjects** never cleaned up except on fleet death. Fixed:
   added `ServerClearPlayerTargetMarker()`, called from both cancellation paths.
4. **Combat never closing ("GameOverCanvas not found")** — a recent commit renamed the scene object to
   `CombatOverCanvas` but `CombatSceneLoader`'s lookup string was never updated. Fixed.
5. **Insignia reveal on contact was one-directional** — only the fleet that was actively "targeting"
   the other as its destination revealed the other side's sprite. Fixed by moving the reveal outside
   that gate so it fires on any physical overlap between enemy fleets, regardless of who initiated.
6. **Premature insignia reveal before any real contact** — `LocalHumanPlayerController.playerCiv`'s
   SyncVar defaults to `CivEnum.FED`, and the player's controller object is assigned well before their
   real civ choice syncs in, creating a race window where a connecting player could be briefly
   misidentified as Federation. Fixed: added an explicit `civConfirmed` SyncVar (set only in
   `CmdSetPlayerCiv`) and gated `FleetController.IsReadyToHandleCivChange` on it.
7. **"Already hosting or connected" after Singleplayer → Previous → Multiplayer** —
   `MainMenuUIController.ReturnToLobbyMenu()` (the actual handler behind the scene's "Previous"
   button — note `PreviousButton()` in the same file is dead code, not wired to anything) never
   stopped a Mirror host/client session that `SetSinglePlayer()` had silently started. Fixed: added
   the same network-teardown logic `CancelButton` already had.
8. **Singleplayer/Multiplayer button text reverting** — a `LocalizeStringEvent` component was
   overwriting the TMP field at runtime from the localization string table, not the other way around.
   Fixed by updating the actual English string table entries directly.

## Deferred by explicit user request (not a mystery — just not done yet)

9. **`TimeManager`'s turn-progression coroutine doesn't pause during combat** — causes AI economy
   processing to try to `StartCoroutine` on GalaxyScene objects deactivated for the combat scene's
   duration ("Coroutine couldn't be started because inactive" / "First item in queue is NULL" errors
   for various star systems). Agreed design direction (not yet implemented): non-combat systems/civs
   should keep progressing normally each turn; only the actual combat participants' turns should
   pause, catching up automatically afterward without needing to click Advance Turn again. Full
   details in Claude's `project_turn_structure` memory.
