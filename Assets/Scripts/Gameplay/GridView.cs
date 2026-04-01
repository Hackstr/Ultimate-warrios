using UnityEngine;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Systems;
using TacticalDuelist.Core.Utils;

namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// Renders the 3D grid: floor tiles, walls, highlights, danger zones.
    /// Spawns geometry from GridSystem data. Subscribes to map change events.
    /// </summary>
    public class GridView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Tile Prefabs")]
        [SerializeField] private GameObject _floorTilePrefab;
        [SerializeField] private GameObject _wallPrefab;

        [Header("Materials")]
        [SerializeField] private Material _floorMaterial;
        [SerializeField] private Material _floorAltMaterial;
        [SerializeField] private Material _wallMaterial;
        [SerializeField] private Material _dangerZoneMaterial;
        [SerializeField] private Material _highlightMoveMaterial;
        [SerializeField] private Material _highlightShootMaterial;
        [SerializeField] private Material _spawnP1Material;
        [SerializeField] private Material _spawnP2Material;

        [Header("Settings")]
        [SerializeField] private float _tileSize = 1f;
        [SerializeField] private float _wallHeight = 1.5f;
        [SerializeField] private float _floorThickness = 0.1f;

        #endregion

        #region Fields

        private Transform _floorParent;
        private Transform _wallParent;
        private Transform _highlightParent;
        private Transform _dangerZoneParent;

        private GameObject[,] _floorTiles;
        private GameObject[,] _wallObjects;

        private GridSystem _grid;
        private int _width;
        private int _height;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            GameEvents.OnDangerZoneExpanded += HandleDangerZoneExpanded;
            GameEvents.OnWallDestroyed += HandleWallDestroyed;
        }

        private void OnDisable()
        {
            GameEvents.OnDangerZoneExpanded -= HandleDangerZoneExpanded;
            GameEvents.OnWallDestroyed -= HandleWallDestroyed;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Builds the entire 3D grid from a GridSystem. Call once at match start.
        /// </summary>
        public void RenderGrid(GridSystem grid, Vector2Int p1Spawn, Vector2Int p2Spawn)
        {
            _grid = grid;
            _width = grid.Width;
            _height = grid.Height;

            ClearGrid();
            CreateParents();

            _floorTiles = new GameObject[_width, _height];
            _wallObjects = new GameObject[_width, _height];

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    var pos = new Vector2Int(x, y);
                    var worldPos = GridHelper.GridToWorld(pos, _tileSize);
                    var tile = grid.GetTile(pos);

                    SpawnFloorTile(x, y, worldPos, pos == p1Spawn, pos == p2Spawn);

                    if (tile == TileType.Wall || tile == TileType.DestructibleWall)
                        SpawnWall(x, y, worldPos, tile == TileType.DestructibleWall);
                }
            }
        }

        /// <summary>
        /// Shows movement highlights on given tiles. Call during planning preview.
        /// </summary>
        public void ShowMoveHighlights(Vector2Int[] tiles)
        {
            ClearHighlights();
            foreach (var pos in tiles)
                CreateHighlight(pos, _highlightMoveMaterial);
        }

        /// <summary>
        /// Shows shoot line highlight. Call during planning preview.
        /// </summary>
        public void ShowShootHighlight(Vector2Int[] tiles)
        {
            ClearHighlights();
            foreach (var pos in tiles)
                CreateHighlight(pos, _highlightShootMaterial);
        }

        public void ClearHighlights()
        {
            if (_highlightParent == null)
                return;

            for (int i = _highlightParent.childCount - 1; i >= 0; i--)
                Destroy(_highlightParent.GetChild(i).gameObject);
        }

        /// <summary>
        /// Shows a preview path on the grid for the planned actions.
        /// Renders semi-transparent markers along the hero's trajectory.
        /// </summary>
        public void ShowPathPreview(Vector2Int startPos, Direction startFacing,
            System.Collections.Generic.List<ActionType> actions, bool isPlayer1)
        {
            ClearHighlights();

            if (actions == null || actions.Count == 0) return;

            var pos = startPos;
            var facing = startFacing;
            var material = isPlayer1 ? _highlightMoveMaterial : _highlightShootMaterial;

            for (int i = 0; i < actions.Count; i++)
            {
                switch (actions[i])
                {
                    case ActionType.Move:
                        var moveDir = GridSystem.DirectionToVector(facing);
                        var nextPos = pos + moveDir;
                        if (_grid != null && _grid.IsInBounds(nextPos) && _grid.IsWalkable(nextPos))
                            pos = nextPos;
                        break;
                    case ActionType.TurnLeft:
                        facing = GridSystem.TurnLeft(facing);
                        break;
                    case ActionType.TurnRight:
                        facing = GridSystem.TurnRight(facing);
                        break;
                    case ActionType.TurnAround:
                        facing = GridSystem.TurnLeft(GridSystem.TurnLeft(facing));
                        break;
                }

                // Show marker at current position after this action
                CreateHighlight(pos, material);
            }

            // Final position — brighter marker
            var finalMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            finalMarker.transform.SetParent(_highlightParent);
            finalMarker.transform.position = GridHelper.GridToWorld(pos, _tileSize) + Vector3.up * 0.15f;
            finalMarker.transform.localScale = Vector3.one * _tileSize * 0.3f;
            finalMarker.name = "FinalPos";

            var col = finalMarker.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var renderer = finalMarker.GetComponent<Renderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;
        }

        /// <summary>
        /// Clears path preview.
        /// </summary>
        public void ClearPathPreview()
        {
            ClearHighlights();
        }

        /// <summary>
        /// Returns the world-space center of the grid for camera framing.
        /// </summary>
        public Vector3 GetGridCenter()
        {
            return new Vector3(
                (_width - 1) * _tileSize * 0.5f,
                0f,
                (_height - 1) * _tileSize * 0.5f
            );
        }

        /// <summary>
        /// Returns the world-space extent for camera fitting.
        /// </summary>
        public float GetGridExtent()
        {
            return Mathf.Max(_width, _height) * _tileSize * 0.5f;
        }

        #endregion

        #region Shoot Lines

        private readonly System.Collections.Generic.List<GameObject> _shootLines = new();

        /// <summary>
        /// Shows a shoot line from source to target on the grid.
        /// Red = hit, grey = miss.
        /// </summary>
        public void ShowShootLine(Vector2Int from, Vector2Int to, bool hit)
        {
            var startPos = GridHelper.GridToWorld(from, _tileSize) + Vector3.up * 0.3f;
            var endPos = GridHelper.GridToWorld(to, _tileSize) + Vector3.up * 0.3f;

            var lineGo = new GameObject("ShootLine");
            lineGo.transform.SetParent(transform);

            var lr = lineGo.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, startPos);
            lr.SetPosition(1, endPos);
            lr.startWidth = hit ? 0.08f : 0.04f;
            lr.endWidth = hit ? 0.08f : 0.04f;

            var mat = new Material(Shader.Find("Sprites/Default"));
            var color = hit ? new Color(1f, 0.3f, 0.2f, 0.8f) : new Color(0.5f, 0.5f, 0.6f, 0.4f);
            mat.color = color;
            lr.material = mat;
            lr.sortingOrder = 10;

            _shootLines.Add(lineGo);
        }

        public void ClearShootLines()
        {
            foreach (var line in _shootLines)
                if (line != null) Destroy(line);
            _shootLines.Clear();
        }

        #endregion

        #region Private Methods — Spawning

        private void SpawnFloorTile(int x, int y, Vector3 worldPos, bool isP1Spawn, bool isP2Spawn)
        {
            var floorPos = worldPos + Vector3.down * (_floorThickness * 0.5f);

            GameObject tile;
            if (_floorTilePrefab != null)
            {
                tile = Instantiate(_floorTilePrefab, floorPos, Quaternion.identity, _floorParent);
            }
            else
            {
                tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.transform.SetParent(_floorParent);
                tile.transform.position = floorPos;
                tile.transform.localScale = new Vector3(_tileSize * 0.95f, _floorThickness, _tileSize * 0.95f);
            }

            tile.name = $"Floor_{x}_{y}";

            var renderer = tile.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (isP1Spawn && _spawnP1Material != null)
                    renderer.sharedMaterial = _spawnP1Material;
                else if (isP2Spawn && _spawnP2Material != null)
                    renderer.sharedMaterial = _spawnP2Material;
                else if ((x + y) % 2 == 0 && _floorMaterial != null)
                    renderer.sharedMaterial = _floorMaterial;
                else if (_floorAltMaterial != null)
                    renderer.sharedMaterial = _floorAltMaterial;
                else if (_floorMaterial != null)
                    renderer.sharedMaterial = _floorMaterial;
            }

            _floorTiles[x, y] = tile;
        }

        private void SpawnWall(int x, int y, Vector3 worldPos, bool isDestructible)
        {
            var wallPos = worldPos + Vector3.up * (_wallHeight * 0.5f);

            GameObject wall;
            if (_wallPrefab != null)
            {
                wall = Instantiate(_wallPrefab, wallPos, Quaternion.identity, _wallParent);
            }
            else
            {
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.SetParent(_wallParent);
                wall.transform.position = wallPos;
                wall.transform.localScale = new Vector3(_tileSize * 0.9f, _wallHeight, _tileSize * 0.9f);
            }

            wall.name = $"Wall_{x}_{y}";

            var renderer = wall.GetComponent<Renderer>();
            if (renderer != null && _wallMaterial != null)
            {
                renderer.sharedMaterial = _wallMaterial;
                if (isDestructible)
                    renderer.material.color = new Color(0.8f, 0.6f, 0.4f);
            }

            _wallObjects[x, y] = wall;
        }

        private void CreateHighlight(Vector2Int pos, Material material)
        {
            if (_highlightParent == null)
                return;

            var worldPos = GridHelper.GridToWorld(pos, _tileSize);
            var highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
            highlight.transform.SetParent(_highlightParent);
            highlight.transform.position = worldPos + Vector3.up * 0.01f;
            highlight.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            highlight.transform.localScale = Vector3.one * _tileSize * 0.9f;
            highlight.name = $"Highlight_{pos.x}_{pos.y}";

            var col = highlight.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var renderer = highlight.GetComponent<Renderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;
        }

        #endregion

        #region Private Methods — Management

        private void CreateParents()
        {
            _floorParent = new GameObject("Floor").transform;
            _floorParent.SetParent(transform);

            _wallParent = new GameObject("Walls").transform;
            _wallParent.SetParent(transform);

            _highlightParent = new GameObject("Highlights").transform;
            _highlightParent.SetParent(transform);

            _dangerZoneParent = new GameObject("DangerZones").transform;
            _dangerZoneParent.SetParent(transform);
        }

        private void ClearGrid()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            _floorTiles = null;
            _wallObjects = null;
        }

        #endregion

        #region Event Handlers

        private void HandleDangerZoneExpanded(Vector2Int[] tiles)
        {
            foreach (var pos in tiles)
            {
                if (pos.x < 0 || pos.x >= _width || pos.y < 0 || pos.y >= _height)
                    continue;

                // Tint floor tile
                if (_floorTiles != null && _floorTiles[pos.x, pos.y] != null)
                {
                    var renderer = _floorTiles[pos.x, pos.y].GetComponent<Renderer>();
                    if (renderer != null && _dangerZoneMaterial != null)
                        renderer.sharedMaterial = _dangerZoneMaterial;
                }

                // Remove wall visual if present
                if (_wallObjects != null && _wallObjects[pos.x, pos.y] != null)
                {
                    Destroy(_wallObjects[pos.x, pos.y]);
                    _wallObjects[pos.x, pos.y] = null;
                }
            }
        }

        private void HandleWallDestroyed(Vector2Int pos)
        {
            if (_wallObjects == null || pos.x < 0 || pos.x >= _width || pos.y < 0 || pos.y >= _height)
                return;

            if (_wallObjects[pos.x, pos.y] != null)
            {
                Destroy(_wallObjects[pos.x, pos.y]);
                _wallObjects[pos.x, pos.y] = null;
            }
        }

        #endregion
    }
}
