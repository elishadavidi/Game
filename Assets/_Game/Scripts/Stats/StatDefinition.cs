using BecomingLegend;
using UnityEngine;

namespace BecomingLegend.Stats
{
    [CreateAssetMenu(menuName = "Becoming Legend/Stats/Stat Definition", fileName = "NewStat")]
    public class StatDefinition : ScriptableObject
    {
        [SerializeField] private StatType statType;
        [SerializeField] private string displayName;
        [SerializeField] [TextArea(2, 4)] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue = 999f;
        [SerializeField] private float baseValue = 1f;

        public StatType StatType => statType;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public float MinValue => minValue;
        public float MaxValue => maxValue;
        public float BaseValue => baseValue;
    }
}
