using System;
using System.Collections.Generic;
using BecomingLegend;
using UnityEngine;

namespace BecomingLegend.Stats
{
    [Serializable]
    public class StatSheet
    {
        [SerializeField] private List<StatEntry> entries = new();
        private Dictionary<StatType, float> modifiers = new();
        private Dictionary<StatType, float> multipliers = new();
        private Dictionary<StatType, float> cache;
        private bool locked;
        private bool dirty;
        private int updateLock;
        private bool changedDuringBatch;

        public event Action OnStatsChanged;

        [Serializable]
        public struct StatEntry
        {
            public StatType type;
            public float value;
        }

        public bool HasEntries => entries.Count > 0;

        public void SetEntry(StatType type, float value)
        {
            if (locked)
            {
                Debug.LogWarning("StatSheet entries are locked after initialization");
                return;
            }
            entries.Add(new StatEntry { type = type, value = value });
        }

        public void Initialize()
        {
            if (modifiers == null) modifiers = new();
            if (multipliers == null) multipliers = new();
            DeduplicateEntries();
            BuildCache();
            locked = true;
        }

        private void DeduplicateEntries()
        {
            var unique = new Dictionary<StatType, StatEntry>();
            foreach (var e in entries)
                unique[e.type] = e;
            if (unique.Count != entries.Count)
            {
                Debug.LogWarning($"StatSheet: Removed {entries.Count - unique.Count} duplicate entries. " +
                                 "Each StatType must appear exactly once.");
                entries.Clear();
                foreach (var kvp in unique)
                    entries.Add(kvp.Value);
            }
        }

        private void BuildCache()
        {
            cache = new();
            foreach (var e in entries)
            {
                float mod = modifiers.GetValueOrDefault(e.type, 0f);
                float mult = multipliers.GetValueOrDefault(e.type, 1f);
                cache[e.type] = Mathf.Max(0, (e.value + mod) * mult);
            }
            dirty = false;
        }

        public float GetStat(StatType type)
        {
            if (cache == null) Initialize();
            if (dirty) BuildCache();
            return cache.GetValueOrDefault(type, 0f);
        }

        public float GetBase(StatType type)
        {
            foreach (var e in entries)
                if (e.type == type) return e.value;
            return 0f;
        }

        public void SetBase(StatType type, float value)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].type == type)
                {
                    var e = entries[i];
                    e.value = value;
                    entries[i] = e;
                    NotifyChanged();
                    return;
                }
            }
            entries.Add(new StatEntry { type = type, value = value });
            NotifyChanged();
        }

        public void AddModifier(StatType type, float value)
        {
            modifiers[type] = modifiers.GetValueOrDefault(type, 0f) + value;
            NotifyChanged();
        }

        public void RemoveModifier(StatType type, float value)
        {
            modifiers[type] = modifiers.GetValueOrDefault(type, 0f) - value;
            NotifyChanged();
        }

        public void SetMultiplier(StatType type, float value)
        {
            multipliers[type] = value;
            NotifyChanged();
        }

        public void BeginUpdate()
        {
            updateLock++;
        }

        public void EndUpdate()
        {
            if (updateLock <= 0) return;
            updateLock--;
            if (updateLock == 0 && changedDuringBatch)
            {
                changedDuringBatch = false;
                dirty = true;
                BuildCache();
                OnStatsChanged?.Invoke();
            }
        }

        private void NotifyChanged()
        {
            dirty = true;
            if (updateLock > 0)
            {
                changedDuringBatch = true;
                return;
            }
            BuildCache();
            OnStatsChanged?.Invoke();
        }

        public StatSheet Clone()
        {
            var clone = new StatSheet();
            foreach (var entry in entries)
                clone.entries.Add(new StatEntry { type = entry.type, value = entry.value });
            foreach (var kvp in modifiers)
                clone.modifiers[kvp.Key] = kvp.Value;
            foreach (var kvp in multipliers)
                clone.multipliers[kvp.Key] = kvp.Value;
            clone.Initialize();
            return clone;
        }
    }
}
