using NUnit.Framework;
using UnityEngine;
using TacticalDuelist.Core.Config;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Systems;

namespace TacticalDuelist.Tests
{
    /// <summary>
    /// Edit Mode unit tests for ActionResolver.
    /// Covers: movement, collision, shooting, mutual cancel, armor, elimination, cooldown.
    /// </summary>
    [TestFixture]
    public class ActionResolverTests
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

        private static GridSystem CreateGridWithWall(int w, int h, Vector2Int wallPos)
        {
            var tiles = new TileType[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    tiles[x, y] = TileType.Empty;
            tiles[wallPos.x, wallPos.y] = TileType.Wall;
            return new GridSystem(w, h, tiles);
        }

        private static HeroConfig CreateHeroConfig(
            int steps = 4, int range = 5, int cooldown = 1,
            int armor = 0, int speed = 1)
        {
            var config = ScriptableObject.CreateInstance<HeroConfig>();
            config.heroId = "test_hero";
            config.heroName = "TestHero";
            config.steps = steps;
            config.range = range;
            config.cooldown = cooldown;
            config.armor = armor;
            config.speed = speed;
            return config;
        }

        #endregion

        #region Basic Movement

        [Test]
        public void Move_FacingUp_MovesOneUp()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(speed: 1);
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(5, 2), Direction.Up);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(8, 8), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Move, ActionType.Wait);

            Assert.AreEqual(new Vector2Int(5, 3), result.P1EndPos);
            Assert.AreEqual(new Vector2Int(8, 8), result.P2EndPos);
        }

        [Test]
        public void Move_Speed2_MovesTwoTiles()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(speed: 2);
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(3, 3), Direction.Right);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(9, 9), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Move, ActionType.Wait);

            Assert.AreEqual(new Vector2Int(5, 3), result.P1EndPos);
        }

        [Test]
        public void Move_FacingDown_MovesOneDown()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(speed: 1);
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(5, 5), Direction.Down);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(9, 9), Direction.Up);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Move, ActionType.Wait);

            Assert.AreEqual(new Vector2Int(5, 4), result.P1EndPos);
        }

        [Test]
        public void Wait_DoesNotMove()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig();
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(5, 5), Direction.Up);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(9, 9), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Wait, ActionType.Wait);

            Assert.AreEqual(new Vector2Int(5, 5), result.P1EndPos);
            Assert.AreEqual(new Vector2Int(9, 9), result.P2EndPos);
        }

        #endregion

        #region Turning

        [Test]
        public void TurnLeft_ChangesFacingCorrectly()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig();
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(5, 5), Direction.Up);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(9, 9), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.TurnLeft, ActionType.Wait);

            Assert.AreEqual(Direction.Left, result.P1EndFacing);
            Assert.AreEqual(new Vector2Int(5, 5), result.P1EndPos, "Turn should not move");
        }

        [Test]
        public void TurnRight_ChangesFacingCorrectly()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig();
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(5, 5), Direction.Up);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(9, 9), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.TurnRight, ActionType.Wait);

            Assert.AreEqual(Direction.Right, result.P1EndFacing);
        }

        [Test]
        public void TurnAround_ReversesFacing()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig();
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(5, 5), Direction.Left);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(9, 9), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.TurnAround, ActionType.Wait);

            Assert.AreEqual(Direction.Right, result.P1EndFacing);
        }

        #endregion

        #region Collision

        [Test]
        public void Collision_BothMoveToSameTile_BothStayPut()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(speed: 1);
            // P1 at (4,5) facing Right; P2 at (6,5) facing Left → both target (5,5)
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(4, 5), Direction.Right);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(6, 5), Direction.Left);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Move, ActionType.Move);

            Assert.AreEqual(new Vector2Int(4, 5), result.P1EndPos, "P1 should stay at original");
            Assert.AreEqual(new Vector2Int(6, 5), result.P2EndPos, "P2 should stay at original");
        }

        [Test]
        public void Collision_SwapPositions_BothStayPut()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(speed: 1);
            // P1 at (5,5) facing Right; P2 at (6,5) facing Left → swap collision
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(5, 5), Direction.Right);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(6, 5), Direction.Left);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Move, ActionType.Move);

            Assert.AreEqual(new Vector2Int(5, 5), result.P1EndPos, "P1 should stay (swap blocked)");
            Assert.AreEqual(new Vector2Int(6, 5), result.P2EndPos, "P2 should stay (swap blocked)");
        }

        [Test]
        public void Collision_MovingIntoStationaryOpponent_StopsBefore()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(speed: 1);
            // P1 at (4,5) facing Right; P2 stationary at (5,5) — P1 tries to move into P2
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(4, 5), Direction.Right);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(5, 5), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Move, ActionType.Wait);

            Assert.AreEqual(new Vector2Int(4, 5), result.P1EndPos, "P1 should stop before opponent");
        }

        #endregion

        #region Wall Blocking

        [Test]
        public void Move_BlockedByWall_StaysAtOriginal()
        {
            // Wall at (5,6) — P1 at (5,5) facing Up → blocked
            var grid = CreateGridWithWall(10, 10, new Vector2Int(5, 6));
            var heroConfig = CreateHeroConfig(speed: 1);
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(5, 5), Direction.Up);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(9, 9), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Move, ActionType.Wait);

            Assert.AreEqual(new Vector2Int(5, 5), result.P1EndPos);
        }

        [Test]
        public void Move_Speed2_StopsBeforeWall()
        {
            // Wall at (7,3) — P1 at (5,3) facing Right with speed 2 → moves to (6,3), blocked at (7,3)
            var grid = CreateGridWithWall(10, 10, new Vector2Int(7, 3));
            var heroConfig = CreateHeroConfig(speed: 2);
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(5, 3), Direction.Right);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(0, 0), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Move, ActionType.Wait);

            Assert.AreEqual(new Vector2Int(6, 3), result.P1EndPos);
        }

        [Test]
        public void Move_AtGridEdge_CannotMoveOut()
        {
            var grid = CreateOpenGrid(10, 10);
            var heroConfig = CreateHeroConfig(speed: 1);
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(0, 0), Direction.Down);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(9, 9), Direction.Up);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Move, ActionType.Wait);

            Assert.AreEqual(new Vector2Int(0, 0), result.P1EndPos, "Should not leave grid");
        }

        #endregion

        #region Shooting

        [Test]
        public void Shoot_OpponentInRange_Hits()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(range: 5, cooldown: 1);
            // P1 at (2,5) facing Right; P2 at (5,5) → distance 3, within range 5
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(2, 5), Direction.Right);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(5, 5), Direction.Left);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Shoot, ActionType.Wait);

            Assert.IsTrue(result.P1Fired, "P1 should fire");
            Assert.IsTrue(result.P1Hit, "P1 should hit P2");
        }

        [Test]
        public void Shoot_OpponentOutOfRange_Misses()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(range: 3, cooldown: 1);
            // P1 at (1,5) facing Right; P2 at (8,5) → distance 7, range only 3
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(1, 5), Direction.Right);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(8, 5), Direction.Left);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Shoot, ActionType.Wait);

            Assert.IsTrue(result.P1Fired);
            Assert.IsFalse(result.P1Hit, "P2 is out of range");
        }

        [Test]
        public void Shoot_WrongDirection_Misses()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(range: 10, cooldown: 1);
            // P1 at (5,5) facing Up; P2 at (5,3) → P2 is below, but P1 faces Up
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(5, 5), Direction.Up);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(5, 3), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Shoot, ActionType.Wait);

            Assert.IsTrue(result.P1Fired);
            Assert.IsFalse(result.P1Hit, "Facing wrong direction");
        }

        [Test]
        public void Shoot_BlockedByWall_Misses()
        {
            // Wall at (4,5) between P1 at (3,5) and P2 at (6,5)
            var grid = CreateGridWithWall(10, 10, new Vector2Int(4, 5));
            var heroConfig = CreateHeroConfig(range: 5, cooldown: 1);
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(3, 5), Direction.Right);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(6, 5), Direction.Left);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Shoot, ActionType.Wait);

            Assert.IsTrue(result.P1Fired);
            Assert.IsFalse(result.P1Hit, "Wall blocks the shot");
        }

        [Test]
        public void Shoot_AfterMove_UsesNewPosition()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(range: 5, cooldown: 1, speed: 1);
            // P1 at (2,5) facing Right; P2 at (5,5)
            // P1 moves to (3,5) this step, P2 also moves right to (4,5)
            // But they're both doing Move, so no shot this step — we need separate steps
            // Instead: P1 Shoots from (2,5), P2 Moves right to (6,5)
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(2, 5), Direction.Right);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(5, 5), Direction.Right);
            var resolver = new ActionResolver(grid, p1, p2);

            // P1 shoots while P2 moves right (5,5) → (6,5)
            var result = resolver.ResolveStep(0, ActionType.Shoot, ActionType.Move);

            // Shot from (2,5) Right, P2 now at (6,5) — distance 4, within range 5
            Assert.IsTrue(result.P1Fired);
            Assert.IsTrue(result.P1Hit, "P1 shoots at P2's new position after movement");
        }

        #endregion

        #region Mutual Cancel

        [Test]
        public void MutualCancel_BothShootAndHit_NoDamage()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(range: 10, cooldown: 1);
            // P1 at (2,5) facing Right; P2 at (8,5) facing Left → both in range
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(2, 5), Direction.Right);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(8, 5), Direction.Left);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Shoot, ActionType.Shoot);

            Assert.IsTrue(result.MutualCancel, "Mutual cancel should trigger");
            Assert.IsFalse(result.P1Eliminated, "P1 should survive");
            Assert.IsFalse(result.P2Eliminated, "P2 should survive");
            Assert.IsTrue(p1.IsAlive, "P1 stays alive");
            Assert.IsTrue(p2.IsAlive, "P2 stays alive");
        }

        [Test]
        public void MutualCancel_ArmorNotBroken()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(range: 10, cooldown: 1, armor: 1);
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(2, 5), Direction.Right);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(8, 5), Direction.Left);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Shoot, ActionType.Shoot);

            Assert.IsTrue(result.MutualCancel);
            Assert.IsTrue(p1.HasArmor, "Armor should be intact after mutual cancel");
            Assert.IsTrue(p2.HasArmor, "Armor should be intact after mutual cancel");
        }

        #endregion

        #region Armor

        [Test]
        public void Armor_AbsorbsHit_NotEliminated()
        {
            var grid = CreateOpenGrid();
            var attackerConfig = CreateHeroConfig(range: 10, cooldown: 1, armor: 0);
            var defenderConfig = CreateHeroConfig(range: 10, cooldown: 1, armor: 1);
            var p1 = new HeroState(attackerConfig, 0, new Vector2Int(2, 5), Direction.Right);
            var p2 = new HeroState(defenderConfig, 1, new Vector2Int(7, 5), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            Assert.IsTrue(p2.HasArmor, "P2 starts with armor");

            var result = resolver.ResolveStep(0, ActionType.Shoot, ActionType.Wait);

            Assert.IsTrue(result.P1Fired);
            Assert.IsTrue(result.P2ArmorBroken, "P2 armor should be broken");
            Assert.IsFalse(result.P2Eliminated, "P2 should NOT be eliminated");
            Assert.IsFalse(p2.HasArmor, "P2 armor should now be gone");
            Assert.IsTrue(p2.IsAlive, "P2 should still be alive");
        }

        [Test]
        public void Armor_SecondShot_Eliminates()
        {
            var grid = CreateOpenGrid();
            var attackerConfig = CreateHeroConfig(range: 10, cooldown: 0, armor: 0);
            var defenderConfig = CreateHeroConfig(range: 10, cooldown: 1, armor: 1);
            var p1 = new HeroState(attackerConfig, 0, new Vector2Int(2, 5), Direction.Right);
            var p2 = new HeroState(defenderConfig, 1, new Vector2Int(7, 5), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            // First shot breaks armor
            resolver.ResolveStep(0, ActionType.Shoot, ActionType.Wait);
            Assert.IsFalse(p2.HasArmor);

            // Second shot eliminates (cooldown 0 means can fire immediately)
            var result2 = resolver.ResolveStep(1, ActionType.Shoot, ActionType.Wait);

            Assert.IsTrue(result2.P1Fired);
            Assert.IsTrue(result2.P2Eliminated, "P2 should be eliminated (no armor)");
            Assert.IsFalse(p2.IsAlive, "P2 should be dead");
        }

        #endregion

        #region Elimination

        [Test]
        public void Shoot_NoArmor_Eliminates()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(range: 10, cooldown: 1, armor: 0);
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(2, 5), Direction.Right);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(7, 5), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Shoot, ActionType.Wait);

            Assert.IsTrue(result.P1Fired);
            Assert.IsTrue(result.P2Eliminated);
            Assert.IsFalse(p2.IsAlive);
        }

        [Test]
        public void OneHitsOther_NotMutualCancel()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(range: 10, cooldown: 1, armor: 0);
            // P1 facing Right toward P2, P2 facing Up (won't hit P1)
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(2, 5), Direction.Right);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(7, 5), Direction.Up);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Shoot, ActionType.Shoot);

            Assert.IsTrue(result.P1Fired);
            Assert.IsTrue(result.P2Fired);
            Assert.IsTrue(result.P1Hit, "P1 hits P2");
            Assert.IsFalse(result.P2Hit, "P2 misses P1 (wrong direction)");
            Assert.IsFalse(result.MutualCancel);
            Assert.IsTrue(result.P2Eliminated);
        }

        #endregion

        #region Cooldown

        [Test]
        public void Cooldown_CannotShootWhileOnCooldown()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(range: 10, cooldown: 2, armor: 0);
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(2, 5), Direction.Right);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(7, 5), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            // Step 0: P1 shoots — sets cooldown to 2
            var r0 = resolver.ResolveStep(0, ActionType.Shoot, ActionType.Wait);
            Assert.IsTrue(r0.P1Fired, "Step 0: P1 should fire");

            // Step 1: P1 tries to shoot again but on cooldown
            var r1 = resolver.ResolveStep(1, ActionType.Shoot, ActionType.Wait);
            Assert.IsFalse(r1.P1Fired, "Step 1: P1 should NOT fire (cooldown)");
        }

        [Test]
        public void Cooldown_TicksDown_CanShootAfterCooldown()
        {
            var grid = CreateOpenGrid();
            // cooldown 1: fire → set CD=1 → tick to 0 → next step CD=0 → can fire
            var heroConfig = CreateHeroConfig(range: 10, cooldown: 1, armor: 0);
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(2, 5), Direction.Right);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(7, 5), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            // Step 0: fire
            var r0 = resolver.ResolveStep(0, ActionType.Shoot, ActionType.Wait);
            Assert.IsTrue(r0.P1Fired);

            // Step 1: cooldown should have ticked to 0 by end of step 0
            var r1 = resolver.ResolveStep(1, ActionType.Shoot, ActionType.Wait);
            Assert.IsTrue(r1.P1Fired, "Should be able to fire after cooldown 1 expires");
        }

        [Test]
        public void Cooldown_Zero_CanFireEveryStep()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(range: 10, cooldown: 0, armor: 0);
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(2, 5), Direction.Right);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(9, 5), Direction.Down);
            // P2 far enough to survive (outside hitbox for now)
            var resolver = new ActionResolver(grid, p1, p2);

            for (int i = 0; i < 3; i++)
            {
                var result = resolver.ResolveStep(i, ActionType.Shoot, ActionType.Wait);
                Assert.IsTrue(result.P1Fired, $"Step {i}: should fire with cooldown 0");
            }
        }

        #endregion

        #region Simultaneous Actions

        [Test]
        public void BothMove_Simultaneously_ToSeparateTiles()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(speed: 1);
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(3, 3), Direction.Up);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(7, 7), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Move, ActionType.Move);

            Assert.AreEqual(new Vector2Int(3, 4), result.P1EndPos);
            Assert.AreEqual(new Vector2Int(7, 6), result.P2EndPos);
        }

        [Test]
        public void P1Moves_P2Shoots_ShootsAtNewPosition()
        {
            var grid = CreateOpenGrid();
            var heroConfig = CreateHeroConfig(range: 10, cooldown: 1, speed: 1);
            // P1 at (5,3) facing Up; P2 at (5,8) facing Down
            // P1 moves to (5,4); P2 shoots down from (5,8)
            // Movement resolves first, then combat
            // P2 ray goes down: (5,7), (5,6), (5,5), (5,4) — hits P1 at new position (5,4)
            var p1 = new HeroState(heroConfig, 0, new Vector2Int(5, 3), Direction.Up);
            var p2 = new HeroState(heroConfig, 1, new Vector2Int(5, 8), Direction.Down);
            var resolver = new ActionResolver(grid, p1, p2);

            var result = resolver.ResolveStep(0, ActionType.Move, ActionType.Shoot);

            Assert.AreEqual(new Vector2Int(5, 4), result.P1EndPos, "P1 moves up");
            Assert.IsTrue(result.P2Fired);
            Assert.IsTrue(result.P2Hit, "P2 should hit P1 at new position (5,4)");
        }

        #endregion
    }
}
