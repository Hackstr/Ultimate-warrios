using UnityEngine;
using UnityEditor;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Core.Models;

namespace TacticalDuelist.Editor
{
    public static class MapCreator
    {
        [MenuItem("Tactical Duelist/Create Maps/Create All New Maps")]
        public static void CreateAllMaps()
        {
            CreateLabyrinth();
            CreateBridge();
            Debug.Log("[MapCreator] Created 2 new maps!");
        }

        /// <summary>
        /// Arena 02 — The Labyrinth: Narrow corridors, lots of walls, close-quarters combat.
        /// 8x8 grid with maze-like structure. Forces frequent turns and ambush tactics.
        /// </summary>
        [MenuItem("Tactical Duelist/Create Maps/Arena 02 — Labyrinth")]
        static void CreateLabyrinth()
        {
            var map = ScriptableObject.CreateInstance<MapConfig>();
            map.mapId = "arena_02";
            map.mapName = "Arena 02 — The Labyrinth";
            map.width = 8;
            map.height = 8;
            map.shrinkPerRound = 1;

            // P1 bottom-left, P2 top-right
            map.player1Spawn = new Vector2Int(0, 0);
            map.player1Facing = Direction.Up;
            map.player2Spawn = new Vector2Int(7, 7);
            map.player2Facing = Direction.Down;

            // Grid layout (0=empty, 1=wall):
            // Row 0 (bottom): . . . W . . . .
            // Row 1:           . W . . . W . .
            // Row 2:           . W . W . W . .
            // Row 3:           . . . W . . . W
            // Row 4:           W . . . W . . .
            // Row 5:           . . W . W . W .
            // Row 6:           . . W . . . W .
            // Row 7 (top):     . . . . W . . .
            map.gridData = new int[]
            {
                // row 0
                0, 0, 0, 1, 0, 0, 0, 0,
                // row 1
                0, 1, 0, 0, 0, 1, 0, 0,
                // row 2
                0, 1, 0, 1, 0, 1, 0, 0,
                // row 3
                0, 0, 0, 1, 0, 0, 0, 1,
                // row 4
                1, 0, 0, 0, 1, 0, 0, 0,
                // row 5
                0, 0, 1, 0, 1, 0, 1, 0,
                // row 6
                0, 0, 1, 0, 0, 0, 1, 0,
                // row 7
                0, 0, 0, 0, 1, 0, 0, 0,
            };

            AssetDatabase.CreateAsset(map, "Assets/ScriptableObjects/Maps/Map_Arena02.asset");
            AssetDatabase.SaveAssets();
            Debug.Log("[MapCreator] Created: Arena 02 — The Labyrinth (8x8, 14 walls)");
        }

        /// <summary>
        /// Arena 03 — The Bridge: Open flanks with a narrow center chokepoint.
        /// 10x8 grid. Two open areas connected by a 2-wide bridge.
        /// Rewards long-range heroes and punishes reckless charging.
        /// </summary>
        [MenuItem("Tactical Duelist/Create Maps/Arena 03 — Bridge")]
        static void CreateBridge()
        {
            var map = ScriptableObject.CreateInstance<MapConfig>();
            map.mapId = "arena_03";
            map.mapName = "Arena 03 — The Bridge";
            map.width = 10;
            map.height = 8;
            map.shrinkPerRound = 1;

            // P1 bottom-center, P2 top-center
            map.player1Spawn = new Vector2Int(4, 0);
            map.player1Facing = Direction.Up;
            map.player2Spawn = new Vector2Int(5, 7);
            map.player2Facing = Direction.Down;

            // Grid layout:
            // Open areas top and bottom, wall corridor in middle
            // Row 0: . . . . . . . . . .   (P1 spawn area)
            // Row 1: . . . . . . . . . .
            // Row 2: W W W . . . . W W W   (bridge walls)
            // Row 3: W W W . . . . W W W   (narrow 4-wide passage)
            // Row 4: W W W . . . . W W W
            // Row 5: W W W . . . . W W W   (bridge walls)
            // Row 6: . . . . . . . . . .
            // Row 7: . . . . . . . . . .   (P2 spawn area)
            map.gridData = new int[]
            {
                // row 0 - open
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                // row 1 - open
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                // row 2 - bridge walls
                1, 1, 1, 0, 0, 0, 0, 1, 1, 1,
                // row 3 - narrow passage
                1, 1, 1, 0, 0, 0, 0, 1, 1, 1,
                // row 4 - narrow passage
                1, 1, 1, 0, 0, 0, 0, 1, 1, 1,
                // row 5 - bridge walls
                1, 1, 1, 0, 0, 0, 0, 1, 1, 1,
                // row 6 - open
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                // row 7 - open
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            };

            AssetDatabase.CreateAsset(map, "Assets/ScriptableObjects/Maps/Map_Arena03.asset");
            AssetDatabase.SaveAssets();
            Debug.Log("[MapCreator] Created: Arena 03 — The Bridge (10x8, 24 walls)");
        }
    }
}
