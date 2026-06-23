using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class SpriteBatchSlicerTool : EditorWindow
{
    private string customPrefix = "";

    [MenuItem("Tools/Sprite Batch Slicer")]
    public static void ShowWindow()
    {
        GetWindow<SpriteBatchSlicerTool>("Sprite Slicer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Slice Sprites by Custom Prefix", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Let the user type their own prefix to strip
        customPrefix = EditorGUILayout.TextField("Prefix to Remove:", customPrefix);

        EditorGUILayout.Space();

        if (GUILayout.Button("Process Selected Folder", GUILayout.Height(40)))
        {
            ProcessSelectedFolder();
        }

        EditorGUILayout.HelpBox("Instructions:\n" +
            "1. Enter the prefix you want to remove (e.g., 'Equipment_'). If left empty, it won't strip anything.\n" +
            "2. Select a folder (or any file inside it) in your Project window.\n" +
            "3. Click the button above.\n\n" +
            "The tool will strip your prefix, group files by the first remaining string before the next '_', and copy slicing layout from the first asset in that group to the rest.", MessageType.Info);
    }

    private void ProcessSelectedFolder()
    {
        // 1. Get the path of whatever is selected
        string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (string.IsNullOrEmpty(folderPath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a folder or an asset inside the target folder first.", "OK");
            return;
        }

        // Automatically grab parent directory if a file inside it is selected
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            folderPath = Path.GetDirectoryName(folderPath).Replace('\\', '/');
        }

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("Error", "Could not resolve a valid folder path. Please select the folder or its contents in the Project pane.", "OK");
            return;
        }

        string parentFolderName = Path.GetFileName(folderPath);

        // 2. Get all files in the folder
        string[] filePaths = Directory.GetFiles(folderPath);
        Dictionary<string, List<string>> groupedSprites = new Dictionary<string, List<string>>();

        foreach (string filePath in filePaths)
        {
            if (filePath.EndsWith(".meta")) continue;

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string cleanedName = fileNameWithoutExt;

            // 3. Remove user-defined prefix if specified
            if (!string.IsNullOrEmpty(customPrefix) && cleanedName.StartsWith(customPrefix))
            {
                cleanedName = cleanedName.Substring(customPrefix.Length);
            }

            // 4. Substring from start to the first "_" to determine category
            int firstUnderscoreIdx = cleanedName.IndexOf('_');
            if (firstUnderscoreIdx == -1) continue; // Skip if it doesn't match the format

            string groupKey = cleanedName.Substring(0, firstUnderscoreIdx);

            if (!groupedSprites.ContainsKey(groupKey))
            {
                groupedSprites[groupKey] = new List<string>();
            }
            groupedSprites[groupKey].Add(filePath);
        }

        // 5. Copy slicing data within groups
        int processedCount = 0;
        AssetDatabase.StartAssetEditing();

        try
        {
            foreach (var group in groupedSprites)
            {
                List<string> paths = group.Value;
                if (paths.Count <= 1) continue;

                string sourcePath = paths[0];
                TextureImporter sourceImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;

                if (sourceImporter == null || sourceImporter.spritesheet == null || sourceImporter.spritesheet.Length == 0)
                {
                    Debug.LogWarning($"[SpriteSlicer] Source sprite '{Path.GetFileName(sourcePath)}' has no slicing data to copy.");
                    continue;
                }

                SpriteMetaData[] sourceMetaData = sourceImporter.spritesheet;

                for (int i = 1; i < paths.Count; i++)
                {
                    string targetPath = paths[i];
                    TextureImporter targetImporter = AssetImporter.GetAtPath(targetPath) as TextureImporter;

                    if (targetImporter != null)
                    {
                        targetImporter.textureType = TextureImporterType.Sprite;
                        targetImporter.spriteImportMode = SpriteImportMode.Multiple;

                        SpriteMetaData[] newMetaData = new SpriteMetaData[sourceMetaData.Length];
                        string targetName = Path.GetFileNameWithoutExtension(targetPath);

                        for (int m = 0; m < sourceMetaData.Length; m++)
                        {
                            newMetaData[m] = sourceMetaData[m];
                            newMetaData[m].name = $"{targetName}_{m}";
                        }

                        targetImporter.spritesheet = newMetaData;
                        EditorUtility.SetDirty(targetImporter);
                        targetImporter.SaveAndReimport();

                        // Detailed logging using the custom prefix context and Category group key
                        Debug.Log($"[SpriteSlicer] Stripped Prefix: '{customPrefix}' | Category: '{group.Key}' | Copied {sourceMetaData.Length} rects from '{Path.GetFileName(sourcePath)}' to '{Path.GetFileName(targetPath)}'");
                        processedCount++;
                    }
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        EditorUtility.DisplayDialog("Complete", $"Successfully synced metadata across {processedCount} sprites!", "Awesome");
    }
}