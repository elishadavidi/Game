using BecomingLegend.Actors;
using BecomingLegend.Combat;
using BecomingLegend.Core;
using BecomingLegend.Events;
using UnityEngine;

namespace BecomingLegend.UI
{
    public class CombatFeedback : MonoBehaviour
    {
        [SerializeField] private int poolSize = 20;
        [SerializeField] private float numberDuration = 0.8f;
        [SerializeField] private float xpDuration = 1.2f;
        [SerializeField] private float levelUpDuration = 1.5f;
        [SerializeField] private Color damageColor = Color.white;
        [SerializeField] private Color critColor = Color.yellow;
        [SerializeField] private Color xpColor = new Color(0.3f, 1f, 0.3f);
        [SerializeField] private Color levelUpColor = Color.cyan;
        [SerializeField] private GameObject deathParticlePrefab;
        [SerializeField] private Color deathParticleColor = new Color(1f, 0.5f, 0f);

        private DamageNumber[] pool;
        private int poolIndex;

        private void Awake()
        {
            InitializePool();
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
            EventBus.Subscribe<LevelUpEvent>(OnLevelUp);
        }

        private void InitializePool()
        {
            pool = new DamageNumber[poolSize];
            var container = new GameObject("DamageNumberPool");
            DontDestroyOnLoad(container);
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject("DamageNumber");
                go.transform.SetParent(container.transform, false);
                go.SetActive(false);
                var dn = go.AddComponent<DamageNumber>();
                if (dn != null)
                    pool[i] = dn;
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<EntityDiedEvent>(OnEntityDied);
            EventBus.Unsubscribe<LevelUpEvent>(OnLevelUp);
        }

        private void OnDamageDealt(DamageDealtEvent e)
        {
            Color color = e.Damage.IsCritical ? critColor : damageColor;
            float amount = e.Damage.Amount;
            string display = e.Damage.IsCritical ? $"{amount:F0}!" : $"{amount:F0}";

            var target = e.Damage.Target;
            Vector3 pos = target is MonoBehaviour mb ? mb.transform.position + Vector3.up * 0.5f : Vector3.zero;

            var dn = GetNumber();
            dn.Show(display, color, pos, numberDuration, ReturnNumber);
        }

        private void OnEntityDied(EntityDiedEvent e)
        {
            if (e.Entity is MonoBehaviour mb)
            {
                Vector3 pos = mb.transform.position;
                if (deathParticlePrefab != null)
                {
                    var go = Instantiate(deathParticlePrefab, pos, Quaternion.identity, null);
                    Destroy(go, 1.5f);
                    var sr = go.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = deathParticleColor;
                }
                else
                {
                    SpawnBuiltinDeathEffect(pos);
                }
            }

            if (e.Entity is EnemyActor enemy && enemy.XPReward > 0)
            {
                var player = PlayerActor.Instance;
                if (player != null)
                {
                    Vector3 pos = player.transform.position + Vector3.up * 0.8f;
                    var dn = GetNumber();
                    dn.Show($"+{enemy.XPReward} XP", xpColor, pos, xpDuration, ReturnNumber);
                }
            }
        }

        private static Sprite whiteParticle;
        private static Sprite WhiteParticle
        {
            get
            {
                if (whiteParticle == null)
                {
                    var tex = new Texture2D(1, 1);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    whiteParticle = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 100);
                }
                return whiteParticle;
            }
        }

        private void SpawnBuiltinDeathEffect(Vector3 pos)
        {
            for (int i = 0; i < 5; i++)
            {
                var go = new GameObject("DeathParticle");
                go.transform.position = pos + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0);
                go.transform.localScale = Vector3.one * 0.12f;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = WhiteParticle;
                sr.color = deathParticleColor;
                sr.sortingOrder = 10;
                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 2f;
                rb.linearVelocity = new Vector2(Random.Range(-2f, 2f), Random.Range(2f, 4f));
                Destroy(go, 0.6f);
            }
        }

        private void OnLevelUp(LevelUpEvent e)
        {
            if (e.Entity is MonoBehaviour mb)
            {
                Vector3 pos = mb.transform.position + Vector3.up * 1f;
                var dn = GetNumber();
                dn.Show($"Level {e.NewLevel}!", levelUpColor, pos, levelUpDuration, ReturnNumber);
            }
        }

        private DamageNumber GetNumber()
        {
            for (int i = 0; i < poolSize; i++)
            {
                int idx = (poolIndex + i) % poolSize;
                var dn = pool[idx];
                if (dn != null)
                {
                    try
                    {
                        if (!dn.gameObject.activeSelf)
                        {
                            poolIndex = (idx + 1) % poolSize;
                            return dn;
                        }
                    }
                    catch
                    {
                        pool[idx] = null;
                    }
                }
            }
            int wrapIdx = poolIndex % poolSize;
            poolIndex = (wrapIdx + 1) % poolSize;
            if (pool[wrapIdx] == null)
            {
                var go = new GameObject("DamageNumber");
                go.SetActive(false);
                DontDestroyOnLoad(go);
                pool[wrapIdx] = go.AddComponent<DamageNumber>();
            }
            return pool[wrapIdx];
        }

        private void ReturnNumber(DamageNumber dn)
        {
            dn.transform.position = Vector3.zero;
            dn.transform.localScale = Vector3.one;
        }
    }
}
