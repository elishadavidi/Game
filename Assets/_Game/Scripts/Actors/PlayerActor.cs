using BecomingLegend;
using BecomingLegend.Combat;
using BecomingLegend.Core;
using BecomingLegend.Stats;
using UnityEngine;

namespace BecomingLegend.Actors
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerActor : Actor
    {
        [SerializeField] private ClassType classType = ClassType.Knight;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 0.5f;

        private Rigidbody2D rb;

        public ClassType ClassType => classType;
        public float MoveSpeed
        {
            get
            {
                float swiftnessBonus = Stats.GetStat(StatType.Swiftness) * GameConstants.BaseSpeedPerSwiftness;
                return moveSpeed + swiftnessBonus;
            }
        }

        private Vector2 moveInput;
        private float lastAttackTime;

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        public void SetMoveInput(Vector2 input)
        {
            moveInput = input;
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
            if (Time.time - lastAttackTime < attackCooldown) return;
            lastAttackTime = Time.time;

            Animator.SetTrigger("Attacking");

            var enemies = FindObjectsByType<EnemyActor>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy.IsDead) continue;
                if (Vector2.Distance(transform.position, enemy.transform.position) <= attackRange)
                {
                    var result = GameManager.Instance.Combat.CalculateDamage(this, enemy);
                    enemy.TakeDamage(result);
                    Debug.Log($"Hit {enemy.ActorName} for {result.Amount} damage. HP: {enemy.CurrentHealth}/{enemy.MaxHealth}");
                    break;
                }
            }
        }
    }
}
