using BecomingLegend;
using BecomingLegend.Combat;
using BecomingLegend.Stats;
using UnityEngine;

namespace BecomingLegend.Actors
{
    public class EnemyActor : Actor
    {
        [SerializeField] private float aggroRange = 8f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private int xpReward = 10;

        public float AggroRange => aggroRange;
        public float AttackRange => attackRange;
        public int XPReward => xpReward;

        private Transform target;
        private float lastAttackTime;
        private PlayerActor playerTarget;
        private Color originalColor;
        private float flashTimer;

        protected override void Awake()
        {
            base.Awake();
            originalColor = SpriteRenderer != null ? SpriteRenderer.color : Color.white;
        }

        private void Update()
        {
            if (SpriteRenderer != null && flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f)
                    SpriteRenderer.color = originalColor;
            }

            if (playerTarget == null || playerTarget.IsDead)
            {
                playerTarget = null;
                target = null;
                return;
            }

            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= attackRange && CanAttack())
                AttackTarget();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public bool CanAttack()
        {
            return Time.time - lastAttackTime >= attackCooldown;
        }

        public void AttackTarget()
        {
            if (!CanAttack() || target == null) return;
            lastAttackTime = Time.time;

            if (target.TryGetComponent<IDamageable>(out var damageable))
            {
                float damage = Stats.GetStat(StatType.Strength) * GameConstants.BaseDamagePerStrength;
                var result = new DamageResult(damage, DamageType.Physical, false, this, damageable);
                damageable.TakeDamage(result);
            }
        }

        public override void TakeDamage(DamageResult damage)
        {
            base.TakeDamage(damage);
            if (!IsDead)
                Animator.SetTrigger("Hurt");

            if (damage.Source is PlayerActor player)
            {
                playerTarget = player;
                target = player.transform;
            }
            if (SpriteRenderer != null)
            {
                SpriteRenderer.color = Color.white;
                flashTimer = 0.15f;
            }
        }

        public override void Die()
        {
            Animator.SetTrigger("Dead");
            if (playerTarget != null)
                playerTarget.AddXP(xpReward);
            base.Die();
        }
    }
}
