using BecomingLegend;
using BecomingLegend.Combat;
using BecomingLegend.Core;
using UnityEngine;

namespace BecomingLegend.Actors
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerActor : Actor
    {
        [Header("Player")]
        [SerializeField] private ClassType classType = ClassType.Knight;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private LayerMask enemyLayers = 1;
        [SerializeField] private int level = 1;
        [SerializeField] private int currentXP;
        [SerializeField] private int xpToNextLevel = GameConstants.BaseXPToLevel;

        private Rigidbody2D rb;

        public static PlayerActor Instance { get; private set; }

        public ClassType ClassType => classType;
        public int Level => level;
        public float MoveSpeed => MoveSpeedDerived;

        private float lastAttackTime;

        protected override void Awake()
        {
            base.Awake();
            if (Instance == null) Instance = this;
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        public override void TakeDamage(DamageResult damage)
        {
            base.TakeDamage(damage);
            if (!IsDead)
                Animator.SetTrigger("Hurt");
        }

        public override void Die()
        {
            Animator.SetTrigger("Dead");
            enabled = false;
        }

        public void Attack()
        {
            if (Time.time - lastAttackTime < AttackCooldown) return;
            lastAttackTime = Time.time;

            Animator.SetTrigger("Attacking");

            var hit = Physics2D.OverlapCircle(transform.position, attackRange, enemyLayers);
            if (hit != null && hit.TryGetComponent<EnemyActor>(out var enemy) && !enemy.IsDead)
            {
                var result = GameManager.Instance.Combat.CalculateDamage(this, enemy);
                enemy.TakeDamage(result);
            }
        }

        public void AddXP(int amount)
        {
            currentXP += amount;
            while (currentXP >= xpToNextLevel)
            {
                currentXP -= xpToNextLevel;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            level++;
            xpToNextLevel = Mathf.RoundToInt(GameConstants.BaseXPToLevel * Mathf.Pow(GameConstants.XPLevelMultiplier, level - 1));
            Stats.BeginUpdate();
            Stats.SetBase(StatType.Strength, Stats.GetBase(StatType.Strength) + 1f);
            Stats.SetBase(StatType.Speed, Stats.GetBase(StatType.Speed) + 1f);
            Stats.SetBase(StatType.Stamina, Stats.GetBase(StatType.Stamina) + 1f);
            Stats.SetBase(StatType.Core, Stats.GetBase(StatType.Core) + 1f);
            Stats.EndUpdate();
        }
    }
}
