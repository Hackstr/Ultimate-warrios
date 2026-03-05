# Match Scene Setup Guide

How to assemble the **MatchScene** in Unity Editor so that `GameManager` can orchestrate all gameplay systems.

---

## Hierarchy Overview

```
MatchScene
├── --- MANAGERS ---
│   ├── PlatformBootstrap          (PlatformBootstrap.cs, DontDestroyOnLoad)
│   └── GameManager                (GameManager.cs)
│
├── --- GAMEPLAY ---
│   ├── GridView                   (GridView.cs)
│   ├── ExecutionController        (ExecutionController.cs)
│   └── CameraController           (CameraController.cs + Camera component)
│
├── --- VFX ---
│   └── VFXManager                 (VFXManager.cs)
│
├── --- UI ---
│   ├── Canvas (Screen Space - Overlay)
│   │   ├── HeroSelectScreen       (HeroSelectScreen.cs)
│   │   ├── PlanningScreen         (PlanningScreen.cs)
│   │   ├── ResultScreen           (ResultScreen.cs)
│   │   └── HUD                    (HUD.cs)
│   └── EventSystem
│
├── --- LIGHTING ---
│   ├── Directional Light
│   └── (optional) Post-Processing Volume
│
└── --- AUDIO ---
    └── AudioManager               (future)
```

---

## Step-by-step Setup

### 1. Create the Scene

1. **File → New Scene** (Basic URP).
2. Save as `Assets/Scenes/MatchScene.unity`.

### 2. PlatformBootstrap (skip if already in a persistent scene)

1. Create empty GameObject → rename `PlatformBootstrap`.
2. Attach `PlatformBootstrap.cs`.
3. It will self-register platform services and persist via `DontDestroyOnLoad`.
4. If using a Boot scene, move this there instead.

### 3. GameManager

1. Create empty GameObject → rename `GameManager`.
2. Attach `GameManager.cs`.
3. Wire SerializeField references in the Inspector:

| Field                   | Drag from hierarchy          |
|-------------------------|------------------------------|
| `_heroSelectScreen`     | HeroSelectScreen object      |
| `_planningScreen`       | PlanningScreen object        |
| `_resultScreen`         | ResultScreen object          |
| `_hud`                  | HUD object                   |
| `_executionController`  | ExecutionController object   |
| `_gridView`             | GridView object              |
| `_cameraController`     | CameraController object      |
| `_defaultMap`           | MapConfig ScriptableObject   |
| `_offlineMode`          | ☑ checked for local testing  |

### 4. GridView

1. Create empty GameObject → rename `GridView`.
2. Attach `GridView.cs`.
3. Assign tile prefabs and materials in Inspector:
   - `_floorTilePrefab` — a 1×1 plane or quad
   - `_wallTilePrefab` — a cube scaled to tile height
   - `_spawnHighlightMaterial`, `_moveHighlightMaterial`, `_shootHighlightMaterial`

### 5. ExecutionController

1. Create empty GameObject → rename `ExecutionController`.
2. Attach `ExecutionController.cs`.
3. Wire Inspector references:
   - `_gridView` → the GridView object
   - `_vfxManager` → the VFXManager object
   - `_cameraController` → the CameraController object

### 6. CameraController

1. Select the Main Camera (or create one).
2. Rename to `CameraController`.
3. Attach `CameraController.cs`.
4. Configure camera:
   - **Projection**: Perspective
   - **Position**: elevated angle (e.g., Y=12, Z=-8)
   - **Rotation**: ~50° X for isometric-like view
   - URP Camera settings as needed.

### 7. VFXManager

1. Create empty GameObject → rename `VFXManager`.
2. Attach `VFXManager.cs`.
3. Assign particle prefabs (muzzle flash, hit spark, armor break, elimination).

### 8. UI Canvas

1. **GameObject → UI → Canvas**.
2. Set Canvas Scaler:
   - **UI Scale Mode**: Scale With Screen Size
   - **Reference Resolution**: 1080 × 1920 (portrait) or 1920 × 1080 (landscape)
   - **Match**: 0.5
3. Create child GameObjects for each screen:

#### HeroSelectScreen
- Attach `HeroSelectScreen.cs`.
- Build UI: hero grid/scroll, stats panel, confirm button, 3D preview area.
- Assign `_heroCatalog` array with all `HeroConfig` ScriptableObjects.

#### PlanningScreen
- Attach `PlanningScreen.cs`.
- Build UI: action queue slots (6), action buttons, timer text, confirm button.
- Assign action icon sprites for `_actionIcons` dictionary.

#### ResultScreen
- Attach `ResultScreen.cs`.
- Build UI: winner text, round score, hero portraits, rematch/menu buttons.

#### HUD
- Attach `HUD.cs`.
- Build UI: player HP bars, armor indicators, round counter, hero portraits, step counter.

4. Add **EventSystem** if not already present.

### 9. Lighting

1. Keep the default Directional Light.
2. Adjust rotation for nice shadows on the grid.
3. Optionally add a URP Post-Processing Volume for bloom/vignette.

### 10. ScriptableObject Assets

Ensure these exist in `Assets/ScriptableObjects/`:

| Asset              | Location                          | Notes                                    |
|--------------------|-----------------------------------|------------------------------------------|
| Hero configs (×4)  | `ScriptableObjects/Heroes/`       | Archer, Tank, Shadow, Scout (MVP)        |
| MapConfig (×1+)    | `ScriptableObjects/Maps/`         | At least one default map                 |

Drag the default MapConfig into `GameManager._defaultMap`.

---

## Testing Offline Mode

1. Set `GameManager._offlineMode = true` in Inspector.
2. Enter Play Mode.
3. Expected flow:
   - Hero select screen appears → pick heroes for P1 and P2 → Confirm
   - Planning screen (P1) → program actions → Confirm
   - Planning screen (P2) → program actions → Confirm
   - Execution playback (step-by-step)
   - If round not decisive → next planning phase
   - After decisive round or 3 rounds → Result screen
   - Rematch → loops back; Main Menu → hero select

### Quick Smoke Test Checklist

- [ ] Hero select shows all 4 MVP heroes with correct stats
- [ ] Planning screen shows correct number of action slots
- [ ] Action buttons respond to taps, undo works
- [ ] Confirm sends actions, screen transitions
- [ ] Execution plays step animations
- [ ] Round results display correctly
- [ ] Rematch resets match state

---

## Testing Online Mode

1. Set `GameManager._offlineMode = false`.
2. Requires a running NestJS server with Socket.IO.
3. Call `GameManager.StartOnlineGame(socketClient)` from a menu/lobby screen.
4. Expected flow:
   - Matchmaking → server finds opponent
   - Hero select → pick hero
   - Planning → program actions → Commit hash → Wait for opponent
   - Both committed → Reveal actions → Server sends results
   - Execution playback
   - Repeat or end match

---

## Common Issues

| Problem                          | Solution                                                   |
|----------------------------------|------------------------------------------------------------|
| Null reference on Start          | Ensure all SerializeField references are wired in Inspector|
| No heroes in select screen       | Assign `HeroConfig` assets to `_heroCatalog` array         |
| Grid not rendering               | Check `GridView` prefab assignments and MapConfig data     |
| Camera wrong angle               | Adjust CameraController position/rotation                  |
| UI not responding                | Verify EventSystem exists in hierarchy                     |
| Execution doesn't play           | Check `ExecutionController` references to GridView/VFX     |
