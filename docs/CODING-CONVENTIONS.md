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
TacticalDuelist.Core.Systems    — Logic (ActionResolver, GridSystem, MatchManager)
TacticalDuelist.Core.Config     — ScriptableObjects (HeroConfig, MapConfig)
TacticalDuelist.Core.Utils      — Pure helpers (GridHelper, HashUtil)
TacticalDuelist.Gameplay        — MonoBehaviours (HeroView3D, GridView, CameraController)
TacticalDuelist.UI              — UI controllers (screens, HUD)
TacticalDuelist.Networking      — Network layer (SocketIOClient, MatchNetworkController)
TacticalDuelist.Platform        — Platform abstraction (IPlatformService, WebGLPlatform, etc.)
TacticalDuelist.Utils           — General extensions, helpers
```

## Core vs View Boundary

```
CORE LAYER (pure C#, no MonoBehaviour):
  ✓ Uses: System, System.Collections.Generic, UnityEngine (Vector2Int only)
  ✗ Never: MonoBehaviour, Transform, GameObject, Coroutine, Update
  ✗ Never: Time.deltaTime, Camera, Input, Physics
  ✓ Communication: returns data, raises events
  ✓ Must be: deterministic, testable, server-reproducible
  ✓ Can be: ported to TypeScript for NestJS backend (same logic)

VIEW LAYER (MonoBehaviour):
  ✓ Uses: everything Unity (Transform, Animator, ParticleSystem, URP)
  ✓ Subscribes to: GameEvents
  ✗ Never: modifies game state directly
  ✗ Never: contains game logic
  ✓ Reads: HeroState, GridSystem (read-only queries)
  ✓ Calls: MatchManager.SubmitActions() (only entry point)
  ✓ Uses: GridHelper.GridToWorld() for 3D positioning

NETWORK LAYER:
  ✓ Translates: server messages → GameEvents, user actions → server messages
  ✗ Never: runs game logic
  ✗ Never: accesses View layer directly
  ✓ Uses: NetworkTypes (CommitMessage, RevealMessage, etc.)
```

## 3D / URP Conventions

```csharp
// Grid ↔ World conversion — ALWAYS use GridHelper, never hardcode
Vector3 worldPos = GridHelper.GridToWorld(gridPos);
Quaternion rotation = GridHelper.DirectionToRotation(facing);

// Camera — NEVER use Camera.main, always cache or serialize
[SerializeField] private Camera _gameCamera;

// Materials — use URP Lit shader for 3D objects
// Assign via SerializeField or ScriptableObject, never Resources.Load

// Animation — drive via Animator parameters, not direct clip control
_animator.SetTrigger("Shoot");
_animator.SetBool("IsMoving", true);

// VFX — pool all particle effects via VFXManager
// Never Instantiate/Destroy in hot paths
VFXManager.Instance.SpawnEffect(EffectType.ShootTrail, position, rotation);
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
        Debug.LogWarning("[HeroView3D] Received step result but views not initialized");
        return;
    }
}

// Network layer: handle disconnects gracefully
void OnDisconnected(string reason)
{
    Debug.LogWarning($"[Network] Disconnected: {reason}. Attempting reconnect...");
    GameEvents.NetworkDisconnected(reason);
}
```

## ScriptableObject Pattern

```csharp
[CreateAssetMenu(fileName = "NewHero", menuName = "TacticalDuelist/Hero Config")]
public class HeroConfig : ScriptableObject
{
    [Header("Identity")]
    public string heroId;
    public string displayName;
    public Sprite portrait;

    [Header("3D Assets")]
    public GameObject heroPrefab;
    public RuntimeAnimatorController animatorController;
    public GameObject specialVFXPrefab;

    [Header("Parameters")]
    [Range(3, 6)] public int steps = 4;
    [Range(2, 10)] public int range = 5;
    [Tooltip("Steps to wait between shots. 0 = no cooldown.")]
    [Range(0, 3)] public int cooldown = 1;
    [Range(0, 1)] public int armor = 0;
    [Range(1, 2)] public int speed = 1;

    [Header("Special")]
    public SpecialAbility specialAbility;
    [TextArea(2, 4)] public string specialDescription;
}
```

## Event Pattern

```csharp
// Define in GameEvents.cs (static class)
public static class GameEvents
{
    // Match lifecycle
    public static event Action<MatchFoundMessage> OnMatchFound;
    public static event Action<StepResult> OnStepResolved;
    public static event Action<RoundResult> OnRoundEnded;
    public static event Action<MatchResult> OnMatchEnded;

    // Network
    public static event Action<string> OnNetworkDisconnected;

    // Invoke with null-check
    public static void StepResolved(StepResult result)
        => OnStepResolved?.Invoke(result);

    public static void NetworkDisconnected(string reason)
        => OnNetworkDisconnected?.Invoke(reason);

    // Clear all — call on match end / scene unload to prevent leaks
    public static void ClearAll()
    {
        OnMatchFound = null;
        OnStepResolved = null;
        OnRoundEnded = null;
        OnMatchEnded = null;
        OnNetworkDisconnected = null;
    }
}

// Subscribe in View (MonoBehaviour) — ALWAYS OnEnable/OnDisable pair
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
System.Threading.Tasks.Task
System.IO.File
System.Net.Sockets

// ✓ ALWAYS — WebGL safe
IEnumerator + yield return         // coroutines for async
PlayerPrefs                        // local storage
UnityWebRequest                    // HTTP calls
// WebSocket via jslib plugin or Socket.IO JS interop

// ⚠ CAUTION
string.GetHashCode()  // NOT deterministic across platforms!
                      // Use SHA256 from System.Security.Cryptography

// ✓ Performance — critical for mobile WebGL at 60 FPS
// Cache everything. Avoid allocations in loops.
// Pre-allocate lists with known capacity.
private readonly List<Vector2Int> _rayBuffer = new(16);

public List<Vector2Int> CastRay(...)
{
    _rayBuffer.Clear();
    // ... fill buffer
    return _rayBuffer;
}

// Avoid LINQ in hot paths (Update, resolution loops)
// Use for/foreach instead of .Where().Select().ToList()

// Object pooling for VFX, projectiles, UI elements
// Never Instantiate/Destroy during gameplay
```

## WebGL Build Settings

```
IL2CPP Code Generation: OptimizeSize
Managed Stripping Level: High
Compression Format: Brotli
Texture Compression: ASTC (mobile-optimized)
Exception Support: None (production) / Full (debug)
WASM 2023: Enabled
LTO Optimization: Enabled (reduces disk size)
Data Caching: Enabled
Debug Symbols: Disabled (production)
```

## NestJS Backend Conventions (server/ directory)

```typescript
// Module pattern — one module per domain
@Module({
  imports: [PrismaModule],
  providers: [MatchService, MatchmakingService],
  controllers: [],
  exports: [MatchService],
})
export class MatchModule {}

// Service — injectable, contains business logic
@Injectable()
export class MatchService {
  constructor(private prisma: PrismaService) {}
}

// Gateway — WebSocket event handling
@WebSocketGateway({ cors: true })
export class MatchGateway {
  @SubscribeMessage('match:find')
  handleFindMatch(@ConnectedSocket() client: Socket) { }
}

// Naming: camelCase for files (match.service.ts, match.gateway.ts)
// Naming: PascalCase for classes (MatchService, MatchGateway)
// Guards: WsAuthGuard for authenticated WebSocket events
// DTOs: class-validator decorators for input validation
// Shared logic: ActionResolverService must produce identical output to C# version
```

## Platform Compilation Directives

```
Platform-specific code MUST be isolated in Scripts/Platform/ folder.
Game logic, UI, Gameplay, and Networking code NEVER use #if platform directives.

ALLOWED (in Platform/ only):
  #if UNITY_WEBGL && !UNITY_EDITOR
      platform = new WebGLPlatform();
  #elif UNITY_ANDROID && !UNITY_EDITOR
      platform = new AndroidPlatform();
  #elif UNITY_IOS && !UNITY_EDITOR
      platform = new IOSPlatform();
  #else
      platform = new EditorPlatform();
  #endif

FORBIDDEN (anywhere outside Platform/):
  #if UNITY_WEBGL
      DoSomethingWebGL();
  #else
      DoSomethingNative();
  #endif

INSTEAD — use abstraction:
  var transport = ServiceLocator.Get<IPlatformService>().Network.CreateWebSocket();
  transport.Connect(url);

Platform/ folder namespace: TacticalDuelist.Platform, TacticalDuelist.Platform.WebGL, etc.
Each platform class implements IPlatformService (see TECHNICAL-SPEC.md section 9).
PlatformBootstrap.cs is the ONLY MonoBehaviour in Platform/ — runs before GameBootstrap.
```

## Comments Policy

```csharp
// DO: explain WHY, not WHAT
// Mutual cancel prevents "both die" scenarios which feel unfair
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
public List<Vector2Int> CastRay(Vector2Int from, Direction dir, int range)

// DON'T: obvious comments, narration of what the code does
```

## Determinism Rules

```
The ActionResolver and all systems it calls MUST be deterministic:
  ✓ Same inputs → same outputs, always, on any platform
  ✗ No Random (use seeded System.Random if ever needed)
  ✗ No floating point math in game logic (use int / Vector2Int)
  ✗ No Dictionary iteration order dependency (use SortedDictionary or List)
  ✗ No string.GetHashCode() for gameplay-affecting hashes
  ✓ Use System.Security.Cryptography.SHA256 for commit-reveal
  ✓ C# logic must produce identical results to TypeScript port on server
```
