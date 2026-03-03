# Tactical Duelist — Coding Conventions

## Naming

```csharp
// Classes, Methods, Properties, Events — PascalCase
public class ActionResolver { }
public void ResolveStep() { }
public int CurrentRound { get; }
public event Action<StepResult> OnStepResolved;

// Private fields — _camelCase
private GridSystem _grid;
private int _currentStep;
[SerializeField] private HeroConfig _heroConfig;

// Local variables, parameters — camelCase
int stepIndex = 0;
void ProcessAction(ActionType action) { }

// Constants — PascalCase (NOT SCREAMING_CASE)
private const int MaxRounds = 3;
private const float PlanningTimeRound1 = 30f;

// Enums — PascalCase for type and values
public enum Direction { Up, Down, Left, Right }

// Interfaces — IPrefixed
public interface IAction { }
```

## File Organization

One class per file. File name = class name exactly.

```csharp
// HeroState.cs
using UnityEngine;

namespace TacticalDuelist.Core.Models
{
    public class HeroState
    {
        #region Constants
        private const int DefaultCooldown = 0;
        #endregion

        #region Fields
        private int _cooldownRemaining;
        #endregion

        #region Properties
        public HeroConfig Config { get; }
        public Vector2Int Position { get; set; }
        public bool IsAlive { get; set; }
        #endregion

        #region Constructor
        public HeroState(HeroConfig config, Vector2Int spawn)
        {
            Config = config;
            Position = spawn;
            IsAlive = true;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Resets temporary state for a new round.
        /// Preserves armor status and position.
        /// </summary>
        public void ResetForNewRound()
        {
            _cooldownRemaining = DefaultCooldown;
        }
        #endregion
    }
}
```

## Namespaces

```
TacticalDuelist.Core.Models     — Data classes (HeroState, StepResult, etc.)
TacticalDuelist.Core.Systems    — Logic (ActionResolver, GridSystem, etc.)
TacticalDuelist.Core.Config     — ScriptableObjects (HeroConfig, MapConfig)
TacticalDuelist.Gameplay        — MonoBehaviour wrappers (GridView, HeroView)
TacticalDuelist.UI              — UI controllers
TacticalDuelist.Networking      — Network layer
TacticalDuelist.Utils           — Helpers, extensions
```

## Core vs View Boundary

```
CORE LAYER (pure C#, no MonoBehaviour):
  ✓ Uses: System, System.Collections.Generic, UnityEngine (Vector2Int only)
  ✗ Never: MonoBehaviour, Transform, GameObject, Coroutine, Update
  ✗ Never: Time.deltaTime, Camera, Input, Physics
  ✓ Communication: returns data, raises events
  ✓ Must be: deterministic, testable, server-reproducible

VIEW LAYER (MonoBehaviour):
  ✓ Uses: everything Unity
  ✓ Subscribes to: GameEvents
  ✗ Never: modifies game state directly
  ✗ Never: contains game logic
  ✓ Reads: HeroState, GridSystem (read-only queries)
  ✓ Calls: MatchManager.SubmitActions() (only entry point)
```

## Error Handling

```csharp
// Core layer: throw on programmer errors
public TileType GetTile(Vector2Int pos)
{
    if (!IsInBounds(pos))
        throw new System.ArgumentOutOfRangeException(
            nameof(pos), $"Position {pos} is out of grid bounds ({Width}x{Height})");
    return _grid[pos.x, pos.y];
}

// View layer: log warnings, never crash
void OnStepResolved(StepResult result)
{
    if (_heroViews == null)
    {
        Debug.LogWarning("[HeroView] Received step result but views not initialized");
        return;
    }
}
```

## ScriptableObject Pattern

```csharp
// Definition in Scripts/Core/Config/
[CreateAssetMenu(fileName = "NewHero", menuName = "TacticalDuelist/Hero Config")]
public class HeroConfig : ScriptableObject
{
    // Use [Header] to group in inspector
    [Header("Identity")]
    public string heroId;
    
    // Use [Range] for numeric bounds
    [Header("Parameters")]
    [Range(3, 6)] public int steps = 4;
    
    // Use [TextArea] for long text
    [Header("Special")]
    [TextArea(2, 4)] public string specialDescription;
    
    // Use [Tooltip] for non-obvious fields
    [Tooltip("Tiles to wait between shots. 0 = no cooldown.")]
    [Range(0, 3)] public int cooldown = 1;
}

// Asset creation: Right-click ScriptableObjects/Heroes → Create → TacticalDuelist → Hero Config
// Naming: Hero_Archer.asset, Hero_Tank.asset, etc.
```

## Event Pattern

```csharp
// Define in GameEvents.cs (static class)
public static class GameEvents
{
    public static event Action<StepResult> OnStepResolved;
    
    // Invoke with null-check
    public static void StepResolved(StepResult result)
    {
        OnStepResolved?.Invoke(result);
    }
    
    // Clear all (call on match end to prevent leaks)
    public static void ClearAll()
    {
        OnStepResolved = null;
        // ... all events
    }
}

// Subscribe in View (MonoBehaviour)
void OnEnable()
{
    GameEvents.OnStepResolved += HandleStepResolved;
}

void OnDisable()
{
    GameEvents.OnStepResolved -= HandleStepResolved;
}
```

## WebGL Rules

```csharp
// ✗ NEVER — breaks WebGL
System.Threading.Thread
System.IO.File
async/await with Task (use UniTask or coroutines)

// ✓ ALWAYS — WebGL safe
IEnumerator + yield return   // coroutines
PlayerPrefs                  // local storage
UnityWebRequest              // network calls
string.GetHashCode()         // NOT deterministic across platforms! 
                             // Use custom hash for gameplay

// ✓ Performance
// Cache everything. Avoid allocations in loops.
// Pre-allocate lists with known capacity.
private readonly List<Vector2Int> _rayBuffer = new List<Vector2Int>(16);

public List<Vector2Int> CastRay(...)
{
    _rayBuffer.Clear();  // reuse, don't new
    // ... fill buffer
    return _rayBuffer;
}
```

## Comments Policy

```csharp
// DO: explain WHY, not WHAT
// Mutual cancel: game design decision to prevent "both die" scenarios
// which feel unfair and create 50/50 coinflip outcomes
if (result.P1Hit && result.P2Hit)
{
    result.MutualCancel = true;
    return;
}

// DO: mark assumptions
// Assumes both players have submitted actions (validated by MatchManager)
public StepResult ResolveStep(int stepIndex, ActionType p1Action, ActionType p2Action)

// DO: XML docs on public methods
/// <summary>
/// Casts a ray from position in direction, returns all tiles crossed.
/// Stops at first wall encountered (does not include wall tile).
/// </summary>
/// <param name="from">Starting tile (excluded from result)</param>
/// <param name="dir">Direction to cast</param>
/// <param name="range">Maximum tiles to travel</param>
/// <returns>List of empty tiles the ray passes through</returns>
public List<Vector2Int> CastRay(Vector2Int from, Direction dir, int range)

// DON'T: obvious comments
// Creates a new hero state  ← useless
public HeroState(HeroConfig config) { }
```
