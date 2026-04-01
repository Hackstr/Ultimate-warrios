using UnityEngine;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.Core.Config
{
    [CreateAssetMenu(fileName = "NewHero", menuName = "TacticalDuelist/Hero Config")]
    public class HeroConfig : ScriptableObject
    {
        #region Identity

        [Header("Identity")]
        public string heroId;
        public string heroName;
        public string displayName;
        public string loreName;        // e.g. "Elara"
        public string loreTitle;       // e.g. "Guardian of the Eternal Forest"
        public string loreDimension;   // e.g. "Silvanis"
        [TextArea(2, 4)] public string loreBio;
        [Range(1, 5)] public int difficulty = 3;

        #endregion

        #region 3D Assets

        [Header("Unlock")]
        public bool isUnlocked = true;
        public int unlockPrice = 0;

        [Header("3D Assets")]
        public GameObject heroPrefab;
        public RuntimeAnimatorController animatorController;
        public Sprite portrait;
        public Color heroColor = Color.white;

        #endregion

        #region Parameters

        [Header("Parameters")]
        [Range(3, 6)] public int steps = 4;
        [Range(2, 10)] public int range = 5;
        [Tooltip("Steps to wait between shots. 0 = no cooldown.")]
        [Range(0, 3)] public int cooldown = 1;
        [Range(0, 1)] public int armor = 0;
        [Range(1, 2)] public int speed = 1;

        #endregion

        #region Special

        [Header("Voice Lines")]
        public string voiceKill = "Target down.";
        public string voiceDeath = "Not like this...";
        public string voiceSpecial = "Special!";
        public string voiceRoundStart = "Let's go.";

        [Header("Projectile")]
        public Color projectileColor = Color.yellow;
        public float projectileSpeed = 15f;
        public float projectileSize = 0.12f;
        public GameObject projectilePrefab; // override if custom prefab exists

        [Header("Special")]
        public SpecialAbility specialAbility;
        public string specialName;
        [TextArea(2, 4)] public string specialDescription;
        public GameObject specialVFXPrefab;

        #endregion
    }
}
