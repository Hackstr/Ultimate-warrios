using System;
using System.Collections.Generic;
using UnityEngine;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.Core.Systems
{
    /// <summary>
    /// Owns the 2D grid state and provides all spatial queries.
    /// Pure game logic — no MonoBehaviour, deterministic, portable to TypeScript.
    /// Coordinate convention: (col, row), origin at bottom-left.
    /// </summary>
    public class GridSystem
    {
        #region Fields

        private readonly TileType[,] _grid;
        private readonly Dictionary<Vector2Int, PickupType> _pickups;
        private readonly HashSet<Vector2Int> _dangerZone;

        // Pre-allocated buffer to avoid GC in hot paths
        private readonly List<Vector2Int> _rayBuffer = new(16);

        #endregion

        #region Properties

        public int Width { get; }
        public int Height { get; }

        #endregion

        #region Constructor

        public GridSystem(MapConfig config)
        {
            Width = config.width;
            Height = config.height;
            _grid = new TileType[Width, Height];
            _pickups = new Dictionary<Vector2Int, PickupType>();
            _dangerZone = new HashSet<Vector2Int>();

            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    _grid[col, row] = config.GetTileAt(col, row);
                }
            }

            if (config.pickups != null)
            {
                foreach (var pickup in config.pickups)
                {
                    _pickups[pickup.position] = pickup.type;
                }
            }
        }

        /// <summary>
        /// Constructor for testing — accepts raw grid data.
        /// </summary>
        public GridSystem(int width, int height, TileType[,] gridData)
        {
            Width = width;
            Height = height;
            _grid = gridData ?? throw new ArgumentNullException(nameof(gridData));
            _pickups = new Dictionary<Vector2Int, PickupType>();
            _dangerZone = new HashSet<Vector2Int>();
        }

        #endregion

        #region Tile Queries

        public TileType GetTile(Vector2Int pos)
        {
            if (!IsInBounds(pos))
                return TileType.OutOfBounds;

            if (_dangerZone.Contains(pos))
                return TileType.DangerZone;

            return _grid[pos.x, pos.y];
        }

        public bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
        }

        /// <summary>
        /// A tile is walkable if it's in bounds, not a wall, and not out of bounds.
        /// Danger zone tiles ARE walkable (heroes can walk through, but die at round end).
        /// </summary>
        public bool IsWalkable(Vector2Int pos)
        {
            if (!IsInBounds(pos))
                return false;

            var tile = _grid[pos.x, pos.y];
            return tile != TileType.Wall && tile != TileType.OutOfBounds;
        }

        public bool IsInDangerZone(Vector2Int pos) => _dangerZone.Contains(pos);

        #endregion

        #region Pickup Queries

        public bool HasPickup(Vector2Int pos) => _pickups.ContainsKey(pos);

        public PickupType? GetPickup(Vector2Int pos)
        {
            return _pickups.TryGetValue(pos, out var type) ? type : null;
        }

        #endregion

        #region Ray Casting

        /// <summary>
        /// Casts a ray from position in direction for up to range tiles.
        /// Returns list of tiles the ray passes through (NOT including start pos).
        /// Stops at first wall — does NOT include the wall tile.
        /// Warning: returns internal buffer — do NOT cache the returned list.
        /// </summary>
        public List<Vector2Int> CastRay(Vector2Int from, Direction dir, int range)
        {
            _rayBuffer.Clear();
            var step = DirectionToVector(dir);
            var current = from;

            for (int i = 0; i < range; i++)
            {
                current += step;

                if (!IsInBounds(current))
                    break;

                var tile = _grid[current.x, current.y];
                if (tile == TileType.Wall || tile == TileType.DestructibleWall)
                    break;

                _rayBuffer.Add(current);
            }

            return _rayBuffer;
        }

        /// <summary>
        /// Mage's PhaseShot: passes through exactly 1 wall, then stops at next.
        /// </summary>
        public List<Vector2Int> CastRayPhase(Vector2Int from, Direction dir, int range)
        {
            _rayBuffer.Clear();
            var step = DirectionToVector(dir);
            var current = from;
            int wallsPassed = 0;

            for (int i = 0; i < range; i++)
            {
                current += step;

                if (!IsInBounds(current))
                    break;

                var tile = _grid[current.x, current.y];
                if (tile == TileType.Wall || tile == TileType.DestructibleWall)
                {
                    wallsPassed++;
                    if (wallsPassed > 1)
                        break;
                    continue;
                }

                _rayBuffer.Add(current);
            }

            return _rayBuffer;
        }

        /// <summary>
        /// Hawk's Pierce: passes through ALL walls and obstacles.
        /// </summary>
        public List<Vector2Int> CastRayPierce(Vector2Int from, Direction dir, int range)
        {
            _rayBuffer.Clear();
            var step = DirectionToVector(dir);
            var current = from;

            for (int i = 0; i < range; i++)
            {
                current += step;

                if (!IsInBounds(current))
                    break;

                _rayBuffer.Add(current);
            }

            return _rayBuffer;
        }

        /// <summary>
        /// Archer's Ricochet: bounces off 1 wall, changing direction 90° (clockwise preference).
        /// Returns all tiles the ray passes through including the bounce path.
        /// </summary>
        public List<Vector2Int> CastRayRicochet(Vector2Int from, Direction dir, int range)
        {
            _rayBuffer.Clear();
            var step = DirectionToVector(dir);
            var current = from;
            bool hasBounced = false;
            int remaining = range;

            for (int i = 0; i < remaining; i++)
            {
                var next = current + step;

                if (!IsInBounds(next))
                    break;

                var tile = _grid[next.x, next.y];
                if (tile == TileType.Wall || tile == TileType.DestructibleWall)
                {
                    if (hasBounced)
                        break;

                    // Bounce: reverse perpendicular. Try clockwise turn first, then counterclockwise.
                    hasBounced = true;
                    var cwDir = TurnRight(dir);
                    var cwStep = DirectionToVector(cwDir);
                    var cwNext = current + cwStep;

                    if (IsInBounds(cwNext) && IsWalkable(cwNext))
                    {
                        dir = cwDir;
                        step = cwStep;
                    }
                    else
                    {
                        var ccwDir = TurnLeft(dir);
                        var ccwStep = DirectionToVector(ccwDir);
                        var ccwNext = current + ccwStep;

                        if (IsInBounds(ccwNext) && IsWalkable(ccwNext))
                        {
                            dir = ccwDir;
                            step = ccwStep;
                        }
                        else
                        {
                            // Reverse direction if no perpendicular is open
                            dir = TurnAround(dir);
                            step = DirectionToVector(dir);
                        }
                    }

                    i--;
                    continue;
                }

                current = next;
                _rayBuffer.Add(current);
            }

            return _rayBuffer;
        }

        #endregion

        #region Movement

        /// <summary>
        /// Calculates the target tile when moving from a position in a direction with given speed.
        /// Checks each intermediate tile: stops at last walkable tile before a wall.
        /// Does NOT check for other heroes — collision handled by ActionResolver.
        /// </summary>
        public Vector2Int GetMoveTarget(Vector2Int from, Direction dir, int speed)
        {
            var step = DirectionToVector(dir);
            var current = from;

            for (int i = 0; i < speed; i++)
            {
                var next = current + step;

                if (!IsWalkable(next))
                    break;

                current = next;
            }

            return current;
        }

        #endregion

        #region Grid Modifications

        public void RemovePickup(Vector2Int pos) => _pickups.Remove(pos);

        public void DestroyWall(Vector2Int pos)
        {
            if (IsInBounds(pos) && _grid[pos.x, pos.y] == TileType.DestructibleWall)
                _grid[pos.x, pos.y] = TileType.Empty;
        }

        public void PlaceBarrier(Vector2Int pos)
        {
            if (IsInBounds(pos) && _grid[pos.x, pos.y] == TileType.Empty)
                _grid[pos.x, pos.y] = TileType.Wall;
        }

        public void RemoveBarrier(Vector2Int pos)
        {
            if (IsInBounds(pos) && _grid[pos.x, pos.y] == TileType.Wall)
                _grid[pos.x, pos.y] = TileType.Empty;
        }

        /// <summary>
        /// Marks tiles as danger zone. Called by ShrinkSystem between rounds.
        /// Also destroys walls within the danger zone.
        /// </summary>
        public void ExpandDangerZone(Vector2Int[] tiles)
        {
            foreach (var tile in tiles)
            {
                if (!IsInBounds(tile))
                    continue;

                _dangerZone.Add(tile);

                if (_grid[tile.x, tile.y] == TileType.DestructibleWall)
                    _grid[tile.x, tile.y] = TileType.Empty;
            }
        }

        #endregion

        #region Direction Helpers (Static)

        public static Vector2Int DirectionToVector(Direction dir) => dir switch
        {
            Direction.Up => new Vector2Int(0, 1),
            Direction.Down => new Vector2Int(0, -1),
            Direction.Left => new Vector2Int(-1, 0),
            Direction.Right => new Vector2Int(1, 0),
            _ => Vector2Int.zero
        };

        public static Direction TurnLeft(Direction dir) => dir switch
        {
            Direction.Up => Direction.Left,
            Direction.Left => Direction.Down,
            Direction.Down => Direction.Right,
            Direction.Right => Direction.Up,
            _ => dir
        };

        public static Direction TurnRight(Direction dir) => dir switch
        {
            Direction.Up => Direction.Right,
            Direction.Right => Direction.Down,
            Direction.Down => Direction.Left,
            Direction.Left => Direction.Up,
            _ => dir
        };

        public static Direction TurnAround(Direction dir) => dir switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => dir
        };

        public static Direction VectorToDirection(Vector2Int vec)
        {
            if (vec.y > 0) return Direction.Up;
            if (vec.y < 0) return Direction.Down;
            if (vec.x < 0) return Direction.Left;
            if (vec.x > 0) return Direction.Right;
            return Direction.Up;
        }

        #endregion
    }
}
