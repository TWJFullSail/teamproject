namespace BulletHeavenWeapons
{
    /// <summary>
    /// Interface that any enemy health script must implement to receive damage from weapons.
    /// This decouples the weapons from your team's specific enemy health implementation.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float damageAmount);
    }
}
