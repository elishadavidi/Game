using BecomingLegend;
using BecomingLegend.Stats;
using UnityEngine;

namespace BecomingLegend.Training
{
    [CreateAssetMenu(menuName = "Becoming Legend/Training/Activity Definition", fileName = "NewActivity")]
    public class ActivityDefinition : ScriptableObject
    {
        [SerializeField] private string activityName;
        [SerializeField] private StatType primaryStat;
        [SerializeField] private float baseStatGain = 1f;
        [SerializeField] private float baseXPGain = 10f;
        [SerializeField] private float intensityMultiplier = 1f;
        [SerializeField] private Sprite icon;

        public string ActivityName => activityName;
        public StatType PrimaryStat => primaryStat;
        public float BaseStatGain => baseStatGain;
        public float BaseXPGain => baseXPGain;
        public float IntensityMultiplier => intensityMultiplier;
        public Sprite Icon => icon;
    }
}
