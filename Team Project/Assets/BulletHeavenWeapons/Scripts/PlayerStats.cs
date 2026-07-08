using UnityEngine;

namespace BulletHeavenWeapons
{
    /// <summary>
    /// Attach this to the Player GameObject. 
    /// Weapons will find this to calculate their final damage based on the player's base damage.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Header("Player Base Stats")]
        [Tooltip("The base damage output of the player. Weapons multiply this value.")]
        public float baseDamage = 10f;
    }
}
