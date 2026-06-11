using BecomingLegend;
using BecomingLegend.Actors;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class AnimalAnimationSetupTool
{
    private const string PngRoot = "Assets/_Game/animals/PNG/With_Shadow";
    private const string AnimRoot = "Assets/_Game/Animations/Animals";
    private const string PrefabRoot = "Assets/_Game/Prefabs/Enemies";

    private static readonly string[] DirectionNames = { "front", "back", "side_left", "side_right" };

    [MenuItem("Tools/Animal Animation Setup")]
    public static void SetupAnimalAnimations()
    {
        var dirs = Directory.GetDirectories(Path.Combine(Application.dataPath, "_Game/animals/PNG/With_Shadow"));
        if (dirs.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", $"No animal directories found in {PngRoot}", "OK");
            return;
        }

        Directory.CreateDirectory(AnimRoot);
        Directory.CreateDirectory(PrefabRoot);

        // Step 1: Fix all sprite rects to 50x50 centered
        var allPngPaths = new List<string>();
        foreach (var dir in dirs)
        {
            foreach (var f in Directory.GetFiles(dir, "*.png"))
            {
                string rel = "Assets" + f.Substring(Application.dataPath.Length).Replace("\\", "/");
                FixSpriteRectsTo50x50(rel);
                allPngPaths.Add(rel);
            }
        }
        AssetDatabase.Refresh();

        // Step 2: Create clips per (animal, state, direction)
        var clipsByAnimal = new Dictionary<string, Dictionary<string, Dictionary<string, AnimationClip>>>();
        int totalClips = 0;

        foreach (var pngPath in allPngPaths)
        {
            string name = Path.GetFileNameWithoutExtension(pngPath);
            TryParseAnimalPngName(name, out string animal, out string state);
            if (string.IsNullOrEmpty(animal) || string.IsNullOrEmpty(state)) continue;

            var sprites = AssetDatabase.LoadAllAssetsAtPath(pngPath).OfType<Sprite>().ToArray();
            if (sprites.Length == 0) continue;

            // Sort by position (top-to-bottom, left-to-right)
            var sorted = sprites.OrderBy(s => -s.rect.y).ThenBy(s => s.rect.x).ToArray();

            // Group into 4 rows by y-position (tolerance 20px)
            var rows = new List<List<Sprite>>();
            var currentRow = new List<Sprite> { sorted[0] };
            float lastY = sorted[0].rect.y;
            for (int i = 1; i < sorted.Length; i++)
            {
                if (Mathf.Abs(sorted[i].rect.y - lastY) > 20f)
                {
                    rows.Add(currentRow);
                    currentRow = new List<Sprite> { sorted[i] };
                    lastY = sorted[i].rect.y;
                }
                else currentRow.Add(sorted[i]);
            }
            rows.Add(currentRow);

            // Sort rows by y descending (top row = front/down)
            rows = rows.OrderBy(r => -r[0].rect.y).ToList();

            string animalDir = $"{AnimRoot}/{animal}";
            Directory.CreateDirectory(animalDir);

            for (int d = 0; d < rows.Count && d < DirectionNames.Length; d++)
            {
                string direction = DirectionNames[d];
                string clipName = $"{animal}_{state}_{direction}";
                var clip = CreateClip(rows[d], clipName);
                if (clip == null) continue;

                string clipPath = $"{animalDir}/{clipName}.anim";
                AssetDatabase.CreateAsset(clip, clipPath);
                totalClips++;

                if (!clipsByAnimal.ContainsKey(animal))
                    clipsByAnimal[animal] = new Dictionary<string, Dictionary<string, AnimationClip>>();
                if (!clipsByAnimal[animal].ContainsKey(state))
                    clipsByAnimal[animal][state] = new Dictionary<string, AnimationClip>();
                clipsByAnimal[animal][state][direction] = clip;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Step 3: Reload saved clips and create controllers + prefabs
        foreach (var animal in clipsByAnimal.Keys)
        {
            var reloaded = ReloadClips(animal, clipsByAnimal[animal]);
            CreateController(animal, reloaded);
            CreatePrefab(animal, reloaded);
        }

        EditorUtility.DisplayDialog("Done",
            $"Created animations for {clipsByAnimal.Count} animals, {totalClips} total clips.\n" +
            "Check Assets/_Game/Prefabs/Enemies/ for prefabs.", "OK");
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
            float cx = r.rect.center.x;
            float cy = r.rect.center.y;
            r.rect = new Rect(cx - 25, cy - 25, 50, 50);
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

    private static void TryParseAnimalPngName(string name, out string animal, out string state)
    {
        animal = state = "";
        name = name.Replace("_with_shadow", "").Replace("With_shadow", "");

        // Split on first capital letter boundary after the animal name
        // e.g. "Fox_walk" -> animal="Fox", state="walk"
        // e.g. "Black_grouse_Walk" -> animal="Black_grouse", state="Walk"
        var parts = name.Split('_');

        if (parts.Length < 2) return;

        // Animal name is everything before the state (state starts at known index)
        // Known animals: Black_grouse, Boar, Deer, Fox, Hare
        // For compound names like Black_grouse, try to detect:
        // First try: animal is everything up to a known state keyword
        string[] stateKeywords = { "Idle", "Walk", "walk", "Run", "Attack", "Hurt", "Death", "Flight" };

        int stateIdx = -1;
        for (int i = 1; i < parts.Length; i++)
        {
            if (stateKeywords.Contains(parts[i]))
            {
                stateIdx = i;
                break;
            }
        }

        if (stateIdx < 0) return;

        animal = string.Join("_", parts.Take(stateIdx));
        state = string.Join("_", parts.Skip(stateIdx));
    }

    private static Dictionary<string, Dictionary<string, AnimationClip>> ReloadClips(
        string animal, Dictionary<string, Dictionary<string, AnimationClip>> original)
    {
        var result = new Dictionary<string, Dictionary<string, AnimationClip>>();
        string animalDir = $"{AnimRoot}/{animal}";

        foreach (var stateKvp in original)
        {
            var dirDict = new Dictionary<string, AnimationClip>();
            foreach (var dirKvp in stateKvp.Value)
            {
                string clipPath = $"{animalDir}/{animal}_{stateKvp.Key}_{dirKvp.Key}.anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip != null)
                    dirDict[dirKvp.Key] = clip;
            }
            if (dirDict.Count > 0)
                result[stateKvp.Key] = dirDict;
        }
        return result;
    }

    private static void CreateController(string animal, Dictionary<string, Dictionary<string, AnimationClip>> stateClips)
    {
        string path = $"{AnimRoot}/{animal}/{animal}.controller";
        AssetDatabase.DeleteAsset(path);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attacking", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        // Idle (always present)
        var idleState = sm.AddState("Idle");
        var idleBT = CreateBlendTree(controller, "IdleBlend", stateClips, "Idle");
        if (idleBT != null) idleState.motion = idleBT;
        else if (stateClips.ContainsKey("Idle") && stateClips["Idle"].Count > 0)
            idleState.motion = stateClips["Idle"].First().Value;

        // Walk (always present for all animals)
        if (stateClips.ContainsKey("Walk") || stateClips.ContainsKey("walk"))
        {
            string walkKey = stateClips.ContainsKey("Walk") ? "Walk" : "walk";
            var walkState = sm.AddState("Walk");
            var walkBT = CreateBlendTree(controller, "WalkBlend", stateClips, walkKey);
            if (walkBT != null) walkState.motion = walkBT;

            var i2w = idleState.AddTransition(walkState);
            i2w.hasExitTime = false; i2w.duration = 0;
            i2w.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var w2i = walkState.AddTransition(idleState);
            w2i.hasExitTime = false; w2i.duration = 0;
            w2i.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        }

        // Run or Flight (one of them may exist)
        string runKey = null;
        if (stateClips.ContainsKey("Run")) runKey = "Run";
        else if (stateClips.ContainsKey("Flight")) runKey = "Flight";

        if (runKey != null)
        {
            var runState = sm.AddState("Run");
            var runBT = CreateBlendTree(controller, "RunBlend", stateClips, runKey);
            if (runBT != null)
            {
                runState.motion = runBT;

                var i2r = idleState.AddTransition(runState);
                i2r.hasExitTime = false; i2r.duration = 0;
                i2r.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed");

                var r2i = runState.AddTransition(idleState);
                r2i.hasExitTime = false; r2i.duration = 0;
                r2i.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            }
        }

        // Attack (if exists)
        if (stateClips.ContainsKey("Attack"))
            AddOneShotState(controller, sm, "Attack", stateClips, "Attack", "Attacking", idleState);

        // Hurt
        if (stateClips.ContainsKey("Hurt"))
            AddOneShotState(controller, sm, "Hurt", stateClips, "Hurt", "Hurt", idleState);

        // Death
        if (stateClips.ContainsKey("Death"))
            AddOneShotState(controller, sm, "Death", stateClips, "Death", "Dead", idleState, hasExit: false);

        EditorUtility.SetDirty(controller);
    }

    private static void CreatePrefab(string animal, Dictionary<string, Dictionary<string, AnimationClip>> stateClips)
    {
        string prefabPath = $"{PrefabRoot}/{animal}.prefab";

        var go = new GameObject(animal);
        go.layer = LayerMask.NameToLayer("Default");

        // EnemyActor first — auto-adds SpriteRenderer + Animator via RequireComponent
        var enemy = go.AddComponent<EnemyActor>();

        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = stateClips.Values.First().Values.First().GetSpriteFromClip();

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb == null) rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;

        var collider = go.AddComponent<CapsuleCollider2D>();
        collider.isTrigger = false;

        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimRoot}/{animal}/{animal}.controller");
        var animator = go.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        var serialized = new SerializedObject(enemy);
        serialized.Update();

        var statsProp = serialized.FindProperty("stats");
        if (statsProp != null)
        {
            var entries = statsProp.FindPropertyRelative("entries");
            if (entries != null)
            {
                entries.ClearArray();
                AddStatEntry(entries, 0, "Strength", 3f);
                AddStatEntry(entries, 1, "Speed", 3f);
                AddStatEntry(entries, 2, "Stamina", 3f);
                AddStatEntry(entries, 3, "Core", 3f);
            }
        }

        var nameProp = serialized.FindProperty("actorName");
        if (nameProp != null) nameProp.stringValue = animal;

        serialized.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
    }

    private static void AddStatEntry(SerializedProperty entries, int index, string statType, float value)
    {
        entries.InsertArrayElementAtIndex(index);
        var entry = entries.GetArrayElementAtIndex(index);
        var typeProp = entry.FindPropertyRelative("type");
        var valueProp = entry.FindPropertyRelative("value");
        if (typeProp != null) typeProp.enumValueIndex = (int)System.Enum.Parse(typeof(StatType), statType);
        if (valueProp != null) valueProp.floatValue = value;
    }

    private static Sprite GetSpriteFromClip(this AnimationClip clip)
    {
        var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        if (bindings.Length == 0) return null;
        var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
        return keyframes.Length > 0 ? keyframes[0].value as Sprite : null;
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
}
