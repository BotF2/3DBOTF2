# Combat Timing & Movement Guide

## Combat Duration (2-Phase System)

### Total Time: ~60 seconds per combat

**Turn 1:**
- Order Selection: 15 seconds
- Resolution (combat): 10 seconds
- Results: 2 seconds
- **Subtotal: 27 seconds**

**Turn 2:**
- Order Selection: 15 seconds
- Resolution (combat): 10 seconds
- Results: 2 seconds
- **Subtotal: 27 seconds**

**Victory Screen:** 3 seconds

**Total: ~57 seconds** ✅

---

## Movement System: Simplified Delta-V Physics

### Speed Calculation
```
Base Speed = ShipData.maxWarpFactor × 3
Actual Speed = Base Speed × Order Speed Factor
```

**Speed Factors by Order:**
- Rush: 1.5x (fastest)
- Engage: 1.0x (normal)
- Retreat: 1.2x (fast escape)
- Formation: 0.65x (defensive)
- AttackTransports: 1.0x (flanking)

### Acceleration
- Ships accelerate to max speed in **0.5 seconds**
- Acceleration = MaxSpeed × 2
- Fast ships (high maxWarpFactor) accelerate faster

### Movement Behavior

**When Facing Target (<30° angle):**
- ✅ Full acceleration
- ✅ Reach max speed quickly
- Ships charge toward targets

**When Turning (>30° angle):**
- ⚠️ Coast (no acceleration)
- ⚠️ Speed reduces by 5% per frame
- Simulates momentum in vacuum

**Formation & Small Adjustments:**
- Distances < 50 units = instant positioning
- No physics simulation needed
- Ships "snap" to formation slots

### Rotation
- Ships smoothly rotate toward movement direction
- Rotation speed: 5 rad/s
- No rotation during formation holding

---

## Visual Results

With **3x speed multiplier** and **10-second resolution**:

**Rush Order:**
- Ships accelerate rapidly
- Cross ~600-800 units in 10 seconds
- Visible charge toward enemy

**Engage Order:**
- Groups move together
- Cross ~400-600 units
- Coordinated approach

**Formation Order:**
- Minimal movement
- Quick adjustments to wall positions
- Focus on blocking LOS

**Attack Transports:**
- Wide flanking arcs
- Ships swing around enemy lines
- Visible bypass maneuver

**Retreat Order:**
- 180° turn animation
- Ships accelerate away
- Exit screen quickly

---

## Why These Timings?

### 15s Order Selection
- Long enough to think strategically
- Short enough to keep pace
- Standard for turn-based space games

### 10s Resolution
- Enough time to see tactics play out
- Ships move meaningful distances
- Weapon fire is visible and impactful
- Not so long that player gets bored

### 2s Results
- Quick recap of damage
- See who won the exchange
- Move to next turn quickly

### Total ~60s
- ✅ Under 1 minute = feels quick
- ✅ Long enough for 2 strategic decisions
- ✅ Short enough for multiplayer (other players wait)
- ✅ Perfect for AI-only auto-resolve

---

## Adjusting Timing (If Needed)

### Make Combat Faster (30-40s total)
```csharp
// In TurnBasedCombatResolver.cs
ResolutionAnimationDuration = 6f;  // 6s instead of 10s
ResultsDisplayDuration = 1f;       // 1s instead of 2s

// In CombatUIManager.cs
remainingTime = 10f;               // 10s instead of 15s
```
**New Total:** ~34 seconds

### Make Combat Slower (more tactical, 90s total)
```csharp
ResolutionAnimationDuration = 15f; // 15s resolution
ResultsDisplayDuration = 3f;       // 3s results
remainingTime = 20f;               // 20s order selection
```
**New Total:** ~76 seconds

### Make Ships Faster (more action)
```csharp
// In CombatController.MoveShipBasedOnOrder()
float baseSpeed = ship.ShipData.maxWarpFactor * 5f; // 5x instead of 3x
```

### Make Ships Slower (more tactical)
```csharp
float baseSpeed = ship.ShipData.maxWarpFactor * 2f; // 2x instead of 3x
```

---

## AI-Only Combat

For AI vs AI (no human player):
```csharp
// Skip order selection UI
// Auto-select orders instantly
// Use same 10s resolution (for visuals if spectating)
// OR: Fast-forward with 1s resolution

Total Time: 2-12 seconds
```

---

## Comparison to Other Games

| Game | Combat Duration | Style |
|------|----------------|-------|
| **Your Game** | **~60s** | **Turn-based with animation** |
| FTL | 30-90s | Real-time with pause |
| Into the Breach | 60-120s | Pure turn-based |
| XCOM | 120-300s | Turn-based tactical |
| Stellaris | Auto-resolve instant | Pure stats |

You're in the sweet spot: strategic depth + quick resolution!

---

## Physics Implementation

### What We Simulate:
- ✅ Acceleration (ships speed up)
- ✅ Momentum/Coasting (ships keep moving while turning)
- ✅ Velocity (stored per-ship)
- ✅ Drag while turning (5% per frame)

### What We Skip:
- ❌ Full Newtonian physics
- ❌ Thrust vectors
- ❌ Orbital mechanics
- ❌ Relativity
- ❌ Fuel consumption

**Why?**
Real physics would require:
- Complex vector math
- Multiple simulation steps
- Hard-to-predict outcomes
- Unintuitive controls

Our simplified system gives:
- ✅ Looks realistic enough
- ✅ Easy to understand
- ✅ Predictable for strategy
- ✅ Fast to compute

---

## Testing Timing

Run a few combats and time them:

**Too Fast (<40s):**
- Feels rushed
- Can't appreciate tactics
- Increase ResolutionAnimationDuration

**Too Slow (>90s):**
- Player gets bored
- Other players wait too long
- Decrease order selection time

**Just Right (50-70s):**
- Engaging throughout
- See tactics unfold
- Quick enough for multiplayer
- Current settings ✅
