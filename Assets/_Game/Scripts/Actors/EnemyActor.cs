using BecomingLegend;
using BecomingLegend.Combat;
using BecomingLegend.Core;
using BecomingLegend.Events;
using UnityEngine;

namespace BecomingLegend.Actors
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class EnemyActor : Actor
    {
        [Header("Combat")]
        [SerializeField] private float aggroRange = 8f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackPointDistance = 0.6f;
        [SerializeField] private LayerMask playerLayers = 1;
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
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

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

            if (moveDir.magnitude > 0.01f)
            {
                Animator.SetFloat("MoveX", moveDir.x);
                Animator.SetFloat("MoveY", moveDir.y);
            }
            float speed = aiState == AIState.Chase ? 1f : aiState == AIState.Patrol ? 0.3f : 0f;
            Animator.SetFloat("Speed", speed);
        }

        private void FixedUpdate()
        {
            if (dying || IsDead)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            rb.linearVelocity = moveDir * MoveSpeedDerived;
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

            if (dist <= attackRange * 0.6f && CanAttack())
            {
                AttackTarget();
                moveDir = ((Vector2)target.position - (Vector2)transform.position).normalized * 0.3f;
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

            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            Vector2 pos = (Vector2)transform.position + dir * attackPointDistance;
            var hit = Physics2D.OverlapCircle(
                pos,
                attackRange * 0.8f,
                playerLayers);

            if (hit != null &&
                hit.TryGetComponent<PlayerActor>(out var player) &&
                !player.IsDead)
            {
                var result = GameManager.Instance.Combat.CalculateDamage(
                    this,
                    player);

                player.TakeDamage(result);
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
        }

        public override void Die()
        {
            if (dying)
                return;

            Animator.SetTrigger("Dead");

            moveDir = Vector2.zero;
            rb.linearVelocity = Vector2.zero;

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

            if (target != null)
            {
                Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
                Vector3 p = (Vector2)transform.position + dir * attackPointDistance;
                Gizmos.color = new Color(1, 0.5f, 0);
                Gizmos.DrawWireSphere(p, attackRange * 0.5f);
                Gizmos.color = new Color(1, 0.5f, 0, 0.12f);
                Gizmos.DrawSphere(p, attackRange * 0.5f);
            }
        }

        public void ApplyPreset(string name, int str, int spd, int sta, int core, int xp, float aggro, float atkRange, Color tint)
        {
            actorName = name;
            xpReward = xp;
            aggroRange = aggro;
            attackRange = atkRange;
            if (SpriteRenderer != null)
            {
                SpriteRenderer.color = tint;
                OriginalColor = tint;
            }
            Stats.BeginUpdate();
            Stats.SetBase(StatType.Strength, str);
            Stats.SetBase(StatType.Speed, spd);
            Stats.SetBase(StatType.Stamina, sta);
            Stats.SetBase(StatType.Core, core);
            Stats.EndUpdate();
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
