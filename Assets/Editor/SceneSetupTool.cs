using BecomingLegend.Actors;
using BecomingLegend.Controllers;
using BecomingLegend.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SceneSetupTool
{
    [MenuItem("Tools/Setup Initial Scene")]
    public static void SetupScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene == null || string.IsNullOrEmpty(scene.path))
        {
            EditorUtility.DisplayDialog("Error", "Save the scene first before running setup.", "OK");
            return;
        }

        // Find or create GameManager
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
            // CombatManager auto-added by GameManager.Initialize
            Undo.RegisterCreatedObjectUndo(go, "Create GameManager");
        }

        // Find or create Player
        var player = GameObject.Find("Player");
        if (player != null)
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc == null) player.AddComponent<PlayerController>();
            var pa = player.GetComponent<PlayerActor>();
            if (pa == null) player.AddComponent<PlayerActor>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog("Done",
            "Scene setup complete!\n" +
            "- GameManager added (auto-adds CombatManager on wake)\n" +
            "- Player ready\n" +
            "Press Play to test.", "OK");
    }
}
