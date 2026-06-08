namespace BecomingLegend.Combat
{
    public interface IDamageable
    {
        void TakeDamage(DamageResult damage);
        void Die();
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsDead { get; }
    }
}
