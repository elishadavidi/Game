using System;
using BecomingLegend;

namespace BecomingLegend.Stats
{
    [Serializable]
    public struct StatModifier
    {
        public StatType TargetStat;
        public float Value;
        public ModifierType Type;
        public object Source;
        public float Duration;

        public enum ModifierType { Flat, Percent }
    }
}
