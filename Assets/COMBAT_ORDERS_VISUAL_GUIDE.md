# Combat Orders - Visual Behavior Guide

## Speed: 10x Multiplier

All ships now move at **10x their maxWarpFactor** for fast, exciting combat.

---

## Weapon Firing

**ONE Torpedo Salvo** → Then **Beams Only**

- First shot of combat: Torpedoes (if ship has them)
- All subsequent shots: Beam weapons only
- Creates visual variety without spam

---

## Order Behaviors (Visual Breakdown)

### 1. **RUSH** - All-Out Attack

**What You See:**
- All ships charge forward at **FULL SPEED**
- Each ship uses its own max speed (no formation)
- Ships **pass through** enemy lines if momentum carries them
- Ships turn while flying by, firing at targets
- Fast ships (scouts) reach enemy first
- Slow ships (cruisers) follow behind

**Speed Factor:** 1.5x (150% of base)

**Tactics:**
- Aggressive, chaotic attack
- Ships may fly past each other
- Turning happens WHILE moving (delta-v)
- Good for overwhelming enemy

---

### 2. **ENGAGE** - Coordinated Attack

**What You See:**
- Ships move in groups of 2-3
- Groups advance together (slowest ship sets pace)
- Coordinated focus fire on targets
- Organized, military-style advance

**Speed Factor:** 1.0x (100% of base, limited by group)

**Tactics:**
- Balanced approach
- Groups target same enemy
- Slower but more controlled

---

### 3. **ATTACK TRANSPORTS** - Flanking Strike

**What You See:**

**Scouts & Destroyers (Flanking Ships):**
1. **At Warp-In End:** Rotate 40° off-center
   - Ships above center line → rotate UP
   - Ships below center line → rotate DOWN
2. **Immediate acceleration** when weapons start firing
3. **Flank wide around enemy wall** at max speed
4. **Target enemy transports** directly
5. Bypass enemy combat ships

**Other Ships (Cruisers, Heavy Cruisers):**
- Move forward at **HALF SPEED**
- Fire on enemy wall ships
- Distract enemy while flankers work

**Speed Factors:**
- Flanking ships: 1.5x (fast)
- Wall ships: 0.5x (slow, half speed)

**Visual Result:**
- Two-pronged attack
- Fast ships swoop around flanks
- Slow ships pin enemy in place
- Exciting multi-directional combat

---

### 4. **FORMATION** - Defensive Wall

**What You See:**
- Ships form tight wall in YZ plane
- Combat ships in front
- Transports behind (protected)
- Minimal forward movement
- Ships hold positions, fire from wall

**Speed Factor:** 0.65x (slow, defensive)

**Tactics:**
- Block line-of-sight to transports
- Ships can intercept fire
- Reduced incoming damage (25%)

---

### 5. **RETREAT** - Escape

**What You See:**
1. Ships rotate **100°** (2.5 seconds) - left/right OR up/down
   - 50% chance: Y-axis turn (left or right)
   - 50% chance: X-axis turn (pitch up or down)
   - Random direction within chosen axis
2. Ships are **vulnerable while turning** (can take damage)
3. After turn complete: **warp-out animation begins**
   - Ships become **invulnerable** (no damage)
   - Acceleration: 0 → 40x max warp speed
   - Ship model stretches along travel direction (1x → 50x)
   - Warp-out duration: 1.5 seconds
4. Ships disappear (escaped successfully)

**Speed Factor:** 0.0x while turning, then 40x during warp-out

**Tactics:**
- High risk: vulnerable during 2.5s turn
- Safe escape: invulnerable during warp-out
- Use when losing badly
- Saves remaining ships
- Varied turning adds visual variety

---

## Movement Physics (Simplified Delta-V)

### Acceleration
- Ships reach max speed in **0.5 seconds**
- Fast ships accelerate faster

### Coasting
- When turning >30° off target direction:
  - Ships **coast** (maintain velocity)
  - No acceleration
  - Speed reduces 5% per frame (drag)

### Small Adjustments
- Formation positioning <50 units: **instant**
- No physics needed for minor corrections

### Collision Avoidance
- Ships in **Rush** avoid obvious collisions
- Ships can **pass through** enemy formations
- Turn while passing to maintain fire

---

## Timing Per Turn (2-Phase Combat)

```
Order Selection: 15 seconds
Resolution: 10 seconds combat
Results: 2 seconds

Total per turn: 27 seconds
Total combat: ~60 seconds (2 turns)
```

---

## What Each Order LOOKS Like

### Rush
```
Side 1 →→→→→ ←←←←← Side 2
    ╲    ╳    ╱
     ╲  ╳ ╱
      ╲╳╱
     Chaos!
```
Ships fly past each other, turn while moving

### Engage
```
Side 1  →  →  →    ←  ←  ←  Side 2
      [Group]    [Group]
```
Organized groups advance together

### Attack Transports
```
         ╱ Scouts flank up
Side 1  → Cruisers forward  ← Enemy Wall
         ╲ Destroyers flank down
                               ← Transports (targeted!)
```
Multi-directional pincer

### Formation
```
Side 1          Side 2
  ║               ║
  ║  Combat      ║  Combat
  ║  Ships       ║  Ships
  ║               ║
  ≈ Transports    ≈ Transports
```
Defensive walls, minimal movement

### Retreat
```
Side 1 (losing)
  ↻ Turn 100° (left/right/up/down)
  → Accelerate + stretch
  → Warp out (invulnerable)
```
Escape animation with Star Trek warp stretch

---

## Speed Comparison

| Order | Scouts/Destroyers | Cruisers | Transports |
|-------|------------------|----------|------------|
| **Rush** | 15x maxWarp | 15x maxWarp | 6x maxWarp |
| **Engage** | 10x maxWarp | 10x maxWarp | 4x maxWarp |
| **Attack Transports** | 15x maxWarp (flank) | 5x maxWarp (wall) | N/A |
| **Formation** | 6.5x maxWarp | 6.5x maxWarp | 4x maxWarp |
| **Retreat** | 0x (turning), 40x (warp-out) | 0x (turning), 40x (warp-out) | 0x (turning), 40x (warp-out) |

Base calculation: `maxWarpFactor × 10 × order_speed_factor`

---

## Why These Changes?

### 10x Speed
- **Before:** Ships barely moved in 10 seconds
- **Now:** Ships cross significant distances
- Combat feels dynamic and exciting

### One Torpedo Salvo
- **Before:** Torpedo spam cluttered screen
- **Now:** Initial torpedo volley, then beams
- Clean visual, easier to follow

### Flanking on AttackTransports
- **Before:** All ships did same thing
- **Now:** Scouts/destroyers flank, others distract
- Matches order name and tactics
- Visually interesting

### Rush Pass-Through
- **Before:** Ships stopped when reaching enemy
- **Now:** Ships fly through, turn while passing
- Realistic space combat (momentum)
- Exciting dogfight feel

### Retreat Warp-Out
- **Before:** Ships just rotated 180° and disappeared
- **Now:** 100° turn (varied directions), then warp stretch
- Invulnerable during warp-out (after turn)
- 40x speed acceleration with visual stretch
- Matches warp-in animation style

---

## Testing Checklist

Run each order and verify:

**Rush:**
- [ ] Ships charge at different speeds
- [ ] Fast ships arrive first
- [ ] Ships turn while moving past enemies
- [ ] Looks chaotic and aggressive

**Engage:**
- [ ] Ships move in visible groups
- [ ] Groups advance together
- [ ] Looks organized

**Attack Transports:**
- [ ] Scouts/destroyers rotate 40° at start
- [ ] Flanking ships accelerate quickly
- [ ] Flanking ships swing wide
- [ ] Wall ships move forward slowly
- [ ] Looks like pincer attack

**Formation:**
- [ ] Ships hold wall position
- [ ] Transports behind combat ships
- [ ] Minimal movement
- [ ] Looks defensive

**Retreat:**
- [ ] Ships rotate 100° (randomly left/right or up/down)
- [ ] Turn takes 2.5 seconds (vulnerable)
- [ ] Warp-out animation: acceleration + stretching
- [ ] Ships become invulnerable during warp-out
- [ ] Ships accelerate to 40x max warp speed
- [ ] Ship models stretch 1x → 50x along travel direction
- [ ] Looks like Star Trek warp escape

---

## Performance Notes

- **10x speed** = ships move ~1000-2000 units in 10 seconds
- Visible, exciting combat
- Not too fast to follow
- Completes in 2 turns (~60 seconds)
