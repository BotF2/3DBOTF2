# Copilot Instructions

## General Guidelines
- When committing ship deploy, wait two frames (yield return null twice) before normalizing UI ownership and rebuilding lists.

## Combat Orders
- Combat orders should NOT use speed multipliers. Ships have a max speed and operate at or below it.
- **Rush Order**: Combat ships rush at their own max speed (advantage vs Retreat, disadvantage vs Formation due to concentrated fire). Vulnerable to Target Transports as it doesn't protect transports.
- **Retreat Order**: Ships turn around first, then warp out (vulnerable during turn).
- **Formation Order**: Maintains formation with overlapping fire (counters Rush by focusing lead ship). Protects transports by positioning between firing ships and targets.
- **Target Transports Order**: Sends ships to flank around blocking ships to hit transports at closer range.

## Combat Weapon Mechanics
- Beam weapons have reduced damage at longer distances.
- Torpedoes have reduced accuracy at higher relative target velocities (faster relative movement = more likely to miss, up to complete miss).

## Ship Rotation Mechanics
- In `CombatController.SetupSingleShip`, the correct Y-axis rotation for ships is:
  - Side 1 (facing +X right): `Quaternion.Euler(0, -90, 0)`
  - Side 2 (facing -X left): `Quaternion.Euler(0, 90, 0)`
