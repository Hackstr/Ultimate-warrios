using UnityEngine;
using TacticalDuelist.Core.Config;

namespace TacticalDuelist.Core.Models
{
    /// <summary>
    /// Runtime state for a single hero during a match.
    /// One instance per player. Mutated by ActionResolver during step resolution.
    /// </summary>
    public class HeroState
    {
        #region Properties

        public HeroConfig Config { get; }
        public int PlayerIndex { get; }

        public Vector2Int Position { get; set; }
        public Direction Facing { get; set; }

        public bool IsAlive { get; set; } = true;
        public bool HasArmor { get; set; }
        public int CooldownRemaining { get; set; }
        public bool SpecialUsedThisRound { get; set; }

        public int BonusSpeed { get; set; }
        public int BonusRange { get; set; }
        public bool HasIntel { get; set; }

        public bool IsCloaked { get; set; }
        public int CloakStepsRemaining { get; set; }

        public int EffectiveSpeed => Config.speed + BonusSpeed;
        public int EffectiveRange => Config.range + BonusRange;

        #endregion

        #region Constructor

        public HeroState(HeroConfig config, int playerIndex, Vector2Int spawnPos, Direction spawnFacing)
        {
            Config = config;
            PlayerIndex = playerIndex;
            Position = spawnPos;
            Facing = spawnFacing;
            HasArmor = config.armor > 0;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets per-round temporary state. Preserves position, facing, alive status, and armor.
        /// </summary>
        public void ResetForNewRound()
        {
            CooldownRemaining = 0;
            SpecialUsedThisRound = false;
            BonusSpeed = 0;
            BonusRange = 0;
            HasIntel = false;
            IsCloaked = false;
            CloakStepsRemaining = 0;
        }

        #endregion
    }
}
