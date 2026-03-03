# Tactical Duelist — System Architecture

## Overview

Tactical Duelist uses a client-server architecture with strict separation of concerns.

- **Client (Unity 6):** 3D isometric presentation, input handling, animation playback. No authoritative game logic.
- **Server (NestJS):** Authoritative match resolution, matchmaking, anti-cheat, persistence.
- **Communication:** WebSocket (Socket.IO) for real-time bidirectional events.

All game logic is deterministic pure C# — identical code can run on both client (for prediction/replay) and server (for authoritative resolution).

### Target Platforms

| Platform | Build Target | Primary Use | Auth Method |
|----------|-------------|-------------|-------------|
| **WebGL** | Telegram Mini App | Primary launch platform | Telegram WebApp initData |
| **Android** | Google Play / APK | Mobile native | Telegram Login / Google Play Games |
| **iOS** | App Store / TestFlight | Mobile native | Telegram Login / Apple Game Center |
| **Web** | Standalone browser | Desktop browser | Telegram Login / Email |

**Principle:** Architecture is multi-platform from day one, but MVP builds and tests on WebGL (TMA) first. Platform-specific code is isolated behind `IPlatformService` interfaces, allowing seamless expansion to native mobile and web builds without touching game logic or view layer.

## High-Level Architecture

```
┌──────────────────────────────────────────────────────┐
│          UNITY 6 CLIENT (WebGL / Android / iOS)      │
│                                                      │
│  ┌──────────────────────────────────────────────┐    │
│  │            VIEW LAYER (MonoBehaviour)         │    │
│  │  GridView · HeroView3D · UIController        │    │
│  │  CameraController · AnimationController      │    │
│  │  AudioManager · VFXManager · InputHandler    │    │
│  └──────────────────┬───────────────────────────┘    │
│                     │ Events / Callbacks              │
│  ┌──────────────────▼───────────────────────────┐    │
│  │            GAME LAYER (Pure C#)              │    │
│  │  MatchManager · RoundManager                 │    │
│  │  ActionResolver · GridSystem                 │    │
│  │  DamageResolver · PickupSystem · ShrinkSystem│    │
│  │  HeroState · ActionQueue                     │    │
│  └──────────────────┬───────────────────────────┘    │
│                     │ ScriptableObjects               │
│  ┌──────────────────▼───────────────────────────┐    │
│  │            DATA LAYER (Config)               │    │
│  │  HeroConfig · MapConfig · GameSettings       │    │
│  └──────────────────────────────────────────────┘    │
│                                                      │
│  ┌──────────────────────────────────────────────┐    │
│  │            NETWORK LAYER                     │    │
│  │  SocketIOClient · MatchNetworkController     │    │
│  │  HashCommitment · MessageSerializer          │    │
│  └──────────────────┬───────────────────────────┘    │
│                     │                                │
│  ┌──────────────────▼───────────────────────────┐    │
│  │      PLATFORM ABSTRACTION LAYER              │    │
│  │  IPlatformService (interface)                │    │
│  │  ├── WebGLPlatform (TMA: jslib, PlayerPrefs) │    │
│  │  ├── AndroidPlatform (native sockets, files) │    │
│  │  └── IOSPlatform (native sockets, files)     │    │
│  │  IPlatformAuth · IPlatformStorage            │    │
│  │  IPlatformHaptics · IPlatformNotifications   │    │
│  └──────────────────┬───────────────────────────┘    │
└──────────────────────┼───────────────────────────────┘
                       │ WebSocket (Socket.IO)
┌──────────────────────▼───────────────────────────────┐
│                 NestJS SERVER                         │
│                                                      │
│  ┌──────────────────────────────────────────────┐    │
│  │            GATEWAY LAYER (WebSocket)          │    │
│  │  MatchGateway · LobbyGateway                 │    │
│  └──────────────────┬───────────────────────────┘    │
│                     │                                │
│  ┌──────────────────▼───────────────────────────┐    │
│  │            SERVICE LAYER (Business Logic)     │    │
│  │  MatchService · MatchmakingService           │    │
│  │  ActionResolverService · ReplayService       │    │
│  │  PlayerService · AuthService · RankService   │    │
│  └──────────────────┬───────────────────────────┘    │
│                     │ Prisma ORM                     │
│  ┌──────────────────▼───────────────────────────┐    │
│  │            DATA LAYER                        │    │
│  │  PostgreSQL (profiles, matches, replays)     │    │
│  │  Redis (sessions, matchmaking queue, cache)  │    │
│  └──────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────┘
```

## Client Architecture (Unity 6)

### Rendering

- **Pipeline:** Universal Render Pipeline (URP)
- **Perspective:** 3D isometric with free camera angle (Brawl Stars style)
- **Camera:** Perspective projection, dynamic follow, configurable tilt/rotation
- **Assets:** 3D models for heroes and environment, URP Lit shaders
- **Lighting:** Baked lightmaps + single realtime directional light
- **Target:** 60 FPS on mobile WebGL browsers

### System Dependency Graph

```
MatchManager (orchestrator)
  ├── RoundManager
  │     ├── ActionResolver (step-by-step resolution)
  │     │     ├── GridSystem (positions, walls, line-of-sight)
  │     │     ├── DamageResolver (hit detection, armor, mutual cancel)
  │     │     └── PickupSystem (items on map)
  │     └── ShrinkSystem (map shrinking between rounds)
  ├── HeroState (per-player runtime state)
  └── MatchResult (outcome data)

MatchNetworkController (client-server bridge)
  ├── SocketIOClient (connection management)
  ├── HashCommitment (SHA-256 hash + nonce)
  └── MessageSerializer (JSON ↔ typed messages)
```

### View Layer (MonoBehaviour)

These components handle Unity-specific rendering, input, and 3D presentation.

| Component | Responsibility |
|-----------|---------------|
| `GridView` | 3D grid rendering, tile highlighting, fog of war |
| `HeroView3D` | 3D hero model, Animator Controller, facing rotation |
| `CameraController` | Perspective camera, dynamic follow, zoom, free-angle rotation |
| `UIController` | Action programming UI, timers, status bars |
| `AnimationController` | Step-by-step animation playback from StepResult data |
| `VFXManager` | Particle effects: shots, hits, explosions, ability VFX |
| `AudioManager` | Sound effects, music, UI sounds |
| `InputHandler` | Touch/mouse input → action queue during planning |

### Game Layer (Pure C#)

Zero Unity dependencies (except Vector2Int). Deterministic. Server-reproducible.

| System | Responsibility |
|--------|---------------|
| `MatchManager` | Match lifecycle orchestration |
| `RoundManager` | Round lifecycle, step iteration |
| `ActionResolver` | Per-step 3-phase resolution (Move → Combat → Damage) |
| `GridSystem` | Grid state, wall data, line-of-sight raycasting |
| `DamageResolver` | Hit detection, armor mechanics, mutual cancel |
| `PickupSystem` | Item spawn positions, collection logic |
| `ShrinkSystem` | Zone danger per round |
| `HeroState` | Per-player runtime: position, facing, cooldowns, armor |
| `ActionQueue` | Player's programmed actions for the round |

## Server Architecture (NestJS)

### Module Structure

```
src/
├── app.module.ts                 # Root module
├── auth/
│   ├── auth.module.ts
│   ├── auth.service.ts           # Telegram WebApp validation, JWT
│   └── auth.guard.ts             # WsAuthGuard for WebSocket
├── match/
│   ├── match.module.ts
│   ├── match.gateway.ts          # @WebSocketGateway — match WS events
│   ├── match.service.ts          # Match lifecycle, hash verification
│   ├── matchmaking.service.ts    # Queue, rating-based pairing
│   └── action-resolver.service.ts # Deterministic resolution (mirrors client)
├── player/
│   ├── player.module.ts
│   ├── player.service.ts         # Profile CRUD, mastery, rank
│   └── player.controller.ts     # REST endpoints for profile
├── replay/
│   ├── replay.module.ts
│   └── replay.service.ts        # Replay storage and retrieval
├── shared/
│   ├── models/                   # Shared DTOs, enums
│   ├── guards/                   # Auth guards
│   └── pipes/                    # Validation pipes
└── prisma/
    ├── prisma.module.ts
    └── prisma.service.ts         # Prisma client provider
```

### Key NestJS Patterns

- **Dependency Injection:** All services injected via constructors, defined as `@Injectable()`
- **WebSocket Gateway:** `@WebSocketGateway()` with `@SubscribeMessage()` decorators for match events
- **Guards:** `WsAuthGuard` validates Telegram initData on every WebSocket connection
- **Modules:** Each feature domain (match, player, replay) is a self-contained module
- **Prisma:** Shared `PrismaService` injected into all data-access services

## Communication Protocol (WebSocket Events)

### Client → Server

| Event | Payload | Description |
|-------|---------|-------------|
| `match:find` | `{ heroId, rankTier, stakeLevel? }` | Request matchmaking |
| `match:cancel` | `{}` | Cancel matchmaking |
| `round:commit` | `{ hash: string }` | Commit SHA-256(actions + nonce) |
| `round:reveal` | `{ actions: ActionType[], nonce: string }` | Reveal actual actions |
| `match:surrender` | `{}` | Forfeit match |

### Server → Client

| Event | Payload | Description |
|-------|---------|-------------|
| `match:found` | `{ matchId, opponent, map, spawns }` | Match paired |
| `match:error` | `{ code, message }` | Matchmaking/match error |
| `round:start` | `{ roundNumber, timeLimit, shrinkZone? }` | Round begins |
| `round:both-committed` | `{}` | Both players committed, request reveal |
| `round:results` | `{ steps: StepResult[] }` | Full round resolution |
| `round:end` | `{ result, replay }` | Round outcome |
| `match:end` | `{ winner, rewards, replay }` | Match outcome |

## Data Flow Per Match

```
1. MATCHMAKING
   Client: player selects hero, taps PLAY
   Client → WS: 'match:find' { heroId, rankTier }
   MatchmakingService: adds to Redis queue, pairs by rank proximity
   WS → Both Clients: 'match:found' { matchId, opponent, map, spawns }

2. PLANNING PHASE (per round)
   Client: InputHandler collects actions → ActionQueue (List<ActionType>)
   Client: computes hash = SHA256(JSON(actions) + nonce)
   Client → WS: 'round:commit' { hash }
   MatchService: waits for both commits OR timeout (30s)
   WS → Both Clients: 'round:both-committed'
   Client → WS: 'round:reveal' { actions, nonce }
   MatchService: validates SHA256(JSON(actions) + nonce) == committed hash

3. EXECUTION PHASE (per round)
   ActionResolverService.ResolveRound(p1Actions, p2Actions, gridState)
   For each step (0 to maxSteps-1):
     Phase 1 — MOVEMENT: both move/turn simultaneously
       → Collision detection (same tile → both stay)
       → Pickup collection
     Phase 2 — COMBAT: both shoot/special simultaneously
       → Raycast from current position in facing direction
       → Range-limited, wall-blocked
     Phase 3 — DAMAGE:
       → One hit + no armor → ELIMINATED
       → One hit + armor → Armor destroyed
       → Both hit → MUTUAL CANCEL (both nullified)
   WS → Both Clients: 'round:results' { steps: StepResult[] }
   Client: AnimationController plays back each step with 3D animations

4. ROUND END
   If elimination → MatchService.EndMatch()
   If no elimination → ShrinkSystem.Shrink() → next round
   WS → Both Clients: 'round:end' { result, shrinkData }

5. MATCH END
   PlayerService: updates Mastery, Rank, BattlePass XP
   ReplayService: saves full replay to PostgreSQL
   If staked match → TON smart contract settlement
   WS → Both Clients: 'match:end' { winner, rewards, replayId }
```

## Event System (Client-Side)

All communication from Model to View goes through events.
View NEVER calls into Model directly except through defined public APIs.

```csharp
public static class GameEvents
{
    // Match lifecycle
    public static event Action<MatchStartData> OnMatchStarted;
    public static event Action<MatchResult> OnMatchEnded;

    // Round lifecycle
    public static event Action<int> OnRoundStarted;
    public static event Action<RoundResult> OnRoundEnded;
    public static event Action OnPlanningPhaseStarted;
    public static event Action OnExecutionPhaseStarted;

    // Step resolution (View uses these for 3D animation playback)
    public static event Action<StepResult> OnStepResolved;
    public static event Action<MoveResult> OnMovementResolved;
    public static event Action<CombatResult> OnCombatResolved;
    public static event Action<DamageResult> OnDamageResolved;

    // Hero state changes
    public static event Action<int, bool> OnArmorChanged;
    public static event Action<int> OnHeroEliminated;
    public static event Action<int, PickupType> OnPickupCollected;

    // Map changes
    public static event Action<List<Vector2Int>> OnMapShrunk;

    // Network events (from SocketIOClient)
    public static event Action OnConnected;
    public static event Action<string> OnDisconnected;
    public static event Action<MatchFoundData> OnMatchFound;
    public static event Action OnBothCommitted;
    public static event Action<RoundResultsData> OnRoundResultsReceived;
}
```

## Platform Abstraction Layer

The game targets WebGL (Telegram Mini App), Android, iOS, and standalone web.
Platform-specific features are isolated behind interfaces so that Core, Game, View, and
Network layers remain 100% platform-agnostic.

### Core Interfaces

```csharp
/// <summary>
/// Root interface. One implementation per platform, injected at startup.
/// All platform-specific behavior is accessed through this service.
/// </summary>
public interface IPlatformService
{
    PlatformType CurrentPlatform { get; }
    IPlatformAuth Auth { get; }
    IPlatformStorage Storage { get; }
    IPlatformNetwork Network { get; }
    IPlatformHaptics Haptics { get; }
    IPlatformNotifications Notifications { get; }
    IPlatformDeepLinks DeepLinks { get; }
    IPlatformShare Share { get; }

    /// <summary>Called once at app startup to initialize SDK/plugins.</summary>
    void Initialize();
}

public enum PlatformType { WebGL, Android, iOS, DesktopWeb }

public interface IPlatformAuth
{
    /// <summary>Returns auth token (JWT). Source differs per platform.</summary>
    UniTask<string> Authenticate();
    string GetDisplayName();
    string GetAvatarUrl();
}

public interface IPlatformStorage
{
    void Save(string key, string value);
    string Load(string key);
    void Delete(string key);
}

public interface IPlatformNetwork
{
    /// <summary>Creates a WebSocket connection suited to the platform.</summary>
    IWebSocketTransport CreateWebSocket(string url);
}

public interface IPlatformHaptics
{
    void LightImpact();
    void MediumImpact();
    void HeavyImpact();
    bool IsSupported { get; }
}

public interface IPlatformNotifications
{
    void ScheduleLocal(string title, string body, TimeSpan delay);
    void CancelAll();
}

public interface IPlatformDeepLinks
{
    event Action<string> OnDeepLinkReceived;
    void OpenUrl(string url);
}

public interface IPlatformShare
{
    void ShareReplay(string replayId, string message);
    void InviteFriend(string matchId);
}
```

### Platform Implementations

| Interface | WebGL (TMA) | Android | iOS |
|-----------|-------------|---------|-----|
| **Auth** | `TelegramWebApp.initData` via jslib | Telegram Login Widget / Google Play Games | Telegram Login Widget / Apple Game Center |
| **Storage** | `PlayerPrefs` (IndexedDB) | `PlayerPrefs` (SharedPreferences) | `PlayerPrefs` (NSUserDefaults) |
| **Network** | jslib WebSocket interop | Native .NET `ClientWebSocket` | Native .NET `ClientWebSocket` |
| **Haptics** | Telegram `HapticFeedback` API | Android `Vibrator` via plugin | `UIImpactFeedbackGenerator` via plugin |
| **Notifications** | Not supported (TMA limitation) | Firebase Cloud Messaging | APNs |
| **DeepLinks** | `tg://` protocol | Android App Links | Universal Links |
| **Share** | Telegram `shareUrl` API | Android native share intent | iOS native share sheet |

### Platform Selection (Startup)

```csharp
public class PlatformBootstrap : MonoBehaviour
{
    [SerializeField] private GameBootstrap _gameBootstrap;

    private void Awake()
    {
        IPlatformService platform;

        #if UNITY_WEBGL && !UNITY_EDITOR
            platform = new WebGLPlatform();
        #elif UNITY_ANDROID && !UNITY_EDITOR
            platform = new AndroidPlatform();
        #elif UNITY_IOS && !UNITY_EDITOR
            platform = new IOSPlatform();
        #else
            platform = new EditorPlatform(); // dev/testing fallback
        #endif

        platform.Initialize();
        ServiceLocator.Register<IPlatformService>(platform);

        _gameBootstrap.Run(platform);
    }
}
```

### Dependency Direction

```
View Layer  ──→  Game Layer  ──→  Core (Pure C#)
     │                │
     ▼                ▼
Network Layer  ──→  IPlatformService  ←── PlatformBootstrap
                         │
              ┌──────────┼──────────┐
              ▼          ▼          ▼
         WebGLPlatform  Android   iOS
```

Platform implementations depend on `IPlatformService` interfaces (defined in Core).
No game code ever depends on a concrete platform class.

### Platform-Specific Build Notes

| Concern | WebGL | Android / iOS |
|---------|-------|---------------|
| **Threading** | No threads — coroutines only | Threads allowed, but game logic stays single-threaded |
| **File I/O** | Forbidden — use `IPlatformStorage` | Allowed — but still use `IPlatformStorage` for consistency |
| **WebSocket** | jslib interop (browser API) | Native `System.Net.WebSockets.ClientWebSocket` |
| **Bundle size** | Critical (< 15 MB target) | Less critical, but still optimize |
| **Texture format** | ASTC (mobile browsers) | ASTC (Android), ASTC (iOS) |
| **Audio** | Web Audio API (limited) | Full native audio |
| **Build system** | IL2CPP, Brotli, WASM 2023 | IL2CPP, ARM64 |

## Key Design Decisions

1. **Pure C# game logic**: ActionResolver, GridSystem, DamageResolver contain
   ZERO Unity dependencies. They use Vector2Int (trivially replaceable).
   Identical code runs on NestJS server (TypeScript port) for authoritative resolution.

2. **Deterministic resolution**: Given same inputs, always same outputs.
   No Random, no Time.deltaTime, no floating point in game logic.
   Integer math only for positions and ranges.

3. **ScriptableObjects for data**: All hero stats, map layouts, pickup effects
   are ScriptableObjects on client. Server mirrors this as JSON config.

4. **Event-driven View**: View subscribes to events, never polls.
   Model has no knowledge of View's existence.

5. **Replay-friendly**: Every StepResult contains complete state snapshot.
   Replay = feeding StepResults to View sequentially.

6. **Server-authoritative**: Client sends actions, server resolves.
   Client only displays results. Hash commitment prevents cheating.

7. **3D presentation, 2D logic**: Game logic operates on a 2D integer grid.
   View layer translates grid positions to 3D world coordinates
   using an isometric projection helper.

8. **WebSocket-first**: All real-time communication uses Socket.IO.
   REST endpoints only for non-real-time data (profiles, leaderboards, replays).

9. **Multi-platform from day one**: All platform-specific behavior is behind
   `IPlatformService` interfaces. Game logic, view layer, and networking code
   never reference concrete platform implementations. Adding a new platform means
   writing one class that implements `IPlatformService` — no changes to game code.
