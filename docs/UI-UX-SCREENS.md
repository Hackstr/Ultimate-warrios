# Tactical Duelist — UI/UX: Экраны, состояния, переходы

## Карта экранов (Screen Map)

```
                    ┌─────────────┐
                    │   SPLASH    │
                    │  (loading)  │
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │  MAIN MENU  │◄──────────────────────┐
                    │             │                        │
                    └──┬───┬───┬──┘                        │
                       │   │   │                           │
            ┌──────────┘   │   └──────────┐                │
            │              │              │                │
     ┌──────▼──────┐ ┌─────▼─────┐ ┌──────▼──────┐        │
     │ HERO SELECT │ │  PROFILE  │ │  SETTINGS   │        │
     │             │ │           │ │             │        │
     └──────┬──────┘ └───────────┘ └─────────────┘        │
            │                                              │
     ┌──────▼──────┐                                       │
     │ MATCHMAKING │                                       │
     │  (waiting)  │                                       │
     └──────┬──────┘                                       │
            │                                              │
     ┌──────▼──────┐                                       │
     │  MATCH HUD  │──┐                                   │
     │  (in-game)  │  │                                   │
     └──────┬──────┘  │                                   │
            │         │ Цикл: Planning → Execution        │
     ┌──────▼──────┐  │        ↓                          │
     │  PLANNING   │──┤  ┌───────────┐                    │
     │   PHASE     │  │  │ EXECUTION │                    │
     └─────────────┘  │  │   PHASE   │                    │
                      │  └─────┬─────┘                    │
                      │        │                           │
                      │  ┌─────▼─────┐                    │
                      └──│POST-ROUND │                    │
                         │  (brief)  │                    │
                         └─────┬─────┘                    │
                               │                           │
                        ┌──────▼──────┐                    │
                        │   RESULT    │────────────────────┘
                        │   SCREEN    │
                        └─────────────┘
```

---

## 1. SPLASH / LOADING

### Назначение
Первый экран при запуске. Загрузка ассетов, подключение к серверу.

### Элементы
```
┌──────────────────────────────────────┐
│                                      │
│          [GAME LOGO]                 │
│       "Tactical Duelist"             │
│                                      │
│         ████████░░ 80%               │
│      "Connecting to server..."       │
│                                      │
│         © 2026 Studio Name           │
└──────────────────────────────────────┘
```

### Состояния
- `Loading Assets` → прогресс-бар
- `Connecting` → текст меняется
- `Connected` → автопереход на Main Menu
- `Connection Failed` → кнопка "Retry" + "Play Offline"

### Длительность: 2-4 секунды

---

## 2. MAIN MENU

### Назначение
Хаб. Точка входа во все режимы.

### Элементы
```
┌──────────────────────────────────────┐
│  [Avatar] PlayerName    🪙 1250      │
│  ⭐ Rank: Gold III      💎 50        │
│──────────────────────────────────────│
│                                      │
│          ╔══════════════╗            │
│          ║              ║            │
│          ║  [3D Hero    ║            │
│          ║   Preview]   ║            │
│          ║              ║            │
│          ╚══════════════╝            │
│                                      │
│      ┌──────────────────────┐        │
│      │     ▶  PLAY          │        │  ← Большая главная кнопка
│      └──────────────────────┘        │
│                                      │
│  [Ranked]  [Casual]  [vs Friend]     │  ← Режимы (MVP: только Casual)
│                                      │
│──────────────────────────────────────│
│  [🏠Home] [🎭Heroes] [🏆Rank] [⚙️]  │  ← Нижняя навигация
└──────────────────────────────────────┘
```

### Кнопки и действия
| Элемент | Действие |
|---------|----------|
| PLAY | → Hero Select |
| Avatar/PlayerName | → Profile |
| Heroes (nav) | → Hero Collection (v1.0) |
| Rank (nav) | → Leaderboard (v1.0) |
| Settings (⚙️) | → Settings |
| Ranked/Casual/Friend | Выбор режима (MVP: только Casual) |

### Состояния
- `Default` — как описано выше
- `Returning from match` — показать +XP, +rank анимацию, затем default

---

## 3. HERO SELECT

### Назначение
Выбор героя перед матчем. Ключевой экран для принятия решения.

### Элементы
```
┌──────────────────────────────────────┐
│  ← Back              CHOOSE HERO     │
│──────────────────────────────────────│
│                                      │
│  ┌────────────────────────────────┐  │
│  │                                │  │
│  │      [3D Hero Preview]        │  │  ← Выбранный герой крутится
│  │      на арене/пьедестале      │  │
│  │                                │  │
│  └────────────────────────────────┘  │
│                                      │
│  ┌──────────────────────────────┐    │
│  │ ARCHER        ★★★☆☆         │    │
│  │ Steps:4  Range:8  CD:2      │    │  ← Stat bar
│  │ Armor:0  Speed:1            │    │
│  │ Special: Ricochet           │    │
│  │ "Long-range sniper..."      │    │
│  └──────────────────────────────┘    │
│                                      │
│  [🏹] [🛡️] [🗡️] [📡]              │  ← Иконки героев (свайп)
│  Archer Tank Shadow Scout            │
│                                      │
│      ┌──────────────────────┐        │
│      │     SELECT           │        │  ← Подтверждение выбора
│      └──────────────────────┘        │
└──────────────────────────────────────┘
```

### Компоненты карточки героя
```
┌─────────────────────────┐
│ [Portrait]  HERO NAME   │
│             ★★★☆☆       │  ← Сложность (1-5 звёзд)
│                         │
│ Steps  ████████░░  4    │  ← Stat bars (визуальные)
│ Range  ████████████ 8   │
│ CD     ████░░░░░░  2    │
│ Armor  ░░░░░░░░░░  0    │
│ Speed  ██░░░░░░░░  1    │
│                         │
│ ⚡ Ricochet              │  ← Special ability
│ Shot bounces off walls  │
└─────────────────────────┘
```

### Состояния
- `Browsing` — листает героев, видит статы
- `Selected` — кнопка SELECT активна
- `Locked` — герой недоступен (v1.0, unlock system)
- `Loading Match` — после SELECT → matchmaking

### Pass-and-play (MVP):
```
P1 выбирает → экран "Pass to Player 2" → P2 выбирает → матч
```

---

## 4. MATCHMAKING (Online, v0.2+)

### Элементы
```
┌──────────────────────────────────────┐
│                                      │
│         FINDING OPPONENT...          │
│                                      │
│     [Player Avatar]   VS   [???]     │
│     "PlayerName"           "..."     │
│     🏹 Archer                        │
│                                      │
│         ⏱ 0:05                       │
│         ●●●○○ (searching animation)  │
│                                      │
│      ┌──────────────────────┐        │
│      │      CANCEL          │        │
│      └──────────────────────┘        │
└──────────────────────────────────────┘
```

### Состояния
- `Searching` — анимация поиска, таймер
- `Found` — оба аватара видны, 3с countdown → переход в матч
- `Timeout` — "No opponents found. Try again?"
- `Cancelled` → Main Menu

---

## 5. MATCH HUD (In-Game)

### Назначение
Постоянный overlay во время матча. Показывает ключевую информацию.

### Элементы (всегда видны)
```
┌──────────────────────────────────────┐
│ [P1 Avatar] ♥  🛡️   R:1/3  🛡️ ♥ [P2]│  ← Верхняя панель
│ "Player1"   ●       ●       "Player2"│
│──────────────────────────────────────│
│                                      │
│                                      │
│          [GAME FIELD]                │
│          [3D Grid]                   │
│                                      │
│                                      │
│──────────────────────────────────────│
│         (context-dependent           │
│          bottom panel)               │
└──────────────────────────────────────┘
```

### Верхняя панель (Top Bar)
```
┌──────────────────────────────────────┐
│ [Ava] Name   ♥🛡️  ROUND 1/3  🛡️♥  Name [Ava] │
└──────────────────────────────────────┘

♥ = alive (красное сердце) / eliminated (серое)
🛡️ = armor intact (синий щит) / broken (серый) / none (пусто)
ROUND = текущий раунд из максимума
```

---

## 6. PLANNING PHASE

### Назначение
Самый важный UI — здесь игрок программирует действия.

### Layout
```
┌──────────────────────────────────────┐
│ [P1] ♥🛡️     ROUND 1     🛡️♥ [P2]  │
│──────────────────────────────────────│
│                                      │
│    [3D Grid — уменьшенный вид]       │
│    Показывает: позиции обоих героев  │
│    Направление взгляда (стрелка)     │
│    Стены, пикапы                     │
│                                      │
│──────────────────────────────────────│
│  ⏱ 25                               │  ← Таймер (мигает < 5с)
│──────────────────────────────────────│
│                                      │
│  ACTIONS:                            │
│  ┌────┐┌────┐┌────┐┌────┐┌────┐     │  ← Кнопки действий
│  │ ➡️ ││ ↰  ││ ↱  ││ ↩️ ││ 🎯 │     │
│  │Move││TrnL││TrnR││Turn││Shot│     │
│  └────┘└────┘└────┘└────┘└────┘     │
│                    ┌────┐┌────┐      │
│                    │ ⏸ ││ ⚡ │      │
│                    │Wait││Spec│      │
│                    └────┘└────┘      │
│                                      │
│  QUEUE: [Step 1][Step 2][Step 3][ ]  │  ← Очередь действий
│          ➡️       🎯      ↰     ░░   │
│                                      │
│  [↩️ Undo]              [✅ Confirm] │
└──────────────────────────────────────┘
```

### Action Queue (Очередь действий)

```
Пустая:
  [ ░░ ][ ░░ ][ ░░ ][ ░░ ]     ← 4 слота (= hero.Steps)
  пунктирные рамки

Заполняется:
  [ ➡️ ][ 🎯 ][ ↰  ][ ░░ ]    ← 3/4 запланировано
    1      2      3    пусто

Полная:
  [ ➡️ ][ 🎯 ][ ↰  ][ ➡️ ]    ← 4/4, кнопка CONFIRM активна
    1      2      3      4
```

### Состояния кнопок действий

```
Move      — всегда активна (зелёная)
TurnLeft  — всегда активна (голубая)
TurnRight — всегда активна (голубая)
TurnAround — всегда активна (голубая)
Shoot     — активна если CD = 0 в текущей позиции очереди (красная)
           — неактивна если CD > 0 (серая, показывает число CD)
Wait      — всегда активна (серая)
Special   — активна если не использован в этом раунде (золотая)
           — неактивна если уже в очереди (серая)
           — MVP: скрыта (нет спешлов)
```

### Предпросмотр на сетке

Во время планирования, при добавлении действий, на сетке
показывается ПРЕДПРОСМОТР пути героя:

```
Герой стоит на (2,2), смотрит Up.
Очередь: [Move, Shoot, TurnRight, Move]

На сетке отображается:
  (2,2) → [сплошная стрелка] → (2,3) — позиция после шага 1
  (2,3) → [красный луч вверх 8 тайлов] — выстрел на шаге 2
  (2,3) → [поворот стрелки направо] — поворот на шаге 3
  (2,3) → [сплошная стрелка] → (3,3) — позиция после шага 4

Путь показан прозрачным цветом героя.
Финальная позиция = яркий маркер.
```

### Cooldown индикатор в очереди

```
Archer (CD=2). Стреляет на шаге 1:
  [ 🎯 ][ 🔒2 ][ 🔒1 ][ 🎯? ]
   shoot   CD=2    CD=1   can shoot!
   
Числа кулдауна показаны НА слоте,
кнопка Shoot серая пока CD > 0.
```

### Таймер

```
> 10с:  белый, спокойный
5-10с:  жёлтый, начинает пульсировать
< 5с:   красный, мигает, тикающий звук
0с:     автосабмит (незаполненные слоты = Wait)
```

### Pass-and-play: между игроками

```
P1 заканчивает → кнопка CONFIRM →

┌──────────────────────────────────────┐
│                                      │
│                                      │
│     📱 PASS DEVICE TO PLAYER 2      │
│                                      │
│          [Tap to continue]           │
│                                      │
│  (скрывает все данные P1)            │
│                                      │
└──────────────────────────────────────┘

→ Tap → P2 видит PLANNING PHASE
```

---

## 7. EXECUTION PHASE

### Назначение
Анимированный проигрыш результатов раунда. Зрелищный момент.

### Layout
```
┌──────────────────────────────────────┐
│ [P1] ♥🛡️     ROUND 1     🛡️♥ [P2]  │
│──────────────────────────────────────│
│                                      │
│                                      │
│        [3D Grid — полный вид]        │
│        Камера: dynamic, следит       │
│        за действием                  │
│                                      │
│                                      │
│──────────────────────────────────────│
│  Step: [●][●][○][○][○][○]   ▶ 1x    │  ← Прогресс + speed
│         1   2  3  4  5  6           │
│                                      │
│  P1: ➡️ Move      P2: 🎯 Shoot      │  ← Текущие действия
└──────────────────────────────────────┘
```

### Step Progress Bar
```
Шаг 1 из 6:
  [●][○][○][○][○][○]
   ↑
  текущий

Шаг 3 из 6:
  [●][●][●][○][○][○]

Завершено:
  [●][●][●][●][●][●]
```

### Speed Controls
```
[▶ 1x]  — нормальная скорость (0.8с на шаг)
[▶▶ 2x] — быстро (0.4с на шаг)
[⏸ ||]  — пауза (пошаговый режим, tap = следующий шаг)
```

### Подписи действий (под сеткой)
```
Показывают что делает каждый игрок на текущем шаге:
  P1: ➡️ Move          P2: ↰ Turn Left
  P1: 🎯 Shoot         P2: ➡️ Move
  P1: ⏸ Wait           P2: 🎯 Shoot    ← P2 выстрелил!
```

### Особые моменты анимации

```
ПОПАДАНИЕ (Hit):
  - Camera slight zoom
  - Screen shake (лёгкий)
  - Slow-motion 0.3с
  - VFX вспышка на тайле
  - SFX удар
  - Если armor: осколки щита + armor icon ломается
  - Если kill: explosion + death anim + "ELIMINATED!" текст

MUTUAL CANCEL:
  - Оба луча летят → встречаются → вспышка столкновения
  - Текст "CANCELED!" по центру (0.5с)
  - Оба луча рассеиваются
  - Специальный VFX (не просто два хита)

ПРОМАХ:
  - Луч пролетает мимо
  - Тихий "woosh" звук
  - Нет замедления
```

---

## 8. POST-ROUND (между раундами)

### Если нет elimination

```
┌──────────────────────────────────────┐
│                                      │
│          ROUND 1 COMPLETE            │
│          No elimination              │
│                                      │
│     [P1 Hero]   VS   [P2 Hero]      │
│       ♥ 🛡️              ♥           │
│                                      │
│   ⚠️ MAP SHRINKING                   │
│   Danger zone expanding...           │
│   (анимация: края карты краснеют)    │
│                                      │
│        ROUND 2 STARTING...           │
│            3... 2... 1...            │
│                                      │
└──────────────────────────────────────┘
```

### Длительность: 3-5 секунд, автопереход

---

## 9. RESULT SCREEN

### Победа

```
┌──────────────────────────────────────┐
│                                      │
│          ⭐ VICTORY! ⭐              │
│                                      │
│     [3D Winner Hero — Victory Pose]  │
│                                      │
│     ┌──────────────────────────┐     │
│     │ [P1 Ava] 🏹 Archer   W  │     │
│     │ [P2 Ava] 🛡️ Tank     L  │     │
│     │                          │     │
│     │ Round 2 — Elimination    │     │
│     │ Duration: 1:42           │     │
│     └──────────────────────────┘     │
│                                      │
│  +25 XP  ████████░░ 245/300         │  ← XP bar
│  +15 🏆  Rating: 1265 (+15)         │  ← Rating change
│                                      │
│  [🔄 Rematch]     [🏠 Menu]         │
│                [📹 Replay]           │
└──────────────────────────────────────┘
```

### Поражение

```
┌──────────────────────────────────────┐
│                                      │
│           DEFEAT                     │
│                                      │
│     [3D Loser Hero — Defeat Pose]    │
│                                      │
│     (та же инфа, но настроение       │
│      другое: серые тона, без звёзд)  │
│                                      │
│  +10 XP  ████████░░ 235/300         │
│  -10 🏆  Rating: 1240 (-10)         │
│                                      │
│  [🔄 Rematch]     [🏠 Menu]         │
│                [📹 Replay]           │
└──────────────────────────────────────┘
```

### Ничья

```
          DRAW
  [P1 Hero]   [P2 Hero]
  оба стоят, нейтральные позы
  
  +15 XP, 0 rating change
```

### Кнопки
| Кнопка | Действие |
|--------|----------|
| Rematch | → Matchmaking (same heroes) |
| Menu | → Main Menu |
| Replay | → Replay viewer (v1.0) |

---

## 10. SETTINGS

```
┌──────────────────────────────────────┐
│  ← Back             SETTINGS        │
│──────────────────────────────────────│
│                                      │
│  SOUND                               │
│  Music         [████████░░] 80%      │
│  SFX           [██████████] 100%     │
│  Haptics       [ON] / OFF           │
│                                      │
│  GAME                                │
│  Language      [English ▼]           │
│  Replay Speed  [1x] [2x]            │
│                                      │
│  ACCOUNT                             │
│  Telegram ID   @username             │
│  Player ID     #12345                │
│                                      │
│  [Privacy Policy]  [Terms]           │
│  [Support / Bug Report]              │
│                                      │
│  v0.1.0-mvp                         │
└──────────────────────────────────────┘
```

---

## 11. PROFILE (v1.0)

```
┌──────────────────────────────────────┐
│  ← Back             PROFILE         │
│──────────────────────────────────────│
│                                      │
│  [Avatar]  PlayerName                │
│            ⭐ Gold III  🏆 1265      │
│            📊 142W / 98L (59%)       │
│                                      │
│  HERO MASTERY                        │
│  🏹 Archer   ████████ Lv.8          │
│  🛡️ Tank     ██████░░ Lv.6          │
│  🗡️ Shadow   ████░░░░ Lv.4          │
│  📡 Scout    ██░░░░░░ Lv.2          │
│                                      │
│  RECENT MATCHES                      │
│  [W] Archer vs Tank    1:32  📹     │
│  [L] Shadow vs Mage    2:01  📹     │
│  [W] Scout vs Scout    0:58  📹     │
│                                      │
└──────────────────────────────────────┘
```

---

## Переходы между экранами (Transitions)

### Таблица переходов

```
FROM              →  TO                TRIGGER              TRANSITION
─────────────────────────────────────────────────────────────────────
Splash            →  Main Menu         Assets loaded         Fade
Main Menu         →  Hero Select       Tap PLAY              Slide Right
Main Menu         →  Settings          Tap ⚙️                Slide Up
Main Menu         →  Profile           Tap Avatar            Slide Right
Hero Select       →  Main Menu         Tap Back              Slide Left
Hero Select       →  Matchmaking       Tap SELECT            Fade
Hero Select       →  Pass Device       SELECT (pass-n-play)  Fade
Matchmaking       →  Main Menu         Tap CANCEL            Fade
Matchmaking       →  Match HUD         Match found           Zoom In
Match HUD         →  Planning Phase    Round starts          Slide Up (panel)
Planning Phase    →  Pass Device       P1 Confirm (PnP)      Fade (instant)
Pass Device       →  Planning Phase    Tap                   Fade (instant)
Planning Phase    →  Execution Phase   Both confirmed        Slide Down (panel)
Execution Phase   →  Post-Round        Round ends, no kill   Fade
Execution Phase   →  Result Screen     Elimination           Slow-mo → Fade
Post-Round        →  Planning Phase    Auto (3s)             Fade
Result Screen     →  Main Menu         Tap Menu              Slide Left
Result Screen     →  Matchmaking       Tap Rematch           Fade
Settings          →  Main Menu         Tap Back              Slide Down
Profile           →  Main Menu         Tap Back              Slide Left
```

### Анимация переходов

```
Fade:        0.3s alpha 1→0 → swap → 0→1
Slide Right: 0.3s, новый экран едет справа
Slide Left:  0.3s, экран уезжает вправо
Slide Up:    0.3s, панель выезжает снизу (planning UI)
Slide Down:  0.3s, панель уезжает вниз
Zoom In:     0.5s, камера приближается к арене (matchmaking → game)
```

---

## MVP vs Full — какие экраны нужны когда

```
🔴 MVP (pass-and-play):
  ✓ Hero Select (simplified — no matchmaking)
  ✓ Pass Device Screen
  ✓ Planning Phase
  ✓ Execution Phase
  ✓ Result Screen
  ✓ Basic HUD (top bar)

🟡 v0.2 (online):
  + Splash / Loading
  + Main Menu
  + Matchmaking
  + Post-Round (map shrink visual)
  + Settings (basic)

🟢 v1.0 (full):
  + Profile
  + Hero Collection
  + Leaderboard
  + Replay Viewer
  + Battle Pass
  + Shop
  + Club / Social
  + Tutorial / Onboarding
```
