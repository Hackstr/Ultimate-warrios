# Tactical Duelist — Cursor AI Usage Guide

## Quick Start

Open the project folder `/Users/khakim/Unity Projects/UW/` in Cursor.
The `.cursorrules` file at the root gives AI full context about the game.

## How to give tasks to Cursor

### Best workflow: reference docs + give specific task

```
Prompt example:

"Read docs/TASKS.md task T-010. Then read docs/TECHNICAL-SPEC.md section 3 (GridSystem).
Create GridSystem.cs following the spec. Put it in Assets/Scripts/Core/Systems/.
Follow conventions from docs/CODING-CONVENTIONS.md."
```

### Quick command patterns

**Create a new script:**
```
Create [ClassName].cs in Assets/Scripts/[Layer]/[Folder]/ 
following docs/TECHNICAL-SPEC.md section [N].
Use namespace TacticalDuelist.[Layer].[Folder].
```

**Create ScriptableObject assets:**
```
Create Hero ScriptableObject assets in Assets/ScriptableObjects/Heroes/
for Archer, Tank, Shadow, Scout using stats from docs/HEROES.md.
```

**Fix a bug:**
```
The mutual cancel logic isn't working. When both players hit each other,
both still take damage. Fix ActionResolver.ResolveDamage() — 
mutual hit should cancel both shots. See docs/TECHNICAL-SPEC.md section 4.
```

**Add a feature:**
```
Add shoot animation to HeroView. When P1Fired is true in StepResult,
draw a line from hero position in facing direction for hero.Range tiles.
Line should flash yellow for 0.2s then disappear. Use LineRenderer.
```

## Recommended Cursor settings

### Model
Use Claude Sonnet 4 or Claude Opus 4 for best Unity C# generation.

### Context
Add these to Cursor's context when working on this project:
- `docs/` folder (always include as workspace context)
- `.cursorrules` (auto-included if at project root)

### Composer mode
For multi-file tasks (creating a full system), use Composer with:
```
@docs/TECHNICAL-SPEC.md @docs/CODING-CONVENTIONS.md

Create the full ActionResolver system:
1. ActionResolver.cs in Scripts/Core/Systems/
2. Include movement, combat, and damage resolution
3. Follow the algorithm in TECHNICAL-SPEC section 4 exactly
```

## File reference map

```
.cursorrules              → AI system prompt (auto-loaded)
docs/ARCHITECTURE.md      → System design, data flow, event system, Solana integration
docs/TECHNICAL-SPEC.md    → Enums, data models, algorithms, full code specs
docs/HEROES.md            → All 12 heroes: stats, abilities, matchups
docs/MVP-SCOPE.md         → What to build, what to skip, implementation order
docs/CODING-CONVENTIONS.md → Naming, structure, patterns, WebGL rules
docs/TASKS.md             → Task list with dependencies (incl. Solana Phase 19)
docs/COLOSSEUM-SPRINT.md  → 4-week Colosseum Eternal sprint plan
docs/GAME-DESIGN.md       → Core design pillars and game rules
```

## Common mistakes to watch for

1. **MonoBehaviour in Core layer** — AI sometimes adds MonoBehaviour to logic classes. 
   All files in Scripts/Core/ must be pure C#. Reject any MonoBehaviour there.

2. **Using float for grid positions** — Grid uses Vector2Int only. 
   Float positions are only for View animations.

3. **Missing mutual cancel** — AI may implement damage as "both take damage" on mutual hit.
   Correct: mutual hit = BOTH shots cancel, NOBODY takes damage.

4. **Using Physics for grid** — No Physics2D.Raycast. CastRay is pure grid math
   (iterate tiles in direction, check if wall).

5. **Hardcoded stats** — All hero stats must come from ScriptableObjects, never literals in code.

6. **Missing namespace** — Every file should have `namespace TacticalDuelist.X.Y`.

7. **Camera.main** — Cache it. Don't call Camera.main in Update.

## MCP commands (if Unity MCP is connected)

With Unity MCP active, Cursor can directly interact with Unity Editor:

```
"Create an empty GameObject called GameManager in the scene"
"Create a new C# script called GridSystem and attach it to GameManager"
"Set the Camera to Orthographic with size 6"
"Run the game in Play mode"
```

This is optional but accelerates prototyping significantly.
