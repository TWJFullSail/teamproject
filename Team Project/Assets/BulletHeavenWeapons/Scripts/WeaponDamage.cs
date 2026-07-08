using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BulletHeavenWeapons
{
    public enum WeaponTargetingType
    {
        SingleTarget, // Hits one enemy
        Piercing,     // Hits all enemies in a line/area
        AOE,          // Hits all enemies in radius
        DOT,          // Damage over time in radius
        RangedZone    // Spawns a zone at a distant location to hit enemies there
    }

    [RequireComponent(typeof(SphereCollider))]
    public class WeaponDamage : MonoBehaviour
    {
        [Header("Weapon Settings")]
        public string weaponName = "New Weapon";
        
        [Tooltip("Percentage multiplier of the player's base damage (e.g. 1.5 = 150% of base damage)")]
        public float damageMultiplier = 1.0f;
        
        public float cooldown = 1f;
        public WeaponTargetingType targetingType = WeaponTargetingType.SingleTarget;
        
        [Header("DOT Settings (Only used if DOT)")]
        public float dotTickInterval = 0.5f;

        [Header("Ranged Zone Settings")]
        [Tooltip("How far away the zone can spawn (for RangedZone type)")]
        public float maxSpawnDistance = 15f;
        [Tooltip("The radius of the damage zone itself")]
        public float zoneRadius = 5f;

        [Header("Visual Effects")]
        public ParticleSystem weaponEffect;
        public Color effectColor = Color.white;
        
        [Header("References")]
        public SphereCollider triggerCollider;

        private PlayerStats playerStats;
        private float nextAttackTime = 0f;
        
        // Track enemies currently inside the collider
        private HashSet<Collider> enemiesInRange = new HashSet<Collider>();
        private Dictionary<Collider, float> enemyDotTimers = new Dictionary<Collider, float>();
        
        private void Reset()
        {
            triggerCollider = GetComponent<SphereCollider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void Start()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<SphereCollider>();
            }
            triggerCollider.isTrigger = true;

            if (targetingType == WeaponTargetingType.RangedZone)
            {
                // Ranged zone weapons use the collider as the detection range, not the damage zone
                triggerCollider.radius = maxSpawnDistance;
            }

            // Find player stats in parent hierarchy
            playerStats = GetComponentInParent<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogWarning($"[{weaponName}] No PlayerStats found in parent hierarchy! Damage will default to base 10 * multiplier.");
            }

            // Apply color to particle system if assigned
            if (weaponEffect != null)
            {
                var main = weaponEffect.main;
                main.startColor = effectColor;
            }
        }

        private void Update()
        {
            // Clean up any destroyed enemies from our tracking set
            enemiesInRange.RemoveWhere(c => c == null);

            if (Time.time >= nextAttackTime && enemiesInRange.Count > 0)
            {
                if (targetingType == WeaponTargetingType.RangedZone)
                {
                    ExecuteRangedZoneAttack();
                }
                else if (targetingType == WeaponTargetingType.AOE || targetingType == WeaponTargetingType.Piercing)
                {
                    ExecuteAOEAttack();
                }
                // Single target is handled in OnTriggerEnter or Update if an enemy is still in range
                else if (targetingType == WeaponTargetingType.SingleTarget)
                {
                    ExecuteSingleTargetAttack();
                }
            }
        }

        private float CalculateFinalDamage()
        {
            float baseDmg = (playerStats != null) ? playerStats.baseDamage : 10f;
            return baseDmg * damageMultiplier;
        }

        private void ExecuteSingleTargetAttack()
        {
            // Find closest enemy
            Collider closest = null;
            float minD = float.MaxValue;
            foreach (var enemy in enemiesInRange)
            {
                float d = Vector3.Distance(transform.position, enemy.transform.position);
                if (d < minD)
                {
                    minD = d;
                    closest = enemy;
                }
            }

            if (closest != null)
            {
                IDamageable dmg = closest.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    dmg.TakeDamage(CalculateFinalDamage());
                    PlayEffect(closest.transform.position);
                    nextAttackTime = Time.time + cooldown;
                }
            }
        }

        private void ExecuteAOEAttack()
        {
            bool hitAny = false;
            foreach (var enemy in enemiesInRange)
            {
                IDamageable dmg = enemy.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    dmg.TakeDamage(CalculateFinalDamage());
                    hitAny = true;
                }
            }

            if (hitAny)
            {
                PlayEffect(transform.position); // Play effect at player/weapon origin
                nextAttackTime = Time.time + cooldown;
            }
        }

        private void ExecuteRangedZoneAttack()
        {
            // Find a cluster. For simplicity, pick a random enemy as the center of the cluster.
            Collider targetCenter = null;
            foreach (var enemy in enemiesInRange)
            {
                targetCenter = enemy;
                break; // Just grab the first one
            }

            if (targetCenter != null)
            {
                Vector3 targetPos = targetCenter.transform.position;
                
                // Deal damage to all enemies near that target position
                Collider[] hits = Physics.OverlapSphere(targetPos, zoneRadius);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Enemy"))
                    {
                        IDamageable dmg = hit.GetComponent<IDamageable>();
                        if (dmg != null)
                        {
                            dmg.TakeDamage(CalculateFinalDamage());
                        }
                    }
                }

                PlayEffect(targetPos);
                nextAttackTime = Time.time + cooldown;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Enemy")) return;

            enemiesInRange.Add(other);

            if (targetingType == WeaponTargetingType.DOT)
            {
                if (!enemyDotTimers.ContainsKey(other))
                {
                    IDamageable enemy = other.GetComponent<IDamageable>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(CalculateFinalDamage());
                        PlayEffect(other.transform.position);
                        enemyDotTimers.Add(other, Time.time + dotTickInterval);
                    }
                }
            }
            // For SingleTarget, if it's off cooldown, we can hit them immediately as they enter
            else if (targetingType == WeaponTargetingType.SingleTarget && Time.time >= nextAttackTime)
            {
                IDamageable enemy = other.GetComponent<IDamageable>();
                if (enemy != null)
                {
                    enemy.TakeDamage(CalculateFinalDamage());
                    PlayEffect(other.transform.position);
                    nextAttackTime = Time.time + cooldown;
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Enemy") || targetingType != WeaponTargetingType.DOT) return;

            if (enemyDotTimers.TryGetValue(other, out float nextTickTime))
            {
                if (Time.time >= nextTickTime)
                {
                    IDamageable enemy = other.GetComponent<IDamageable>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(CalculateFinalDamage());
                        PlayEffect(other.transform.position);
                        enemyDotTimers[other] = Time.time + dotTickInterval;
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Enemy")) return;

            enemiesInRange.Remove(other);

            if (targetingType == WeaponTargetingType.DOT)
            {
                if (enemyDotTimers.ContainsKey(other))
                {
                    enemyDotTimers.Remove(other);
                }
            }
        }

        private void PlayEffect(Vector3 targetPosition)
        {
            if (weaponEffect != null)
            {
                if (targetingType == WeaponTargetingType.AOE || targetingType == WeaponTargetingType.DOT)
                {
                    weaponEffect.Play();
                }
                else
                {
                    weaponEffect.transform.position = targetPosition;
                    weaponEffect.Play();
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (triggerCollider != null)
            {
                Gizmos.color = new Color(effectColor.r, effectColor.g, effectColor.b, 0.3f);
                Gizmos.DrawWireSphere(transform.position + triggerCollider.center, triggerCollider.radius * transform.lossyScale.x);

                if (targetingType == WeaponTargetingType.RangedZone)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(transform.position, zoneRadius);
                }
            }
        }
    }
}
