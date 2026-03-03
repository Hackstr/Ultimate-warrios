# Tactical Duelist — Technical Specification

## 1. Enums & Constants

```csharp
public enum Direction { Up, Down, Left, Right }

public enum ActionType
{
    Move,       // Move hero.Speed tiles in facing direction
    TurnLeft,   // Rotate 90° counter-clockwise
    TurnRight,  // Rotate 90° clockwise
    TurnAround, // Rotate 180°
    Shoot,      // Fire ray in facing direction, range = hero.Range
    Wait,       // Stay in place, keep facing
    Special     // Hero-unique ability, 1 per round
}

public enum GamePhase
{
    HeroSelect,
    Planning,
    Execution,
    PostRound,
    PostMatch
}

public enum MatchResult { Player1Win, Player2Win, Draw }
public enum RoundResult { Player1Kill, Player2Kill, MutualCancel, NoKill }
public enum TileType { Empty, Wall, DestructibleWall, DangerZone, OutOfBounds }
public enum PickupType { ArmorShard, IntelOrb, SpeedBoost, RangeBoost }

public enum SpecialAbility
{
    Ricochet,   // Archer: shot bounces off 1 wall
    Push,       // Tank: push entity 2 tiles in facing direction
    Blink,      // Shadow: teleport up to 4 tiles any direction
    Scan,       // Scout: reveal opponent's next action
    PhaseShot,  // Mage: shot passes through 1 wall
    Bomb,       // Demo: place 3x3 delayed explosion
    Barrier,    // Guardian: place temporary wall on adjacent tile
    Cloak,      // Ghost: invisible for 2 steps
    Turret,     // Engineer: place auto-shooting turret
    Charge,     // Berserker: move 3 tiles + shoot in 1 action
    Pierce,     // Hawk: shot passes through ALL obstacles
    Decoy       // Mirage: place visual clone at range 5
}
```

## 2. Data Models

### HeroConfig (ScriptableObject)
```csharp
[CreateAssetMenu(fileName = "NewHero", menuName = "TacticalDuelist/Hero Config")]
public class HeroConfig : ScriptableObject
{
    [Header("Identity")]
    public string heroId;        // "archer", "tank", etc.
    public string heroName;      // "Archer", "Tank", etc.
    public string displayName;   // Localized display name
    public Sprite portrait;
    public Sprite gridSprite;    // Top-down sprite for grid
    public Color heroColor;      // Fallback color if no sprite
    
    [Header("Parameters")]
    [Range(3, 6)] public int steps = 4;
    [Range(2, 10)] public int range = 5;
    [Range(0, 3)] public int cooldown = 1;
    [Range(0, 1)] public int armor = 0;
    [Range(1, 2)] public int speed = 1;
    
    [Header("Special")]
    public SpecialAbility specialAbility;
    public string specialName;
    [TextArea] public string specialDescription;
}
```

### MapConfig (ScriptableObject)
```csharp
[CreateAssetMenu(fileName = "NewMap", menuName = "TacticalDuelist/Map Config")]
public class MapConfig : ScriptableObject
{
    public string mapId;
    public string mapName;
    public int width;          // 8, 10, or 12
    public int height;         // 8, 10, or 12
    
    // Grid data: flattened 2D array [y * width + x]
    // 0 = empty, 1 = wall, 2 = destructible wall
    public int[] gridData;
    
    public Vector2Int player1Spawn;
    public Direction player1Facing;
    public Vector2Int player2Spawn;
    public Direction player2Facing;
    
    // Pickup spawn points
    public PickupSpawn[] pickups;
    
    // Shrink schedule: tiles that become danger zone each round
    public Vector2Int[][] shrinkRings; // [roundIndex] = array of tiles
}

[System.Serializable]
public class PickupSpawn
{
    public Vector2Int position;
    public PickupType type;
}
```

### HeroState (Runtime — per player per match)
```csharp
public class HeroState
{
    public HeroConfig Config { get; }
    public int PlayerIndex { get; }     // 0 or 1
    
    // Position & orientation
    public Vector2Int Position { get; set; }
    public Direction Facing { get; set; }
    
    // Combat state
    public bool IsAlive { get; set; } = true;
    public bool HasArmor { get; set; }
    public int CooldownRemaining { get; set; } = 0;
    public bool SpecialUsedThisRound { get; set; } = false;
    
    // Temporary buffs (from pickups, last 1 round)
    public int BonusSpeed { get; set; } = 0;
    public int BonusRange { get; set; } = 0;
    public bool HasIntel { get; set; } = false; // sees opponent's 1st action
    
    // Computed
    public int EffectiveSpeed => Config.speed + BonusSpeed;
    public int EffectiveRange => Config.range + BonusRange;
    
    // Ghost-specific
    public bool IsCloaked { get; set; } = false;
    public int CloakStepsRemaining { get; set; } = 0;
    
    public HeroState(HeroConfig config, int playerIndex, Vector2Int spawnPos, Direction spawnFacing)
    {
        Config = config;
        PlayerIndex = playerIndex;
        Position = spawnPos;
        Facing = spawnFacing;
        HasArmor = config.armor > 0;
    }
    
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
}
```

### StepResult (emitted per step for View/Replay)
```csharp
public class StepResult
{
    public int StepIndex;
    
    // Player 1
    public ActionType P1Action;
    public Vector2Int P1StartPos;
    public Vector2Int P1EndPos;
    public Direction P1StartFacing;
    public Direction P1EndFacing;
    
    // Player 2
    public ActionType P2Action;
    public Vector2Int P2StartPos;
    public Vector2Int P2EndPos;
    public Direction P2StartFacing;
    public Direction P2EndFacing;
    
    // Combat results
    public bool P1Fired;
    public bool P2Fired;
    public bool P1Hit;           // P1's shot hit P2
    public bool P2Hit;           // P2's shot hit P1
    public bool MutualCancel;    // both hit = cancel
    public bool P1ArmorBroken;
    public bool P2ArmorBroken;
    public bool P1Eliminated;
    public bool P2Eliminated;
    
    // Special ability data (nullable)
    public SpecialResult P1Special;
    public SpecialResult P2Special;
    
    // Pickups
    public PickupType? P1PickedUp;
    public PickupType? P2PickedUp;
}

public class SpecialResult
{
    public SpecialAbility Ability;
    public Vector2Int? TargetPosition;  // for Blink, Bomb, Barrier, Turret, Decoy
    public Direction? TargetDirection;  // for Ricochet bounce direction
}
```

## 3. GridSystem

```csharp
public class GridSystem
{
    private TileType[,] _grid;        // [col, row]
    private Dictionary<Vector2Int, PickupType> _pickups;
    private HashSet<Vector2Int> _dangerZone;
    
    public int Width { get; }
    public int Height { get; }
    
    // Constructor from MapConfig
    public GridSystem(MapConfig config) { ... }
    
    // Queries
    public TileType GetTile(Vector2Int pos);
    public bool IsWalkable(Vector2Int pos);          // Empty or has pickup
    public bool IsInBounds(Vector2Int pos);
    public bool IsInDangerZone(Vector2Int pos);
    public bool HasPickup(Vector2Int pos);
    public PickupType? GetPickup(Vector2Int pos);
    
    // Line of sight: returns list of tiles ray passes through
    // Stops at wall (unless pierce/phase)
    public List<Vector2Int> CastRay(Vector2Int from, Direction dir, int range);
    
    // Same but passes through 1 wall (Mage PhaseShot)
    public List<Vector2Int> CastRayPhase(Vector2Int from, Direction dir, int range);
    
    // Same but passes through ALL walls (Hawk Pierce)  
    public List<Vector2Int> CastRayPierce(Vector2Int from, Direction dir, int range);
    
    // Ricochet: bounces off first wall hit
    public List<Vector2Int> CastRayRicochet(Vector2Int from, Direction dir, int range);
    
    // Movement: target tile for moving in direction with speed
    public Vector2Int GetMoveTarget(Vector2Int from, Direction dir, int speed);
    
    // Modifications
    public void RemovePickup(Vector2Int pos);
    public void DestroyWall(Vector2Int pos);         // Destructible → Empty
    public void PlaceBarrier(Vector2Int pos);        // Empty → Wall (temporary)
    public void RemoveBarrier(Vector2Int pos);
    public void ExpandDangerZone(Vector2Int[] tiles);
    
    // Direction helpers (static)
    public static Vector2Int DirectionToVector(Direction dir);
    // Up=(0,1), Down=(0,-1), Left=(-1,0), Right=(1,0)
    
    public static Direction TurnLeft(Direction dir);
    public static Direction TurnRight(Direction dir);
    public static Direction TurnAround(Direction dir);
    public static Direction VectorToDirection(Vector2Int vec); // nearest
}
```

## 4. ActionResolver — Step Resolution Algorithm

This is the CORE of the entire game. Must be 100% deterministic.

```csharp
public class ActionResolver
{
    private GridSystem _grid;
    private HeroState _p1;
    private HeroState _p2;
    
    public StepResult ResolveStep(int stepIndex, ActionType p1Action, ActionType p2Action)
    {
        var result = new StepResult { StepIndex = stepIndex };
        
        // Record start state
        result.P1StartPos = _p1.Position;
        result.P1StartFacing = _p1.Facing;
        result.P2StartPos = _p2.Position;
        result.P2StartFacing = _p2.Facing;
        result.P1Action = p1Action;
        result.P2Action = p2Action;
        
        // ═══ PHASE 1: MOVEMENT ═══
        ResolveMovement(p1Action, p2Action, result);
        
        // ═══ PHASE 2: COMBAT ═══
        ResolveCombat(p1Action, p2Action, result);
        
        // ═══ PHASE 3: DAMAGE ═══
        ResolveDamage(result);
        
        // Update cooldowns
        UpdateCooldowns(p1Action, p2Action);
        
        // Record end state
        result.P1EndPos = _p1.Position;
        result.P1EndFacing = _p1.Facing;
        result.P2EndPos = _p2.Position;
        result.P2EndFacing = _p2.Facing;
        
        return result;
    }
    
    private void ResolveMovement(ActionType p1Act, ActionType p2Act, StepResult result)
    {
        Vector2Int p1Target = _p1.Position;
        Vector2Int p2Target = _p2.Position;
        
        // Calculate intended positions
        if (p1Act == ActionType.Move)
            p1Target = _grid.GetMoveTarget(_p1.Position, _p1.Facing, _p1.EffectiveSpeed);
        if (p1Act == ActionType.TurnLeft)
            _p1.Facing = GridSystem.TurnLeft(_p1.Facing);
        if (p1Act == ActionType.TurnRight)
            _p1.Facing = GridSystem.TurnRight(_p1.Facing);
        if (p1Act == ActionType.TurnAround)
            _p1.Facing = GridSystem.TurnAround(_p1.Facing);
            
        if (p2Act == ActionType.Move)
            p2Target = _grid.GetMoveTarget(_p2.Position, _p2.Facing, _p2.EffectiveSpeed);
        if (p2Act == ActionType.TurnLeft)
            _p2.Facing = GridSystem.TurnLeft(_p2.Facing);
        if (p2Act == ActionType.TurnRight)
            _p2.Facing = GridSystem.TurnRight(_p2.Facing);
        if (p2Act == ActionType.TurnAround)
            _p2.Facing = GridSystem.TurnAround(_p2.Facing);
        
        // Collision check: both moving to same tile
        if (p1Target == p2Target && p1Target != _p1.Position && p2Target != _p2.Position)
        {
            // Both stay at original positions
            p1Target = _p1.Position;
            p2Target = _p2.Position;
        }
        
        // Also check: moving INTO each other's current position (swap)
        if (p1Target == _p2.Position && p2Target == _p1.Position)
        {
            p1Target = _p1.Position;
            p2Target = _p2.Position;
        }
        
        // Apply movement
        _p1.Position = p1Target;
        _p2.Position = p2Target;
        
        // Pickup check
        if (_grid.HasPickup(_p1.Position))
        {
            result.P1PickedUp = _grid.GetPickup(_p1.Position);
            ApplyPickup(_p1, result.P1PickedUp.Value);
            _grid.RemovePickup(_p1.Position);
        }
        if (_grid.HasPickup(_p2.Position))
        {
            result.P2PickedUp = _grid.GetPickup(_p2.Position);
            ApplyPickup(_p2, result.P2PickedUp.Value);
            _grid.RemovePickup(_p2.Position);
        }
    }
    
    private void ResolveCombat(ActionType p1Act, ActionType p2Act, StepResult result)
    {
        bool p1Shoots = (p1Act == ActionType.Shoot) && (_p1.CooldownRemaining <= 0);
        bool p2Shoots = (p2Act == ActionType.Shoot) && (_p2.CooldownRemaining <= 0);
        
        result.P1Fired = p1Shoots;
        result.P2Fired = p2Shoots;
        
        if (p1Shoots)
        {
            var rayTiles = _grid.CastRay(_p1.Position, _p1.Facing, _p1.EffectiveRange);
            result.P1Hit = rayTiles.Contains(_p2.Position);
        }
        
        if (p2Shoots)
        {
            var rayTiles = _grid.CastRay(_p2.Position, _p2.Facing, _p2.EffectiveRange);
            result.P2Hit = rayTiles.Contains(_p1.Position);
        }
    }
    
    private void ResolveDamage(StepResult result)
    {
        // MUTUAL HIT = CANCEL
        if (result.P1Hit && result.P2Hit)
        {
            result.MutualCancel = true;
            result.P1Hit = false;
            result.P2Hit = false;
            return; // Nobody takes damage
        }
        
        // P1 hit P2
        if (result.P1Hit)
        {
            if (_p2.HasArmor)
            {
                _p2.HasArmor = false;
                result.P2ArmorBroken = true;
            }
            else
            {
                _p2.IsAlive = false;
                result.P2Eliminated = true;
            }
        }
        
        // P2 hit P1
        if (result.P2Hit)
        {
            if (_p1.HasArmor)
            {
                _p1.HasArmor = false;
                result.P1ArmorBroken = true;
            }
            else
            {
                _p1.IsAlive = false;
                result.P1Eliminated = true;
            }
        }
    }
}
```

## 5. MatchManager — Orchestration

```csharp
public class MatchManager
{
    public GamePhase CurrentPhase { get; private set; }
    public int CurrentRound { get; private set; }
    public HeroState Player1 { get; private set; }
    public HeroState Player2 { get; private set; }
    
    private GridSystem _grid;
    private ActionResolver _resolver;
    private ShrinkSystem _shrink;
    
    private const int MAX_ROUNDS = 3;
    private const float PLANNING_TIME_ROUND1 = 30f;
    private const float PLANNING_TIME_LATER = 20f;
    
    public void StartMatch(HeroConfig p1Hero, HeroConfig p2Hero, MapConfig map)
    {
        _grid = new GridSystem(map);
        Player1 = new HeroState(p1Hero, 0, map.player1Spawn, map.player1Facing);
        Player2 = new HeroState(p2Hero, 1, map.player2Spawn, map.player2Facing);
        _resolver = new ActionResolver(_grid, Player1, Player2);
        _shrink = new ShrinkSystem(_grid, map);
        CurrentRound = 1;
        
        GameEvents.OnMatchStarted?.Invoke(new MatchStartData { ... });
        StartPlanningPhase();
    }
    
    public void SubmitActions(int playerIndex, List<ActionType> actions)
    {
        // Validate action list:
        // - Length must equal hero.Steps
        // - Shoot must respect cooldown gaps
        // - Special must appear at most once
        // - If no actions programmed, fill with Wait
    }
    
    public void ExecuteRound(List<ActionType> p1Actions, List<ActionType> p2Actions)
    {
        int maxSteps = Mathf.Max(p1Actions.Count, p2Actions.Count);
        
        for (int step = 0; step < maxSteps; step++)
        {
            var p1Act = step < p1Actions.Count ? p1Actions[step] : ActionType.Wait;
            var p2Act = step < p2Actions.Count ? p2Actions[step] : ActionType.Wait;
            
            StepResult result = _resolver.ResolveStep(step, p1Act, p2Act);
            GameEvents.OnStepResolved?.Invoke(result);
            
            if (result.P1Eliminated || result.P2Eliminated)
            {
                EndRound(result.P1Eliminated ? RoundResult.Player2Kill : RoundResult.Player1Kill);
                return;
            }
        }
        
        // No elimination this round
        if (CurrentRound >= MAX_ROUNDS)
        {
            EndMatch(MatchResult.Draw);
        }
        else
        {
            EndRound(RoundResult.NoKill);
        }
    }
    
    private void EndRound(RoundResult roundResult)
    {
        GameEvents.OnRoundEnded?.Invoke(roundResult);
        
        if (roundResult == RoundResult.Player1Kill)
            EndMatch(MatchResult.Player1Win);
        else if (roundResult == RoundResult.Player2Kill)
            EndMatch(MatchResult.Player2Win);
        else
        {
            CurrentRound++;
            _shrink.ApplyShrink(CurrentRound);
            Player1.ResetForNewRound();
            Player2.ResetForNewRound();
            StartPlanningPhase();
        }
    }
}
```

## 6. Action Validation Rules

```
SHOOT validation:
  - Cannot shoot if CooldownRemaining > 0
  - After shooting: CooldownRemaining = hero.Cooldown
  - Each step: CooldownRemaining = max(0, CooldownRemaining - 1)
  - Example: Archer (CD=2) shoots at step 0
    Step 0: SHOOT ✓ (CD was 0) → CD becomes 2
    Step 1: CD = 1, cannot shoot
    Step 2: CD = 0, CAN shoot
    Step 3: SHOOT ✓

SPECIAL validation:
  - Max 1 per round
  - Replaces the action for that step
  - Some specials also count as Shoot (Charge = Move+Shoot)
  
MOVE validation:
  - Target tile must be walkable (not wall, not out of bounds)
  - If blocked by wall mid-path (Speed 2): stop at last walkable tile
  - Cannot move through other player (stop before them)
  
TURN validation:
  - Always valid. No restrictions.
  
WAIT validation:
  - Always valid. Hero stays in place.
```

## 7. Map Shrinking

```
Round 1: Full map playable
Round 2: Outermost 1-tile ring becomes danger zone
Round 3: Outermost 2-tile ring becomes danger zone

Danger zone rules:
  - Players CAN move through danger zone during steps
  - At END of round: anyone standing in danger zone = ELIMINATED
  - Visual: red/fire overlay on danger tiles
  - Walls in danger zone are destroyed

Example 10x10 map:
  Round 1: 100 playable tiles (full 10x10)
  Round 2: 64 playable tiles (inner 8x8)  
  Round 3: 36 playable tiles (inner 6x6)
```
