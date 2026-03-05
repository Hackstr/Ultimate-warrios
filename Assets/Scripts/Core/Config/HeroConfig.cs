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
        [Range(1, 5)] public int difficulty = 3;

        #endregion

        #region 3D Assets

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

        [Header("Special")]
        public SpecialAbility specialAbility;
        public string specialName;
        [TextArea(2, 4)] public string specialDescription;
        public GameObject specialVFXPrefab;

        #endregion
    }
}
