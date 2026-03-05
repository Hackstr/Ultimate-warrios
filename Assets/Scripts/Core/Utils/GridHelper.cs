using UnityEngine;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.Core.Utils
{
    /// <summary>
    /// Pure static helper for converting between 2D grid coordinates (game logic)
    /// and 3D world positions (rendering). Grid origin is bottom-left.
    /// </summary>
    public static class GridHelper
    {
        public const float DefaultTileSize = 1f;

        /// <summary>
        /// Converts grid coordinates (col, row) to 3D world position.
        /// X = col, Y = 0 (ground), Z = row.
        /// </summary>
        public static Vector3 GridToWorld(Vector2Int gridPos, float tileSize = DefaultTileSize)
        {
            return new Vector3(
                gridPos.x * tileSize,
                0f,
                gridPos.y * tileSize
            );
        }

        /// <summary>
        /// Converts 3D world position to nearest grid coordinates.
        /// Rounds to nearest integer grid cell.
        /// </summary>
        public static Vector2Int WorldToGrid(Vector3 worldPos, float tileSize = DefaultTileSize)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / tileSize),
                Mathf.RoundToInt(worldPos.z / tileSize)
            );
        }

        /// <summary>
        /// Converts a grid Direction to a world-space Quaternion for rotating 3D models.
        /// Up = +Z (0°), Right = +X (90°), Down = -Z (180°), Left = -X (270°).
        /// </summary>
        public static Quaternion DirectionToRotation(Direction dir) => dir switch
        {
            Direction.Up => Quaternion.Euler(0f, 0f, 0f),
            Direction.Right => Quaternion.Euler(0f, 90f, 0f),
            Direction.Down => Quaternion.Euler(0f, 180f, 0f),
            Direction.Left => Quaternion.Euler(0f, 270f, 0f),
            _ => Quaternion.identity
        };

        /// <summary>
        /// Converts a Direction enum to an integer movement vector on the grid.
        /// Up = (0,1), Down = (0,-1), Left = (-1,0), Right = (1,0).
        /// </summary>
        public static Vector2Int DirectionToVector(Direction dir) => dir switch
        {
            Direction.Up => Vector2Int.up,
            Direction.Down => Vector2Int.down,
            Direction.Left => Vector2Int.left,
            Direction.Right => Vector2Int.right,
            _ => Vector2Int.zero
        };
    }
}
