# Tactical Duelist — Implementation Tasks

## How to use this file

Each task is a self-contained unit of work. Reference docs/ and .cursorrules
for full context when implementing. Phases must be completed in order;
tasks within a phase can be parallelized unless dependencies say otherwise.

Format: `[STATUS] TASK-ID: Description`
Status: `[ ]` todo, `[→]` in progress, `[✓]` done, `[✗]` blocked

---

## Phase 1: Project Setup & Data Foundation

```
[✓] T-001: Create enums file Scripts/Core/Models/Enums.cs
     Contains: Direction, ActionType, GamePhase, TileType, PickupType,
     SpecialAbility, MatchResult, RoundResult
     Reference: docs/TECHNICAL-SPEC.md section 1

[✓] T-000: Create HeroConfig.cs in Scripts/Core/Config/
     ScriptableObject with all hero parameters + 3D asset references
     Fields: heroPrefab (GameObject), animatorController, portrait (Sprite),
     specialVFXPrefab (GameObject)
     Reference: docs/TECHNICAL-SPEC.md section 2 (HeroConfig)

[✓] T-000: Create MapConfig.cs in Scripts/Core/Config/
     ScriptableObject with grid data, spawns, pickup spawns
     Fields: mapPrefab (GameObject), floorMaterial, wallMaterial, dangerZoneMaterial
     Reference: docs/TECHNICAL-SPEC.md section 2 (MapConfig)

[✓] T-000: Create HeroState.cs in Scripts/Core/Models/
     Runtime per-player state: position, facing, armor, cooldown, buffs
     Reference: docs/TECHNICAL-SPEC.md section 2 (HeroState)

[✓] T-000: Create StepResult.cs + SpecialResult.cs in Scripts/Core/Models/
     Data classes for step resolution output
     Reference: docs/TECHNICAL-SPEC.md section 2 (StepResult)

[✓] T-000: Create GameEvents.cs in Scripts/Core/Systems/
     Static event hub for Model→View and Network events
     Includes: match lifecycle, round lifecycle, step resolution,
     hero state, map changes, network events
     Reference: docs/ARCHITECTURE.md (Event System)

[✓] T-000: Create GridHelper.cs in Scripts/Core/Utils/
     GridToWorld, WorldToGrid, DirectionToRotation conversions
     Reference: docs/TECHNICAL-SPEC.md section 3 (Grid-to-World)

[✓] T-000: Create 4 HeroConfig assets in ScriptableObjects/Heroes/
     Hero_Archer.asset, Hero_Tank.asset, Hero_Shadow.asset, Hero_Scout.asset
     Use parameter values from docs/HEROES.md
     Leave 3D prefab fields empty (assigned in Phase 5)

[✓] T-000: Create NetworkTypes.cs in Scripts/Core/Models/
     CommitMessage, RevealMessage, MatchFoundMessage,
     RoundResultsMessage, MatchEndMessage
     Reference: docs/TECHNICAL-SPEC.md section 8
```

## Phase 2: Grid System

```
[✓] T-010: Create GridSystem.cs in Scripts/Core/Systems/
     Constructor from MapConfig. TileType[,] grid storage.
     Methods: GetTile, IsWalkable, IsInBounds, HasPickup, GetPickup
     Reference: docs/TECHNICAL-SPEC.md section 3

[✓] T-011: Add direction helpers to GridSystem
     Static methods: DirectionToVector, TurnLeft, TurnRight, TurnAround
     DirectionToVector: Up=(0,1), Down=(0,-1), Left=(-1,0), Right=(1,0)

[✓] T-012: Implement GridSystem.GetMoveTarget
     Calculate target tile for Move action. Speed = tiles moved.
     Must check: wall collision (stop before wall), bounds check,
     Speed 2 heroes check intermediate tile.

[✓] T-013: Implement GridSystem.CastRay
     Ray from position in direction for range tiles.
     Returns List<Vector2Int> of tiles ray passes through.
     Stops at first wall (does not include wall tile).
     Does not include starting position.

[✓] T-014: Create MapConfig asset for MVP map
     ScriptableObjects/Maps/Map_Arena01.asset
     10x10 grid. 6-8 wall tiles symmetrically placed.
     P1 spawn: (1,1) facing Up. P2 spawn: (8,8) facing Down.
     No pickups for MVP.
```

## Phase 3: Action Resolution

```
[✓] T-020: Create ActionResolver.cs in Scripts/Core/Systems/
     ResolveStep(stepIndex, p1Action, p2Action) → StepResult
     Three phases: Movement → Combat → Damage
     Must be 100% deterministic (server will mirror this logic)
     Reference: docs/TECHNICAL-SPEC.md section 4

[✓] T-021: Implement movement resolution in ActionResolver
     Move: apply GridSystem.GetMoveTarget
     Turn: apply GridSystem.TurnLeft/Right/Around
     Collision: both to same tile = both stay at original
     Swap collision: moving into each other = both stay

[✓] T-022: Implement combat resolution in ActionResolver
     Shoot: check cooldown, cast ray, check if opponent in ray
     Set P1Fired, P2Fired, P1Hit, P2Hit on StepResult

[✓] T-023: Implement damage resolution in ActionResolver
     Mutual cancel: both hit = nobody takes damage
     Single hit + armor = armor broken
     Single hit + no armor = eliminated

[✓] T-024: Implement cooldown tracking in ActionResolver
     After Shoot: hero.CooldownRemaining = hero.Config.cooldown
     Each step: CooldownRemaining = max(0, CooldownRemaining - 1)
     Cannot shoot if CooldownRemaining > 0

[✓] T-025: Create ActionValidator.cs in Scripts/Core/Systems/
     ValidateActionList: checks step count, cooldown gaps, special cap
     Shared validation rules for client-side preview and server auth
     Reference: docs/TECHNICAL-SPEC.md section 6
```

## Phase 4: Match Management

```
[✓] T-030: Create MatchManager.cs in Scripts/Core/Systems/
     Orchestrates full match: hero select → rounds → result
     Methods: StartMatch, SubmitActions, ExecuteRound, EndMatch
     Fires GameEvents for state transitions
     Reference: docs/TECHNICAL-SPEC.md section 5

[✓] T-031: Implement action validation in MatchManager
     Calls ActionValidator to validate player action lists
     Pads short action lists with Wait

[✓] T-032: Create ShrinkSystem.cs in Scripts/Core/Systems/
     ApplyShrink(roundIndex): expands danger zone per MapConfig.shrinkRings
     Destroys walls in danger zone
     End-of-round check: eliminate heroes in danger zone
     Reference: docs/TECHNICAL-SPEC.md section 7
```

## Phase 5: 3D View Layer (Unity URP)

```
[✓] T-040: Create GridView.cs in Scripts/Gameplay/
     MonoBehaviour. Renders 3D grid from GridSystem.
     Each tile = 3D plane/quad with URP material.
     Walls = 3D cube meshes. DangerZone = VFX overlay.
     Scale: 1 Unity unit = 1 tile.
     Method: RenderGrid(GridSystem grid)

[✓] T-041: Create HeroView3D.cs in Scripts/Gameplay/
     MonoBehaviour on hero prefab root.
     Controls 3D model position, rotation (smooth lerp), animations.
     Methods: SetGridPosition(Vector2Int), SetFacing(Direction),
     AnimateMove(from, to, duration), AnimateShoot(direction, range),
     PlayHitVFX(), PlayArmorBreakVFX(), PlayEliminationVFX()
     Interfaces with Animator component for state transitions.

[✓] T-042: Create AnimationController.cs in Scripts/Gameplay/
     Manages hero animation states: Idle, Move, Shoot, Special, Hit, Death
     Subscribes to StepResult events to trigger correct animation clips
     Coordinates animation timing with ExecutionController

[✓] T-043: Create CameraController.cs in Scripts/Gameplay/
     3D perspective camera in isometric-like angle (Brawl Stars style).
     Rotation: ~45° X, ~45° Y for isometric look.
     Projection: Perspective with narrow FOV (~30-40) for pseudo-ortho feel.
     Follows action: smooth track between hero positions.
     Auto-fit grid with margin.
     Reference: docs/ARCHITECTURE.md (Rendering section)

[✓] T-044: Create VFXManager.cs in Scripts/Gameplay/
     Object pooling for VFX: shoot trails, hit impacts, armor break,
     elimination, danger zone fire.
     Uses URP-compatible particle systems / Shader Graph effects.

[✓] T-045: Create ExecutionController.cs in Scripts/Gameplay/
     MonoBehaviour. Plays back List<StepResult> with 3D animations.
     Subscribes to GameEvents.OnStepResolved.
     Coroutine-based: play step → wait for animations → next step.
     Support speed: 1x (0.8s/step), 2x (0.4s/step), pause.
     Coordinates HeroView3D, VFXManager, CameraController.

[✓] T-046: Create placeholder 3D hero prefabs (4 heroes for MVP)
     Simple capsule + colored material + direction arrow.
     Animator with Idle/Move/Shoot states (can be empty clips).
     Assigned to HeroConfig assets from T-008.
```

## Phase 6: UI

```
[✓] T-050: Create HeroSelectScreen.cs in Scripts/UI/
     Shows 4 hero cards: portrait, name, stats summary.
     Online: player selects hero, server confirms match.
     Offline/test: P1 picks → P2 picks.
     Emits event with selected HeroConfigs.

[✓] T-051: Create PlanningScreen.cs in Scripts/UI/
     Grid mini-map preview + action queue.
     Action slots = hero.Steps count.
     Buttons: Move, TurnLeft, TurnRight, TurnAround, Shoot, Wait.
     Shoot button grayed when on cooldown in sequence.
     Undo last action button. Confirm → sends commit hash to server.
     Timer: 30s round 1, 20s later. Auto-submit on timeout.

[✓] T-052: Create ResultScreen.cs in Scripts/UI/
     Shows match result: winner portrait + name, "VICTORY" / "DRAW".
     Rank change, XP gained.
     Buttons: Rematch, New Match, Main Menu.

[✓] T-053: Create HUD.cs in Scripts/UI/
     In-game overlay during execution:
     Round counter, step counter, hero health indicators (alive/armor/dead),
     playback speed toggle, pause button.
```

## Phase 7: Network Layer (Client-Side)

```
[x] T-060: Create SocketIOClient.cs in Scripts/Networking/ — COMPLETED
     WebGL-compatible WebSocket client (Socket.IO protocol).
     Connect, Disconnect, Emit, On, Off.
     Auto-reconnect with exponential backoff.
     Reference: docs/ARCHITECTURE.md (Network Layer)

[x] T-061: Create MatchNetworkController.cs in Scripts/Networking/ — COMPLETED
     Sends: match:find, round:commit (hash), round:reveal (actions+nonce)
     Receives: match:found, round:start, round:results, match:end
     Translates server messages → GameEvents
     Reference: docs/ARCHITECTURE.md (Communication Protocol)

[x] T-062: Create HashUtil.cs in Scripts/Core/Utils/ — COMPLETED
     ComputeActionHash(ActionType[] actions, string nonce) → string
     SHA256(JSON(actions) + nonce) for commit-reveal scheme
     Must match server-side implementation exactly
```

## Phase 8: Integration & Game Flow

```
[x] T-070: Create GameManager.cs in Scripts/Gameplay/
     MonoBehaviour (scene entry point). Wires all systems together.
     Manages screen flow: MainMenu → Matchmaking → HeroSelect →
     Planning ↔ Execution → Result.
     Holds references to MatchManager, NetworkController, all UI screens.
     Also fixed ExecutionController: replaced missing GamePhase.RoundEnd
     with OnPlaybackComplete event for clean decoupling.

[x] T-071: Wire offline test flow
     HeroSelect → Planning (P1) → Planning (P2) → Execute → Result
     No networking. Direct local MatchManager calls.
     Implemented as GameManager._offlineMode = true path.

[x] T-072: Wire online match flow
     Matchmaking → HeroSelect → Planning → Commit → Reveal → Results → ...
     Uses MatchNetworkController for all server communication.
     Implemented as GameManager._offlineMode = false path with
     commit-reveal scheme via HashUtil.

[x] T-073: Create MatchScene setup
     Unity scene with: Camera, Grid parent, UI Canvas, GameManager,
     Event System. All references wired in inspector.
     Setup guide: docs/MATCH-SCENE-SETUP.md
```

## Phase 9: Testing & Polish

```
[✓] T-080: Create test scene with hardcoded match
     Bypass UI: hardcode 2 heroes, hardcode actions, run ExecuteRound.
     Visual verification that resolution works correctly in 3D.
     Files: Scripts/Testing/QuickMatchTest.cs

[✓] T-081: Unit tests for ActionResolver
     Test cases: basic movement, collision, shooting, mutual cancel,
     armor break, elimination, cooldown enforcement.
     Use Unity Test Framework (Edit Mode tests).
     Files: Tests/EditMode/ActionResolverTests.cs (25 tests)

[✓] T-082: Unit tests for GridSystem
     Test cases: CastRay (open, wall-blocked), GetMoveTarget (open,
     wall-blocked, speed 2), IsWalkable, bounds checking, direction helpers,
     danger zone, grid modifications.
     Files: Tests/EditMode/GridSystemTests.cs (28 tests)

[ ] T-083: Playtest session #1
     Two humans play 5+ matches (offline mode).
     Record: what's fun, what's confusing, what's broken.
     Known issues list → prioritize fixes.

Infrastructure:
[✓] Assembly definitions: TacticalDuelist.asmdef, TacticalDuelist.Tests.EditMode.asmdef
```

## Phase 10: NestJS Backend Foundation

```
[✓] T-090: Initialize NestJS project (server/ directory) ✅
     NestJS with TypeScript, Socket.IO adapter, Prisma ORM
     Folder structure: src/auth, src/match, src/player, src/replay, src/prisma
     Files: server/package.json, tsconfig.json, nest-cli.json, .env.example,
            src/main.ts, src/app.module.ts
     Reference: docs/ARCHITECTURE.md (Server Architecture)

[✓] T-091: Create Prisma schema ✅
     Models: Player, HeroMastery, Match, Round, Replay
     Enums: MatchStatus, MatchOutcome, RoundOutcome
     Relations: Player → Match (many-to-many), Match → Round (1:N),
     Match → Replay (1:1), Player → HeroMastery (1:N)
     Files: server/prisma/schema.prisma

[✓] T-092: Create MatchGateway (WebSocket) ✅
     @WebSocketGateway with Socket.IO adapter
     Events: match:find, match:cancel, round:commit, round:reveal, match:surrender
     WsAuthGuard for JWT validation, ValidationPipe for DTOs
     Files: server/src/match/match.gateway.ts

[✓] T-093: Create MatchmakingService ✅
     ELO-based matching: ±200 rating window, expands by 50 every 5s
     In-memory queue for MVP (Redis upgrade path documented)
     Files: server/src/match/matchmaking.service.ts

[✓] T-094: Create MatchService ✅
     Manages active match state (in-memory maps)
     Commit-reveal with SHA-256 hash verification
     Round resolution → DB persistence → ELO updates → replay saving
     Files: server/src/match/match.service.ts

[✓] T-095: Create ActionResolverService (TypeScript) ✅
     Deterministic port of C# ActionResolver + GridSystem
     3-phase resolution: Movement → Combat → Damage (identical to C#)
     Files: server/src/match/action-resolver.service.ts,
            server/src/match/grid-system.ts
     Shared types: server/src/shared/models/enums.ts, game-types.ts, dto.ts

Infrastructure created:
     [✓] AuthModule: auth.service.ts, auth.controller.ts, ws-auth.guard.ts,
          guards/jwt-auth.guard.ts
     [✓] PlayerModule: player.service.ts, player.controller.ts
     [✓] ReplayModule: replay.service.ts
     [✓] SharedModule: redis.service.ts (ioredis)
     [✓] PrismaModule: prisma.service.ts (global)
     [✓] TypeScript compiles with 0 errors
```

---

## Phase 11: Platform Abstraction Layer

> Architecture is multi-platform from day one. MVP implements WebGL only,
> but all platform-specific code goes through IPlatformService interfaces.
> See docs/ARCHITECTURE.md → Platform Abstraction Layer.

```
[x] T-100: Create PlatformType enum and IPlatformService interface ✅
     Scripts/Platform/IPlatformService.cs
     Includes: PlatformType enum (WebGL, Android, iOS, DesktopWeb)
     Sub-interfaces: IPlatformAuth, IPlatformStorage, IPlatformNetwork,
     IPlatformHaptics, IPlatformNotifications, IPlatformDeepLinks, IPlatformShare
     Reference: docs/TECHNICAL-SPEC.md section 9, docs/ARCHITECTURE.md

[x] T-101: Create ServiceLocator.cs in Scripts/Platform/ ✅
     Minimal service locator for platform services only
     Register<T>, Get<T>, TryGet<T>, Clear methods
     NOT a general-purpose DI — only for IPlatformService tree

[x] T-102: Create WebGLPlatform.cs in Scripts/Platform/WebGL/ ✅
     Implements IPlatformService for WebGL (Telegram Mini App)
     WebGLAuth: Telegram initData via jslib DllImport
     WebGLStorage: PlayerPrefs wrapper (IndexedDB on WebGL)
     WebGLNetwork: jslib WebSocket interop (stub)
     WebGLHaptics: Telegram HapticFeedback bridge via DllImport

[x] T-103: Create EditorPlatform.cs in Scripts/Platform/Editor/ ✅
     Mock implementation for Unity Editor testing
     EditorAuth: returns mock token
     EditorStorage: uses PlayerPrefs
     EditorNetwork: mock WebSocket with Debug.Log
     All haptics/notifications/deeplinks = Debug.Log stubs

[x] T-104: Create PlatformBootstrap.cs in Scripts/Platform/ ✅
     MonoBehaviour [DefaultExecutionOrder(-1000)]
     Uses #if UNITY_WEBGL, #if UNITY_ANDROID, etc. to select implementation.
     Registers IPlatformService + all sub-services via ServiceLocator.
     DontDestroyOnLoad.

[x] T-105: Create IWebSocketTransport.cs in Scripts/Platform/ ✅
     Interface for WebSocket transport (platform-agnostic)
     Connect (UniTask), Send, OnMessage, OnError, OnDisconnected, Disconnect
     WebGL: jslib interop. Native: ClientWebSocket.
     Used by SocketIOClient instead of direct WebSocket calls.
```

## Phase 12: Future Platform Implementations (Post-MVP)

```
[ ] T-110: Create AndroidPlatform.cs in Scripts/Platform/Android/
     Implements IPlatformService for Android
     Auth: Telegram Login Widget or Google Play Games
     Storage: PlayerPrefs (SharedPreferences)
     Network: native ClientWebSocket
     Haptics: Android Vibrator API via AndroidJavaObject

[ ] T-111: Create IOSPlatform.cs in Scripts/Platform/iOS/
     Implements IPlatformService for iOS
     Auth: Telegram Login Widget or Apple Game Center
     Storage: PlayerPrefs (NSUserDefaults)
     Network: native ClientWebSocket
     Haptics: UIImpactFeedbackGenerator via native plugin

[ ] T-112: Platform-specific build configurations
     Android: IL2CPP ARM64, ASTC textures, Gradle build
     iOS: IL2CPP ARM64, ASTC textures, Xcode project
     WebGL: see Phase 8 WebGL build settings
```

---

## Task Dependencies

```
Phase 1: T-001 → T-002..T-009 (enums first, then everything else)
Phase 2: T-003 → T-010; T-010 → T-011..T-014
Phase 3: T-004 + T-010 → T-020; T-020 → T-021..T-025
Phase 4: T-020 → T-030; T-010 → T-032
Phase 5: T-010 → T-040; T-004 → T-041; T-005 → T-045; T-008 → T-046
Phase 6: T-030 → T-050..T-053
Phase 7: (independent, can start after Phase 1)
Phase 8: T-030 + T-040..T-053 + T-060..T-062 → T-070..T-073
Phase 9: T-070 → T-080..T-083
Phase 10: (independent, can start after Phase 3 for shared logic)
Phase 11: T-001 → T-100..T-101; T-100 → T-102..T-105 (can start after Phase 1)
          T-105 → T-070 (PlatformBootstrap must exist before GameManager wiring)
          T-105 → T-060 (SocketIOClient uses IWebSocketTransport from T-105)
Phase 12: Post-MVP. T-100 → T-110..T-112

Critical path: T-001 → T-010 → T-020 → T-030 → T-070
Platform path:  T-001 → T-100 → T-102 + T-103 → T-104 → T-105
```
