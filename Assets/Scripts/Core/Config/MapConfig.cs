using UnityEngine;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.Core.Config
{
    [CreateAssetMenu(fileName = "NewMap", menuName = "TacticalDuelist/Map Config")]
    public class MapConfig : ScriptableObject
    {
        #region Identity

        [Header("Identity")]
        public string mapId;
        public string mapName;

        #endregion

        #region Grid

        [Header("Grid")]
        [Range(6, 16)] public int width = 10;
        [Range(6, 16)] public int height = 10;

        [Tooltip("Flattened 2D array [row * width + col]. 0=Empty, 1=Wall, 2=DestructibleWall")]
        public int[] gridData;

        #endregion

        #region Spawns

        [Header("Spawns")]
        public Vector2Int player1Spawn;
        public Direction player1Facing = Direction.Up;
        public Vector2Int player2Spawn;
        public Direction player2Facing = Direction.Down;

        #endregion

        #region Pickups

        [Header("Pickups")]
        public PickupSpawn[] pickups;

        #endregion

        #region Shrink

        [Header("Shrink")]
        [Tooltip("Number of tiles to shrink per round (starting from round 2)")]
        [Range(1, 3)] public int shrinkPerRound = 1;

        #endregion

        #region 3D Environment

        [Header("3D Environment")]
        public GameObject mapPrefab;
        public Material floorMaterial;
        public Material wallMaterial;
        public Material dangerZoneMaterial;

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the TileType at (col, row) from the flattened gridData array.
        /// </summary>
        public TileType GetTileAt(int col, int row)
        {
            if (col < 0 || col >= width || row < 0 || row >= height)
                return TileType.OutOfBounds;

            int index = row * width + col;
            if (gridData == null || index >= gridData.Length)
                return TileType.Empty;

            return gridData[index] switch
            {
                1 => TileType.Wall,
                2 => TileType.DestructibleWall,
                _ => TileType.Empty
            };
        }

        #endregion
    }

    [System.Serializable]
    public class PickupSpawn
    {
        public Vector2Int position;
        public PickupType type;
    }
}
