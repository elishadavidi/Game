using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class AnimationSetupTool
{
    private const string PngRoots = "Assets/swordman/PNG";
    private const string AnimationOut = "Assets/_Game/Animations/Player";
    private const string ControllerPath = "Assets/_Game/Animations/Player/Player.controller";

    private static readonly string[] Directions = { "front", "side_left", "side_right", "back" };

    [MenuItem("Tools/Player Animation Setup")]
    public static void SetupPlayerAnimations()
    {
        Directory.CreateDirectory(AnimationOut);

        // Clean up old clips
        string fullOut = Path.Combine(Application.dataPath, AnimationOut.Substring("Assets/".Length));
        if (Directory.Exists(fullOut))
        {
            foreach (var f in Directory.GetFiles(fullOut, "*.anim"))
            {
                string rel = "Assets" + f.Substring(Application.dataPath.Length).Replace("\\", "/");
                AssetDatabase.DeleteAsset(rel);
            }
            AssetDatabase.Refresh();
        }

        // Find all With_shadow PNGs
        var pngFiles = new List<string>();
        string fullRoot = Path.Combine(Application.dataPath, PngRoots.Substring("Assets/".Length));
        foreach (var dir in Directory.GetDirectories(fullRoot))
        {
            string shadowDir = Path.Combine(dir, "With_shadow");
            if (Directory.Exists(shadowDir))
            {
                foreach (var f in Directory.GetFiles(shadowDir, "*.png"))
                {
                    string rel = "Assets" + f.Substring(Application.dataPath.Length).Replace("\\", "/");
                    pngFiles.Add(rel);
                }
            }
        }

        if (pngFiles.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", $"No PNG files found in {PngRoots}/*/With_shadow/", "OK");
            return;
        }

        // Step 1: Fix all sprite rects to 50x50 centered
        foreach (var pngPath in pngFiles)
        {
            FixSpriteRectsTo50x50(pngPath);
        }
        AssetDatabase.Refresh();

        // Step 2: Create clips from all sprites, organized by level/state/direction
        var allSavedPaths = new Dictionary<string, string>(); // png_path -> clip_path
        int totalClips = 0;

        foreach (var pngPath in pngFiles)
        {
            string name = Path.GetFileNameWithoutExtension(pngPath);
            TryParsePngName(name, out string level, out string state);
            if (string.IsNullOrEmpty(level) || string.IsNullOrEmpty(state)) continue;

            var allAssets = AssetDatabase.LoadAllAssetsAtPath(pngPath);
            var sprites = allAssets.OfType<Sprite>().ToArray();
            if (sprites.Length == 0) continue;

            // Sort by position (top-to-bottom, left-to-right)
            var sorted = sprites
                .OrderBy(s => -s.rect.y)  // higher y = visually higher row (top of image)
                .ThenBy(s => s.rect.x)
                .ToArray();

            // Group into 4 rows by y-position (tolerance 20px)
            var rows = new List<List<Sprite>>();
            List<Sprite> currentRow = new List<Sprite> { sorted[0] };
            float lastY = sorted[0].rect.y;
            for (int i = 1; i < sorted.Length; i++)
            {
                if (Mathf.Abs(sorted[i].rect.y - lastY) > 20f)
                {
                    rows.Add(currentRow);
                    currentRow = new List<Sprite> { sorted[i] };
                    lastY = sorted[i].rect.y;
                }
                else
                {
                    currentRow.Add(sorted[i]);
                }
            }
            rows.Add(currentRow);

            // Sort rows by y descending (top row first)
            rows = rows.OrderBy(r => -r[0].rect.y).ToList();

            for (int d = 0; d < rows.Count && d < Directions.Length; d++)
            {
                string direction = Directions[d];
                string clipName = $"{level}_{state}_{direction}";
                var clip = CreateClip(rows[d], clipName);
                if (clip == null) continue;

                string clipPath = $"{AnimationOut}/{clipName}.anim";
                AssetDatabase.CreateAsset(clip, clipPath);
                allSavedPaths[clipPath] = clipPath;
                totalClips++;
            }
        }

        if (allSavedPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No clips could be created. Check Console for details.", "OK");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Step 3: Load saved clips and organize by level
        var clipsByLevel = new Dictionary<string, Dictionary<string, Dictionary<string, AnimationClip>>>();
        foreach (var kvp in allSavedPaths)
        {
            string fileName = Path.GetFileNameWithoutExtension(kvp.Key);
            TryParseClipName(fileName, out string level, out string state, out string direction);
            if (string.IsNullOrEmpty(level)) continue;

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(kvp.Value);
            if (clip == null) continue;

            if (!clipsByLevel.ContainsKey(level))
                clipsByLevel[level] = new Dictionary<string, Dictionary<string, AnimationClip>>();
            if (!clipsByLevel[level].ContainsKey(state))
                clipsByLevel[level][state] = new Dictionary<string, AnimationClip>();
            clipsByLevel[level][state][direction] = clip;
        }

        // Step 4: Build controller for Level 1
        string levelToBuild = clipsByLevel.ContainsKey("lvl1") ? "lvl1" : clipsByLevel.Keys.First();
        PopulateAnimatorController(clipsByLevel[levelToBuild]);

        var player = GameObject.Find("Player");
        if (player != null)
        {
            var animator = player.GetComponent<Animator>();
            if (animator != null)
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            EditorUtility.SetDirty(player);
        }

        EditorUtility.DisplayDialog("Done",
            $"Player animation ready! {totalClips} clips, {clipsByLevel[levelToBuild].Count} states for {levelToBuild}.\n" +
            "Press Play to test.", "OK");
    }

    private static void FixSpriteRectsTo50x50(string pngPath)
    {
        var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
        if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple) return;

        var rects = importer.spritesheet;
        bool changed = false;

        for (int i = 0; i < rects.Length; i++)
        {
            var r = rects[i];
            // Keep pivot as-is but expand to 50x50 centered
            float cx = r.rect.center.x;
            float cy = r.rect.center.y;
            var newRect = new Rect(cx - 25, cy - 25, 50, 50);
            r.rect = newRect;
            rects[i] = r;
            changed = true;
        }

        if (changed)
        {
            importer.spritesheet = rects;
            importer.SaveAndReimport();
        }
    }

    private static AnimationClip CreateClip(List<Sprite> sprites, string clipName)
    {
        if (sprites.Count == 0) return null;

        var clip = new AnimationClip();
        clip.name = clipName;
        clip.frameRate = 10;

        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        float frameTime = 1f / clip.frameRate;
        var keyframes = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i * frameTime,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        return clip;
    }

    private static void TryParsePngName(string name, out string level, out string state)
    {
        level = state = "";
        // e.g. "Swordsman_lvl1_Idle_with_shadow"
        name = name.Replace("_with_shadow", "").Replace("With_shadow", "");
        var parts = name.Split('_');
        int lvlIdx = -1;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].StartsWith("lvl")) { lvlIdx = i; break; }
        }
        if (lvlIdx < 0 || lvlIdx + 1 >= parts.Length) return;
        level = parts[lvlIdx];
        state = string.Join("_", parts.Skip(lvlIdx + 1));
    }

    private static void TryParseClipName(string name, out string level, out string state, out string direction)
    {
        level = state = direction = "";
        // e.g. "lvl1_Idle_front"
        foreach (var d in Directions)
        {
            if (name.EndsWith("_" + d))
            {
                direction = d;
                name = name.Substring(0, name.Length - d.Length - 1);
                break;
            }
        }
        if (string.IsNullOrEmpty(direction)) return;

        var parts = name.Split('_');
        int lvlIdx = -1;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].StartsWith("lvl")) { lvlIdx = i; break; }
        }
        if (lvlIdx < 0 || lvlIdx + 1 >= parts.Length) return;
        level = parts[lvlIdx];
        state = string.Join("_", parts.Skip(lvlIdx + 1));
    }

    private static void PopulateAnimatorController(Dictionary<string, Dictionary<string, AnimationClip>> stateClips)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attacking", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        var idleState = sm.AddState("Idle");
        var idleBT = CreateBlendTree(controller, "IdleBlend", stateClips, "Idle");
        if (idleBT != null) idleState.motion = idleBT;

        var walkState = sm.AddState("Walk");
        var walkBT = CreateBlendTree(controller, "WalkBlend", stateClips, "Walk");
        if (walkBT != null) walkState.motion = walkBT;

        var i2w = idleState.AddTransition(walkState);
        i2w.hasExitTime = false; i2w.duration = 0;
        i2w.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var w2i = walkState.AddTransition(idleState);
        w2i.hasExitTime = false; w2i.duration = 0;
        w2i.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // Attack
        string attackName = stateClips.ContainsKey("Attack") ? "Attack" :
            stateClips.ContainsKey("Run_Attack") ? "Run_Attack" :
            stateClips.ContainsKey("Walk_Attack") ? "Walk_Attack" : null;
        if (attackName != null) AddOneShotState(controller, sm, "Attack", stateClips, attackName, "Attacking", idleState);

        // Run
        if (stateClips.ContainsKey("Run"))
        {
            var runState = sm.AddState("Run");
            var runBT = CreateBlendTree(controller, "RunBlend", stateClips, "Run");
            if (runBT != null)
            {
                runState.motion = runBT;
                var i2r = idleState.AddTransition(runState);
                i2r.hasExitTime = false; i2r.duration = 0;
                i2r.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed");

                var r2i = runState.AddTransition(idleState);
                r2i.hasExitTime = false; r2i.duration = 0;
                r2i.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

                var w2r = walkState.AddTransition(runState);
                w2r.hasExitTime = false; w2r.duration = 0;
                w2r.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed");

                var r2w = runState.AddTransition(walkState);
                r2w.hasExitTime = false; r2w.duration = 0;
                r2w.AddCondition(AnimatorConditionMode.Less, 0.5f, "Speed");
                r2w.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            }
        }

        if (stateClips.ContainsKey("Hurt"))
            AddOneShotState(controller, sm, "Hurt", stateClips, "Hurt", "Hurt", idleState);

        if (stateClips.ContainsKey("Death"))
            AddOneShotState(controller, sm, "Death", stateClips, "Death", "Dead", idleState, hasExit: false);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
    }

    private static AnimatorState AddOneShotState(AnimatorController ctrl, AnimatorStateMachine sm,
        string stateName, Dictionary<string, Dictionary<string, AnimationClip>> stateClips,
        string clipKey, string triggerParam, AnimatorState idleState, bool hasExit = true)
    {
        var state = sm.AddState(stateName);
        var bt = CreateBlendTree(ctrl, stateName + "Blend", stateClips, clipKey);
        if (bt != null) state.motion = bt;
        else if (stateClips[clipKey].Count > 0)
            state.motion = stateClips[clipKey].First().Value;

        var anyTo = sm.AddAnyStateTransition(state);
        anyTo.hasExitTime = false; anyTo.duration = 0;
        anyTo.AddCondition(AnimatorConditionMode.If, 0, triggerParam);

        if (hasExit)
        {
            var toIdle = state.AddTransition(idleState);
            toIdle.hasExitTime = true; toIdle.duration = 0; toIdle.exitTime = 1;
        }

        return state;
    }

    private static BlendTree CreateBlendTree(AnimatorController ctrl,
        string name, Dictionary<string, Dictionary<string, AnimationClip>> stateClips,
        string stateKey)
    {
        if (!stateClips.ContainsKey(stateKey)) return null;
        var clips = stateClips[stateKey];
        if (clips.Count == 0) return null;

        var bt = new BlendTree();
        bt.name = name;
        bt.blendType = BlendTreeType.SimpleDirectional2D;
        bt.blendParameter = "MoveX";
        bt.blendParameterY = "MoveY";
        bt.useAutomaticThresholds = true;

        AddDirection(bt, clips, "front", new Vector2(0f, -1f));
        AddDirection(bt, clips, "back", new Vector2(0f, 1f));
        AddDirection(bt, clips, "side_left", new Vector2(-1f, 0f));
        AddDirection(bt, clips, "side_right", new Vector2(1f, 0f));

        bt.hideFlags = HideFlags.HideInHierarchy;
        if (AssetDatabase.Contains(ctrl))
            AssetDatabase.AddObjectToAsset(bt, ctrl);
        return bt;
    }

    private static void AddDirection(BlendTree bt, Dictionary<string, AnimationClip> clips,
        string direction, Vector2 pos)
    {
        if (clips.TryGetValue(direction, out var clip) && clip != null)
            bt.AddChild(clip, pos);
    }
}