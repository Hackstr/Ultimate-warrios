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
     Folder structure: src/auth, src/match, src/player, src/prisma
     Files: server/package.json, tsconfig.json, nest-cli.json, .env.example,
            src/main.ts, src/app.module.ts
     Reference: docs/ARCHITECTURE.md (Server Architecture)

[✓] T-091: Create Prisma schema ✅
     Models: Player, HeroMastery, Match, Round
     Enums: MatchStatus, MatchOutcome, RoundOutcome
     Relations: Player → Match (many-to-many), Match → Round (1:N),
     Player → HeroMastery (1:N)
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
     Round resolution → DB persistence → ELO updates
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

## Phase 12: Special Abilities — All 12 Heroes

> Core differentiator of the game. Each hero's special is usable once per round
> via ActionType.Special. Architecture is ready: enum, HeroConfig.specialAbility,
> GridSystem ray variants, SpecialResult class, UI button — all exist.
> Missing piece: ActionResolver doesn't handle ActionType.Special.

```
[ ] T-120: Add ResolveSpecial() to ActionResolver.cs
     Central dispatch: switch on hero.Config.specialAbility
     Called between Movement and Combat phases in ResolveStep()
     Populates StepResult.P1Special / P2Special with SpecialResult data
     Sets hero.SpecialUsedThisRound = true
     Must remain 100% deterministic
     Files: Assets/Scripts/Core/Systems/ActionResolver.cs

[ ] T-121: Implement alternative-shot specials (Ricochet, PhaseShot, Pierce)
     ARCHER — Ricochet: Replace CastRay with CastRayRicochet (already in GridSystem)
       Shot bounces off 1 wall, 90° clockwise preference. Full remaining range after bounce.
     MAGE — PhaseShot: Replace CastRay with CastRayPhase (already in GridSystem)
       Shot passes through exactly 1 wall, stops at 2nd wall.
     HAWK — Pierce: Replace CastRay with CastRayPierce (already in GridSystem)
       Shot passes through ALL obstacles on full range (10 tiles = entire map).
     Note: These modify combat phase — when action is Special, use hero-specific
     ray variant instead of normal CastRay. Still counts as a shot (sets cooldown).
     Files: ActionResolver.cs (ResolveCombat modification)

[ ] T-122: Implement movement specials (Blink, Charge)
     SHADOW — Blink: Teleport up to 4 tiles in facing direction.
       Ignores walls (passes through). Keeps facing direction.
       Target must be walkable and in-bounds. If blocked at range 4, try 3, 2, 1.
       Does NOT count as a shot. No cooldown interaction.
     BERSERKER — Charge: Move 3 tiles forward + Shoot in 1 action.
       Stops at wall like normal move. Shoots from ARRIVAL position.
       Range = hero.range (2). Sets cooldown after shot.
       Combination: movement phase + combat phase in one action.
     Files: ActionResolver.cs (ResolveMovement + ResolveCombat)

[ ] T-123: Implement placement specials (Bomb, Barrier, Turret, Decoy)
     DEMO — Bomb: Place bomb on adjacent tile in facing direction.
       Detonates at START of next step. 3×3 explosion zone.
       Destroys: walls, pickups, armor. Kills any hero in zone.
       Requires new HeroState fields: BombPosition, BombStepPlaced.
       Bomb resolution happens at start of step (before movement).
     GUARDIAN — Barrier: Place temporary wall on adjacent tile (facing direction).
       Lasts until end of round. Blocks movement and shots (except PhaseShot).
       Max 1 barrier active. Uses GridSystem.PlaceBarrier (already exists).
       Requires new HeroState field: BarrierPosition (for cleanup at round end).
     ENGINEER — Turret: Place auto-shooting turret on adjacent tile.
       Activates next step. Shoots forward (engineer's facing at placement) 3 range.
       Fires every step, no cooldown. Has 1 HP (destroyed by any shot).
       Max 1 turret. Requires new fields: TurretPosition, TurretFacing, TurretActive.
       Turret combat resolves after hero combat in each step.
     MIRAGE — Decoy: Place visual clone at up to 5 tiles in facing direction.
       Clone appears as the hero to opponent. Absorbs 1 shot (destroyed on hit).
       Opponent cannot distinguish real from decoy until shot connects.
       Requires new fields: DecoyPosition, DecoyActive.
     Files: ActionResolver.cs, HeroState.cs (new fields)

[ ] T-124: Implement buff specials (Cloak, Scan)
     GHOST — Cloak: Become invisible for 2 steps.
       Opponent cannot see Ghost's position (client shows "???").
       Ghost can move, turn, shoot while cloaked.
       Shooting while cloaked BREAKS cloak immediately.
       Uses existing HeroState.IsCloaked + CloakStepsRemaining.
       UpdateCloaking() already exists — just needs activation in ResolveSpecial.
     SCOUT — Scan: Reveals opponent's NEXT action.
       Use on step N → reveals what opponent does on step N+1.
       Information appears during execution playback.
       Requires new StepResult field: ScannedNextAction (ActionType?).
       Useful for learning patterns for next round.
     Files: ActionResolver.cs, StepResult.cs (ScannedNextAction field)

[ ] T-125: Implement force special (Push)
     TANK — Push: Push opponent 2 tiles in Tank's facing direction.
       Opponent must be on adjacent tile (1 tile away in facing direction).
       If opponent not adjacent → Push fails silently, action wasted.
       Pushed into wall → stop at wall. Pushed off map edge → ELIMINATED.
       Also pushes destructible walls (destroys them).
       Does NOT count as a shot. No cooldown interaction.
     Files: ActionResolver.cs

[ ] T-126: Port all specials to TypeScript ActionResolverService
     Must produce identical results to C# implementation.
     Mirror every change from T-121..T-125 in server code.
     Add corresponding fields to game-types.ts (HeroState, StepResult).
     Files: server/src/match/action-resolver.service.ts,
            server/src/shared/models/game-types.ts

[ ] T-127: Unit tests for all special abilities (C#)
     Minimum 3 test cases per ability (36+ tests total):
       - Basic usage (ability works as expected)
       - Edge case (wall, map boundary, mutual interaction)
       - Validation (can't use twice per round, cooldown interaction)
     Test cross-ability interactions:
       - Pierce shot vs Barrier (passes through)
       - PhaseShot vs Barrier (passes through 1)
       - Bomb vs Cloaked hero (kills if in zone)
       - Turret vs Decoy (shoots decoy)
       - Push vs Barrier (push into barrier = stop)
       - Charge into occupied tile (stop before)
     Files: Assets/Tests/EditMode/SpecialAbilityTests.cs

[ ] T-128: Unit tests for all special abilities (TypeScript)
     Mirror C# tests from T-127 in server test suite.
     Cross-language determinism: same inputs → same outputs.
     Files: server/test/special-abilities.spec.ts

[ ] T-129: Special ability VFX & animations
     ExecutionController + HeroView3D must handle each special visually:
       - Ricochet: shot trail bounces off wall
       - PhaseShot: ghostly trail through wall
       - Pierce: trail through everything
       - Blink: disappear + reappear with particle flash
       - Charge: fast move + shot combo animation
       - Bomb: place anim → tick-tick → 3×3 explosion
       - Barrier: wall rise animation
       - Turret: deploy animation + periodic shots
       - Decoy: shimmer/clone spawn
       - Cloak: fade out / fade in
       - Scan: scan wave / highlight opponent
       - Push: knockback force visual
     Files: ExecutionController.cs, HeroView3D.cs, VFXManager.cs
```

## Phase 13: Map Shrinking & Multiple Maps

```
[ ] T-130: Wire ShrinkSystem into server MatchService
     Currently ShrinkSystem exists on client but server doesn't call it.
     After round ends with no kill: expandDangerZone() on server GridSystem.
     Send shrinkData in round:end event to client.
     Client applies matching shrink to local GridSystem.
     Round 2: outer 1-tile ring → danger zone. Round 3: outer 2-tile ring.
     End-of-round check: hero in danger zone → eliminated.
     Files: server/src/match/match.service.ts, MatchManager.cs

[ ] T-131: Create 2 additional maps
     Map_Corridor.asset: 8×12, narrow corridors with many walls.
       Favors: Mage (PhaseShot), Guardian (Barrier), Archer (Ricochet)
     Map_OpenField.asset: 12×12, few walls, wide open.
       Favors: Hawk (Pierce across map), Shadow (Blink mobility), Berserker (Charge)
     Both need: symmetric spawns, wall placement, shrink ring definitions.
     Files: ScriptableObjects/Maps/, server hero configs (add map data)

[ ] T-132: Map selection system
     Random map selection from pool (server decides).
     Send mapId in match:found event. Client loads correct MapConfig.
     Future: map banning in ranked mode.
     Files: match.service.ts, GameManager.cs
```

## Phase 14: Sound & Music

> ART-ASSET-LIST.md defines 5 music tracks + 14 SFX for MVP.

```
[ ] T-140: Create AudioManager.cs in Scripts/Gameplay/
     Singleton. Manages BGM + SFX channels.
     BGM: crossfade between tracks. Volume control.
     SFX: pooled one-shot sources. Spatial audio for grid-based sounds.
     Settings: master/music/sfx volume (persisted via IPlatformStorage).
     Files: Assets/Scripts/Gameplay/AudioManager.cs

[ ] T-141: Implement gameplay SFX
     Triggered by GameEvents:
       - Shoot: shot_fire.wav (per hero variant if available)
       - Hit: hit_impact.wav
       - Armor break: armor_break.wav
       - Elimination: hero_death.wav
       - Mutual cancel: mutual_cancel.wav
       - Move: footstep.wav
       - Turn: swish.wav
       - Bomb place/explode: bomb_tick.wav, explosion.wav
       - Barrier/Turret deploy: deploy.wav
       - Blink: teleport.wav
       - Cloak on/off: cloak_activate.wav, cloak_deactivate.wav
     Files: AudioManager.cs, sound asset files

[ ] T-142: Implement UI SFX
     - Button tap: ui_tap.wav
     - Action queued: action_slot_fill.wav
     - Action undo: action_slot_remove.wav
     - Timer warning (5s): tick_urgent.wav
     - Confirm actions: confirm.wav
     - Match found: match_found_fanfare.wav
     - Victory/defeat: victory.wav, defeat.wav
     Files: AudioManager.cs, UI screen scripts

[ ] T-143: Implement background music
     Tracks (from ART-ASSET-LIST.md):
       - Main menu theme
       - Planning phase (tension building)
       - Execution phase (action)
       - Victory fanfare
       - Defeat theme
     Crossfade on phase transitions.
     Files: AudioManager.cs, music asset files

[ ] T-144: Source/create audio assets
     Option A: Royalty-free asset packs (Freesound, Mixkit)
     Option B: AI-generated (Suno for music, ElevenLabs for SFX)
     Option C: Commission from audio designer
     Target: placeholder sounds first, polish later.
```

## Phase 15: Tutorial & Onboarding

```
[ ] T-150: Create TutorialManager.cs
     First-time player experience. Detects first launch via IPlatformStorage.
     Step-by-step guided match against scripted bot actions:
       Step 1: "This is your hero. It faces this direction."
       Step 2: "Tap MOVE to move forward."
       Step 3: "Tap TURN RIGHT to face the enemy."
       Step 4: "Tap SHOOT to fire in your facing direction."
       Step 5: "Fill all slots and press CONFIRM."
       Step 6: Watch execution. "You hit the enemy!"
       Step 7: "Now play a real match!"
     Highlight system: dim everything except target button/area.
     Skip button for returning players.
     Files: Assets/Scripts/UI/TutorialManager.cs

[ ] T-151: Create scripted tutorial match data
     Predefined bot actions that guarantee the player wins if they follow instructions.
     Round 1: Bot walks into line of fire.
     Round 2: Bot uses armor — player learns armor mechanic.
     Round 3: Bot dodges — player learns to predict movement.
     Files: Assets/ScriptableObjects/Tutorial/TutorialScript.asset

[ ] T-152: Create tooltip/hint system for planning screen
     First 3 real matches: contextual tips appear:
       - "Cooldown! You can't shoot for 2 more steps."
       - "Your special ability can be used once per round."
       - "Try to predict where your opponent will be!"
     Dismissed on tap. Don't show after 3 matches.
     Files: PlanningScreen.cs (hint overlay), IPlatformStorage (seen count)
```

## Phase 16: Bot AI

> Needed for: solo play, tutorial, testing, filling matchmaking gaps.

```
[ ] T-160: Create BotAI.cs in Scripts/Core/AI/
     Interface: IBotStrategy with method:
       List<ActionType> PlanActions(HeroState self, HeroState opponent,
                                     GridSystem grid, int round)
     Difficulty levels:
       - Easy: random valid actions (respect cooldowns)
       - Medium: move toward opponent + shoot when in range
       - Hard: predict opponent position, use specials strategically
     Files: Assets/Scripts/Core/AI/BotAI.cs, IBotStrategy.cs

[ ] T-161: Implement Easy bot
     Random actions with basic validity:
       - Don't shoot on cooldown
       - Use special once randomly
       - Prefer Move/Shoot over Wait
     Good enough for tutorial and casual play.

[ ] T-162: Implement Medium bot
     Heuristic-based:
       - Calculate distance to opponent
       - If in range: face opponent → shoot
       - If not in range: move toward opponent
       - Use special ability with basic logic (Blink toward, Barrier when exposed, etc.)
       - Avoid danger zone tiles
     Files: Assets/Scripts/Core/AI/MediumBotStrategy.cs

[ ] T-163: Implement Hard bot
     Predictive:
       - Simulate likely opponent moves
       - Choose actions that maximize kill probability
       - Use specials optimally (Ricochet around corners, Bomb on predicted position)
       - Minimax or Monte Carlo tree search (lightweight, ≤10ms per plan)
     Files: Assets/Scripts/Core/AI/HardBotStrategy.cs

[ ] T-164: Wire bot into GameManager
     Offline mode: option to play vs bot (Easy/Medium/Hard).
     Bot replaces P2 planning phase — instant action generation.
     Online mode: server can spawn bot if matchmaking times out (>30s).
     Files: GameManager.cs, server match.service.ts (server-side bot)
```

## Phase 17: Reconnection & Error Handling

```
[ ] T-170: Client reconnection flow
     When WebSocket disconnects mid-match:
       1. Show "Reconnecting..." overlay (don't kill match state)
       2. SocketIOClient auto-reconnect (already has exponential backoff)
       3. On reconnect: emit "match:rejoin" { matchId }
       4. Server sends current match state snapshot
       5. Client restores: current round, phase, hero positions, scores
       6. If reconnect fails after 30s: show "Connection Lost" + Return to Menu
     Files: SocketIOClient.cs, MatchNetworkController.cs, GameManager.cs

[ ] T-171: Server-side reconnection support
     Add "match:rejoin" event handler in MatchGateway.
     On rejoin: re-map socket to player, send full match state.
     Timeout: if player doesn't rejoin within 60s → auto-forfeit.
     Opponent sees: "Opponent reconnecting..." → timer → auto-win.
     Files: server/src/match/match.gateway.ts, match.service.ts

[ ] T-172: Error state UI
     Standard error overlay component. Shows:
       - "Server unavailable" (can't connect at all)
       - "Connection lost" (mid-session disconnect, unrecoverable)
       - "Match expired" (server cleaned up the match)
       - "Opponent left" (opponent disconnected permanently)
       - "Invalid action" (server rejected action list — should never happen)
     Each state has: message, icon, and action button (Retry / Main Menu).
     Files: Assets/Scripts/UI/ErrorOverlay.cs

[ ] T-173: Timeout handling
     Planning phase: timer already auto-submits (T-051). Verify server-side too.
     Commit phase: if opponent doesn't commit within 45s → opponent forfeits.
     Reveal phase: if opponent doesn't reveal within 15s → opponent forfeits.
     Server sends "match:end" with timeout reason.
     Files: server/src/match/match.service.ts (add timeout timers)
```

## Phase 18: Progression System

> Prisma schema has Player.battlePassXp, Player.rankTier, HeroMastery.xp/level
> but none are updated after matches.

```
[ ] T-180: Implement rating updates after match
     Already partially done (Elo K=32 in MatchService).
     Verify: rating change persisted to DB, sent to client in match:end.
     Client shows +/- rating on ResultScreen.
     Rank tiers derived from rating: Bronze (0-999), Silver (1000-1499),
     Gold (1500-1999), Diamond (2000-2499), Legend (2500+).
     Files: server/src/match/match.service.ts, ResultScreen.cs

[ ] T-181: Implement hero mastery XP
     After each match: +50 XP win, +20 XP loss, +10 XP draw.
     XP goes to the hero used in that match.
     Mastery levels: 1→2 at 100 XP, 2→3 at 300 XP, etc. (exponential curve).
     Show mastery level on hero select card.
     Files: server/src/match/match.service.ts (update HeroMastery),
            HeroSelectScreen.cs (display level)
```

## Phase 19: Solana Integration

```
[ ] T-190: Create Anchor escrow program
     Match escrow with 3 instructions:
       - create_match: PDA escrow, both players deposit SOL/SPL
       - settle_match: server signs, winner receives stakes (minus fee)
       - cancel_match: refund if opponent doesn't join (60s timeout)
     Platform fee: 5% of total pot, sent to treasury wallet.
     Program must be deployed on devnet for testing, mainnet for launch.
     Files: programs/tactical-duelist/src/lib.rs (Anchor/Rust)

[ ] T-191: Create SolanaService on NestJS backend
     Interfaces with Anchor program via @solana/web3.js:
       - verifyDeposit(matchId, playerWallet): check on-chain escrow
       - settleMatch(matchId, winnerWallet): trigger settlement
       - cancelMatch(matchId): refund on timeout
     Uses server keypair for signing settlement transactions.
     Files: server/src/solana/solana.module.ts, solana.service.ts

[ ] T-192: Wallet connection in TMA (client-side)
     Phantom deep link integration:
       - "Connect Wallet" button on main menu
       - Deep link: phantom://connect?... → returns publicKey
       - Store wallet address in PlayerPrefs + server profile
     Alternative: Privy embedded wallet (lower friction, no external app)
     jslib interop for wallet signing in WebGL build.
     Files: Scripts/Platform/WebGL/WebGLWallet.cs, wallet.jslib

[ ] T-193: Staked match flow (end-to-end)
     Client flow:
       1. Player taps "Staked Match" → selects stake amount (0.1 / 0.5 / 1 SOL)
       2. Wallet signs deposit transaction → escrow PDA
       3. Server verifies deposit → starts matchmaking
       4. Normal match plays out
       5. Server calls settleMatch → winner gets payout
       6. Client shows settlement confirmation + tx link
     Files: GameManager.cs (staked mode), MatchService (settlement trigger)

[ ] T-194: SPL token rewards (optional)
     Create $DUEL SPL token on Solana.
     Reward distribution: +10 DUEL per win, +3 per loss, +5 per draw.
     Minted from reward pool account (server-controlled).
     Show token balance on main menu.
     Files: server/src/solana/solana.service.ts (mintReward method)
```

## Phase 20: Future Platform Implementations (Post-Sprint)

```
[ ] T-200: Create AndroidPlatform.cs in Scripts/Platform/Android/
     Implements IPlatformService for Android
     Auth: Telegram Login Widget or Google Play Games
     Storage: PlayerPrefs (SharedPreferences)
     Network: native ClientWebSocket
     Haptics: Android Vibrator API via AndroidJavaObject

[ ] T-201: Create IOSPlatform.cs in Scripts/Platform/iOS/
     Implements IPlatformService for iOS
     Auth: Telegram Login Widget or Apple Game Center
     Storage: PlayerPrefs (NSUserDefaults)
     Network: native ClientWebSocket
     Haptics: UIImpactFeedbackGenerator via native plugin

[ ] T-202: Platform-specific build configurations
     Android: IL2CPP ARM64, ASTC textures, Gradle build
     iOS: IL2CPP ARM64, ASTC textures, Xcode project
     WebGL: see Phase 8 WebGL build settings
```

---

## Task Dependencies

```
Phase 1–11: COMPLETED (see above)

Phase 12 (Specials):
  T-120 → T-121..T-125 (dispatch first, then individual abilities)
  T-121..T-125 → T-126 (C# first, then TS port)
  T-121..T-125 → T-127 (implement then test)
  T-126 → T-128 (TS port then TS tests)
  T-121..T-125 → T-129 (logic first, then VFX)

Phase 13 (Maps):     T-120 → T-130 (specials may affect shrink interactions)
Phase 14 (Sound):    Independent, can start anytime
Phase 15 (Tutorial): T-120 → T-150 (tutorial should show specials)
Phase 16 (Bot AI):   T-120 → T-160 (bots need to use specials)
Phase 17 (Network):  Independent, can start anytime
Phase 18 (Progress): Independent, can start anytime
Phase 19 (Solana):   T-094 (MatchService) → T-190..T-194
Phase 20 (Platforms): Post-Sprint. T-100 → T-200..T-202

Critical path:  T-120 → T-121..T-125 → T-126 → T-127..T-128 → T-150 → T-160
Parallel paths: T-130 (maps), T-140 (sound), T-170 (reconnect), T-180 (progression), T-190 (Solana)
```
