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
        [SerializeField] private float health;
        [SerializeField] private float maxHealth;
        [SerializeField] private int level = 1;
        [SerializeField] private int currentXP;
        [SerializeField] private int xpToNextLevel = GameConstants.BaseXPToLevel;

        protected SpriteRenderer SpriteRenderer { get; private set; }
        protected Animator Animator { get; private set; }

        public string ActorName => actorName;
        public ActorTeam Team => team;
        public StatSheet Stats => stats;
        public float CurrentHealth => health;
        public float MaxHealth => maxHealth;
        public int Level => level;
        public bool IsDead => health <= 0f;

        protected virtual void Awake()
        {
            SpriteRenderer = GetComponent<SpriteRenderer>();
            Animator = GetComponent<Animator>();
            if (!stats.HasEntries)
            {
                stats.SetEntry(StatType.Strength, 5f);
                stats.SetEntry(StatType.Swiftness, 5f);
                stats.SetEntry(StatType.Vitality, 5f);
            }
            stats.Initialize();
            RecalculateMaxHealth();
            health = maxHealth;
        }

        protected virtual void RecalculateMaxHealth()
        {
            float vitality = stats.GetStat(StatType.Vitality);
            maxHealth = GameConstants.BaseHPPerVitality * Mathf.Max(1, vitality);
        }

        public virtual void TakeDamage(DamageResult damage)
        {
            if (IsDead) return;
            health = Mathf.Max(0, health - damage.Amount);
            if (IsDead) Die();
        }

        public virtual void Die()
        {
            gameObject.SetActive(false);
        }

        public virtual void Heal(float amount)
        {
            health = Mathf.Min(maxHealth, health + amount);
        }

        public virtual void AddXP(int amount)
        {
            currentXP += amount;
            while (currentXP >= xpToNextLevel)
            {
                currentXP -= xpToNextLevel;
                LevelUp();
            }
        }

        protected virtual void LevelUp()
        {
            level++;
            xpToNextLevel = Mathf.RoundToInt(GameConstants.BaseXPToLevel * Mathf.Pow(GameConstants.XPLevelMultiplier, level - 1));
            RecalculateMaxHealth();
            health = maxHealth;
        }
    }
}
