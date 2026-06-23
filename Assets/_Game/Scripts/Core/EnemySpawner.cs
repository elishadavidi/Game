using BecomingLegend.Actors;
using System.Collections.Generic;
using UnityEngine;

namespace BecomingLegend.Core
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        public GameObject prefab;
        public string displayName = "Enemy";
        public int strength = 3;
        public int speed = 3;
        public int stamina = 3;
        public int core = 3;
        public int xpReward = 10;
        public float aggroRange = 8f;
        public float attackRange = 1.5f;
        public Color tint = Color.white;
    }

    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private List<EnemySpawnEntry> entries = new();

        public int EntryCount => entries.Count;

        public EnemyActor Spawn(int index, Vector2 position)
        {
            if (index < 0 || index >= entries.Count)
            {
                Debug.LogWarning($"EnemySpawner: index {index} out of range");
                return null;
            }

            var entry = entries[index];
            if (entry.prefab == null)
            {
                Debug.LogWarning($"EnemySpawner: entry {index} has no prefab");
                return null;
            }

            var go = Instantiate(entry.prefab, position, Quaternion.identity);
            var enemy = go.GetComponent<EnemyActor>();
            if (enemy != null)
            {
                enemy.ApplyPreset(entry.displayName, entry.strength, entry.speed,
                    entry.stamina, entry.core, entry.xpReward,
                    entry.aggroRange, entry.attackRange, entry.tint);
            }
            return enemy;
        }

        public EnemyActor SpawnRandom(Vector2 position)
        {
            if (entries.Count == 0) return null;
            int index = Random.Range(0, entries.Count);
            return Spawn(index, position);
        }
    }
}
