# UI Design Prompt — Tactical Duelist: Mind Games Arena

> Use this prompt with Galileo AI, v0.dev, Figma AI, or any UI design tool.
> Target: Mobile portrait (1080x1920), dark gaming theme, Telegram Mini App.

---

## CONTEXT

Design the complete UI for a **1v1 tactical mobile game** called "Tactical Duelist". Players secretly program a sequence of actions (move, turn, shoot), then watch them execute simultaneously. Think Chess meets programming — every decision matters, no randomness.

**Platform**: Mobile portrait (9:16), primarily played inside Telegram as a Mini App.
**Theme**: Dark sci-fi/cyberpunk. Deep navy/charcoal backgrounds, neon accent colors.
**Feel**: Clean, readable, competitive. Information density is medium — not cluttered, but all critical info visible.

**Color palette**:
- Background: #0E0E18 (deep navy-black)
- Panel: #1A1A28 (dark card)
- Card: #222233 (slightly lighter)
- Primary accent: #FF6B35 (orange — main CTA buttons)
- Blue accent: #3388FF (Player 1 / movement)
- Red accent: #FF3344 (Player 2 / shoot / danger)
- Gold accent: #FFD633 (selected / highlight / special)
- Purple accent: #8844CC (special ability)
- Green accent: #33CC66 (alive / success)
- Text white: #F0F0FF
- Text gray: #8888AA
- Text dim: #555570

**Typography**: Bold sans-serif (e.g., Inter, Rajdhani, Orbitron for headers).
- Page titles: 28-32px bold
- Section headers: 20-24px semibold
- Body: 16-18px regular
- Labels/captions: 12-14px
- Minimum touch target: 48x48px

---

## SCREEN 1: MAIN MENU

**Purpose**: Hub. Entry point to all modes.

```
┌──────────────────────────────────────┐
│                                      │
│        TACTICAL DUELIST              │  ← Game logo, 32px bold gold
│        Mind Games Arena              │  ← 16px gray subtitle
│                                      │
│   ┌──────────────────────────────┐   │
│   │                              │   │
│   │    [3D Hero Preview Area]    │   │  ← 40% of screen, dark panel
│   │    Hero rotates slowly       │   │     Shows last-used hero
│   │                              │   │
│   └──────────────────────────────┘   │
│                                      │
│    ┌──────────────────────────┐      │
│    │      ▶  PLAY OFFLINE     │      │  ← Orange primary button, full width
│    └──────────────────────────┘      │     48px height, rounded 12px
│                                      │
│    ┌──────────────────────────┐      │
│    │      ▶  PLAY ONLINE      │      │  ← Blue outline button
│    └──────────────────────────┘      │
│                                      │
│           v0.3.0                     │  ← Version, 12px gray, bottom
│                                      │
└──────────────────────────────────────┘
```

**States**: Default (as above), Returning from match (show +XP animation then default).

---

## SCREEN 2: HERO SELECT

**Purpose**: Choose your hero before the match. Critical decision-making screen.

**Layout** (top to bottom):

```
┌──────────────────────────────────────┐
│  [< BACK]          PLAYER 1         │  ← Header: 56px, back button left
│                  CHOOSE HERO         │     Title centered, 24px bold
│──────────────────────────────────────│
│                                      │
│  ┌─┐ ┌─┐ ┌─┐ ┌─┐ ┌─┐ ┌─┐ ┌─┐ ┌─┐  │  ← Hero roster: horizontal scroll
│  │🏹│ │🛡│ │🗡│ │📡│ │✦│ │💣│ │⚡│ │👻│  │     Each card: 100x120px
│  │Ar│ │Tn│ │Sh│ │Sc│ │Mg│ │Dm│ │Bs│ │Gh│  │     Selected = gold border + glow
│  └─┘ └─┘ └─┘ └─┘ └─┘ └─┘ └─┘ └─┘  │     Normal = gray border
│──────────────────────────────────────│
│                                      │
│         ARCHER                       │  ← Hero name: 36px bold gold
│         ★★★☆☆                        │  ← Difficulty: 5 dots (filled=gold, empty=gray)
│                                      │
│  Special: Ricochet                   │  ← 20px bold blue
│  Shot bounces off one wall,          │  ← 14px gray description
│  changing direction.                 │
│                                      │
│  Steps    ████████░░░░  4            │  ← Stat bars: colored fill + value
│  Range    ████████████████  8        │     Steps=blue, Range=green
│  Cooldown ████████░░░░  2            │     Cooldown=red, Armor=gold
│  Armor    ░░░░░░░░░░░░  0            │     Speed=purple
│  Speed    ████░░░░░░░░  1            │     Bar height: 24px, rounded
│                                      │
│  ┌──────────────────────────────┐    │
│  │                              │    │
│  │    [3D Hero Preview]         │    │  ← Hero model on pedestal
│  │    Dark panel background     │    │     Rotates slowly
│  │                              │    │
│  └──────────────────────────────┘    │
│                                      │
│    ┌──────────────────────────┐      │
│    │       SELECT             │      │  ← Orange CTA, 56px height
│    └──────────────────────────┘      │     Pass-and-play: changes to
│                                      │     "START MATCH" for Player 2
└──────────────────────────────────────┘
```

**Hero roster cards** (scrollable row):
- Each card: 100x120px, dark card background
- Top 70%: Hero portrait/icon area (placeholder = colored silhouette)
- Bottom 30%: Hero name (14px bold white)
- Selected state: Gold border (3px), background brightens, slight scale-up
- Normal state: Gray border (1px), dark background
- Touch target: entire card is tappable

**Stat bars**:
- Each bar: full width row [Label 80px] [Bar flexible] [Value 40px]
- Bar fill: colored, rounded ends
- Max values for normalization: Steps/6, Range/10, Cooldown/3, Armor/1, Speed/2

**Pass-and-play flow**:
1. Player 1 selects → taps SELECT
2. Full-screen overlay: "PASS DEVICE TO PLAYER 2" + "Tap to continue" button
3. Player 2 sees same screen, title says "PLAYER 2 — CHOOSE HERO"
4. Player 2 selects → taps "START MATCH" → game begins

---

## SCREEN 3: MATCH HUD (always visible during game)

**Purpose**: Persistent top bar showing match state. Visible during Planning and Execution.

```
┌──────────────────────────────────────┐
│ [P1 Ava] Name  0 🛡♥ R:1 ♥🛡 0 Name [P2 Ava] │
└──────────────────────────────────────┘
```

**Detailed layout** (horizontal, 56px height):

```
┌──────────────────────────────────────┐
│ ┌──┐                          ┌──┐  │
│ │P1│ Archer   0  🛡♥  R1  ♥🛡  0  Archer │P2│  │
│ │  │ (blue)        ││       ││     (red)│  │  │
│ └──┘               ││       ││         └──┘  │
└──────────────────────────────────────┘
```

- P1 avatar: 40x40px, blue border frame
- P2 avatar: 40x40px, red border frame
- Hero name: 14px bold
- Score: 16px bold (kills this match)
- Armor icon: shield icon, bright if intact, dim/cracked if broken, hidden if armor=0
- Health icon: heart, green if alive, red/gray if eliminated
- Round indicator: "R1" / "R2" / "R3" center, 14px bold

---

## SCREEN 4: PLANNING PHASE (most important screen)

**Purpose**: Player programs their action sequence. This is where the game is played.

**Layout**:

```
┌──────────────────────────────────────┐
│ [========= HUD TOP BAR =========]   │  ← 56px, see Screen 3
│──────────────────────────────────────│
│                                      │
│                                      │
│         [3D GRID VIEW]               │  ← Flexible height, ~50% of screen
│                                      │     Shows: grid, walls, both heroes
│      Blue hero (you) faces →         │     Your hero = blue, opponent = red
│                     Red hero (them)  │     Direction arrows visible
│                                      │     Transparent overlay allowed
│                                      │
│──────────────────────────────────────│
│  ┌──────────────────────────────┐    │  ← Bottom panel: dark, opaque
│  │                              │    │     ~45% of screen
│  │  ⏱ 25  [①▶][②?][③·][④·]    │    │  ← Timer (red badge) + Queue (4 slots)
│  │                              │    │
│  │  [MOVE] [<TURN] [TURN>] [180]│    │  ← Row 1: movement actions
│  │  blue   yellow  yellow  yellow│    │     Color-coded by type
│  │                              │    │
│  │  [SHOOT]  [WAIT]  [SPECIAL]  │    │  ← Row 2: combat actions
│  │   red      gray    purple    │    │
│  │                              │    │
│  │  [UNDO]      [CONFIRM 2/4]  │    │  ← Row 3: control buttons
│  │   gray        orange         │    │     CONFIRM shows count progress
│  │                              │    │
│  └──────────────────────────────┘    │
└──────────────────────────────────────┘
```

### Action Queue (horizontal strip)

```
Empty (start of planning):
  ⏱25  [ ① ? ][ ② · ][ ③ · ][ ④ · ]
        blue    dim     dim     dim
        "next"  empty   empty   empty

After 2 actions (Move, Shoot):
  ⏱22  [ ① ▶ ][ ② ✕ ][ ③ ? ][ ④ · ]
        blue    red     blue    dim
        MOVE    SHOOT   "next"  empty

Full queue (ready to confirm):
  ⏱18  [ ① ▶ ][ ② ✕ ][ ③ ◀ ][ ④ ▶ ]
        blue    red     yellow  blue
        MOVE    SHOOT   TURN<   MOVE
```

**Queue slot states**:
- **Empty**: Dim background (#333345, 0.5 alpha), "·" icon, no label
- **Next** (first empty): Highlighted border (blue pulse), "?" icon, "Choose action..."
- **Filled**: Colored background matching action type, action icon, short label
- **Cooldown locked**: Red-tinted background, "CD:2" text, locked icon
- Each slot: ~56x48px, rounded 8px

**Action colors**:
- MOVE: Blue (#2266AA)
- TURN LEFT / RIGHT / AROUND: Yellow (#8B8B2B)
- SHOOT: Red (#CC3333)
- WAIT: Gray (#444455)
- SPECIAL: Purple (#7733AA)

### Action Buttons

Two rows of buttons, color-coded to match queue slot colors:

Row 1 (movement): `[MOVE] [< TURN] [TURN >] [180]`
- MOVE: Blue background, white text, 18px bold
- TURNs: Yellow-brown background, white text, 16px
- All: flexible width, 52px height, 6px gap

Row 2 (combat): `[SHOOT] [WAIT] [SPECIAL]`
- SHOOT: Red background, white bold text, 20px
  - When on cooldown: dark overlay + red "CD:2" text
- WAIT: Dark gray background, 18px
- SPECIAL: Purple background, white bold text, 20px
  - When locked: dark overlay + "LOCKED" text

Row 3 (controls): `[UNDO] [===== CONFIRM =====]`
- UNDO: Dark gray, 20px, flexible width 1x
- CONFIRM: Orange primary color, bold 22px, flexible width 2x
  - Disabled state: shows "2/4" (actions done / total needed)
  - Enabled state: shows "CONFIRM", full orange, pulse animation

### Timer

- Red badge: 48x48px square, rounded 8px
- Number: 22px bold white centered
- Timer > 10s: normal red (#CC4433)
- Timer 5-10s: yellow (#CCAA33), starts pulsing
- Timer < 5s: bright red (#FF2222), pulsing scale animation
- Timer = 0: auto-submit (unfilled slots become Wait)

### Path Preview on Grid (during planning)

When actions are queued, show a ghost path on the 3D grid:
- Transparent hero-colored tiles showing where hero will be after each step
- Move = arrow → next tile highlighted
- Shoot = red line extending from hero's position in facing direction (range tiles)
- Turn = rotation indicator on current tile
- Final position = bright marker (brighter than path tiles)

### Pass-and-play overlay

After Player 1 confirms:
```
┌──────────────────────────────────────┐
│                                      │
│       (full black overlay 85%)       │
│                                      │
│     PASS THE DEVICE                  │  ← 32px bold white
│     TO PLAYER 2                      │
│                                      │
│    ┌──────────────────────────┐      │
│    │    TAP TO CONTINUE       │      │  ← Orange button, 24px bold
│    └──────────────────────────┘      │
│                                      │
└──────────────────────────────────────┘
```

---

## SCREEN 5: EXECUTION PHASE

**Purpose**: Animated playback of both players' actions, step by step.

```
┌──────────────────────────────────────┐
│ [========= HUD TOP BAR =========]   │
│──────────────────────────────────────│
│                                      │
│                                      │
│                                      │
│         [3D GRID VIEW]               │  ← Full-screen grid, dynamic camera
│         Camera follows action        │     Zooms on hits, shakes on damage
│                                      │
│                                      │
│                                      │
│──────────────────────────────────────│
│                                      │
│  Step: [●][●][○][○]   3/4           │  ← Progress dots + counter
│                                      │     ● = completed, ○ = pending
│  P1: ▶ MOVE        P2: ✕ SHOOT      │  ← Current step actions for each player
│                                      │     Color-coded: blue for P1, red for P2
│  [1x]  [2x]  [⏸]                   │  ← Speed controls (optional, v1.0)
│                                      │
└──────────────────────────────────────┘
```

**Special visual moments** (overlay text during execution):
- Hit landed: "HIT!" text appears (red, 48px bold, fades out 0.5s)
- Armor broken: "ARMOR BROKEN!" (orange, 36px)
- Elimination: "ELIMINATED!" (red, 56px bold, screen shakes)
- Mutual cancel: "CANCELED!" (yellow, 48px, both rays collide)
- Miss: no text, ray passes through empty space

---

## SCREEN 6: POST-ROUND (brief, between rounds)

**Purpose**: 3-5 second screen showing round result before next round starts.

```
┌──────────────────────────────────────┐
│                                      │
│                                      │
│        ROUND 1 COMPLETE              │  ← 36px bold white
│        No elimination                │  ← 20px gray
│                                      │
│   [P1 Hero]     VS     [P2 Hero]    │  ← Hero silhouettes/icons
│     ♥ 🛡              ♥             │     Status icons below each
│                                      │
│   ⚠ MAP SHRINKING                   │  ← Warning text (if shrink active)
│   Danger zone expanding...           │     16px yellow, with icon
│                                      │
│        ROUND 2 STARTING...           │  ← 24px white
│            3... 2... 1...            │  ← Countdown, large numbers
│                                      │
└──────────────────────────────────────┘
```

Auto-advances after 3-5 seconds. If elimination happened → go to Result Screen instead.

---

## SCREEN 7: RESULT SCREEN

**Purpose**: End of match. Show winner, stats, and options.

### Victory variant:

```
┌──────────────────────────────────────┐
│                                      │
│                                      │
│          ⭐ VICTORY! ⭐              │  ← 48px bold gold, glow effect
│       "Opponent eliminated"          │  ← 18px gray subtitle
│                                      │
│   ┌──────────────────────────────┐   │
│   │ [P1 Avatar]  VS  [P2 Avatar]│   │  ← Hero portraits/icons
│   │  Archer           Tank       │   │     Winner = larger + gold frame
│   │    W                L        │   │     Loser = smaller + dim
│   └──────────────────────────────┘   │
│                                      │
│          1  —  0                     │  ← Score, 48px bold
│                                      │
│   Round 1: P1 eliminated P2         │  ← Round details, 14px gray
│   Round 2: No kill                   │     One line per round
│                                      │
│                                      │
│    ┌──────────────────────────┐      │
│    │       REMATCH            │      │  ← Orange CTA
│    └──────────────────────────┘      │
│    ┌──────────────────────────┐      │
│    │       MAIN MENU          │      │  ← Gray secondary button
│    └──────────────────────────┘      │
│                                      │
└──────────────────────────────────────┘
```

### Defeat variant:
Same layout but:
- Title: "DEFEAT" (gray, no glow, no stars)
- Tone: darker, no celebration

---

## SCREEN 8: MATCHMAKING (Online mode)

```
┌──────────────────────────────────────┐
│                                      │
│       FINDING OPPONENT...            │  ← 28px bold white
│                                      │
│  ┌────┐                 ┌────┐       │
│  │ P1 │      VS         │ ?? │       │  ← P1 avatar vs "?" placeholder
│  │ You│                 │    │       │
│  └────┘                 └────┘       │
│  Archer                  ...         │  ← Hero name
│                                      │
│        ⏱ 0:12                        │  ← Search timer
│        ●●●○○ (pulsing dots)         │  ← Search animation
│                                      │
│    ┌──────────────────────────┐      │
│    │        CANCEL            │      │  ← Gray button
│    └──────────────────────────┘      │
│                                      │
└──────────────────────────────────────┘
```

When match found: opponent avatar appears, 3s countdown → transition to game.

---

## ALL 12 HEROES (for hero cards/portraits)

Design distinct visual identities for each hero. Each needs a unique silhouette/icon.

| # | Hero | Color | Icon concept | Steps | Range | CD | Armor | Speed | Difficulty |
|---|------|-------|-------------|-------|-------|----|-------|-------|------------|
| 1 | Archer | Green | Bow/crossbow | 4 | 8 | 2 | 0 | 1 | ★★★ |
| 2 | Tank | Steel blue | Heavy shield | 4 | 4 | 1 | 1 | 1 | ★★ |
| 3 | Shadow | Dark purple | Dagger/smoke | 6 | 3 | 1 | 0 | 2 | ★★★★ |
| 4 | Scout | Teal | Binoculars | 5 | 5 | 1 | 0 | 2 | ★★★ |
| 5 | Mage | Blue-violet | Staff/orb | 4 | 6 | 2 | 0 | 1 | ★★★★ |
| 6 | Demo | Orange | Bomb | 4 | 5 | 2 | 0 | 1 | ★★★ |
| 7 | Berserker | Crimson | Dual axes | 6 | 2 | 0 | 0 | 1 | ★★★★★ |
| 8 | Ghost | Pale cyan | Spectral cloak | 5 | 4 | 1 | 0 | 1 | ★★★★ |
| 9 | Hawk | Gold | Eagle/sniper | 3 | 10 | 3 | 0 | 1 | ★★★ |
| 10 | Engineer | Brown/brass | Wrench/turret | 4 | 5 | 2 | 0 | 1 | ★★★ |
| 11 | Guardian | White/silver | Tower shield | 4 | 5 | 2 | 1 | 1 | ★★ |
| 12 | Mirage | Pink/illusion | Mirror/shadow | 5 | 4 | 1 | 0 | 1 | ★★★★ |

---

## INTERACTION STATES SUMMARY

### Button states (all buttons):
- **Normal**: Colored background, white text
- **Pressed**: Slightly darker, scale down 95%
- **Disabled**: Gray background (#333345), gray text (#666680), no interaction
- **Highlighted/Selected**: Brighter background, gold border (2px)

### Common patterns:
- All panels: rounded corners 12px
- All buttons: rounded corners 8px, minimum 48px height
- Spacing system: 4, 8, 12, 16, 24, 32px
- Card shadows: subtle drop shadow (0, 2, 8, rgba(0,0,0,0.3))
- Screen transitions: 0.3s fade between screens

### Touch targets:
- All interactive elements: minimum 44x44px
- Action buttons: full width in their row, 52px height
- Hero cards: 100x120px
- Queue slots: 56x48px minimum

---

## DELIVERABLES

Please design all 8 screens listed above as separate frames/artboards at 1080x1920px resolution. Include:
1. All visual states described (empty, filled, disabled, highlighted)
2. Color-coded action buttons matching queue slot colors
3. Hero cards with distinct visual identities
4. Timer states (normal, warning, danger)
5. Planning screen with filled queue example and empty queue example
6. Execution screen showing a "HIT!" moment
7. Both Victory and Defeat result screen variants
8. Pass-device overlay
