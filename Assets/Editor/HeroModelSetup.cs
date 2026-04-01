using UnityEngine;
using UnityEditor;
using TacticalDuelist.Core.Config;

namespace TacticalDuelist.Editor
{
    /// <summary>
    /// One-click setup for a hero 3D model.
    /// Creates material from texture, creates prefab, assigns to HeroConfig.
    /// Menu: Tactical Duelist > Setup Hero Model
    /// </summary>
    public static class HeroModelSetup
    {
        [MenuItem("Tactical Duelist/Setup Archer Model")]
        public static void SetupArcherModel()
        {
            SetupHeroModel(
                fbxPath: "Assets/TD_Archer_ver001/TD_Archer_ver001.fbx",
                texturePath: "Assets/TD_Archer_ver001/Textures/fantasyadventurer_ver3_basecolor.JPEG",
                heroConfigPath: "Assets/ScriptableObjects/Heroes/Hero_Archer.asset",
                prefabName: "Hero_Archer",
                scale: 1f
            );
        }

        [MenuItem("Tactical Duelist/Setup All Hero Models")]
        public static void SetupAllModels()
        {
            SetupArcherModel();

            SetupHeroModel(
                fbxPath: "Assets/Unity Export/Ferro/Ferro.fbx",
                texturePath: "Assets/Unity Export/Ferro/Ferro(Tank)_basecolor.JPEG",
                heroConfigPath: "Assets/ScriptableObjects/Heroes/Hero_Tank.asset",
                prefabName: "Hero_Tank"
            );

            SetupHeroModel(
                fbxPath: "Assets/Unity Export/Kai/Kai.fbx",
                texturePath: "Assets/Unity Export/Kai/Kai(Shadow)_basecolor.JPEG",
                heroConfigPath: "Assets/ScriptableObjects/Heroes/Hero_Shadow.asset",
                prefabName: "Hero_Shadow"
            );

            SetupHeroModel(
                fbxPath: "Assets/Unity Export/Nova/Nova.fbx",
                texturePath: "Assets/Unity Export/Nova/Nova(Scout)_basecolor.JPEG",
                heroConfigPath: "Assets/ScriptableObjects/Heroes/Hero_Scout.asset",
                prefabName: "Hero_Scout"
            );

            SetupHeroModel(
                fbxPath: "Assets/Unity Export/Boomer/Boomer.fbx",
                texturePath: "Assets/Unity Export/Boomer/Boomer_basecolor.JPEG",
                heroConfigPath: "Assets/ScriptableObjects/Heroes/Hero_Demo.asset",
                prefabName: "Hero_Demo"
            );

            SetupHeroModel(
                fbxPath: "Assets/Unity Export/Tinker/Tinker.fbx",
                texturePath: "Assets/Unity Export/Tinker/Tinker(Engineer)_basecolor.JPEG",
                heroConfigPath: "Assets/ScriptableObjects/Heroes/Hero_Engineer.asset",
                prefabName: "Hero_Engineer"
            );

            SetupHeroModel(
                fbxPath: "Assets/Unity Export/Whisper/Whisper.fbx",
                texturePath: "Assets/Unity Export/Whisper/Whisper(Ghost)_basecolor.JPEG",
                heroConfigPath: "Assets/ScriptableObjects/Heroes/Hero_Ghost.asset",
                prefabName: "Hero_Ghost"
            );

            Debug.Log("[HeroModelSetup] All 7 hero models set up!");
        }

        public static void SetupHeroModel(
            string fbxPath, string texturePath, string heroConfigPath,
            string prefabName, float scale = 1f)
        {
            // 1. Load FBX
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null)
            {
                Debug.LogError($"[HeroModelSetup] FBX not found: {fbxPath}");
                return;
            }

            // 2. Configure FBX import (Humanoid rig + extract materials)
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null)
            {
                bool needsReimport = false;

                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    needsReimport = true;
                }

                // Extract materials so they become editable assets
                if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
                {
                    importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    needsReimport = true;
                }

                if (needsReimport)
                {
                    // Extract materials to a folder next to the FBX
                    var matExtractFolder = System.IO.Path.GetDirectoryName(fbxPath) + "/Materials";
                    EnsureFolder(matExtractFolder);
                    importer.ExtractTextures(matExtractFolder);
                    importer.SaveAndReimport();
                    fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                    Debug.Log("[HeroModelSetup] Configured FBX import (Humanoid + materials)");
                }
            }

            // 3. Find material — use existing from FBX, or create with texture
            Material mat = null;

            // Try to find extracted/embedded material from FBX
            var fbxAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in fbxAssets)
            {
                if (asset is Material m && m.name != "Default-Material")
                {
                    mat = m;
                    Debug.Log($"[HeroModelSetup] Using FBX embedded material: {m.name}");
                    break;
                }
            }

            // Also check extracted materials folder
            if (mat == null)
            {
                var extractedMatDir = System.IO.Path.GetDirectoryName(fbxPath) + "/Materials";
                var matGuids = AssetDatabase.FindAssets("t:Material", new[] { extractedMatDir });
                if (matGuids.Length > 0)
                {
                    var extractedMatPath = AssetDatabase.GUIDToAssetPath(matGuids[0]);
                    mat = AssetDatabase.LoadAssetAtPath<Material>(extractedMatPath);
                    Debug.Log($"[HeroModelSetup] Using extracted material: {extractedMatPath}");
                }
            }

            // Fallback: create material from texture using a known-good shader reference
            if (mat == null)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

                // Find a URP Lit shader by referencing an existing project material
                var refMatGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });
                Shader shader = null;
                foreach (var guid in refMatGuids)
                {
                    var refMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    if (refMat != null && refMat.shader != null)
                    {
                        shader = refMat.shader;
                        break;
                    }
                }
                if (shader == null) shader = Shader.Find("Standard");

                mat = new Material(shader);
                if (texture != null)
                {
                    mat.mainTexture = texture;
                    mat.SetFloat("_Smoothness", 0.2f);
                }

                var matFolder = "Assets/Materials/Heroes";
                EnsureFolder(matFolder);
                var matPath = $"{matFolder}/{prefabName}_Mat.mat";
                AssetDatabase.CreateAsset(mat, matPath);
                Debug.Log($"[HeroModelSetup] Created fallback material: {matPath}");
            }

            // 4. Instantiate FBX in scene to create prefab
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            instance.name = prefabName;

            // Apply material to all renderers
            var renderers = instance.GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                var mats = new Material[rend.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                rend.sharedMaterials = mats;
            }

            // Scale to ~2 units height
            var bounds = CalculateBounds(instance);
            if (bounds.size.y > 0)
            {
                float targetHeight = 2f * scale;
                float currentHeight = bounds.size.y;
                float scaleFactor = targetHeight / currentHeight;
                instance.transform.localScale = Vector3.one * scaleFactor;

                // Center at feet
                bounds = CalculateBounds(instance);
                float yOffset = -bounds.min.y;
                instance.transform.position = new Vector3(0, yOffset, 0);
            }

            // Ensure Animator component exists with Avatar for humanoid retargeting
            var animator = instance.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                Debug.Log($"[HeroModelSetup] Animator found: avatar={animator.avatar?.name ?? "NONE"}, isHuman={animator.avatar?.isHuman}");
            }
            else
            {
                Debug.LogWarning($"[HeroModelSetup] No Animator on {prefabName} — animations won't work");
            }

            // 5. Save as prefab
            var prefabFolder = "Assets/Prefabs/Heroes";
            EnsureFolder(prefabFolder);
            var prefabPath = $"{prefabFolder}/{prefabName}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            Debug.Log($"[HeroModelSetup] Created prefab: {prefabPath}");

            // 6. Assign to HeroConfig
            var heroConfig = AssetDatabase.LoadAssetAtPath<HeroConfig>(heroConfigPath);
            if (heroConfig != null)
            {
                var so = new SerializedObject(heroConfig);
                var prop = so.FindProperty("heroPrefab");
                if (prop != null)
                {
                    prop.objectReferenceValue = prefab;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(heroConfig);
                    Debug.Log($"[HeroModelSetup] Assigned prefab to {heroConfig.heroName}");
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[HeroModelSetup] Setup complete for {prefabName}!");
        }

        private static Bounds CalculateBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
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
