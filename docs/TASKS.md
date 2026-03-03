# Tactical Duelist — Implementation Tasks

## How to use this file

Each task below is a self-contained unit of work. Give a task to Cursor AI and
it will have enough context from docs/ + .cursorrules to implement it correctly.

Format: `[STATUS] TASK-ID: Description`
Status: `[ ]` todo, `[→]` in progress, `[✓]` done, `[✗]` blocked

---

## Phase 1: Data Foundation

```
[ ] T-001: Create enums file Scripts/Core/Models/Enums.cs
     Contains: Direction, ActionType, GamePhase, TileType, PickupType,
     SpecialAbility, MatchResult, RoundResult
     Reference: docs/TECHNICAL-SPEC.md section 1

[ ] T-002: Create HeroConfig.cs in Scripts/Core/Config/
     ScriptableObject with all hero parameters
     Reference: docs/TECHNICAL-SPEC.md section 2 (HeroConfig)

[ ] T-003: Create MapConfig.cs in Scripts/Core/Config/
     ScriptableObject with grid data, spawns, pickup spawns
     Reference: docs/TECHNICAL-SPEC.md section 2 (MapConfig)

[ ] T-004: Create HeroState.cs in Scripts/Core/Models/
     Runtime per-player state: position, facing, armor, cooldown, buffs
     Reference: docs/TECHNICAL-SPEC.md section 2 (HeroState)

[ ] T-005: Create StepResult.cs in Scripts/Core/Models/
     Data class for step resolution output
     Reference: docs/TECHNICAL-SPEC.md section 2 (StepResult)

[ ] T-006: Create GameEvents.cs in Scripts/Core/Systems/
     Static event hub for Model→View communication
     Reference: docs/ARCHITECTURE.md (Event System)

[ ] T-007: Create 4 HeroConfig assets in ScriptableObjects/Heroes/
     Hero_Archer.asset, Hero_Tank.asset, Hero_Shadow.asset, Hero_Scout.asset
     Use parameter values from docs/HEROES.md
```

## Phase 2: Grid System

```
[ ] T-010: Create GridSystem.cs in Scripts/Core/Systems/
     Constructor from MapConfig. TileType[,] grid storage.
     Methods: GetTile, IsWalkable, IsInBounds, HasPickup, GetPickup
     Reference: docs/TECHNICAL-SPEC.md section 3

[ ] T-011: Add direction helpers to GridSystem
     Static methods: DirectionToVector, TurnLeft, TurnRight, TurnAround
     DirectionToVector: Up=(0,1), Down=(0,-1), Left=(-1,0), Right=(1,0)

[ ] T-012: Implement GridSystem.GetMoveTarget
     Calculate target tile for Move action. Speed = tiles moved.
     Must check: wall collision (stop before wall), bounds check,
     Speed 2 heroes check intermediate tile.

[ ] T-013: Implement GridSystem.CastRay
     Ray from position in direction for range tiles.
     Returns List<Vector2Int> of tiles ray passes through.
     Stops at first wall (does not include wall tile).
     Does not include starting position.

[ ] T-014: Create MapConfig asset for MVP map
     ScriptableObjects/Maps/Map_Arena01.asset
     10x10 grid. 6-8 wall tiles symmetrically placed.
     P1 spawn: (1,1) facing Up. P2 spawn: (8,8) facing Down.
     No pickups for MVP.
```

## Phase 3: Action Resolution

```
[ ] T-020: Create ActionResolver.cs in Scripts/Core/Systems/
     ResolveStep(stepIndex, p1Action, p2Action) → StepResult
     Three phases: Movement → Combat → Damage
     Reference: docs/TECHNICAL-SPEC.md section 4 (complete algorithm)

[ ] T-021: Implement movement resolution in ActionResolver
     Move: apply GridSystem.GetMoveTarget
     Turn: apply GridSystem.TurnLeft/Right/Around
     Collision: both to same tile = both stay at original
     Swap collision: moving into each other = both stay

[ ] T-022: Implement combat resolution in ActionResolver
     Shoot: check cooldown, cast ray, check if opponent in ray
     Set P1Fired, P2Fired, P1Hit, P2Hit on StepResult

[ ] T-023: Implement damage resolution in ActionResolver
     Mutual cancel: both hit = nobody takes damage
     Single hit + armor = armor broken
     Single hit + no armor = eliminated
     Reference: docs/TECHNICAL-SPEC.md section 4 (ResolveDamage)

[ ] T-024: Implement cooldown tracking in ActionResolver
     After Shoot: hero.CooldownRemaining = hero.Config.cooldown
     Each step: CooldownRemaining = max(0, CooldownRemaining - 1)
     Cannot shoot if CooldownRemaining > 0
```

## Phase 4: Match Management

```
[ ] T-030: Create MatchManager.cs in Scripts/Core/Systems/
     Orchestrates full match: hero select → rounds → result
     Methods: StartMatch, SubmitActions, ExecuteRound, EndMatch
     Reference: docs/TECHNICAL-SPEC.md section 5

[ ] T-031: Implement action validation in MatchManager
     Validate player's action list:
     - Length == hero.Steps (pad with Wait if short)
     - Shoot respects cooldown gaps
     - Special max once per round (not in MVP but prep the check)
     - All actions are valid enum values

[ ] T-032: Create RoundManager logic (can be inside MatchManager for MVP)
     Execute all steps in sequence
     Detect elimination → end round
     After max steps with no kill → end round (no kill)
     Track round count, handle match end after 3 rounds
```

## Phase 5: View Layer

```
[ ] T-040: Create GridView.cs in Scripts/Gameplay/
     MonoBehaviour. Reads GridSystem, renders tile GameObjects.
     Each tile = quad/sprite. Walls = dark color. Empty = light.
     Scale: 1 Unity unit = 1 tile.
     Method: RenderGrid(GridSystem grid)

[ ] T-041: Create HeroView.cs in Scripts/Gameplay/
     MonoBehaviour. Visual representation of one hero.
     Colored square + triangle for facing direction.
     Methods: SetPosition(Vector2Int), SetFacing(Direction),
     AnimateMove(from, to, duration), AnimateShoot(direction, range),
     ShowHit(), ShowArmorBreak(), ShowElimination()

[ ] T-042: Create ExecutionController.cs in Scripts/Gameplay/
     MonoBehaviour. Plays back List<StepResult> with animations.
     Subscribes to GameEvents.OnStepResolved.
     Coroutine-based: play step → wait for animations → next step.
     Support speed: 1x (0.8s/step), 2x (0.4s/step), pause.

[ ] T-043: Create CameraSetup.cs in Scripts/Gameplay/
     MonoBehaviour. Orthographic camera centered on grid.
     Auto-calculate size to fit grid with padding.
     For 10x10 grid: ortho size ≈ 6, position = (4.5, 4.5, -10)
```

## Phase 6: UI

```
[ ] T-050: Create HeroSelectUI.cs in Scripts/UI/
     Shows 4 hero cards with name, portrait, stats.
     Player 1 picks first → "Pass to Player 2" → Player 2 picks.
     Emits event with selected HeroConfigs.

[ ] T-051: Create PlanningUI.cs in Scripts/UI/
     Shows grid preview (small) + action queue.
     Action slots = hero.Steps count.
     Buttons: Move, TurnLeft, TurnRight, TurnAround, Shoot, Wait.
     Shoot button grayed when on cooldown in sequence.
     Undo last action button. Confirm button.
     Timer: 30s round 1, 20s later. Auto-submit on timeout.

[ ] T-052: Create PassDeviceScreen.cs in Scripts/UI/
     Full-screen overlay: "Pass device to Player [N]"
     Tap to continue. Prevents screen peeking.
     Shows between P1 planning and P2 planning.

[ ] T-053: Create ResultUI.cs in Scripts/UI/
     Shows match result: winner portrait + name, "VICTORY" / "DRAW".
     Buttons: Rematch (same heroes), New Match (hero select).

[ ] T-054: Create GameManager.cs in Scripts/Gameplay/
     MonoBehaviour singleton. Wires everything together.
     Manages scene flow: HeroSelect → Planning → Execution → Result.
     Holds references to MatchManager, all UI controllers.
     Entry point for the game.
```

## Phase 7: Integration & Testing

```
[ ] T-060: Wire GameManager: hero select → match start
     HeroSelectUI emits heroes → GameManager calls MatchManager.StartMatch

[ ] T-061: Wire GameManager: planning → execution
     PlanningUI emits actions → PassDevice → PlanningUI →
     GameManager calls MatchManager.ExecuteRound

[ ] T-062: Wire GameManager: execution → result or next round
     Listen for RoundEnded/MatchEnded → show ResultUI or start next planning

[ ] T-063: Create test scene with hardcoded match
     Bypass UI: hardcode 2 heroes, hardcode actions, run ExecuteRound.
     Visual verification that resolution works correctly.

[ ] T-064: Playtest session #1
     Two humans play 5+ matches.
     Record: what's fun, what's confusing, what's broken.
     Known issues list → prioritize fixes.
```

---

## Task Dependencies

```
T-001 → T-002, T-003, T-004, T-005, T-006  (enums first)
T-002, T-003 → T-007, T-014               (configs before assets)
T-004, T-010 → T-020                       (state + grid before resolver)
T-010 → T-011, T-012, T-013               (grid base before features)
T-020 → T-030                              (resolver before match manager)
T-010 → T-040                              (grid system before grid view)
T-005 → T-042                              (step result before execution)
T-030 → T-054                              (match manager before game manager)
T-040..T-053 → T-060..T-062               (all views before wiring)
```
