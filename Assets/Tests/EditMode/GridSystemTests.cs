using NUnit.Framework;
using UnityEngine;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Systems;

namespace TacticalDuelist.Tests
{
    /// <summary>
    /// Edit Mode unit tests for GridSystem.
    /// Covers: CastRay (open/wall-blocked), GetMoveTarget (open/wall/speed2),
    /// IsWalkable, bounds checking, direction helpers.
    /// </summary>
    [TestFixture]
    public class GridSystemTests
    {
        #region Test Helpers

        private static GridSystem CreateOpenGrid(int w = 10, int h = 10)
        {
            var tiles = new TileType[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    tiles[x, y] = TileType.Empty;
            return new GridSystem(w, h, tiles);
        }

        private static GridSystem CreateGridWithWalls(int w, int h, params Vector2Int[] walls)
        {
            var tiles = new TileType[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    tiles[x, y] = TileType.Empty;
            foreach (var wall in walls)
                tiles[wall.x, wall.y] = TileType.Wall;
            return new GridSystem(w, h, tiles);
        }

        #endregion

        #region IsWalkable

        [Test]
        public void IsWalkable_EmptyTile_True()
        {
            var grid = CreateOpenGrid();
            Assert.IsTrue(grid.IsWalkable(new Vector2Int(5, 5)));
        }

        [Test]
        public void IsWalkable_WallTile_False()
        {
            var grid = CreateGridWithWalls(10, 10, new Vector2Int(3, 3));
            Assert.IsFalse(grid.IsWalkable(new Vector2Int(3, 3)));
        }

        [Test]
        public void IsWalkable_OutOfBounds_False()
        {
            var grid = CreateOpenGrid(10, 10);
            Assert.IsFalse(grid.IsWalkable(new Vector2Int(-1, 5)));
            Assert.IsFalse(grid.IsWalkable(new Vector2Int(5, -1)));
            Assert.IsFalse(grid.IsWalkable(new Vector2Int(10, 5)));
            Assert.IsFalse(grid.IsWalkable(new Vector2Int(5, 10)));
        }

        [Test]
        public void IsWalkable_Corners_True()
        {
            var grid = CreateOpenGrid(10, 10);
            Assert.IsTrue(grid.IsWalkable(new Vector2Int(0, 0)));
            Assert.IsTrue(grid.IsWalkable(new Vector2Int(9, 0)));
            Assert.IsTrue(grid.IsWalkable(new Vector2Int(0, 9)));
            Assert.IsTrue(grid.IsWalkable(new Vector2Int(9, 9)));
        }

        #endregion

        #region IsInBounds

        [Test]
        public void IsInBounds_ValidPositions_True()
        {
            var grid = CreateOpenGrid(10, 10);
            Assert.IsTrue(grid.IsInBounds(new Vector2Int(0, 0)));
            Assert.IsTrue(grid.IsInBounds(new Vector2Int(9, 9)));
            Assert.IsTrue(grid.IsInBounds(new Vector2Int(5, 5)));
        }

        [Test]
        public void IsInBounds_NegativeCoords_False()
        {
            var grid = CreateOpenGrid(10, 10);
            Assert.IsFalse(grid.IsInBounds(new Vector2Int(-1, 0)));
            Assert.IsFalse(grid.IsInBounds(new Vector2Int(0, -1)));
            Assert.IsFalse(grid.IsInBounds(new Vector2Int(-5, -5)));
        }

        [Test]
        public void IsInBounds_ExactBoundary_False()
        {
            var grid = CreateOpenGrid(10, 10);
            Assert.IsFalse(grid.IsInBounds(new Vector2Int(10, 0)));
            Assert.IsFalse(grid.IsInBounds(new Vector2Int(0, 10)));
            Assert.IsFalse(grid.IsInBounds(new Vector2Int(10, 10)));
        }

        #endregion

        #region CastRay — Open Grid

        [Test]
        public void CastRay_Open_RightRange5_Returns5Tiles()
        {
            var grid = CreateOpenGrid(10, 10);
            var tiles = grid.CastRay(new Vector2Int(2, 5), Direction.Right, 5);

            Assert.AreEqual(5, tiles.Count);
            Assert.AreEqual(new Vector2Int(3, 5), tiles[0]);
            Assert.AreEqual(new Vector2Int(4, 5), tiles[1]);
            Assert.AreEqual(new Vector2Int(5, 5), tiles[2]);
            Assert.AreEqual(new Vector2Int(6, 5), tiles[3]);
            Assert.AreEqual(new Vector2Int(7, 5), tiles[4]);
        }

        [Test]
        public void CastRay_Open_UpRange3_Returns3Tiles()
        {
            var grid = CreateOpenGrid(10, 10);
            var tiles = grid.CastRay(new Vector2Int(5, 3), Direction.Up, 3);

            Assert.AreEqual(3, tiles.Count);
            Assert.AreEqual(new Vector2Int(5, 4), tiles[0]);
            Assert.AreEqual(new Vector2Int(5, 5), tiles[1]);
            Assert.AreEqual(new Vector2Int(5, 6), tiles[2]);
        }

        [Test]
        public void CastRay_DoesNotIncludeStartTile()
        {
            var grid = CreateOpenGrid();
            var tiles = grid.CastRay(new Vector2Int(5, 5), Direction.Up, 2);

            foreach (var tile in tiles)
                Assert.AreNotEqual(new Vector2Int(5, 5), tile);
        }

        [Test]
        public void CastRay_StopsAtGridEdge()
        {
            var grid = CreateOpenGrid(10, 10);
            // From (8, 5) facing Right with range 10 — only 1 tile (9,5) before edge
            var tiles = grid.CastRay(new Vector2Int(8, 5), Direction.Right, 10);

            Assert.AreEqual(1, tiles.Count);
            Assert.AreEqual(new Vector2Int(9, 5), tiles[0]);
        }

        #endregion

        #region CastRay — Wall Blocked

        [Test]
        public void CastRay_WallBlocked_StopsBeforeWall()
        {
            // Wall at (5,5). Ray from (2,5) Right with range 8
            var grid = CreateGridWithWalls(10, 10, new Vector2Int(5, 5));
            var tiles = grid.CastRay(new Vector2Int(2, 5), Direction.Right, 8);

            Assert.AreEqual(2, tiles.Count, "Should stop before wall");
            Assert.AreEqual(new Vector2Int(3, 5), tiles[0]);
            Assert.AreEqual(new Vector2Int(4, 5), tiles[1]);
        }

        [Test]
        public void CastRay_WallImmediatelyAdjacent_ReturnsEmpty()
        {
            // Wall at (3,5). Ray from (2,5) Right with range 5
            var grid = CreateGridWithWalls(10, 10, new Vector2Int(3, 5));
            var tiles = grid.CastRay(new Vector2Int(2, 5), Direction.Right, 5);

            Assert.AreEqual(0, tiles.Count, "Wall is immediately adjacent — no tiles");
        }

        [Test]
        public void CastRay_DoesNotIncludeWallTile()
        {
            var grid = CreateGridWithWalls(10, 10, new Vector2Int(6, 5));
            var tiles = grid.CastRay(new Vector2Int(3, 5), Direction.Right, 10);

            foreach (var tile in tiles)
                Assert.AreNotEqual(new Vector2Int(6, 5), tile, "Wall tile should not be in results");
        }

        #endregion

        #region GetMoveTarget

        [Test]
        public void GetMoveTarget_OpenGrid_Speed1_MovesOneTile()
        {
            var grid = CreateOpenGrid();
            var target = grid.GetMoveTarget(new Vector2Int(5, 5), Direction.Up, 1);
            Assert.AreEqual(new Vector2Int(5, 6), target);
        }

        [Test]
        public void GetMoveTarget_OpenGrid_Speed2_MovesTwoTiles()
        {
            var grid = CreateOpenGrid();
            var target = grid.GetMoveTarget(new Vector2Int(5, 5), Direction.Right, 2);
            Assert.AreEqual(new Vector2Int(7, 5), target);
        }

        [Test]
        public void GetMoveTarget_WallBlocked_StopsBeforeWall()
        {
            // Wall at (5,7). Move from (5,5) Up speed 2 → (5,6) is ok, (5,7) is wall → target = (5,6)
            var grid = CreateGridWithWalls(10, 10, new Vector2Int(5, 7));
            var target = grid.GetMoveTarget(new Vector2Int(5, 5), Direction.Up, 2);
            Assert.AreEqual(new Vector2Int(5, 6), target);
        }

        [Test]
        public void GetMoveTarget_WallDirectlyAhead_StaysInPlace()
        {
            // Wall at (5,6). Move from (5,5) Up speed 1 → blocked immediately
            var grid = CreateGridWithWalls(10, 10, new Vector2Int(5, 6));
            var target = grid.GetMoveTarget(new Vector2Int(5, 5), Direction.Up, 1);
            Assert.AreEqual(new Vector2Int(5, 5), target);
        }

        [Test]
        public void GetMoveTarget_AtGridEdge_StaysInPlace()
        {
            var grid = CreateOpenGrid(10, 10);
            // At row 9, facing Up with speed 1 → (5,10) is out of bounds → stay
            var target = grid.GetMoveTarget(new Vector2Int(5, 9), Direction.Up, 1);
            Assert.AreEqual(new Vector2Int(5, 9), target);
        }

        [Test]
        public void GetMoveTarget_Speed2_PartiallyBlocked()
        {
            // Wall at (5,8). Move from (5,5) Up speed 3 → (5,6) ok, (5,7) ok, (5,8) wall → stops at (5,7)
            var grid = CreateGridWithWalls(10, 10, new Vector2Int(5, 8));
            var target = grid.GetMoveTarget(new Vector2Int(5, 5), Direction.Up, 3);
            Assert.AreEqual(new Vector2Int(5, 7), target);
        }

        [Test]
        public void GetMoveTarget_AllDirections()
        {
            var grid = CreateOpenGrid();
            var center = new Vector2Int(5, 5);

            Assert.AreEqual(new Vector2Int(5, 6), grid.GetMoveTarget(center, Direction.Up, 1));
            Assert.AreEqual(new Vector2Int(5, 4), grid.GetMoveTarget(center, Direction.Down, 1));
            Assert.AreEqual(new Vector2Int(4, 5), grid.GetMoveTarget(center, Direction.Left, 1));
            Assert.AreEqual(new Vector2Int(6, 5), grid.GetMoveTarget(center, Direction.Right, 1));
        }

        #endregion

        #region Direction Helpers

        [Test]
        public void DirectionToVector_AllDirections()
        {
            Assert.AreEqual(new Vector2Int(0, 1), GridSystem.DirectionToVector(Direction.Up));
            Assert.AreEqual(new Vector2Int(0, -1), GridSystem.DirectionToVector(Direction.Down));
            Assert.AreEqual(new Vector2Int(-1, 0), GridSystem.DirectionToVector(Direction.Left));
            Assert.AreEqual(new Vector2Int(1, 0), GridSystem.DirectionToVector(Direction.Right));
        }

        [Test]
        public void TurnLeft_CyclesCorrectly()
        {
            Assert.AreEqual(Direction.Left, GridSystem.TurnLeft(Direction.Up));
            Assert.AreEqual(Direction.Down, GridSystem.TurnLeft(Direction.Left));
            Assert.AreEqual(Direction.Right, GridSystem.TurnLeft(Direction.Down));
            Assert.AreEqual(Direction.Up, GridSystem.TurnLeft(Direction.Right));
        }

        [Test]
        public void TurnRight_CyclesCorrectly()
        {
            Assert.AreEqual(Direction.Right, GridSystem.TurnRight(Direction.Up));
            Assert.AreEqual(Direction.Down, GridSystem.TurnRight(Direction.Right));
            Assert.AreEqual(Direction.Left, GridSystem.TurnRight(Direction.Down));
            Assert.AreEqual(Direction.Up, GridSystem.TurnRight(Direction.Left));
        }

        [Test]
        public void TurnAround_ReversesAllDirections()
        {
            Assert.AreEqual(Direction.Down, GridSystem.TurnAround(Direction.Up));
            Assert.AreEqual(Direction.Up, GridSystem.TurnAround(Direction.Down));
            Assert.AreEqual(Direction.Right, GridSystem.TurnAround(Direction.Left));
            Assert.AreEqual(Direction.Left, GridSystem.TurnAround(Direction.Right));
        }

        [Test]
        public void VectorToDirection_AllVectors()
        {
            Assert.AreEqual(Direction.Up, GridSystem.VectorToDirection(new Vector2Int(0, 1)));
            Assert.AreEqual(Direction.Down, GridSystem.VectorToDirection(new Vector2Int(0, -1)));
            Assert.AreEqual(Direction.Left, GridSystem.VectorToDirection(new Vector2Int(-1, 0)));
            Assert.AreEqual(Direction.Right, GridSystem.VectorToDirection(new Vector2Int(1, 0)));
        }

        #endregion

        #region DangerZone

        [Test]
        public void DangerZone_IsWalkable_ButMarkedDanger()
        {
            var grid = CreateOpenGrid(10, 10);
            grid.ExpandDangerZone(new[] { new Vector2Int(3, 3) });

            Assert.IsTrue(grid.IsWalkable(new Vector2Int(3, 3)), "Danger zone is still walkable");
            Assert.IsTrue(grid.IsInDangerZone(new Vector2Int(3, 3)));
            Assert.AreEqual(TileType.DangerZone, grid.GetTile(new Vector2Int(3, 3)));
        }

        [Test]
        public void DangerZone_DestroysDestructibleWalls_RayPassesThrough()
        {
            var tiles = new TileType[10, 10];
            for (int x = 0; x < 10; x++)
                for (int y = 0; y < 10; y++)
                    tiles[x, y] = TileType.Empty;
            tiles[4, 5] = TileType.DestructibleWall;
            var grid = new GridSystem(10, 10, tiles);

            // Before: CastRay from (2,5) Right is blocked by DestructibleWall at (4,5)
            var rayBefore = grid.CastRay(new Vector2Int(2, 5), Direction.Right, 5);
            Assert.AreEqual(1, rayBefore.Count, "Ray should stop before destructible wall");

            grid.ExpandDangerZone(new[] { new Vector2Int(4, 5) });

            // After: DestructibleWall → Empty, ray passes through
            var rayAfter = grid.CastRay(new Vector2Int(2, 5), Direction.Right, 5);
            Assert.AreEqual(5, rayAfter.Count, "Ray should now pass through destroyed wall");
        }

        #endregion

        #region Grid Modifications

        [Test]
        public void PlaceBarrier_MakesTileUnwalkable()
        {
            var grid = CreateOpenGrid();
            Assert.IsTrue(grid.IsWalkable(new Vector2Int(5, 5)));

            grid.PlaceBarrier(new Vector2Int(5, 5));

            Assert.IsFalse(grid.IsWalkable(new Vector2Int(5, 5)));
        }

        [Test]
        public void RemoveBarrier_MakesTileWalkable()
        {
            var grid = CreateGridWithWalls(10, 10, new Vector2Int(5, 5));
            Assert.IsFalse(grid.IsWalkable(new Vector2Int(5, 5)));

            grid.RemoveBarrier(new Vector2Int(5, 5));

            Assert.IsTrue(grid.IsWalkable(new Vector2Int(5, 5)));
        }

        #endregion
    }
}
