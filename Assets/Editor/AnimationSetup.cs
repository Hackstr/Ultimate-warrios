using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace TacticalDuelist.Editor
{
    public static class AnimationSetup
    {
        private const string AnimFBX = "Assets/Unity Export/Animations/Animations.fbx";
        private const string OutputPath = "Assets/Animations";

        private static readonly Dictionary<string, string> HeroConfigMap = new() {
            { "Boomer", "Assets/ScriptableObjects/Heroes/Hero_Demo.asset" },
            { "Ferro", "Assets/ScriptableObjects/Heroes/Hero_Tank.asset" },
            { "Kai", "Assets/ScriptableObjects/Heroes/Hero_Shadow.asset" },
            { "Nova", "Assets/ScriptableObjects/Heroes/Hero_Scout.asset" },
            { "Tinker", "Assets/ScriptableObjects/Heroes/Hero_Engineer.asset" },
            { "Whisper", "Assets/ScriptableObjects/Heroes/Hero_Ghost.asset" },
            { "Archer", "Assets/ScriptableObjects/Heroes/Hero_Archer.asset" },
        };

        [MenuItem("Tactical Duelist/Setup Animations")]
        public static void SetupAnimations()
        {
            // 1. Ensure FBX is set to import animations with Humanoid type
            var importer = AssetImporter.GetAtPath(AnimFBX) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[AnimationSetup] FBX not found: {AnimFBX}");
                return;
            }

            // Force reimport: Humanoid + auto-detect animations + CLEAR manual clips
            importer.animationType = ModelImporterAnimationType.Human;
            importer.importAnimation = true;

            // CRITICAL: clear clipAnimations to let Unity auto-detect take durations
            // When clipAnimations is empty, Unity reads each take's full frame range
            importer.clipAnimations = new ModelImporterClipAnimation[0];

            importer.SaveAndReimport();
            Debug.Log("[AnimationSetup] FBX reimported: Humanoid, auto-detect clips");

            // 2. Find all clips from the FBX
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(AnimFBX);
            var clips = allAssets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")).ToList();
            Debug.Log($"[AnimationSetup] Found {clips.Count} clips in FBX:");
            foreach (var clip in clips)
                Debug.Log($"  - {clip.name} ({clip.length:F2}s, loop={clip.isLooping})");

            if (clips.Count == 0)
            {
                Debug.LogWarning("[AnimationSetup] No clips found! The FBX may need manual clip configuration.");
                return;
            }

            // 3. Create Animator Controller for each hero
            EnsureFolder(OutputPath);

            foreach (var kvp in HeroConfigMap)
            {
                CreateControllerForHero(kvp.Key, kvp.Value, clips);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[AnimationSetup] All animation controllers created!");
        }

        private static void CreateControllerForHero(string heroName, string configPath, List<AnimationClip> allClips)
        {
            string controllerPath = $"{OutputPath}/{heroName}_Controller.controller";

            // Delete existing
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath) != null)
                AssetDatabase.DeleteAsset(controllerPath);

            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            // Parameters matching HeroView3D
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Victory", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Defeat", AnimatorControllerParameterType.Trigger);

            var rootSM = controller.layers[0].stateMachine;

            // Find clips for this hero (or use Boomer as fallback)
            // Clips are named: "Boomer_Idle", "Ferro_Attack", etc.
            var idleClip = FindClip(allClips, heroName, "Idle") ?? FindClip(allClips, "Boomer", "Idle");
            var walkClip = FindClip(allClips, heroName, "Walk") ?? FindClip(allClips, "Boomer", "Walk");
            var attackClip = FindClip(allClips, heroName, "Attack") ?? FindClip(allClips, "Boomer", "Attack");
            var dieClip = FindClip(allClips, heroName, "Die") ?? FindClip(allClips, "Boomer", "Die");

            // States
            var idleState = rootSM.AddState("Idle");
            idleState.motion = idleClip;
            rootSM.defaultState = idleState;

            var walkState = rootSM.AddState("Walk");
            walkState.motion = walkClip;

            var shootState = rootSM.AddState("Shoot");
            shootState.motion = attackClip;

            var deathState = rootSM.AddState("Death");
            deathState.motion = dieClip;

            // Idle <-> Walk
            var t1 = idleState.AddTransition(walkState);
            t1.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsMoving");
            t1.hasExitTime = false; t1.duration = 0.1f;

            var t2 = walkState.AddTransition(idleState);
            t2.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsMoving");
            t2.hasExitTime = false; t2.duration = 0.1f;

            // Any -> Shoot
            var t3 = rootSM.AddAnyStateTransition(shootState);
            t3.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Shoot");
            t3.hasExitTime = false; t3.duration = 0.05f;

            // Shoot -> Idle
            var t4 = shootState.AddTransition(idleState);
            t4.hasExitTime = true; t4.exitTime = 0.9f; t4.duration = 0.1f;

            // Any -> Death
            var t5 = rootSM.AddAnyStateTransition(deathState);
            t5.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Death");
            t5.hasExitTime = false; t5.duration = 0.1f;

            EditorUtility.SetDirty(controller);

            // Assign to HeroConfig
            var heroConfig = AssetDatabase.LoadAssetAtPath<Core.Config.HeroConfig>(configPath);
            if (heroConfig != null)
            {
                var so = new SerializedObject(heroConfig);
                var prop = so.FindProperty("animatorController");
                if (prop != null)
                {
                    prop.objectReferenceValue = controller;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(heroConfig);
                }
            }

            Debug.Log($"[AnimationSetup] {heroName}: idle={idleClip?.name ?? "NONE"}, walk={walkClip?.name ?? "NONE"}, attack={attackClip?.name ?? "NONE"}, die={dieClip?.name ?? "NONE"}");
        }

        private static AnimationClip FindClip(List<AnimationClip> clips, string heroName, string action)
        {
            // Try: "Boomer_Idle"
            var c = clips.FirstOrDefault(x => x.name == $"{heroName}_{action}");
            if (c != null) return c;

            // Try: "TD_Boomer_Idle"
            c = clips.FirstOrDefault(x => x.name == $"TD_{heroName}_{action}");
            if (c != null) return c;

            // Try contains both
            c = clips.FirstOrDefault(x =>
                x.name.IndexOf(heroName, System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                x.name.IndexOf(action, System.StringComparison.OrdinalIgnoreCase) >= 0);
            return c;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts = path.Split('/');
                var current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }
    }
}
