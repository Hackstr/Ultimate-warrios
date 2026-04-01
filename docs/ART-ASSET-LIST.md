# Tactical Duelist — Art Asset List (Updated 2026-03-16)

Полный список ассетов с текущим статусом. Что есть, что нужно.

---

## Статус
- ✅ Готово
- 🔶 Placeholder (работает, но не финальное)
- ❌ Отсутствует

---

## 1. 3D МОДЕЛИ ГЕРОЕВ (0/12)

Стиль: Low-poly stylized, 3000-5000 tri, Humanoid rig.
Pipeline: Nano Banana Pro → Tripo AI → Blender → Mixamo → Unity.

| # | Герой | Цвет | Приоритет | Статус |
|---|-------|------|-----------|--------|
| 1 | Archer | Зелёный | MVP | ❌ Capsule |
| 2 | Tank | Синий | MVP | ❌ Capsule |
| 3 | Scout | Жёлтый | MVP | ❌ Capsule |
| 4 | Shadow | Фиолетовый | MVP | ❌ Capsule |
| 5 | Mage | Голубой | v0.2 | ❌ Capsule |
| 6 | Demo | Красный | v0.2 | ❌ Capsule |
| 7 | Guardian | Золотой | v0.2 | ❌ Capsule |
| 8 | Ghost | Серый | v0.2 | ❌ Capsule |
| 9 | Engineer | Оранжевый | v0.2 | ❌ Capsule |
| 10 | Berserker | Тёмно-красный | v0.2 | ❌ Capsule |
| 11 | Hawk | Белый | v0.2 | ❌ Capsule |
| 12 | Mirage | Розовый | v0.2 | ❌ Capsule |

**Путь**: `Assets/Prefabs/Heroes/Hero_{Name}.prefab` → `HeroConfig.heroPrefab`

---

## 2. АНИМАЦИИ (0/12 контроллеров)

Humanoid Animator Controllers. Нужны после получения rigged моделей.

| Анимация | Trigger/Bool | Длительность | Приоритет |
|----------|-------------|-------------|-----------|
| Idle | default state | ~2s loop | MVP |
| Walk | IsMoving (bool) | 0.25s/step | MVP |
| Turn | (blend with walk) | 0.15s | MVP |
| Shoot | Shoot (trigger) | 0.3s | MVP |
| Hit | Hit (trigger) | 0.4s | MVP |
| Death | Death (trigger) | 0.8s | MVP |
| Victory | Victory (trigger) | 2s | v0.2 |
| Defeat | Defeat (trigger) | 2s | v0.2 |

**Путь**: `Assets/Animations/{HeroName}/` → `HeroConfig.animatorController`

---

## 3. ПОРТРЕТЫ ГЕРОЕВ (0/12)

| Размер | Формат | Приоритет |
|--------|--------|-----------|
| 256×256 | PNG transparent | MVP (4 стартовых) |
| 512×512 | PNG (Collection) | v0.2 |

**Путь**: `Assets/Art/Sprites/Heroes/{name}_portrait.png` → `HeroConfig.portrait`

---

## 4. UI ИКОНКИ (0/27)

### Действия (PlanningScreen) — 7 штук
icon_move, icon_turn_left, icon_turn_right, icon_turn_around, icon_shoot, icon_wait, icon_special
128×128 PNG

### Интерфейс — 6 штук
icon_armor (64), icon_cooldown (64), icon_coin (64), icon_elo (64), icon_lock (64), icon_back (48)

### Ранги — 7 штук
rank_bronze, rank_silver, rank_gold, rank_platinum, rank_diamond, rank_master, rank_grandmaster
128×128 PNG

### Навбар — 4 штуки
nav_home, nav_heroes, nav_rank, nav_settings
48×48 PNG

### Прочее — 3 штуки
logo_tactical_duelist, icon_telegram, icon_share

**Путь**: `Assets/Art/Sprites/UI/`, `Assets/Art/Sprites/Ranks/`

---

## 5. VFX ПРЕФАБЫ (3/12 runtime placeholder)

| VFX | Описание | Приоритет | Статус |
|-----|----------|-----------|--------|
| VFX_Shoot | Трейсер пули | MVP | 🔶 Runtime sphere+trail |
| VFX_Hit | Вспышка попадания | MVP | 🔶 Runtime red sphere |
| VFX_Elimination | Взрыв смерти | MVP | 🔶 Runtime orange sphere |
| VFX_ArmorBreak | Разлёт брони | MVP | ❌ |
| VFX_MutualCancel | Столкновение | v0.2 | ❌ |
| VFX_Pickup | Подбор бонуса | v0.2 | ❌ |
| VFX_DangerZone | Мерцание зоны | v0.2 | ❌ |
| VFX_Blink | Телепортация | v0.2 | ❌ |
| VFX_Bomb | Взрыв | v0.2 | ❌ |
| VFX_Barrier | Щит | v0.2 | ❌ |
| VFX_Cloak | Невидимость | v0.2 | ❌ |
| VFX_Charge | Рывок | v0.2 | ❌ |

**Путь**: `Assets/Prefabs/VFX/` → VFXManager + `HeroConfig.specialVFXPrefab`

---

## 6. ОКРУЖЕНИЕ (0/6)

| Ассет | Описание | Приоритет |
|-------|----------|-----------|
| Floor Tile | Клетка пола 1×1 | v0.2 |
| Wall Block | Стена 1×1×2 | v0.2 |
| Destructible Wall | Разрушаемая | v0.2 |
| Spawn Marker | Точка появления | v0.2 |
| Map Props | Бочки, ящики, столбы | v1.0 |
| Skybox | Арена/небо | v1.0 |

---

## 7. АУДИО (34/34 ✅ ГОТОВО)

### Музыка (4/4 ✅)
music_menu, music_planning, music_execution, music_execution_alt

### SFX (30/30 ✅)
**Бой**: shoot, hit, death, armor_break, mutual_cancel, miss
**Движение**: step, turn
**Способности**: blink, bomb, barrier, cloak, charge, scan, turret, special_generic
**UI**: ui_click, ui_confirm, ui_cancel, ui_navigate, ui_queue, ui_undo, ui_countdown_tick, toast
**Матч**: match_found, round_start, round_end, victory, defeat

**Путь**: `Assets/Resources/Audio/SFX/` и `Assets/Resources/Audio/Music/`

---

## 8. ШРИФТЫ (2/2 ✅ ГОТОВО)

| Шрифт | Назначение |
|-------|------------|
| Inter | Body text, UI |
| Lilita One | Заголовки (font-display) |

**Путь**: `Assets/Fonts/`

---

## 9. МАТЕРИАЛЫ (19/23 ✅)

| Категория | Есть | Всего |
|-----------|------|-------|
| Grid | 9 | 9 ✅ |
| Hero цвета | 8 | 12 (❌ archer, tank, shadow, scout) |
| Hero P1/P2 | 2 | 2 ✅ |

---

## СВОДКА

| Категория | Нужно MVP | Есть | Делать |
|-----------|-----------|------|--------|
| 3D модели | 4 | 0 | **4** |
| Animators | 4 | 0 | **4** |
| Портреты | 4 | 0 | **4** |
| UI иконки | 7 | 0 | **7** |
| VFX | 4 | 3🔶 | **1** |
| Аудио | 34 | 34 | **0 ✅** |
| Шрифты | 2 | 2 | **0 ✅** |
| Материалы | 11 | 11 | **0 ✅** |

**Итого для MVP**: ~20 ассетов нужно создать.
Основной блокер — **3D модели героев + animations**.
