using BecomingLegend.Actors;
using BecomingLegend.Stats;
using UnityEngine;

namespace BecomingLegend.Training
{
    public class TrainingManager : MonoBehaviour
    {
        [SerializeField] private ActivityDefinition[] activities;

        public void LogActivity(ActivityDefinition activity, float intensity, PlayerActor player)
        {
            if (activity == null || player == null) return;

            float statGain = activity.BaseStatGain * intensity * activity.IntensityMultiplier;
            float current = player.Stats.GetBase(activity.PrimaryStat);
            player.Stats.SetBase(activity.PrimaryStat, current + statGain);

            int xpGain = Mathf.RoundToInt(activity.BaseXPGain * intensity);
            player.AddXP(xpGain);

            Debug.Log($"[Training] {activity.ActivityName}: {activity.PrimaryStat} +{statGain:F1}, XP +{xpGain}");
        }

        public void LogActivity(string activityName, float intensity, PlayerActor player)
        {
            foreach (var a in activities)
            {
                if (a.ActivityName == activityName)
                {
                    LogActivity(a, intensity, player);
                    return;
                }
            }
            Debug.LogWarning($"[Training] No activity named '{activityName}' found.");
        }
    }
}
