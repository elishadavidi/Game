using BecomingLegend;
using BecomingLegend.Combat;
using UnityEngine;

namespace BecomingLegend.Actors
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyActor : Actor
    {
        [Header("Combat")]
        [SerializeField] private float aggroRange = 8f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private int xpReward = 10;

        [Header("Patrol")]
        [SerializeField] private Bounds patrolBounds = new Bounds(Vector2.zero, new Vector2(10, 10));
        [SerializeField] private float arrivalThreshold = 0.5f;

        public float AggroRange => aggroRange;
        public float AttackRange => attackRange;
        public int XPReward => xpReward;

        private Transform target;
        private float lastAttackTime;
        private PlayerActor playerTarget;
        private Color originalColor;
        private float flashTimer;
        private float deathTimer;

        private Rigidbody2D rb;
        private Vector2 moveDir;
        private Vector2[] patrolCorners = new Vector2[4];
        private int currentCornerIndex;
        private float stateTimer;
        private bool dying;

        private enum AIState { Patrol, Chase, Wait }
        private AIState aiState;

        protected override void Awake()
        {
            base.Awake();
            originalColor = SpriteRenderer != null ? SpriteRenderer.color : Color.white;
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;
            patrolBounds.center = transform.position;
            SetupCorners();
        }

        private void Update()
        {
            if (dying)
            {
                deathTimer -= Time.deltaTime;
                if (deathTimer <= 0f)
                    gameObject.SetActive(false);
                return;
            }

            if (IsDead) return;
            if (SpriteRenderer != null && flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f)
                    SpriteRenderer.color = originalColor;
            }

            if (playerTarget != null && (playerTarget.IsDead || Vector2.Distance(transform.position, playerTarget.transform.position) > aggroRange * 1.5f))
            {
                playerTarget = null;
                target = null;
                aiState = AIState.Patrol;
                PickNextCorner();
            }

            if (playerTarget == null)
            {
                var player = PlayerActor.Instance;
                if (player != null && !player.IsDead && Vector2.Distance(transform.position, player.transform.position) <= aggroRange)
                {
                    playerTarget = player;
                    target = player.transform;
                    aiState = AIState.Chase;
                }
            }

            switch (aiState)
            {
                case AIState.Patrol: UpdatePatrol(); break;
                case AIState.Chase: UpdateChase(); break;
                case AIState.Wait: UpdateWait(); break;
            }

            transform.position += (Vector3)moveDir * MoveSpeedDerived * Time.deltaTime;
            rb.position = transform.position;

            if (moveDir.magnitude > 0.01f)
            {
                Animator.SetFloat("MoveX", moveDir.x);
                Animator.SetFloat("MoveY", moveDir.y);
            }
            float speed = aiState == AIState.Chase ? 1f : aiState == AIState.Patrol ? 0.3f : 0f;
            Animator.SetFloat("Speed", speed);
        }

        private void UpdatePatrol()
        {
            Vector2 toTarget = patrolCorners[currentCornerIndex] - (Vector2)transform.position;
            float dist = toTarget.magnitude;

            if (dist <= arrivalThreshold)
            {
                bool pause = Random.value < 0.4f;
                if (pause)
                {
                    aiState = AIState.Wait;
                    stateTimer = Random.Range(1f, 3f);
                    moveDir = Vector2.zero;
                }
                else
                {
                    AdvanceCorner();
                }
            }
            else
            {
                moveDir = toTarget.normalized;
            }
        }

        private void UpdateWait()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                aiState = AIState.Patrol;
                AdvanceCorner();
            }
        }

        private void UpdateChase()
        {
            if (target == null) return;
            float dist = Vector2.Distance(transform.position, target.position);

            if (dist <= attackRange && CanAttack())
            {
                AttackTarget();
                moveDir = Vector2.zero;
            }
            else
            {
                moveDir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            }
        }

        public bool CanAttack() => Time.time - lastAttackTime >= AttackCooldown;

        public void AttackTarget()
        {
            if (!CanAttack() || target == null) return;
            lastAttackTime = Time.time;
            Animator.SetTrigger("Attacking");

            if (target.TryGetComponent<IDamageable>(out var damageable))
            {
                var result = new DamageResult(AttackDamage, DamageType.Physical, false, this, damageable);
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
                aiState = AIState.Chase;
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
            moveDir = Vector2.zero;
            dying = true;
            deathTimer = 1f;
            if (playerTarget != null)
                playerTarget.AddXP(xpReward);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 c = patrolBounds.center;
            Vector3 s = patrolBounds.size;
            Vector3[] corners = new Vector3[4];
            corners[0] = c + new Vector3(-s.x, -s.y) * 0.5f;
            corners[1] = c + new Vector3(s.x, -s.y) * 0.5f;
            corners[2] = c + new Vector3(s.x, s.y) * 0.5f;
            corners[3] = c + new Vector3(-s.x, s.y) * 0.5f;

            Gizmos.color = Color.green;
            for (int i = 0; i < 4; i++)
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);

            Gizmos.color = Color.green;
            for (int i = 0; i < 4; i++)
            {
                Vector3 from = corners[i];
                Vector3 to = corners[(i + 1) % 4];
                Vector3 mid = (from + to) * 0.5f;
                Vector3 dir = (to - from).normalized;
                Vector3 perp = new Vector3(-dir.y, dir.x);
                Gizmos.DrawLine(mid, mid + dir * 0.4f);
                Gizmos.DrawLine(mid + dir * 0.4f, mid + dir * 0.25f + perp * 0.12f);
                Gizmos.DrawLine(mid + dir * 0.4f, mid + dir * 0.25f - perp * 0.12f);
            }

            Gizmos.color = Color.white;
            foreach (var corner in corners)
                Gizmos.DrawWireSphere(corner, 0.25f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, aggroRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        private void SetupCorners()
        {
            Vector3 c = patrolBounds.center;
            Vector3 s = patrolBounds.size;
            patrolCorners[0] = new Vector2(c.x - s.x * 0.5f, c.y - s.y * 0.5f);
            patrolCorners[1] = new Vector2(c.x + s.x * 0.5f, c.y - s.y * 0.5f);
            patrolCorners[2] = new Vector2(c.x + s.x * 0.5f, c.y + s.y * 0.5f);
            patrolCorners[3] = new Vector2(c.x - s.x * 0.5f, c.y + s.y * 0.5f);

            float nearestDist = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                float d = Vector2.Distance(transform.position, patrolCorners[i]);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    currentCornerIndex = i;
                }
            }
            moveDir = (patrolCorners[currentCornerIndex] - (Vector2)transform.position).normalized;
        }

        private void AdvanceCorner()
        {
            currentCornerIndex = (currentCornerIndex + 1) % 4;
            moveDir = (patrolCorners[currentCornerIndex] - (Vector2)transform.position).normalized;
        }

        private void PickNextCorner()
        {
            currentCornerIndex = Random.Range(0, 4);
            moveDir = (patrolCorners[currentCornerIndex] - (Vector2)transform.position).normalized;
        }
    }
}
