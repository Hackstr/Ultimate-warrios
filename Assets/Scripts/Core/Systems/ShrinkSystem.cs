using System.Collections.Generic;
using UnityEngine;
using TacticalDuelist.Core.Config;

namespace TacticalDuelist.Core.Systems
{
    /// <summary>
    /// Manages map shrinking between rounds.
    /// Round 1 = full map. Round 2+ = outermost ring(s) become danger zones.
    /// Walls in danger zones are destroyed. Heroes in danger zone at round end = eliminated.
    /// </summary>
    public class ShrinkSystem
    {
        #region Fields

        private readonly GridSystem _grid;
        private readonly int _shrinkPerRound;
        private int _currentShrinkDepth;

        #endregion

        #region Constructor

        public ShrinkSystem(GridSystem grid, MapConfig config)
        {
            _grid = grid;
            _shrinkPerRound = config.shrinkPerRound;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Expands danger zone for the given round number.
        /// Called between rounds by MatchManager. No-op for round 1.
        /// </summary>
        public void ApplyShrink(int roundNumber)
        {
            if (roundNumber <= 1)
                return;

            int newDepth = _currentShrinkDepth + _shrinkPerRound;
            var newTiles = GetRingTiles(_currentShrinkDepth, newDepth);
            _currentShrinkDepth = newDepth;

            if (newTiles.Length > 0)
            {
                _grid.ExpandDangerZone(newTiles);
                GameEvents.DangerZoneExpanded(newTiles);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns all grid positions in the ring between fromDepth and toDepth (exclusive).
        /// Depth 0 = outermost ring (row/col 0 and max), depth 1 = next ring inward, etc.
        /// </summary>
        private Vector2Int[] GetRingTiles(int fromDepth, int toDepth)
        {
            var tiles = new List<Vector2Int>();

            for (int depth = fromDepth; depth < toDepth; depth++)
            {
                int minX = depth;
                int maxX = _grid.Width - 1 - depth;
                int minY = depth;
                int maxY = _grid.Height - 1 - depth;

                if (minX > maxX || minY > maxY)
                    break;

                // Top and bottom edges
                for (int x = minX; x <= maxX; x++)
                {
                    tiles.Add(new Vector2Int(x, minY));
                    if (minY != maxY)
                        tiles.Add(new Vector2Int(x, maxY));
                }

                // Left and right edges (excluding corners already added)
                for (int y = minY + 1; y < maxY; y++)
                {
                    tiles.Add(new Vector2Int(minX, y));
                    if (minX != maxX)
                        tiles.Add(new Vector2Int(maxX, y));
                }
            }

            return tiles.ToArray();
        }

        #endregion
    }
}
