using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TacticalDuelist.Editor
{
    public static class PortraitSetup
    {
        private static readonly Dictionary<string, string> PortraitMap = new()
        {
            { "Assets/Sprites/Heroes/Portrait_Archer.png", "Assets/ScriptableObjects/Heroes/Hero_Archer.asset" },
            { "Assets/Sprites/Heroes/Portrait_Tank.png", "Assets/ScriptableObjects/Heroes/Hero_Tank.asset" },
            { "Assets/Sprites/Heroes/Portrait_Scout.png", "Assets/ScriptableObjects/Heroes/Hero_Scout.asset" },
            { "Assets/Sprites/Heroes/Portrait_Shadow.png", "Assets/ScriptableObjects/Heroes/Hero_Shadow.asset" },
            { "Assets/Sprites/Heroes/Portrait_Demo.png", "Assets/ScriptableObjects/Heroes/Hero_Demo.asset" },
            { "Assets/Sprites/Heroes/Portrait_Ghost.png", "Assets/ScriptableObjects/Heroes/Hero_Ghost.asset" },
            { "Assets/Sprites/Heroes/Portrait_Engineer.png", "Assets/ScriptableObjects/Heroes/Hero_Engineer.asset" },
            { "Assets/Sprites/Heroes/Portrait_Guardian.png", "Assets/ScriptableObjects/Heroes/Hero_Guardian.asset" },
            { "Assets/Sprites/Heroes/Portrait_Berserker.png", "Assets/ScriptableObjects/Heroes/Hero_Berserker.asset" },
            { "Assets/Sprites/Heroes/Portrait_Mage.png", "Assets/ScriptableObjects/Heroes/Hero_Mage.asset" },
            { "Assets/Sprites/Heroes/Portrait_Hawk.png", "Assets/ScriptableObjects/Heroes/Hero_Hawk.asset" },
            { "Assets/Sprites/Heroes/Portrait_Mirage.png", "Assets/ScriptableObjects/Heroes/Hero_Mirage.asset" },
        };

        [MenuItem("Tactical Duelist/Setup Hero Portraits")]
        public static void SetupPortraits()
        {
            // First, ensure all portrait PNGs are imported as Sprite
            foreach (var kvp in PortraitMap)
            {
                var importer = AssetImporter.GetAtPath(kvp.Key) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"[PortraitSetup] Portrait not found: {kvp.Key}");
                    continue;
                }

                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                    Debug.Log($"[PortraitSetup] Set {kvp.Key} to Sprite mode");
                }
            }

            // Assign portraits to HeroConfigs
            int assigned = 0;
            foreach (var kvp in PortraitMap)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(kvp.Key);
                if (sprite == null)
                {
                    Debug.LogWarning($"[PortraitSetup] Could not load sprite: {kvp.Key}");
                    continue;
                }

                var config = AssetDatabase.LoadAssetAtPath<Core.Config.HeroConfig>(kvp.Value);
                if (config == null)
                {
                    Debug.LogWarning($"[PortraitSetup] HeroConfig not found: {kvp.Value}");
                    continue;
                }

                var so = new SerializedObject(config);
                var prop = so.FindProperty("portrait");
                if (prop != null)
                {
                    prop.objectReferenceValue = sprite;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(config);
                    assigned++;
                    Debug.Log($"[PortraitSetup] Assigned {sprite.name} -> {config.heroName}");
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[PortraitSetup] Done! Assigned {assigned} portraits.");
        }
    }
}
