# Tactical Duelist — System Architecture

## Overview

Tactical Duelist follows a strict Model-View separation. All game logic lives in
pure C# classes (no MonoBehaviour). Unity integration is a thin presentation layer.

```
┌─────────────────────────────────────────────────────────┐
│                    UNITY LAYER (View)                    │
│  GridView · HeroView · UIController · InputHandler      │
│  AnimationController · AudioManager · CameraController  │
└──────────────────────┬──────────────────────────────────┘
                       │ Events / Callbacks
┌──────────────────────▼──────────────────────────────────┐
│                   GAME LAYER (Model)                     │
│  MatchManager · RoundManager · ActionResolver            │
│  GridSystem · HeroState · ActionQueue                    │
│  DamageResolver · PickupSystem · ShrinkSystem            │
└──────────────────────┬──────────────────────────────────┘
                       │ ScriptableObjects
┌──────────────────────▼──────────────────────────────────┐
│                   DATA LAYER (Config)                    │
│  HeroConfig · MapConfig · PickupConfig · GameSettings    │
└─────────────────────────────────────────────────────────┘
```

## System Dependency Graph

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
```

## Data Flow Per Match

```
1. MATCH START
   MatchManager.StartMatch(player1HeroConfig, player2HeroConfig, mapConfig)
   → Creates HeroState for each player
   → Creates GridSystem from MapConfig
   → Starts Round 1

2. PLANNING PHASE (per round)
   InputHandler collects actions → List<ActionType>
   Player commits hash(actions) to server
   Both ready → reveal actions
   Server validates hash

3. EXECUTION PHASE (per round)  
   RoundManager.ExecuteRound(p1Actions, p2Actions)
   For each step (0 to maxSteps-1):
     ActionResolver.ResolveStep(step, p1Action, p2Action)
       Phase1: ResolveMovement(p1Action, p2Action)
       Phase2: ResolveCombat(p1Action, p2Action)
       Phase3: ResolveDamage(p1Hit, p2Hit)
     → Emit StepResolved event (View animates)
   
   If elimination → RoundManager emits RoundEnded(winner)
   If no elimination → ShrinkSystem.Shrink() → next round

4. MATCH END
   MatchManager.EndMatch(result)
   → Calculate rewards (XP, rank points)
   → Save replay data
   → Emit MatchEnded event
```

## Event System

All communication from Model to View goes through events.
View NEVER calls into Model directly except through defined public APIs.

```csharp
// Core events (defined in Scripts/Core/Systems/GameEvents.cs)
public static class GameEvents
{
    // Match lifecycle
    public static event Action<MatchStartData> OnMatchStarted;
    public static event Action<MatchResult> OnMatchEnded;
    
    // Round lifecycle  
    public static event Action<int> OnRoundStarted;          // roundNumber
    public static event Action<RoundResult> OnRoundEnded;
    public static event Action OnPlanningPhaseStarted;
    public static event Action OnExecutionPhaseStarted;
    
    // Step resolution (View uses these to animate)
    public static event Action<StepResult> OnStepResolved;
    public static event Action<MoveResult> OnMovementResolved;
    public static event Action<CombatResult> OnCombatResolved;
    public static event Action<DamageResult> OnDamageResolved;
    
    // Hero state changes
    public static event Action<int, bool> OnArmorChanged;     // playerIndex, hasArmor
    public static event Action<int> OnHeroEliminated;         // playerIndex
    public static event Action<int, PickupType> OnPickupCollected;
    
    // Map changes
    public static event Action<List<Vector2Int>> OnMapShrunk; // danger zone tiles
}
```

## Key Design Decisions

1. **Pure C# game logic**: ActionResolver, GridSystem, DamageResolver contain
   ZERO Unity dependencies. They use Vector2Int (which is a Unity struct but
   trivially replaceable). This means game logic can run on a server identically.

2. **Deterministic resolution**: Given same inputs, always same outputs.
   No Random, no Time.deltaTime, no floating point in game logic.
   Integer math only for positions and ranges.

3. **ScriptableObjects for data**: All hero stats, map layouts, pickup effects
   are ScriptableObjects. Zero hardcoded values in code.

4. **Event-driven View**: View subscribes to events, never polls.
   Model has no knowledge of View's existence.

5. **Replay-friendly**: Every StepResult contains complete state snapshot.
   Replay = feeding StepResults to View sequentially.
