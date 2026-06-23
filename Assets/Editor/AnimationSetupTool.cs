using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class AnimationSetupTool
{
    private const string PartsRoot = "Assets/swordman/PNG/Swordsman_lvl1";
    private const string AnimOut = "Assets/_Game/Animations/Player";
    private const string ControllerPath = "Assets/_Game/Animations/Player/Player.controller";

    private static readonly string[] Directions = { "front", "side_left", "side_right", "back" };
    private static readonly string[] States = { "Idle", "Walk", "Run", "Attack", "Hurt", "Death" };

    private static readonly Dictionary<string, string> PartToPath = new()
    {
        {"shadow", "Shadow"},
        {"body", "Body"},
        {"head", "Head"},
        {"sword", "WeaponFront"},
        {"sword_back", "WeaponBack"},
        {"swing", "SwingEffect"},
    };

    private static readonly Dictionary<string, int[]> PartDirectionRowMap = new()
    {
        {"shadow", new[] {0, 1, 2, 3}},
        {"body", new[] {0, 1, 2, 3}},
        {"head", new[] {0, 1, 2, 3}},
        {"sword", new[] {0, -1, 1, -1}},
        {"sword_back", new[] {-1, 0, -1, 1}},
        {"swing", new[] {0, 1, 2, 3}},
    };

    [MenuItem("Tools/Player Animation Setup")]
    public static void SetupPlayerAnimations()
    {
        Directory.CreateDirectory(AnimOut);
        CleanOldClips();

        var pngFiles = FindPartPngs();
        if (pngFiles.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", $"No Part PNGs found in {PartsRoot}/Parts/", "OK");
            return;
        }

        var clipsByStateDir = CreateClips(pngFiles);
        if (clipsByStateDir.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No clips created. Check Console.", "OK");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        BuildController(clipsByStateDir);
        SetupPlayerHierarchy();

        EditorUtility.DisplayDialog("Done",
            $"Modular player animation ready! Total: {clipsByStateDir.Sum(kvp => kvp.Value.Count)} clips " +
            $"across {clipsByStateDir.Count} states.\n" +
            "Run Tools/Normalize Part Slicing if layers are misaligned.", "OK");
    }

    private static Dictionary<string, Dictionary<string, AnimationClip>> CreateClips(List<string> pngFiles)
    {
        var clipsByStateDir = new Dictionary<string, Dictionary<string, AnimationClip>>();

        foreach (var targetState in States)
        {
            var partsByPart = new Dictionary<string, List<Sprite>[]>();

            foreach (var pngPath in pngFiles)
            {
                string name = Path.GetFileNameWithoutExtension(pngPath);
                if (!TryParsePartName(name, out string _, out string state, out string part))
                    continue;
                if (!string.Equals(state, targetState, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!PartToPath.ContainsKey(part))
                    continue;

                var allAssets = AssetDatabase.LoadAllAssetsAtPath(pngPath);
                var sprites = allAssets.OfType<Sprite>().ToArray();
                if (sprites.Length == 0) continue;

                var rows = SplitIntoRows(sprites);
                partsByPart[part] = rows;
            }

            if (partsByPart.Count == 0) continue;

            for (int d = 0; d < 4; d++)
            {
                var dirSprites = new Dictionary<string, List<Sprite>>();

                foreach (var kvp in partsByPart)
                {
                    string part = kvp.Key;
                    var rows = kvp.Value;
                    int rowIdx = PartDirectionRowMap[part][d];
                    if (rowIdx < 0 || rowIdx >= rows.Length) continue;
                    dirSprites[part] = rows[rowIdx];
                }

                if (dirSprites.Count == 0) continue;

                int minFrames = dirSprites.Min(kvp => kvp.Value.Count);

                string clipName = $"lvl1_{targetState}_{Directions[d]}";
                var clip = new AnimationClip();
                clip.name = clipName;
                clip.frameRate = 10;

                foreach (var kvp in dirSprites)
                {
                    string path = PartToPath[kvp.Key];
                    var sprites = kvp.Value.GetRange(0, minFrames);

                    var binding = new EditorCurveBinding
                    {
                        type = typeof(SpriteRenderer),
                        path = path,
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
                }

                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = StatesWithLoop.Contains(targetState);
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                string clipPath = $"{AnimOut}/{clipName}.anim";
                AssetDatabase.CreateAsset(clip, clipPath);

                if (!clipsByStateDir.ContainsKey(targetState))
                    clipsByStateDir[targetState] = new Dictionary<string, AnimationClip>();
                clipsByStateDir[targetState][Directions[d]] = clip;
            }
        }

        return clipsByStateDir;
    }

    private static readonly HashSet<string> StatesWithLoop = new() { "Idle", "Walk", "Run" };

    private static void CleanOldClips()
    {
        string fullOut = Path.Combine(Application.dataPath, AnimOut.Substring("Assets/".Length));
        if (Directory.Exists(fullOut))
        {
            foreach (var f in Directory.GetFiles(fullOut, "*.anim"))
            {
                string rel = "Assets" + f.Substring(Application.dataPath.Length).Replace("\\", "/");
                AssetDatabase.DeleteAsset(rel);
            }
        }
        AssetDatabase.DeleteAsset(ControllerPath);
        AssetDatabase.Refresh();
    }

    private static List<string> FindPartPngs()
    {
        var files = new List<string>();
        string fullDir = Path.Combine(Application.dataPath, "swordman/PNG/Swordsman_lvl1/Parts");
        if (!Directory.Exists(fullDir)) return files;
        foreach (var f in Directory.GetFiles(fullDir, "*.png"))
        {
            string rel = "Assets" + f.Substring(Application.dataPath.Length).Replace("\\", "/");
            files.Add(rel);
        }
        return files;
    }

    private static bool TryParsePartName(string name, out string level, out string state, out string part)
    {
        level = state = part = "";
        var parts = name.Split('_');
        int lvlIdx = -1;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].StartsWith("lvl")) { lvlIdx = i; break; }
        }
        if (lvlIdx < 0 || lvlIdx + 2 >= parts.Length) return false;
        level = parts[lvlIdx];

        int stateEnd = name.Length;
        string nameLower = name.ToLower();

        foreach (var knownPart in PartToPath.Keys)
        {
            string suffix = "_" + knownPart.ToLower();
            if (nameLower.EndsWith(suffix))
            {
                part = knownPart;
                stateEnd = nameLower.Length - suffix.Length;
                break;
            }
        }

        if (string.IsNullOrEmpty(part)) return false;

        string stateRaw = name.Substring(0, stateEnd);
        var stateParts = stateRaw.Split('_');
        state = string.Join("_", stateParts.Skip(lvlIdx + 1));

        return !string.IsNullOrEmpty(state);
    }

    private static List<Sprite>[] SplitIntoRows(Sprite[] sprites)
    {
        var sorted = sprites
            .OrderByDescending(s => s.rect.y)
            .ThenBy(s => s.rect.x)
            .ToArray();

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
            else
            {
                currentRow.Add(sorted[i]);
            }
        }
        rows.Add(currentRow);

        return rows.Select(r => r.ToList()).ToArray();
    }

    private static void SetupPlayerHierarchy()
    {
        var player = GameObject.Find("Player");
        if (player == null) return;

        var animator = player.GetComponent<Animator>();
        if (animator != null)
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

        // Remove existing part children
        var existing = new List<GameObject>();
        foreach (Transform child in player.transform)
            existing.Add(child.gameObject);
        foreach (var go in existing)
            Object.DestroyImmediate(go);

        // Create children in correct sorting order
        var parts = new (string name, int order)[]
        {
            ("Shadow", 0),
            ("WeaponBack", 1),
            ("Body", 2),
            ("Head", 3),
            ("WeaponFront", 4),
            ("SwingEffect", 5),
        };

        foreach (var (name, order) in parts)
        {
            var child = new GameObject(name);
            child.transform.SetParent(player.transform, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            var sr = child.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
        }

        EditorUtility.SetDirty(player);
    }

    private static void BuildController(Dictionary<string, Dictionary<string, AnimationClip>> stateClips)
    {
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

        if (stateClips.ContainsKey("Attack"))
            AddOneShotState(controller, sm, "Attack", stateClips, "Attack", "Attacking", idleState, true);

        if (stateClips.ContainsKey("Hurt"))
            AddOneShotState(controller, sm, "Hurt", stateClips, "Hurt", "Hurt", idleState, true);

        if (stateClips.ContainsKey("Death"))
            AddOneShotState(controller, sm, "Death", stateClips, "Death", "Dead", idleState);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
    }

    private static void AddOneShotState(AnimatorController ctrl, AnimatorStateMachine sm,
        string stateName, Dictionary<string, Dictionary<string, AnimationClip>> stateClips,
        string clipKey, string triggerParam, AnimatorState idleState, bool hasExit = false)
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
