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
        private Dictionary<StatType, float> baseValues;

        [Serializable]
        public struct StatEntry
        {
            public StatType type;
            public float value;
        }

        public bool HasEntries => entries.Count > 0;

        public void SetEntry(StatType type, float value)
        {
            entries.Add(new StatEntry { type = type, value = value });
        }

        public void Initialize()
        {
            baseValues = new Dictionary<StatType, float>();
            modifiers = new Dictionary<StatType, float>();
            multipliers = new Dictionary<StatType, float>();

            foreach (var entry in entries)
            {
                baseValues[entry.type] = entry.value;
            }
        }

        public float GetStat(StatType type)
        {
            float baseVal = baseValues.GetValueOrDefault(type, 0f);
            float mod = modifiers.GetValueOrDefault(type, 0f);
            float mult = multipliers.GetValueOrDefault(type, 1f);
            return Mathf.Max(0, (baseVal + mod) * mult);
        }

        public void SetBase(StatType type, float value)
        {
            if (baseValues == null) Initialize();
            baseValues[type] = value;
        }

        public float GetBase(StatType type)
        {
            if (baseValues == null) Initialize();
            return baseValues.GetValueOrDefault(type, 0f);
        }

        public void AddModifier(StatType type, float value)
        {
            modifiers[type] = modifiers.GetValueOrDefault(type, 0f) + value;
        }

        public void RemoveModifier(StatType type, float value)
        {
            modifiers[type] = modifiers.GetValueOrDefault(type, 0f) - value;
        }

        public void SetMultiplier(StatType type, float value)
        {
            multipliers[type] = value;
        }

        public StatSheet Clone()
        {
            var clone = new StatSheet();
            foreach (var entry in entries)
                clone.entries.Add(new StatEntry { type = entry.type, value = entry.value });
            return clone;
        }
    }
}
