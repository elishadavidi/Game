using BecomingLegend;
using BecomingLegend.Stats;
using UnityEngine;

namespace BecomingLegend.Training
{
    public class TrainingManager : MonoBehaviour
    {
        [System.Serializable]
        public struct TrainingMapping
        {
            public StatType primaryStat;
            public float statGain;
            public float xpGain;
        }

        [SerializeField] private TrainingMapping[] mappings;

        public void LogActivity(string activityName, float intensity)
        {
            foreach (var mapping in mappings)
            {
                float gain = mapping.statGain * intensity;
            }
        }
    }
}
