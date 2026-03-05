# Tactical Duelist — Full Asset List

## Как читать этот документ

Приоритеты:
- 🔴 **MVP (Phase A)** — нужно для первой играбельной версии
- 🟡 **Phase B (Online)** — нужно для онлайн-версии
- 🟢 **Post-launch** — можно добавлять итеративно после релиза

Статусы:
- `[ ]` — не начато
- `[WIP]` — в работе  
- `[✓]` — готово

---

## 1. ГЕРОИ (3D Models + Animations)

### 1.1 Модели героев

Каждый герой = 1 модель с ригом для анимации.
Стиль: low-poly stylized (как Brawl Stars / Clash Mini).
Полигонаж: 2000-5000 tri на героя (WebGL оптимизация).

```
🔴 MVP (4 героя):
[ ] Hero_Archer     — лучник, средний рост, капюшон, лук за спиной
[ ] Hero_Tank       — массивный, тяжёлая броня, щит
[ ] Hero_Shadow     — худощавый, плащ, маска ниндзя
[ ] Hero_Scout      — лёгкая экипировка, бинокль/визор

🟢 Post-launch (8 героев):
[ ] Hero_Mage       — мантия, посох, светящиеся руки
[ ] Hero_Demo       — грузный, пояс с бомбами, безумная ухмылка
[ ] Hero_Guardian   — рыцарь, большой щит, сияющая броня
[ ] Hero_Ghost      — полупрозрачный, капюшон, развевающийся плащ
[ ] Hero_Engineer   — очки-гогглы, пояс с инструментами, рюкзак
[ ] Hero_Berserker  — огромный, полуголый, двуручное оружие
[ ] Hero_Hawk       — стройный, снайперская винтовка, один глаз прищурен
[ ] Hero_Mirage     — маска-зеркало, дым вокруг, двойники в позе
```

### 1.2 Анимации героев (на каждого)

Все анимации в одном Animator Controller на героя.
Каждая анимация зациклена или one-shot где указано.

```
🔴 MVP (минимальный набор):
[ ] Idle            — дыхание, покачивание (loop)
[ ] Move            — бег/ходьба вперёд (loop)
[ ] Turn            — быстрый поворот тела (one-shot, ~0.2s)
[ ] Shoot           — атака выстрел (one-shot, ~0.3s)
[ ] Hit_Receive     — получение урона, отшатывание (one-shot, ~0.3s)
[ ] Death           — падение/исчезновение (one-shot, ~0.8s)
[ ] Victory         — победная поза (one-shot → hold)
[ ] Defeat          — поза поражения (one-shot → hold)

🟡 Phase B:
[ ] Wait            — стоит, осматривается (loop, отличается от Idle)
[ ] Armor_Break     — визуальная реакция на потерю брони (one-shot)
[ ] Special_Cast    — уникальная анимация спешла (per hero, one-shot)

🟢 Post-launch:
[ ] Taunt           — дразнилка (per hero, one-shot)
[ ] Spawn           — появление на карте (one-shot)
[ ] Level_Up        — анимация повышения уровня (one-shot)
```

### 1.3 Портреты героев (2D)

Для UI: выбор героя, HUD, результаты матча.

```
🔴 MVP:
[ ] Portrait_Archer      — 256x256, стилизованный, на прозрачном фоне
[ ] Portrait_Tank
[ ] Portrait_Shadow
[ ] Portrait_Scout

🟢 Post-launch:
[ ] Portrait_Mage ... Portrait_Mirage (x8)
[ ] Portrait_Locked     — силуэт для залоченных героев
```

---

## 2. КАРТА / ОКРУЖЕНИЕ (3D Environment)

### 2.1 Тайлы сетки

Стиль: плоские 3D тайлы с лёгким рельефом по краям.
Размер: 1x1 Unity unit.

```
🔴 MVP:
[ ] Tile_Floor          — базовый пол (лёгкая текстура, grid lines)
[ ] Tile_Floor_Alt      — альтернативный пол (шахматный паттерн)
[ ] Tile_Wall           — стена-блок, высота ~1.5 юнита
[ ] Tile_DangerZone     — опасная зона (красное свечение / лава / огонь)
[ ] Tile_Highlight_Move — подсветка: куда можно двигаться (зелёный)
[ ] Tile_Highlight_Shot — подсветка: линия выстрела (красный)
[ ] Tile_Spawn_P1       — точка спавна игрока 1 (синее свечение)
[ ] Tile_Spawn_P2       — точка спавна игрока 2 (красное свечение)

🟡 Phase B:
[ ] Tile_DestructibleWall — разрушаемая стена (трещины, другой цвет)
[ ] Tile_Shrink_Warning  — предупреждение о сжатии (мигающий)

🟢 Post-launch:
[ ] Themed tile sets (Arena, Forest, Cyber, Volcano — по 8 тайлов каждый)
```

### 2.2 Окружение карты

```
🔴 MVP:
[ ] Arena_Border        — граница карты (стена/обрыв по краям)
[ ] Arena_Background    — фон за границами (простой skybox или fog)

🟡 Phase B:
[ ] Arena_Decorations   — небольшие декорации вне игрового поля
[ ] Skybox_Default      — стилизованное небо

🟢 Post-launch:
[ ] Themed arenas (4 темы × полный набор окружения)
```

### 2.3 Карты (MapConfig assets)

```
🔴 MVP:
[ ] Map_Arena01         — 10x10, симметричная, 6-8 стен, простая

🟡 Phase B:
[ ] Map_Arena02         — 10x10, другой layout стен
[ ] Map_Arena03         — 8x8, маленькая, агрессивная

🟢 Post-launch:
[ ] 10+ карт разных размеров (8x8, 10x10, 12x12)
[ ] Карты с тематическим окружением
```

---

## 3. ПРЕДМЕТЫ / ПИКАПЫ (3D Objects)

```
🟡 Phase B (пикапов нет в MVP):
[ ] Pickup_ArmorShard    — фрагмент брони, парящий, вращается
[ ] Pickup_IntelOrb      — светящийся шар с глазом
[ ] Pickup_SpeedBoost    — молния / стрелка
[ ] Pickup_RangeBoost    — прицел / перекрестие
[ ] Pickup_Glow          — общий эффект парения + свечения для всех пикапов
```

---

## 4. VFX (Particle Effects)

### 4.1 Боевые эффекты

```
🔴 MVP:
[ ] VFX_Shot_Trail       — след выстрела (луч/пуля от героя в направлении)
[ ] VFX_Shot_Hit         — попадание (искры/вспышка на месте попадания)
[ ] VFX_Shot_Miss        — промах (луч уходит в стену или за карту)
[ ] VFX_Mutual_Cancel    — взаимная отмена (два луча столкнулись, искры)
[ ] VFX_Armor_Break      — разрушение брони (осколки разлетаются)
[ ] VFX_Death            — смерть героя (взрыв/исчезновение)
[ ] VFX_Move_Dust        — пыль при движении

🟡 Phase B:
[ ] VFX_DangerZone_Fire  — огонь/энергия на danger zone тайлах (loop)
[ ] VFX_Shrink_Wave      — волна сжатия карты
[ ] VFX_Pickup_Collect   — сбор пикапа (вспышка + частицы вверх)
[ ] VFX_Spawn            — появление героя на карте

🟢 Post-launch (per-hero specials):
[ ] VFX_Ricochet         — рикошет от стены (искры + смена направления)
[ ] VFX_Blink            — телепортация (дым + появление)
[ ] VFX_Push             — ударная волна толчка
[ ] VFX_Scan             — сканирующий луч / радар
[ ] VFX_PhaseShot        — призрачный луч через стену
[ ] VFX_Bomb_Place       — размещение бомбы
[ ] VFX_Bomb_Explode     — взрыв 3x3
[ ] VFX_Barrier_Create   — создание стены
[ ] VFX_Cloak_On         — исчезновение (прозрачность)
[ ] VFX_Cloak_Off        — появление
[ ] VFX_Turret_Place     — установка турели
[ ] VFX_Turret_Shoot     — выстрел турели
[ ] VFX_Charge           — рывок берсерка (пламя/скорость)
[ ] VFX_Pierce           — пробивающий выстрел (через стены)
[ ] VFX_Decoy_Create     — создание клона (дым)
[ ] VFX_Decoy_Destroy    — разрушение клона
```

---

## 5. UI ЭЛЕМЕНТЫ (2D)

### 5.1 Общие компоненты

```
🔴 MVP:
[ ] UI_Button_Primary     — основная кнопка (Play, Confirm, etc.)
[ ] UI_Button_Secondary   — второстепенная кнопка (Cancel, Back)
[ ] UI_Button_Icon        — кнопка-иконка (настройки, закрыть)
[ ] UI_Panel_Default      — стандартная панель / фрейм
[ ] UI_Panel_Dark         — тёмная панель (для модалок)
[ ] UI_Timer_Circle       — круговой таймер обратного отсчёта
[ ] UI_ProgressBar        — полоска прогресса (generic)
[ ] UI_Tooltip            — тултип с текстом
[ ] UI_Divider            — разделитель
```

### 5.2 Иконки действий

```
🔴 MVP:
[ ] Icon_Move            — стрелка вперёд
[ ] Icon_TurnLeft        — стрелка поворота влево
[ ] Icon_TurnRight       — стрелка поворота вправо
[ ] Icon_TurnAround      — стрелка разворота
[ ] Icon_Shoot           — прицел / перекрестие
[ ] Icon_Wait            — часы / пауза
[ ] Icon_Special         — звезда / молния (generic)
[ ] Icon_Cooldown        — перечёркнутый прицел / серый

🟢 Post-launch:
[ ] Icon_Special_[Hero]  — уникальная иконка спешла для каждого героя (x12)
```

### 5.3 HUD элементы

```
🔴 MVP:
[ ] HUD_ActionSlot       — слот для действия в очереди (пустой/заполненный)
[ ] HUD_ActionSlot_Active — активный слот (подсветка)
[ ] HUD_Armor_Icon       — индикатор брони (щит целый / сломанный)
[ ] HUD_Cooldown_Overlay — оверлей кулдауна на кнопке Shoot
[ ] HUD_PlayerInfo       — фрейм: портрет + имя + броня
[ ] HUD_StepCounter      — "Step 2/4" индикатор

🟡 Phase B:
[ ] HUD_Timer_Warning    — предупреждение таймера (<5 секунд)
[ ] HUD_Connection       — индикатор соединения (зелёный/жёлтый/красный)
[ ] HUD_RoundCounter     — "Round 2/3"
```

### 5.4 Экранные элементы

```
🔴 MVP:
[ ] Screen_Logo          — лого Tactical Duelist
[ ] Screen_Background    — фон главного меню (3D арена или арт)
[ ] Screen_Overlay_Dark  — затемнение для модалок/переходов

🟡 Phase B:
[ ] Screen_MatchFound    — анимация "Opponent Found"
[ ] Screen_VS            — "Player 1 VS Player 2" экран
[ ] Screen_Victory       — экран победы (конфетти, золото)
[ ] Screen_Defeat        — экран поражения
[ ] Screen_Draw          — экран ничьи
```

---

## 6. АУДИО

### 6.1 SFX (Sound Effects)

```
🔴 MVP:
[ ] SFX_Shoot            — выстрел (универсальный)
[ ] SFX_Hit              — попадание
[ ] SFX_Miss             — промах (свист)
[ ] SFX_Mutual_Cancel    — взаимная отмена (металлический звон)
[ ] SFX_Armor_Break      — треск брони
[ ] SFX_Death            — смерть (драматический)
[ ] SFX_Move             — шаг/бег (1-2 варианта)
[ ] SFX_Turn             — поворот (быстрый свуш)
[ ] SFX_UI_Tap           — нажатие кнопки
[ ] SFX_UI_Confirm       — подтверждение
[ ] SFX_UI_Cancel        — отмена
[ ] SFX_Timer_Tick       — тик таймера (последние 5 секунд)
[ ] SFX_Timer_End        — таймер истёк
[ ] SFX_Round_Start      — начало раунда
[ ] SFX_Victory          — победа (фанфары)
[ ] SFX_Defeat           — поражение

🟡 Phase B:
[ ] SFX_Pickup_Collect   — сбор предмета
[ ] SFX_Shrink_Warning   — предупреждение сжатия
[ ] SFX_Shrink_Fire      — огонь danger zone (loop)
[ ] SFX_Match_Found      — матч найден

🟢 Post-launch:
[ ] SFX per hero special (x12)
[ ] SFX ambient per arena theme (x4)
```

### 6.2 Музыка

```
🔴 MVP:
[ ] BGM_Menu             — главное меню (loop, ~60-90 BPM, атмосферная)
[ ] BGM_Planning         — фаза планирования (loop, напряжённая, тикающая)
[ ] BGM_Execution        — фаза исполнения (loop, динамичная, быстрая)

🟡 Phase B:
[ ] BGM_Victory_Sting    — короткий победный стинг (5-10 сек)
[ ] BGM_Defeat_Sting     — короткий стинг поражения

🟢 Post-launch:
[ ] BGM per arena theme (x4 × 3 tracks)
```

---

## 7. ШРИФТЫ

```
🔴 MVP:
[ ] Font_Primary         — основной (заголовки, кнопки) — bold, readable
[ ] Font_Secondary       — вторичный (описания, мелкий текст) — regular
[ ] Font_Numbers         — для чисел (таймер, статы) — monospace или tabular

Рекомендация: Russo One (заголовки) + Inter (текст) — оба бесплатные, 
поддерживают кириллицу, хорошо читаются на мобильных.
```

---

## 8. ПРОЧЕЕ

```
🔴 MVP:
[ ] App_Icon             — иконка приложения (512x512 + 1024x1024)
[ ] Loading_Screen       — экран загрузки (лого + прогресс бар)

🟡 Phase B:
[ ] Emoji_Set            — набор эмодзи для quick chat (8 штук)
[ ] Rank_Icons           — иконки рангов (Bronze → Grandmaster, 8 штук)
[ ] Season_Banner        — баннер текущего сезона

🟢 Post-launch:
[ ] Skin system assets
[ ] Battle Pass tier icons
[ ] Club badges
```

---

## Сводка по объёму

| Категория | 🔴 MVP | 🟡 Phase B | 🟢 Post-launch | Всего |
|-----------|--------|-----------|----------------|-------|
| 3D модели героев | 4 | 0 | 8 | 12 |
| Анимации (на героя) | 8 | 3 | 3 | 14 |
| Анимации всего | 32 | 12 | 24+ | 68+ |
| 3D тайлы/окружение | 10 | 4 | 30+ | 44+ |
| VFX | 7 | 5 | 16+ | 28+ |
| UI элементы | 25+ | 10+ | 20+ | 55+ |
| Иконки действий | 8 | 0 | 12 | 20 |
| Портреты | 4 | 0 | 9 | 13 |
| SFX | 16 | 4 | 16+ | 36+ |
| Музыка | 3 | 2 | 12+ | 17+ |
| Шрифты | 3 | 0 | 0 | 3 |
