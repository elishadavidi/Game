using BecomingLegend;
using BecomingLegend.Actors;
using BecomingLegend.Combat;
using BecomingLegend.Controllers;
using BecomingLegend.Core;
using BecomingLegend.Quests;
using BecomingLegend.Stats;
using BecomingLegend.Training;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BecomingLegend.Editor
{
    public static class ProjectSetup
    {
        [MenuItem("Becoming Legend/Setup Initial Scene")]
        public static void SetupScene()
        {
            var gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
            gmObj.AddComponent<CombatManager>();
            gmObj.AddComponent<QuestManager>();
            gmObj.AddComponent<TrainingManager>();

            var camera = Camera.main;
            if (camera != null)
            {
                camera.gameObject.name = "MainCamera";
                camera.orthographic = true;
                camera.orthographicSize = 5f;
            }

            var playerObj = new GameObject("Player");
            playerObj.AddComponent<PlayerActor>();
            var rb = playerObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var collider = playerObj.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;
            playerObj.tag = "Player";
            playerObj.layer = LayerMask.NameToLayer("Default");

            playerObj.AddComponent<PlayerController>();

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();

            Undo.RegisterCreatedObjectUndo(gmObj, "Setup Scene");
            Undo.RegisterCreatedObjectUndo(playerObj, "Setup Scene");
            Undo.RegisterCreatedObjectUndo(eventSystem, "Setup Scene");

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Initial scene setup complete.");
        }

        [MenuItem("Becoming Legend/Create Enemy")]
        public static void CreateEnemy()
        {
            var enemyObj = new GameObject("Enemy");
            var enemy = enemyObj.AddComponent<EnemyActor>();
            var rb = enemyObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            enemyObj.AddComponent<BoxCollider2D>();

            var so = new SerializedObject(enemy);
            var stats = so.FindProperty("stats");
            if (stats != null)
            {
                var entries = stats.FindPropertyRelative("entries");
                entries.ClearArray();

                void AddEntry(StatType type, float value)
                {
                    entries.InsertArrayElementAtIndex(entries.arraySize);
                    var entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                    entry.FindPropertyRelative("type").enumValueIndex = (int)type;
                    entry.FindPropertyRelative("value").floatValue = value;
                }

                AddEntry(StatType.Strength, 3f);
                AddEntry(StatType.Speed, 3f);
                AddEntry(StatType.Stamina, 3f);
                AddEntry(StatType.Core, 3f);
            }
            so.ApplyModifiedProperties();

            var sr = enemyObj.AddComponent<SpriteRenderer>();
            sr.color = Color.red;

            Undo.RegisterCreatedObjectUndo(enemyObj, "Create Enemy");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Enemy created.");
        }
    }
}
