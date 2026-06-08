using BecomingLegend;
using BecomingLegend.Stats;
using BecomingLegend.Training;
using UnityEditor;
using UnityEngine;

namespace BecomingLegend.Editor
{
    public static class ActivityDefinitionCreator
    {
        private static readonly (string, StatType, float, float, float)[] DefaultActivities =
        {
            ("Push-ups",       StatType.Strength,  2f,   18f, 1f),
            ("Pull-ups",       StatType.Strength,  2.5f, 20f, 1.2f),
            ("Running",        StatType.Vitality,  2f,   15f, 1.5f),
            ("Squats",         StatType.Vitality,  1.5f, 12f, 1f),
            ("Jumping Jacks",  StatType.Swiftness, 1.5f, 12f, 0.8f),
            ("Stretching",     StatType.Swiftness, 1f,   8f,  0.7f),
        };

        [MenuItem("Becoming Legend/Create Default Activity Definitions")]
        public static void CreateDefaultActivities()
        {
            string path = "Assets/_Game/ScriptableObjects/Training";
            System.IO.Directory.CreateDirectory(path);

            foreach (var (name, stat, statGain, xpGain, intensityMult) in DefaultActivities)
            {
                var existing = AssetDatabase.LoadAssetAtPath<ActivityDefinition>($"{path}/{name}.asset");
                if (existing != null) continue;

                var activity = ScriptableObject.CreateInstance<ActivityDefinition>();
                activity.SetPrivateField("activityName", name);
                activity.SetPrivateField("primaryStat", stat);
                activity.SetPrivateField("baseStatGain", statGain);
                activity.SetPrivateField("baseXPGain", xpGain);
                activity.SetPrivateField("intensityMultiplier", intensityMult);

                AssetDatabase.CreateAsset(activity, $"{path}/{name}.asset");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Default activity definitions created.");
        }

        private static void SetPrivateField<T>(this Object obj, string fieldName, T value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(obj, value);
        }
    }
}
