# Turn-Based Combat System - Implementation Guide

## Overview

Your combat system has been converted from real-time to **simultaneous turn-based** combat. This provides:
- ✅ Strategic depth (counter-picking orders)
- ✅ Clear cause/effect relationships
- ✅ Easy to understand and control
- ✅ Consistent with paused galaxy time
- ✅ Simpler to code and maintain

---

## How It Works

### Combat Flow

```
1. WARP-IN (Animated)
   ├─ Ships warp into combat positions
   └─ Camera frames both fleets

2. ORDER SELECTION (Paused)
   ├─ Player selects order from 5 choices
   ├─ AI simultaneously picks order
   └─ Both lock in choices

3. RESOLUTION (5-10 seconds)
   ├─ Ships move to order-based positions
   ├─ Damage calculated with tactical multipliers
   ├─ Visual effects (explosions, damage numbers)
   └─ Health bars update

4. RESULTS (3 seconds)
   ├─ Show damage dealt by each side
   ├─ Display tactical advantage/disadvantage
   └─ Show surviving ships

5. NEXT TURN or VICTORY
   └─ Repeat from step 2, or end if one side eliminated
```

---

## The Five Orders

### 1. **ENGAGE** (Balanced)
**What it does:** Ships attack in coordinated groups, focus fire on priority targets
**Strengths:** Counters Rush (prepared), Counters Retreat (pursuit)
**Weaknesses:** Formation (hard target), AttackTransports (divided)
**Damage Modifier:** +25% vs Rush/Retreat, -25% vs Formation/AttackTransports

### 2. **RUSH** (Aggressive)
**What it does:** All-out attack, maximize damage this turn
**Strengths:** Counters Formation (overwhelming), Counters Retreat (catch them)
**Weaknesses:** Engage (ambushed), AttackTransports (exposed flanks)
**Damage Modifier:** +25% vs Formation/Retreat, -25% vs Engage/AttackTransports

### 3. **RETREAT** (Escape)
**What it does:** Turn and warp out - vulnerable this turn, gone next
**Strengths:** Counters Formation (escape), Counters AttackTransports (out of position)
**Weaknesses:** Engage (cut off), Rush (pursued and destroyed)
**Special:** Deals NO damage this turn, combat ends if successful

### 4. **FORMATION** (Defensive)
**What it does:** Defensive wall formation, protects transports, reduces damage taken
**Strengths:** Counters Engage (prepared defense), Counters AttackTransports (blocks LOS)
**Weaknesses:** Rush (overwhelmed), Retreat (enemy escapes)
**Damage Modifier:** +25% vs Engage/AttackTransports, -25% vs Rush/Retreat, ALSO reduces incoming damage by 25%

### 5. **ATTACK TRANSPORTS** (Flanking)
**What it does:** Bypass combat ships, flank wide to hit transports directly
**Strengths:** Counters Engage (catches divided), Counters Rush (they miss transports)
**Weaknesses:** Formation (blocks access), Retreat (wasted effort)
**Special:** Only available if enemy has transports

---

## Key Files

### New Files Created

1. **`TurnBasedCombatResolver.cs`**
   - Core turn-based logic
   - Handles order selection, damage calculation, turn resolution
   - Location: `Assets/Script/Combat/`

2. **`TurnResultsUI.cs`**
   - Displays turn results on screen
   - Shows orders, damage, tactical feedback
   - Location: `Assets/Script/UI/`

3. **`CombatPhase` enum** (added to GameEnums.cs)
   - Tracks combat state: Warping → OrderSelection → Resolution → Results → Victory

### Modified Files

1. **`CombatController.cs`**
   - Added `UseTurnBasedCombat` toggle (default: true)
   - Added `TurnResolver` reference
   - Modified `Update()` and `LateUpdate()` to skip real-time logic
   - Warp-in completion starts turn-based combat

2. **`CombatUIManager.cs`**
   - Modified `EnterShipCombatPhase()` to submit orders to resolver
   - Player order selection now feeds turn-based system

3. **`GameEnums.cs`**
   - Added `CombatPhase` enum
   - Updated `CombatOrders` descriptions for turn-based

---

## Testing the System

### Quick Test (Console Only)

1. Start a combat with multiple ships on each side
2. Watch the console logs - you'll see:
   ```
   🎮 Turn-Based Combat Resolver initialized
   📋 Turn 1: Order Selection Phase
   🤖 AI Side 2 selected: Rush
   ✅ Side 1 locked in: Engage
   ⚔️ Turn 1: Resolving Engage vs Rush
   ⚔️ Multipliers: Side1=1.25x, Side2=0.75x
   💥 Damage: Side1 dealt 450, Side2 dealt 280
   📊 Turn 1 Results
   ```

### With UI (When Ready)

1. Create a simple UI panel in CombatScene with:
   - Turn number text
   - Both sides' order names
   - Damage dealt text
   - Continue button

2. Assign to `TurnResultsUI` component references

3. Results will display automatically between turns

---

## Toggling Between Systems

Want to test real-time combat again?

In **CombatController** inspector (or code):
```csharp
public bool UseTurnBasedCombat = false; // Change to false
```

This lets you A/B test both systems!

---

## Next Steps

### Immediate (System Works Now)
- [x] Core turn resolution
- [x] Order selection
- [x] Damage calculation
- [x] Tactical multipliers
- [x] AI order selection

### Polish (Recommended)
- [ ] Create proper UI panel for turn results
- [ ] Add animated damage numbers on ships
- [ ] Show "TACTICAL ADVANTAGE" banner
- [ ] Highlight destroyed ships before removal
- [ ] Add sound effects for each order
- [ ] Camera zoom/pan to action moments

### Advanced (Optional)
- [ ] Order history log (show last 3 turns)
- [ ] Detailed combat replay
- [ ] AI difficulty levels (affects order choices)
- [ ] Player can save/load order presets
- [ ] Multiplayer: both players pick simultaneously

---

## How Damage is Calculated

```csharp
For each ship on attacking side:
  baseDamage = (BeamDamage + TorpedoDamage)
  
  // Apply order matchup multiplier
  if (MyOrder counters TheirOrder)
    baseDamage *= 1.25
  else if (TheirOrder counters MyOrder)
    baseDamage *= 0.75
  
  // Apply formation defense bonus
  if (TheirOrder == Formation)
    baseDamage *= 0.75
  
  totalSideDamage += baseDamage

// Distribute total damage across enemy ships
damagePerShip = totalSideDamage / numberOfEnemyShips

// Apply to shields first, then hull
if (shieldHealth > 0)
  shieldHealth -= min(damagePerShip, shieldHealth)
  
remainingDamage = damagePerShip - shieldDamage
hullHealth -= remainingDamage

// Destroy if hull reaches 0
if (hullHealth <= 0)
  ship.Destroyed = true
```

---

## Customization

### Change Turn Duration
In `TurnBasedCombatResolver.cs`:
```csharp
public float ResolutionAnimationDuration = 5f; // Animation length
public float ResultsDisplayDuration = 3f;      // Results screen time
```

### Adjust Tactical Multipliers
In `CombatOrderHelper.cs`:
```csharp
private const float ADVANTAGE_MULTIPLIER = 1.25f;  // +25% damage
private const float DISADVANTAGE_MULTIPLIER = 0.75f; // -25% damage
```

### Change Order Matchups
Modify the matrix in `CombatOrderHelper.GetOrderMultiplier()`:
```csharp
case CombatOrders.Engage:
    if (targetOrder == CombatOrders.Rush) return ADVANTAGE_MULTIPLIER;
    // Add/change matchups here
```

---

## Troubleshooting

### "Combat doesn't start after warp-in"
Check console for:
- `🎮 Turn-Based Combat Resolver initialized`
- `📋 Turn 1: Order Selection Phase`

If missing, check `CombatController.UseTurnBasedCombat` is `true`

### "AI doesn't pick orders"
Check `IsAIControlled()` method - currently assumes Side 2 is AI.
Update for your player system.

### "No damage is dealt"
- Verify ships have BeamDamage/TorpedoDamage > 0
- Check console logs for multipliers and damage values
- Ensure ships aren't starting destroyed

### "Orders don't match descriptions"
The tactical matrix is in `CombatOrderHelper.cs` - you can customize any matchup!

---

## Design Philosophy

**Why Turn-Based?**
- Player commands *fleets*, not individual ships
- Strategic choices matter more than APM
- Clear feedback: "I chose X, enemy chose Y, I won/lost"
- Consistent with paused galaxy time
- Easier to balance and tune
- Less code complexity than real-time AI

**Why Simultaneous?**
- No turn-order advantage
- Both players make informed decisions
- Creates mind-game: "What will they pick?"
- Faster than alternating turns

**Why These 5 Orders?**
- Rock-Paper-Scissors-Lizard-Spock balance
- Each has clear counter and weakness
- Thematic and intuitive
- Covers all tactical situations (attack/defend/escape/special)

---

## Credits

This system draws inspiration from:
- **Master of Orion** (fleet orders, tactical combat)
- **FTL: Faster Than Light** (simultaneous decisions)
- **Total War** (formation-based tactics)
- **Into The Breach** (preview outcomes, clear counters)

Adapted for your 3D space combat with multiple ships per side!
