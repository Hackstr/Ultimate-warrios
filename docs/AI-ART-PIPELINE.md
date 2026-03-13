# Tactical Duelist — AI Art Pipeline
## Полный пайплайн от идеи до Unity

> **Стек:** Nano Banana Pro (Gemini 3.0) → Tripo AI / Meshy → Blender (доработка) → Mixamo (риг) → Unity 6 URP
> **Стиль-якорь:** Brawl Stars × Into The Breach. Chibi 2.5-3 головы, low-poly stylized, "Smart Cartoon"

---

## ЭТАП 0: СТИЛЬ-ЯКОРЬ (сделать ОДИН раз)

### Цель
Сгенерить 3-5 изображений, которые станут эталоном стиля для всего проекта.
Всё остальное генерируется с этими картинками как референсом.

### Промпт для стиль-якоря (Nano Banana Pro)

```
ТЕКСТОВЫЙ ПРОМПТ (без загрузки изображения):

"Create a stylized 3D game character concept art sheet.
The character is a fantasy archer — slim build, large oversized bow,
hooded cloak, green and brown color palette.

Style requirements:
- Chibi proportions: head is 1/3 of body height (2.5 heads tall total)
- Clean low-poly 3D look with cel-shading
- Bright saturated colors on neutral dark gray background
- Similar aesthetic to Brawl Stars and Clash Mini characters
- Game-ready design, simple geometric shapes
- Front view and 3/4 isometric view side by side
- No background clutter, studio lighting"
```

### Как использовать
1. Сгенерить 3-4 варианта этим промптом
2. Выбрать лучший вариант с Амирханом
3. Сохранить как `style-anchor-v1.png`
4. ВСЕ дальнейшие генерации — загружать этот файл как референс стиля

---

## ЭТАП 1: КОНЦЕПТ-АРТЫ ГЕРОЕВ (Nano Banana Pro)

### Базовый шаблон промпта

Для каждого героя загружаем `style-anchor-v1.png` + текстовый промпт:

```
"Create a [CHARACTER] game character in the EXACT SAME art style
as the reference image. Match the proportions, shading, color saturation,
and level of detail precisely.

[CHARACTER DESCRIPTION]

Show front view and 3/4 isometric view on dark gray background.
Clean studio lighting, no background props."
```

---

### 🔴 ARCHER (Лучник) — MVP Hero #1

**Промпт (загрузить style-anchor + текст):**
```
"Create an archer character in the EXACT SAME art style as the reference image.

- Slim, vertical silhouette, 2.5 heads tall chibi proportions
- OVERSIZED bow (bow is 1.5x character height) — the defining element
- Hooded cloak, quiver on back with 3-4 visible arrows
- Color palette: forest green cloak, brown leather armor, gold trim
- Calm, calculating expression, one eye slightly hidden by hood
- Idle pose: bow lowered, arrow loosely nocked

Front view and 3/4 isometric view. Dark gray background."
```

**Итерации:**
- Вариант A: классический лесной рейнджер (Robin Hood)
- Вариант B: более tech/futuristic лук (energy string)
- Вариант C: азиатский стиль (самурай-лучник, восточный лук)

---

### 🔴 TANK (Танк) — MVP Hero #2

**Промпт:**
```
"Create a tank/heavy warrior character in the EXACT SAME art style
as the reference image.

- WIDE, stocky silhouette — widest character in the roster
- 2.5 heads tall chibi, but horizontally exaggerated (square shape)
- MASSIVE shield in left hand (covers 60% of body)
- Short-barrel shotgun/blunderbuss in right hand
- Color palette: gunmetal gray armor, orange energy accents, dark visor
- Immovable, confident stance — slightly crouched behind shield
- Heavy boots, armored shoulders, minimal neck visible

Front view and 3/4 isometric view. Dark gray background."
```

**Итерации:**
- Вариант A: medieval knight + tech shield
- Вариант B: riot police / futuristic enforcer
- Вариант C: dwarf-like proportions, even wider

---

### 🔴 SHADOW (Тень) — MVP Hero #3

**Промпт:**
```
"Create a shadow assassin / ninja character in the EXACT SAME art style
as the reference image.

- Angular, SHARP silhouette — everything points and edges
- 2.5 heads tall chibi but lean and dynamic
- Flowing scarf/cloak trailing behind (motion implied even in idle)
- TWO short blades (kunai/tanto style), one in each hand
- Face mask covering lower face, glowing cyan eyes visible
- Color palette: deep purple body, black cloak, cyan accent glows
- Low combat stance — ready to dash, weight forward
- Stealthy, dangerous energy

Front view and 3/4 isometric view. Dark gray background."
```

**Итерации:**
- Вариант A: classic ninja (чёрный + фиолетовый)
- Вариант B: cyber-assassin (неоновые линии на теле)
- Вариант C: phantom/spectral (полупрозрачные элементы)

---

### 🔴 SCOUT (Разведчик) — MVP Hero #4

**Промпт:**
```
"Create a tech scout / recon specialist character in the EXACT SAME
art style as the reference image.

- Medium build, 2.5 heads tall chibi proportions
- DEFINING ELEMENT: tactical visor/scanner over one eye (glowing blue)
- Shoulder-mounted gadget (antenna or small drone)
- Utility belt with pouches and small devices
- Color palette: navy blue bodysuit, white armor plates, blue tech glow
- Smart, alert expression — head slightly tilted, scanning
- Relaxed but ready pose, one hand near ear-comm
- Tech/intel specialist vibe, not a fighter

Front view and 3/4 isometric view. Dark gray background."
```

**Итерации:**
- Вариант A: military recon (шлем, антенна)
- Вариант B: hacker/tech (AR очки, голограмма)
- Вариант C: explorer (бинокль, рюкзак с гаджетами)

---

## ЭТАП 2: ОКРУЖЕНИЕ (Nano Banana Pro)

### Тайл пола

**Промпт:**
```
"Create a single square floor tile for a tactical grid game,
same art style as reference image.

- Top-down view, perfectly square 1:1 ratio
- Sandstone/ancient stone texture, warm neutral tones
- Subtle grid lines visible on edges
- Low-poly stylized, NOT photorealistic
- Clean, minimal detail — must tile seamlessly
- Slight wear/cracks for character, but very subtle
- Flat lighting, no dramatic shadows

Single tile centered, dark gray background."
```

### Стена (Wall Block)

**Промпт:**
```
"Create a wall block for a tactical grid game,
same art style as reference image.

- Cube proportions: 1 wide × 1 deep × 1.5 tall
- Stone/brick construction, stylized low-poly
- Color: darker than floor (gray-brown), with subtle moss/detail
- 3/4 isometric view to show three faces
- Must feel SOLID and INDESTRUCTIBLE
- Clean geometric shapes, visible edges
- Matches the floor tile aesthetic

Single block centered, dark gray background."
```

### Декоративные пропсы (арена)

**Промпт-шаблон для пропсов:**
```
"Create a [PROP] for a stylized tactical arena game,
same art style as reference image.

- Low-poly stylized, bright colors, clean shapes
- Small prop, sits on a single grid tile
- Isometric 3/4 view
- [PROP-SPECIFIC DETAILS]

Single object centered, dark gray background."
```

**Подстановки [PROP]:**
| Проп | Детали |
|------|--------|
| wooden barrel | Metal bands, slightly worn, warm brown tones |
| ammunition crate | Military green, stenciled markings, metal corners |
| stone pillar | Broken top, ancient, matches wall style |
| torch/lamp post | Glowing warm light, mounted on small base |
| sandbag pile | Stacked 3 high, military olive color |
| banner/flag | Team-colored fabric on pole, tattered edges |

---

## ЭТАП 3: 2D → 3D КОНВЕРТАЦИЯ

### Для ГЕРОЕВ: Tripo AI (рекомендован)

**Почему Tripo:** чистая quad-топология, автоматический риг, сегментация.
Цена: ~$12/мес, 200 кредитов.

**Workflow:**
1. Берём утверждённый концепт-арт героя (фронт + 3/4 вид)
2. Загружаем в Tripo AI → Image to 3D
3. Настройки:
   - Topology: **Quads** (для анимации)
   - Target polycount: **3000-5000**
   - Style: оставить default (AI сохраняет стиль с картинки)
4. Генерируем 2-3 варианта, выбираем лучший
5. Используем встроенный **Retopology** если polycount > 5000
6. **Auto-Rig** прямо в Tripo → выбрать "Humanoid" skeleton
7. Экспорт: **FBX** (для Unity) с текстурами

**Промпт для Tripo (text-to-3D fallback):**
```
"Stylized chibi game character, [HERO NAME], low-poly,
2.5 heads tall, [KEY FEATURE], [COLOR PALETTE],
game-ready, clean topology, front-facing T-pose"
```

### Для ПРОПСОВ: Meshy AI (рекомендован)

**Почему Meshy:** быстрый, хорош для простых объектов, плагин для Unity.
Цена: ~$16/мес, 200 кредитов.

**Workflow:**
1. Загружаем концепт-арт пропса
2. Image to 3D → настройки:
   - Art style: **Delit** (без запечённого света — для контроля в Unity)
   - Quality: Standard (экономим кредиты на пропсах)
3. Если нужна другая текстура → **Retexture** промптом
4. Экспорт: **GLB** или **FBX**
5. Импорт в Unity напрямую (без Blender)

### Альтернатива: 3DAI Studio ($14/мес, 1000 кредитов)

Агрегатор — доступ к Tripo + Meshy + Rodin за одну подписку.
Рекомендую если:
- Нужен объём (>50 ассетов)
- Хочется сравнить результаты разных движков на одном ассете
- Бюджет ограничен (5x больше кредитов за ~ту же цену)

---

## ЭТАП 4: ДОРАБОТКА В BLENDER (только герои)

### Что дорабатывает Амирхан:

```
□ Проверить polycount (цель: 3000-5000 tri)
  → Decimate modifier если >5000
□ Очистить UV развёртку
  → Если текстура "поплыла" — перепаковать UV
□ Проверить нормали (Mesh → Normals → Recalculate Outside)
□ Убрать артефакты генерации
  → Лишние вершины, дыры в mesh, перекрывающиеся грани
□ Масштабировать: высота героя = 1 Unity unit
□ Центрировать origin (дно ног = origin point)
□ Экспорт: FBX, Apply Transform, Forward -Z, Up Y
```

### Для пропсов — Blender НЕ нужен (если качество ок из Meshy)

Критерий: если проп выглядит хорошо в Unity при импорте → пропускаем Blender.

---

## ЭТАП 5: РИГГИНГ + АНИМАЦИИ

### Вариант A: Mixamo (бесплатно, рекомендован для MVP)

1. Загрузить FBX на mixamo.com
2. Auto-rig (расставить маркеры на модели — 30 секунд)
3. Скачать анимации для каждого героя:

**MVP набор анимаций:**

| Анимация | Mixamo search | Настройки |
|----------|--------------|-----------|
| Idle | "Idle" или "Breathing Idle" | Loop, 30fps |
| Walk | "Walking" | Loop, In Place |
| Shoot | "Standing Draw Arrow" / "Rifle Aiming" | No loop, 30fps |
| Hit | "Hit Reaction" / "Standing React Small From Front" | No loop |
| Death | "Dying" / "Death From The Front" | No loop |
| Turn | "Turn" / "Standing Turn Left 90" | No loop |

4. Скачать каждую анимацию:
   - Format: FBX for Unity
   - Skin: **Without Skin** (кроме первой — с skin для T-pose)
   - Keyframe Reduction: ON
5. Складываем в `Assets/Art/Heroes/[HeroName]/Animations/`

### Вариант B: Tripo AI Auto-Rig (быстрее, но менее гибко)

- Прямо в Tripo после генерации: Rigging → Auto Rig → Humanoid
- Выбрать preset анимацию из библиотеки Tripo
- Экспорт FBX с анимацией включённой
- Минус: меньше выбор анимаций чем Mixamo

---

## ЭТАП 6: ИМПОРТ В UNITY

### Структура папок

```
Assets/
  Art/
    Heroes/
      Archer/
        Models/
          Archer_Model.fbx          ← mesh + rig
        Textures/
          Archer_Albedo.png         ← 512×512
          Archer_Normal.png         ← опционально
        Animations/
          Archer_Idle.fbx
          Archer_Walk.fbx
          Archer_Shoot.fbx
          Archer_Hit.fbx
          Archer_Death.fbx
        Materials/
          Archer_Mat.mat            ← URP Lit
      Tank/
        ... (та же структура)
    Environment/
      Tiles/
        Tile_Floor.fbx
        Tile_Floor_Albedo.png
        Tile_Wall.fbx
        Tile_Wall_Albedo.png
      Props/
        Barrel.fbx
        Crate.fbx
        ...
```

### Настройки импорта FBX в Unity

```
Model tab:
  Scale Factor: 1
  Mesh Compression: Medium
  Read/Write: OFF (экономит память)
  Generate Lightmap UVs: OFF (мобильное — нет lightmaps)

Rig tab:
  Animation Type: Humanoid
  Avatar Definition: Create From This Model (первый FBX)
  Avatar Definition: Copy From Other Avatar (анимации без skin)

Animation tab:
  Loop Time: ON для Idle, Walk
  Loop Time: OFF для Shoot, Hit, Death
  Root Transform: Bake Into Pose → ON (все оси)
```

### Материал для героев (URP Lit)

```
Shader: Universal Render Pipeline/Lit
  Surface: Opaque
  Base Map: [Hero]_Albedo.png
  Smoothness: 0.1 (матовый, стилизованный)
  Specular: минимальный
  Normal Map: опционально (если есть)
  Emission: для светящихся элементов (глаза Shadow, сканер Scout)
```

---

## ПОЛНЫЙ ТАЙМЛАЙН: 1 герой от идеи до Unity

| Шаг | Инструмент | Время | Кто делает |
|-----|-----------|-------|-----------|
| Концепт-арт (3 варианта) | Nano Banana Pro | 15 мин | Хаким |
| Утверждение варианта | Figma / чат | 10 мин | Хаким + Амирхан |
| 2D → 3D конвертация | Tripo AI | 5 мин | Хаким |
| Доработка mesh | Blender | 30-60 мин | Амирхан |
| Риг + анимации | Mixamo | 20 мин | Хаким |
| Импорт + настройка | Unity | 15 мин | Кодер |
| Тест на сетке | Unity Play Mode | 10 мин | Все |
| **ИТОГО** | | **~2 часа** | |

*Традиционный пайплайн: 3-5 дней на героя. AI пайплайн: 2 часа.*

---

## ЧЕКЛИСТ: ПЕРВЫЙ ПРОГОН (Archer)

Это checklist для валидации всего пайплайна на одном герое:

```
ДЕНЬ 1 — Концепт:
□ Сгенерить стиль-якорь (Nano Banana Pro, промпт из Этапа 0)
□ Утвердить стиль-якорь с Амирханом
□ Сгенерить 3 варианта Archer (Этап 1)
□ Выбрать финальный вариант
□ Сохранить в Figma мудборд

ДЕНЬ 1 — 3D:
□ Загрузить концепт в Tripo AI
□ Получить 3D модель, проверить:
  - Polycount < 5000?
  - Текстура соответствует концепту?
  - Нет дыр в mesh?
□ Экспорт FBX

ДЕНЬ 1 — Blender:
□ Амирхан открывает FBX в Blender
□ Чек: нормали, UV, scale, origin
□ Доработка если нужна
□ Экспорт чистый FBX

ДЕНЬ 1 — Риг:
□ Mixamo → загрузить → авто-риг
□ Скачать: Idle, Walk, Shoot, Hit, Death (FBX for Unity)

ДЕНЬ 1 — Unity:
□ Импорт всех FBX
□ Настроить Humanoid rig
□ Создать материал URP Lit
□ Поставить на тайл 10×10 сетки
□ Проверить: читаемо? красиво? стиль единый?
□ Записать скриншот/видео для team review

РЕЗУЛЬТАТ: Если всё ок — пайплайн валидирован.
Остальные 3 героя делаются по тому же шаблону.
```

---

## ИНСТРУМЕНТЫ — БЫСТРАЯ СПРАВКА

| Инструмент | Для чего | Цена | Ссылка |
|-----------|---------|------|--------|
| Nano Banana Pro | Концепт-арт 2D, style transfer | Free tier + Pro | nanobanana.io |
| Tripo AI | 2D→3D герои (quad topo, auto-rig) | $12/мес | tripo3d.ai |
| Meshy AI | 2D→3D пропсы (быстро, Unity плагин) | $16/мес | meshy.ai |
| 3DAI Studio | Агрегатор (Tripo+Meshy+Rodin) | $14/мес | 3daistudio.com |
| Mixamo | Автоматический риг + анимации | Бесплатно | mixamo.com |
| Blender | Доработка mesh, UV, экспорт | Бесплатно | blender.org |
| Figma / FigJam | Мудборды, UI дизайн | Бесплатно | figma.com |
| PureRef | Desktop reference board | Бесплатно | pureref.com |
| Scenario | Кастомная AI модель на вашем стиле | От $9/мес | scenario.com |

---

*Документ: v1.0 | Март 2026 | Tactical Duelist — Colosseum Eternal Sprint*
