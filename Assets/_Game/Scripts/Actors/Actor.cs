using BecomingLegend;
using BecomingLegend.Combat;
using BecomingLegend.Stats;
using UnityEngine;

namespace BecomingLegend.Actors
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
    public abstract class Actor : MonoBehaviour, IDamageable
    {
        [SerializeField] private string actorName = "Actor";
        [SerializeField] private ActorTeam team = ActorTeam.Neutral;
        [SerializeField] private StatSheet stats = new();
        [SerializeField] protected float health;
        [SerializeField] protected float mp;
        [SerializeField] protected float stamina;

        private float lastMaxHealth;
        private float lastMaxMP;
        private float lastMaxStamina;

        protected SpriteRenderer SpriteRenderer { get; private set; }
        protected Animator Animator { get; private set; }

        public string ActorName => actorName;
        public ActorTeam Team => team;
        public StatSheet Stats => stats;
        public float CurrentHealth => health;
        public float CurrentMP => mp;
        public float CurrentStamina => stamina;
        public bool IsDead => health <= 0f;

        // Max resources derived from core stats
        public float MaxHealth => 50f + Stats.GetStat(StatType.Core) * 10f + Stats.GetStat(StatType.Strength) * 2f;
        public float MaxMP => 20f + Stats.GetStat(StatType.Stamina) * 5f;
        public float MaxStamina => 30f + Stats.GetStat(StatType.Stamina) * 8f;

        // Derived combat stats (read-only, computed from core stats)
        public float AttackDamage => Stats.GetStat(StatType.Strength) * GameConstants.BaseDamagePerStrength;
        public float MoveSpeedDerived => GameConstants.MoveSpeedBase + Stats.GetStat(StatType.Speed) * GameConstants.MoveSpeedPerSpeed;
        public float AttackCooldown => Mathf.Max(GameConstants.MinAttackCooldown, GameConstants.BaseAttackCooldown - Stats.GetStat(StatType.Speed) * GameConstants.AtkSpeedPerSpeed);
        public float Defense => Stats.GetStat(StatType.Core) * GameConstants.DefPerCore;
        public float HPRegenPerSec => Stats.GetStat(StatType.Core) * GameConstants.HpRegenPerCore;
        public float MPRegenPerSec => Stats.GetStat(StatType.Stamina) * GameConstants.MpRegenPerStamina;
        public float StaminaRegenPerSec => Stats.GetStat(StatType.Stamina) * GameConstants.StaminaRegenPerStamina;

        protected virtual void Awake()
        {
            SpriteRenderer = GetComponent<SpriteRenderer>();
            Animator = GetComponent<Animator>();
            if (!stats.HasEntries)
            {
                stats.SetEntry(StatType.Strength, 5f);
                stats.SetEntry(StatType.Speed, 5f);
                stats.SetEntry(StatType.Stamina, 5f);
                stats.SetEntry(StatType.Core, 5f);
            }
            stats.Initialize();
            stats.OnStatsChanged += RecalculateVitals;
            health = MaxHealth;
            mp = MaxMP;
            stamina = MaxStamina;
            lastMaxHealth = MaxHealth;
            lastMaxMP = MaxMP;
            lastMaxStamina = MaxStamina;
        }

        protected virtual void OnDestroy()
        {
            if (stats != null)
                stats.OnStatsChanged -= RecalculateVitals;
        }

        public virtual void TakeDamage(DamageResult damage)
        {
            if (IsDead) return;
            health = Mathf.Max(0, health - damage.Amount);
            if (IsDead) Die();
        }

        public void RecalculateVitals()
        {
            if (lastMaxHealth > 0f && MaxHealth > lastMaxHealth)
                health *= MaxHealth / lastMaxHealth;
            else
                health = Mathf.Min(health, MaxHealth);

            if (lastMaxMP > 0f && MaxMP > lastMaxMP)
                mp *= MaxMP / lastMaxMP;
            else
                mp = Mathf.Min(mp, MaxMP);

            if (lastMaxStamina > 0f && MaxStamina > lastMaxStamina)
                stamina *= MaxStamina / lastMaxStamina;
            else
                stamina = Mathf.Min(stamina, MaxStamina);

            lastMaxHealth = MaxHealth;
            lastMaxMP = MaxMP;
            lastMaxStamina = MaxStamina;
        }

        public virtual void Die()
        {
            gameObject.SetActive(false);
        }

        public virtual void Heal(float amount)
        {
            health = Mathf.Min(MaxHealth, health + amount);
        }
    }
}
