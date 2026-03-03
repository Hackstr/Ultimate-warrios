# Tactical Duelist — MVP Scope

## Goal

Playable 1v1 prototype on a single device (pass-and-play).
Two players, one screen. Validate that the core loop is fun.

**Time estimate:** 2 weeks  
**Definition of done:** Two humans can play 3 rounds and want to play again.

---

## IN SCOPE (Must have)

### Core Systems
- [ ] GridSystem: 10×10 grid, walls, position tracking
- [ ] HeroState: position, facing, armor, cooldown
- [ ] ActionResolver: full step-by-step resolution (Move, Turn, Shoot, Wait)
- [ ] DamageResolver: hit detection, armor break, mutual cancel, elimination
- [ ] MatchManager: round lifecycle, match lifecycle

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
- [ ] NO shrinking in MVP (3 rounds with same map)

### Planning UI
- [ ] Action queue: drag-and-drop or tap-to-add actions
- [ ] Shows hero's available actions based on stats
- [ ] Visual: shows action slots = hero.Steps
- [ ] Cooldown indicator: grayed-out Shoot when on cooldown
- [ ] Confirm button to lock in actions
- [ ] Timer: 30 seconds for round 1, 20 seconds later rounds

### Execution View
- [ ] Grid renders with tiles and walls
- [ ] Heroes rendered as colored squares with direction arrow
- [ ] Step-by-step playback: each step animates sequentially
- [ ] Move animation: smooth slide between tiles (~0.3s)
- [ ] Shoot animation: line/ray from hero to target (~0.2s)
- [ ] Hit feedback: flash, screen shake
- [ ] Armor break: visual indicator
- [ ] Elimination: hero disappears/explodes
- [ ] Playback speed control: 1x, 2x, or step-by-step

### Pass-and-Play Flow
```
1. Hero Select: P1 picks hero → screen handoff → P2 picks hero
2. Planning: P1 programs actions → hides screen → P2 programs actions
3. Execution: both watch replay together
4. If no kill → next round (repeat from step 2)
5. After 3 rounds no kill → draw
6. Result screen: winner, replay option, rematch
```

### Basic UI Screens
- [ ] Hero Select screen (4 heroes with stat cards)
- [ ] Planning screen (grid preview + action queue)
- [ ] Execution screen (animated grid)
- [ ] Result screen (winner announcement)

---

## OUT OF SCOPE (v0.2+)

- Special abilities (Ricochet, Blink, Push, Scan, etc.)
- Remaining 8 heroes
- Pickups (Armor Shard, Intel Orb, etc.)
- Map shrinking
- Multiple maps
- Online multiplayer / networking
- Hash commitment protocol
- Sound effects / music
- Particle effects / polish
- Blockchain / $DUEL token
- Progression system (XP, ranks, seasons)
- Bot AI
- Replay save/share
- Spectator mode
- Telegram Mini App integration
- Localization

---

## Implementation Order

Build in this exact order. Each step should be testable before moving to next.

### Week 1: Core Logic + Grid

```
Day 1-2: Foundation
  ├── Enums (Direction, ActionType, TileType, GamePhase)
  ├── HeroConfig ScriptableObject
  ├── MapConfig ScriptableObject
  ├── HeroState class
  └── Create 4 hero SO assets + 1 map SO asset

Day 3-4: Grid & Movement
  ├── GridSystem (create from MapConfig, tile queries, walkability)
  ├── GridSystem.CastRay (shoot raycast on grid)
  ├── GridSystem direction helpers (TurnLeft, TurnRight, etc.)
  ├── Movement resolution (Move action, collision detection)
  └── Unit tests: GridSystem (ray hits wall, movement blocked, etc.)

Day 5: Action Resolution
  ├── ActionResolver.ResolveStep (full Phase1→Phase2→Phase3)
  ├── DamageResolver (hit, armor break, mutual cancel, elimination)
  ├── StepResult data class
  └── Unit tests: resolver edge cases (mutual cancel, armor, etc.)
```

### Week 2: View + UI

```
Day 6-7: Grid View
  ├── GridView MonoBehaviour (renders tiles + walls from GridSystem)
  ├── HeroView MonoBehaviour (sprite/square + direction arrow)
  ├── Camera setup (orthographic, centered on grid)
  └── Basic tile sprites (empty=light, wall=dark)

Day 8-9: Planning UI
  ├── ActionQueueUI (shows slots, tap to add action)
  ├── Cooldown display
  ├── Confirm button
  ├── Pass-and-play screen handoff ("Pass to Player 2")
  └── Timer (30s/20s)

Day 10: Execution Playback
  ├── ExecutionController (plays StepResults sequentially)
  ├── Move animation (DOTween or coroutine lerp)
  ├── Shoot animation (line renderer flash)
  ├── Hit/kill feedback
  └── Speed control (1x/2x/step)

Day 11-12: Integration + Polish
  ├── MatchManager wiring (hero select → plan → execute → result)
  ├── Hero select screen
  ├── Result screen
  ├── Bug fixing from playtesting
  └── First playtest session with 2 humans
```

---

## Testing Checklist

After MVP is built, validate these scenarios:

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
[ ] Rematch option works
[ ] Full match plays smoothly without errors
```
