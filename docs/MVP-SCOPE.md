# Tactical Duelist — MVP Scope

## Goal

Playable 1v1 prototype with core loop validation.
**Phase A** — offline (pass-and-play), **Phase B** — online via NestJS backend.

> **Multi-platform note:** Architecture is multi-platform from day one (WebGL, Android, iOS, Web)
> via `IPlatformService` abstraction layer. However, **MVP builds and tests on WebGL (TMA) only**.
> Platform-specific code is isolated so native mobile builds can be added without touching game logic.
> See docs/ARCHITECTURE.md → Platform Abstraction Layer for details.

**Definition of done:**
- Phase A: Two humans play 3+ rounds on one device, find it fun.
- Phase B: Two players match online via Telegram Mini App WebGL build.

---

## Phase A: Offline Prototype (Core Loop Validation)

### Core Systems
- [ ] GridSystem: 10×10 grid, walls, position tracking
- [ ] HeroState: position, facing, armor, cooldown
- [ ] ActionResolver: deterministic step-by-step resolution (Move, Turn, Shoot, Wait)
- [ ] DamageResolver: hit detection, armor break, mutual cancel, elimination
- [ ] MatchManager: round lifecycle, match lifecycle
- [ ] ActionValidator: validate action lists (step count, cooldown gaps)
- [ ] GridHelper: grid ↔ world coordinate conversion for 3D

### Heroes (4 for MVP)
- [ ] Archer (Steps:4, Range:8, CD:2, Armor:0, Speed:1) — NO Ricochet yet
- [ ] Tank (Steps:4, Range:4, CD:1, Armor:1, Speed:1) — NO Push yet
- [ ] Shadow (Steps:6, Range:3, CD:1, Armor:0, Speed:2) — NO Blink yet
- [ ] Scout (Steps:5, Range:5, CD:1, Armor:0, Speed:2) — NO Scan yet

> **Note:** MVP heroes have base stats only. Special abilities come in v0.2.

### Map
- [ ] One hardcoded 10×10 map with 6-8 walls
- [ ] Fixed spawn positions (opposite corners)
- [ ] NO pickups in MVP
- [ ] NO shrinking in MVP (3 rounds same map)

### 3D View (URP)
- [ ] Grid rendered as 3D planes with URP Lit materials
- [ ] Walls as 3D cube meshes
- [ ] Hero = capsule + colored material + direction arrow (placeholder)
- [ ] Perspective camera, isometric angle (~45° X, ~45° Y), narrow FOV
- [ ] Camera follows action (smooth track between heroes)
- [ ] Move animation: smooth position lerp (~0.3s)
- [ ] Shoot animation: VFX trail from hero in facing direction
- [ ] Hit feedback: VFX flash + camera shake
- [ ] Armor break: particle VFX
- [ ] Elimination: hero dissolve/explode VFX
- [ ] Playback speed: 1x, 2x, pause

### Planning UI
- [ ] Action queue: tap-to-add actions into slots
- [ ] Shows hero's available actions based on stats
- [ ] Slots = hero.Steps count
- [ ] Cooldown indicator: grayed-out Shoot when on cooldown
- [ ] Undo last action button
- [ ] Confirm button to lock in actions
- [ ] Timer: 30 seconds for round 1, 20 seconds later rounds

### Pass-and-Play Flow
```
1. Hero Select: P1 picks hero → screen handoff → P2 picks hero
2. Planning: P1 programs actions → hides screen → P2 programs actions
3. Execution: both watch execution together
4. If no kill → next round (repeat from step 2)
5. After 3 rounds no kill → draw
6. Result screen: winner, rematch
```

### UI Screens
- [ ] Hero Select screen (4 heroes with portrait + stat cards)
- [ ] Planning screen (3D grid preview + action queue)
- [ ] Execution screen (3D animated execution)
- [ ] Result screen (winner announcement + rematch)
- [ ] HUD: round counter, step counter, health/armor status

---

## Phase B: Online Multiplayer

### Networking (Client-Side)
- [ ] SocketIO client (WebGL-compatible WebSocket)
- [ ] MatchNetworkController: events for match lifecycle
- [ ] Commit-reveal scheme: SHA256 hash commitment
- [ ] Reconnect handling with exponential backoff

### NestJS Backend
- [ ] Project scaffold: NestJS + Socket.IO + Prisma
- [ ] PostgreSQL schema: Player, Match, Round
- [ ] Redis: session cache, matchmaking queue
- [ ] MatchGateway: WebSocket event handlers
- [ ] MatchmakingService: ELO-based matching (±200 range)
- [ ] MatchService: match state, commit/reveal validation
- [ ] ActionResolverService: TypeScript port of C# logic (identical output)
- [ ] WsAuthGuard: JWT validation on WebSocket connections

### Online Flow Changes
- [ ] Matchmaking screen (searching for opponent)
- [ ] Hero select: server-coordinated
- [ ] Planning: submit commit hash, then reveal
- [ ] Results received from server (authoritative)
- [ ] Rank change display, XP display
---

## OUT OF SCOPE (v0.2+)

- Special abilities (Ricochet, Blink, Push, Scan, Phase Shot, Bomb, etc.)
- Remaining 8 heroes (Mage, Demo, Guardian, Ghost, Engineer, Berserker, Hawk, Mirage)
- Pickups (Armor Shard, Intel Orb, etc.)
- Map shrinking
- Multiple maps
- Final 3D character models (MVP uses placeholder capsules)
- Sound effects / music
- Progression system (XP, ranks, seasons)
- Bot AI
- Spectator mode
- Solana staked matches (see docs/COLOSSEUM-SPRINT.md Week 4)
- Telegram Mini App deep integration (payments, ads, social)
- Localization
- Anti-cheat beyond commit-reveal

---

## Implementation Order

### Phase A — Week 1-2: Core Logic + 3D View

```
Days 1-2: Data Foundation (TASKS.md Phase 1)
  ├── Enums, HeroConfig, MapConfig, HeroState, StepResult
  ├── GridHelper (grid ↔ world conversion)
  ├── GameEvents static hub
  ├── NetworkTypes (data classes, used later)
  └── Create 4 hero + 1 map ScriptableObject assets

Days 3-4: Grid & Resolution (TASKS.md Phase 2-3)
  ├── GridSystem (from MapConfig, tile queries, CastRay)
  ├── ActionResolver (Movement → Combat → Damage)
  ├── ActionValidator
  └── Unit tests for GridSystem + ActionResolver

Days 5-6: 3D View Layer (TASKS.md Phase 5)
  ├── GridView (3D tiles + walls)
  ├── HeroView3D (placeholder capsule + animations)
  ├── CameraController (perspective isometric)
  ├── VFXManager (shoot trails, hit effects)
  └── ExecutionController (playback with animations)

Days 7-8: UI + Planning (TASKS.md Phase 6)
  ├── HeroSelectScreen
  ├── PlanningScreen (action queue + timer)
  ├── ResultScreen
  └── HUD

Days 9-10: Integration + Testing (TASKS.md Phase 8-9)
  ├── GameManager (full offline flow wiring)
  ├── MatchScene setup
  ├── Bug fixing from self-testing
  └── First playtest session
```

### Phase B — Week 3-4: Online Multiplayer

```
Days 11-12: NestJS Backend Foundation
  ├── Project scaffold, Prisma schema, Redis config
  ├── MatchGateway (WebSocket)
  └── MatchmakingService

Days 13-14: Server Game Logic
  ├── MatchService (state management, commit/reveal)
  ├── ActionResolverService (TS port of C# logic)
  └── Cross-language test vectors

Days 15-16: Client Networking
  ├── SocketIOClient (WebGL WebSocket)
  ├── MatchNetworkController
  ├── HashUtil (commit-reveal)
  └── Wire online match flow

Days 17-18: Integration + WebGL Build
  ├── WebGL build settings (IL2CPP, Brotli, ASTC, etc.)
  ├── Telegram Mini App wrapper
  ├── End-to-end online match test
  └── Performance profiling (60 FPS target)
```

---

## Testing Checklist (Phase A)

After offline MVP is built, validate these scenarios:

```
[ ] Both players can select different heroes
[ ] Planning timer counts down and auto-submits
[ ] Move action moves hero correct number of tiles
[ ] Move into wall = stay in place
[ ] Move collision (both to same tile) = both stay
[ ] Turn changes facing correctly
[ ] Shoot fires ray in facing direction
[ ] Shoot hits opponent in ray path
[ ] Shoot stopped by wall
[ ] Shoot misses if opponent not in ray
[ ] Shoot cooldown prevents consecutive shots
[ ] Mutual hit = both shots cancel (CRITICAL)
[ ] Single hit on armored hero = armor breaks
[ ] Single hit on unarmored hero = elimination
[ ] Hit on armored hero does NOT eliminate
[ ] Archer (Range 8) can snipe across map
[ ] Shadow (Speed 2) moves 2 tiles per Move action
[ ] Shadow (6 steps) has more actions than Archer (4 steps)
[ ] Tank starts with armor
[ ] Round ends when someone is eliminated
[ ] Match ends after elimination or 3 rounds
[ ] Draw if no elimination after 3 rounds
[ ] 3D camera shows grid at proper isometric angle
[ ] Animations play in correct order during execution
[ ] VFX for shoot/hit/armor break visible and timed
[ ] Playback speed toggle works (1x / 2x / pause)
[ ] Rematch option works
[ ] Full match plays smoothly without errors at 60 FPS
```

## Testing Checklist (Phase B)

```
[ ] Two clients connect to NestJS server via WebSocket
[ ] Matchmaking pairs two players within rating range
[ ] Commit hash sent, reveal validated against hash
[ ] Server resolves round and sends identical results to both clients
[ ] Client execution matches server-computed results
[ ] Disconnect + reconnect resumes match
[ ] Match results persisted to PostgreSQL
[ ] WebGL build loads in Telegram Mini App
[ ] Frame rate stays above 60 FPS on mobile browser
[ ] Bundle size under 20 MB (compressed)
```
