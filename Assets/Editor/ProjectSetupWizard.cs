using UnityEngine;
using UnityEditor;
using TacticalDuelist.Core.Config;

namespace TacticalDuelist.Editor
{
    /// <summary>
    /// One-click project setup. Creates grid materials, wires HeroConfig prefabs,
    /// assigns materials to GameBootstrap, and ensures URP Lit shader is in build.
    /// Run via menu: Tactical Duelist > Setup Project.
    /// </summary>
    public static class ProjectSetupWizard
    {
        private const string MaterialsPath = "Assets/Materials/Grid";
        private const string HeroesPath = "Assets/ScriptableObjects/Heroes";
        private const string PrefabsPath = "Assets/Prefabs/Heroes";
        private const string MapPath = "Assets/ScriptableObjects/Maps/Map_Arena01.asset";

        [MenuItem("Tactical Duelist/Setup Project")]
        public static void RunFullSetup()
        {
            CreateGridMaterials();
            WireMapConfigMaterials();
            WireHeroPrefabs();
            WireGameBootstrapMaterials();
            AddShaderToAlwaysIncluded();
            UIToolkitSetup.SetupUIToolkit();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[ProjectSetup] Setup complete!");
        }

        private static void CreateGridMaterials()
        {
            EnsureFolder(MaterialsPath);

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("[ProjectSetup] URP Lit shader not found!");
                return;
            }

            CreateMat("Floor",          shader, new Color(0.20f, 0.22f, 0.28f, 1f));
            CreateMat("FloorAlt",       shader, new Color(0.16f, 0.18f, 0.24f, 1f));
            CreateMat("Wall",           shader, new Color(0.35f, 0.30f, 0.25f, 1f));
            CreateMat("DangerZone",     shader, new Color(0.80f, 0.20f, 0.10f, 1f));
            CreateMat("HighlightMove",  shader, new Color(0.20f, 0.60f, 1.00f, 0.35f));
            CreateMat("HighlightShoot", shader, new Color(1.00f, 0.30f, 0.20f, 0.35f));
            CreateMat("SpawnP1",        shader, new Color(0.20f, 0.50f, 1.00f, 0.80f));
            CreateMat("SpawnP2",        shader, new Color(1.00f, 0.30f, 0.20f, 0.80f));
            CreateMat("HeroP1",         shader, new Color(0.20f, 0.50f, 1.00f, 1f));
            CreateMat("HeroP2",         shader, new Color(1.00f, 0.30f, 0.20f, 1f));
        }

        private static void WireMapConfigMaterials()
        {
            var map = AssetDatabase.LoadAssetAtPath<Core.Config.MapConfig>(MapPath);
            if (map == null) return;

            map.floorMaterial = LoadMat("Floor");
            map.wallMaterial = LoadMat("Wall");
            map.dangerZoneMaterial = LoadMat("DangerZone");
            EditorUtility.SetDirty(map);
        }

        private static void WireHeroPrefabs()
        {
            EnsureFolder(PrefabsPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit");

            var guids = AssetDatabase.FindAssets("t:TacticalDuelist.Core.Config.HeroConfig", new[] { HeroesPath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var hero = AssetDatabase.LoadAssetAtPath<Core.Config.HeroConfig>(path);
                if (hero == null || string.IsNullOrEmpty(hero.heroId)) continue;

                var prefabName = $"Hero_{hero.heroId.Substring(0,1).ToUpper()}{hero.heroId.Substring(1)}";
                var prefabPath = $"{PrefabsPath}/{prefabName}.prefab";

                // Create prefab if it doesn't exist
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null && shader != null)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    go.name = prefabName;
                    go.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

                    var mat = new Material(shader) { color = hero.heroColor };
                    var matPath = $"Assets/Materials/Heroes/{hero.heroId}.mat";
                    EnsureFolder("Assets/Materials/Heroes");

                    var existingMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (existingMat == null)
                    {
                        AssetDatabase.CreateAsset(mat, matPath);
                    }
                    else
                    {
                        existingMat.color = hero.heroColor;
                        mat = existingMat;
                        EditorUtility.SetDirty(existingMat);
                    }

                    go.GetComponent<Renderer>().sharedMaterial = mat;
                    go.AddComponent<Gameplay.HeroView3D>();

                    prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                    Object.DestroyImmediate(go);
                    Debug.Log($"[ProjectSetup] Created hero prefab: {prefabName}");
                }

                if (prefab != null)
                    hero.heroPrefab = prefab;

                EditorUtility.SetDirty(hero);
            }
        }

        private static void WireGameBootstrapMaterials()
        {
            var bootstrap = Object.FindAnyObjectByType<Gameplay.GameBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogWarning("[ProjectSetup] GameBootstrap not in scene — open BootstrapScene first");
                return;
            }

            var so = new SerializedObject(bootstrap);
            SetMat(so, "_matFloor", "Floor");
            SetMat(so, "_matFloorAlt", "FloorAlt");
            SetMat(so, "_matWall", "Wall");
            SetMat(so, "_matDangerZone", "DangerZone");
            SetMat(so, "_matHighlightMove", "HighlightMove");
            SetMat(so, "_matHighlightShoot", "HighlightShoot");
            SetMat(so, "_matSpawnP1", "SpawnP1");
            SetMat(so, "_matSpawnP2", "SpawnP2");
            SetMat(so, "_matHeroP1", "HeroP1");
            SetMat(so, "_matHeroP2", "HeroP2");

            // Wire all maps
            var allMapGuids = AssetDatabase.FindAssets("t:MapConfig", new[] { "Assets/ScriptableObjects/Maps" });
            var mapsProp = so.FindProperty("_allMaps");
            if (mapsProp != null)
            {
                mapsProp.arraySize = allMapGuids.Length;
                for (int i = 0; i < allMapGuids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(allMapGuids[i]);
                    var map = AssetDatabase.LoadAssetAtPath<MapConfig>(path);
                    mapsProp.GetArrayElementAtIndex(i).objectReferenceValue = map;
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(bootstrap);
        }

        private static void AddShaderToAlwaysIncluded()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return;

            var so = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var prop = so.FindProperty("m_AlwaysIncludedShaders");

            for (int i = 0; i < prop.arraySize; i++)
                if (prop.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;

            prop.InsertArrayElementAtIndex(prop.arraySize);
            prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = shader;
            so.ApplyModifiedProperties();
        }

        #region Helpers

        private static void SetMat(SerializedObject so, string field, string matName)
        {
            var prop = so.FindProperty(field);
            if (prop != null) prop.objectReferenceValue = LoadMat(matName);
        }

        private static void CreateMat(string name, Shader shader, Color color)
        {
            var path = $"{MaterialsPath}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return;
            }
            var mat = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(mat, path);
        }

        private static Material LoadMat(string name)
            => AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/{name}.mat");

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
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

        #endregion
    }
}
