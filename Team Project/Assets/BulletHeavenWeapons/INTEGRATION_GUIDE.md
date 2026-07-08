# Bullet-Heaven Weapons Pack — Integration Guide

## Overview

This package delivers five ready-to-use weapon prefabs for a 3D top-down bullet-heaven game, built around a single shared `WeaponDamage.cs` script. Each weapon uses a **SphereCollider set to Is Trigger** to detect enemies automatically. 

**Damage is now percentage-based:** Weapons multiply the player's base damage (defined in `PlayerStats.cs`) rather than dealing fixed flat damage. This allows you to scale enemy HP and player power infinitely without rewriting weapon logic.

---

## Package Contents

```
Assets/BulletHeavenWeapons/
├── Scripts/
│   ├── WeaponDamage.cs        ← Core damage logic (attach to every weapon)
│   ├── PlayerStats.cs         ← Base damage stat (attach to Player)
│   ├── IDamageable.cs         ← Interface for enemy health scripts
│   └── WeaponRotator.cs       ← Optional orbital rotation utility
├── Prefabs/
│   ├── StormBlade.prefab      ← Close range melee
│   ├── PoisonCloud.prefab     ← Close range AOE DOT
│   ├── VoidLance.prefab       ← Medium range single target
│   ├── SolarJavelin.prefab    ← Long range piercing
│   └── NovaBurst.prefab       ← Ranged zone AOE
└── Effects/
    ├── StormBlade_Effect.mat
    ├── PoisonCloud_Effect.mat
    ├── VoidLance_Effect.mat
    ├── SolarJavelin_Effect.mat
    └── NovaBurst_Effect.mat
```

---

## Quick Start

### Step 1 — Import the Package
Unzip `BulletHeavenWeapons.zip` and drag the `BulletHeavenWeapons` folder into your Unity project's `Assets` directory.

### Step 2 — Tag Your Enemies
The `WeaponDamage` script only detects GameObjects tagged **"Enemy"**. In the Unity Editor, select your enemy prefabs and set their Tag to `Enemy`.

### Step 3 — Set Up Enemy Health Interface
For weapons to damage your enemies, your enemy health script must implement the `IDamageable` interface included in this package.

Example:
```csharp
using UnityEngine;
using BulletHeavenWeapons;

public class MyEnemyHealth : MonoBehaviour, IDamageable
{
    public float health = 100f;

    // Required by IDamageable
    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0) Die();
    }
    
    void Die() { Destroy(gameObject); }
}
```

### Step 4 — Set Up Player Stats
Attach the `PlayerStats.cs` component to your **Player GameObject**. Set the `Base Damage` field in the inspector (e.g., `10`). Weapons will multiply this base damage by their specific multiplier.

### Step 5 — Attach Weapons
For each weapon you want to equip, drag the corresponding prefab from `Prefabs/` into the scene as a **child of your Player GameObject**. Ensure its local position is `(0, 0, 0)`.

---

## Weapon Reference

| Weapon | Type | Damage Multiplier | Cooldown | Detection Radius | Theme |
|---|---|---|---|---|---|
| **Storm Blade** | Close Melee | 3.5x | 0.6 s | 1.5 u | Cyan / White |
| **Poison Cloud** | Close AOE DOT | 0.5x / tick | 4.0 s | 2.5 u | Green / Yellow |
| **Void Lance** | Med Single Target | 7.5x | 0.4 s | 5.0 u | Purple / Violet |
| **Solar Javelin** | Long Piercing | 4.0x | 1.5 s | 9.0 u | Orange / Gold |
| **Nova Burst** | Ranged Zone AOE | 12.0x | 8.0 s | 20.0 u (max dist) | White / Ice Blue |

---

## Weapon Design Details

### Storm Blade — Close Range Melee
A crackling electric blade that discharges on contact. It uses the `SingleTarget` targeting type. It strikes the closest enemy inside its 1.5-unit radius. The 3.5x multiplier provides strong burst damage for aggressive play.

### Poison Cloud — Close Range AOE DOT
A toxic miasma that surrounds the player. Uses the `DOT` targeting type. Every enemy inside the 2.5-unit radius receives a 0.5x damage tick every 0.3 seconds. The 4-second cooldown dictates how often the cloud is "recast", but the DOT applies continuously while active.

### Void Lance — Medium Range Single Target
A focused beam of void energy. At a 7.5x multiplier and 0.4s cooldown, this is the highest single-target DPS weapon. Its 5-unit radius allows the player to snipe tough targets before they close in.

### Solar Javelin — Long Range Piercing
A blazing lance of solar energy. Uses the `Piercing` targeting type. It fires once per cooldown cycle and applies a 4.0x damage hit to **all enemies currently inside the 9-unit radius simultaneously**.

### Nova Burst — Ranged Zone AOE
A massive localized explosion. It uses the new `RangedZone` targeting type. The weapon detects enemies up to 20 units away. When off cooldown, it picks a cluster of enemies, spawns a damage zone (8-unit radius) at that location, and hits all enemies in that zone for a massive 12.0x damage multiplier. It does not hit the entire screen, but rather a dense cluster far away from the player.

---

## Script Architecture

### `WeaponDamage.cs`
This script tracks enemies inside its `SphereCollider` using a `HashSet<Collider>`. When the cooldown is ready, it evaluates the `Targeting Type` enum to determine how to apply damage.
- `SingleTarget`: Finds the closest enemy in the set and damages them.
- `Piercing / AOE`: Damages all enemies currently in the set.
- `DOT`: Applies damage every `dotTickInterval` to enemies in the set.
- `RangedZone`: Picks an enemy from the set, performs a `Physics.OverlapSphere` at that enemy's position using `zoneRadius`, and damages everything caught in the blast.

### `PlayerStats.cs`
A simple data container attached to the player. `WeaponDamage` searches its parent hierarchy for this component on `Start()`. If found, it calculates final damage as `playerStats.baseDamage * damageMultiplier`. If not found, it defaults to a base of 10.
