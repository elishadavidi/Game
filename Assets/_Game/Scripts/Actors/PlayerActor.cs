using BecomingLegend;
using BecomingLegend.Combat;
using BecomingLegend.Core;
using BecomingLegend.Events;
using UnityEngine;

namespace BecomingLegend.Actors
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class PlayerActor : Actor
    {
        [Header("Player")]
        [SerializeField] private ClassType classType = ClassType.Knight;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private LayerMask enemyLayers = 1;
        [SerializeField] private int level = 1;
        [SerializeField] private int currentXP;
        [SerializeField] private int xpToNextLevel = GameConstants.BaseXPToLevel;
        [SerializeField] private float attackPointDistance = 0.75f;

        private Rigidbody2D rb;

        public static PlayerActor Instance { get; private set; }
        public float AttackPointDistance => attackPointDistance;

        public ClassType ClassType => classType;
        public int Level => level;
        public int CurrentXP => currentXP;
        public int XPToNextLevel => xpToNextLevel;
        public float MoveSpeed => MoveSpeedDerived;

        private float lastAttackTime;

        protected override void Awake()
        {
            base.Awake();
            if (Instance == null) Instance = this;
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 pos = transform.position;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pos, attackRange);
            Gizmos.color = new Color(1, 0, 0, 0.15f);
            Gizmos.DrawSphere(pos, attackRange);
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

        public void Attack(Vector2 direction)
        {
            if (Time.time - lastAttackTime < AttackCooldown)
                return;

            direction = direction.normalized;

            if (direction.sqrMagnitude < 0.01f)
                direction = Vector2.down;

            lastAttackTime = Time.time;

            Animator.SetTrigger("Attacking");

            Vector2 pos = (Vector2)transform.position + direction * attackPointDistance;

            Collider2D[] hits = Physics2D.OverlapCircleAll(
                pos,
                attackRange,
                enemyLayers
            );

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<EnemyActor>(out var enemy) && !enemy.IsDead)
                {
                    var result = GameManager.Instance.Combat.CalculateDamage(this, enemy);
                    enemy.TakeDamage(result);
                }
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
            EventBus.Publish(new LevelUpEvent(this, level));
        }
    }
}
