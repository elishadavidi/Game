using BecomingLegend;
using BecomingLegend.Stats;
using UnityEditor;
using UnityEngine;

namespace BecomingLegend.Editor
{
    public static class StatDefinitionCreator
    {
        private static readonly (StatType, string, string, float)[] DefaultStats =
        {
            (StatType.Strength,  "Strength",  "Physical damage dealt in combat", 5f),
            (StatType.Swiftness, "Swiftness", "Attack speed, move speed, and dodge", 5f),
            (StatType.Vitality,  "Vitality",  "Maximum health and survivability", 5f),
        };

        [MenuItem("Becoming Legend/Create Default Stat Definitions")]
        public static void CreateDefaultStats()
        {
            string path = "Assets/_Game/ScriptableObjects/Stats";

            foreach (var (type, name, desc, baseVal) in DefaultStats)
            {
                var existing = AssetDatabase.LoadAssetAtPath<StatDefinition>($"{path}/{name}.asset");
                if (existing != null) continue;

                var stat = ScriptableObject.CreateInstance<StatDefinition>();
                stat.SetPrivateField("statType", type);
                stat.SetPrivateField("displayName", name);
                stat.SetPrivateField("description", desc);
                stat.SetPrivateField("baseValue", baseVal);
                stat.SetPrivateField("minValue", 0f);
                stat.SetPrivateField("maxValue", 999f);

                AssetDatabase.CreateAsset(stat, $"{path}/{name}.asset");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Default stat definitions created.");
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
