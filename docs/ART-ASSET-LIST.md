# Tactical Duelist — Asset List

## Как читать этот документ

Каждый ассет помечен приоритетом:
- 🔴 **MVP** — нужно для первой играбельной версии
- 🟡 **v0.2** — нужно для онлайн-версии / специальных способностей
- 🟢 **v1.0** — нужно для полного релиза
- ⚪ **Post-launch** — контентные обновления

---

## 1. ГЕРОИ (3D модели)

### Модель героя — требования
- Стилистика: low-poly stylized (Brawl Stars / Clash Mini уровень детализации)
- Полигонаж: 3000-5000 tri на героя (WebGL лимит)
- Риг: гуманоидный, Unity Humanoid compatible
- Текстуры: 512×512 atlas, URP Lit shader
- Масштаб: ~1 Unity unit высота

### Список героев

```
🔴 MVP (4 героя):
  [ ] Hero_Archer     — лучник, стройный, капюшон, лук за спиной
  [ ] Hero_Tank       — тяжёлый, массивный, щит, короткий ствол
  [ ] Hero_Shadow     — ниндзя/ассасин, лёгкий, плащ, двойные клинки
  [ ] Hero_Scout      — разведчик, бинокль/сканер, средний билд

🟡 v0.2 (ещё 4 героя):
  [ ] Hero_Mage       — маг, посох, мантия, магические руны
  [ ] Hero_Demo       — подрывник, рюкзак со взрывчаткой, детонатор
  [ ] Hero_Guardian   — страж, тяжёлая броня, энергетический щит
  [ ] Hero_Ghost      — призрак, полупрозрачный, парящий

🟢 v1.0 (финальные 4 героя):
  [ ] Hero_Engineer   — инженер, очки, инструменты, дрон/турель
  [ ] Hero_Berserker  — берсерк, огромный, минимум брони, два топора
  [ ] Hero_Hawk       — ястреб, снайпер, длинная винтовка, прицел
  [ ] Hero_Mirage     — мираж, двоящийся силуэт, зеркальные элементы
```

### Анимации на каждого героя

```
🔴 MVP (минимальный набор):
  [ ] Idle            — дыхание, лёгкое покачивание (loop)
  [ ] Walk            — движение вперёд (loop, 0.4s на тайл)
  [ ] Turn            — поворот на месте (0.2s)
  [ ] Shoot           — атака дальнего боя (0.3s)
  [ ] Hit_Receive     — получение урона, отшатывание (0.3s)
  [ ] Death           — падение / исчезновение (0.5s)

🟡 v0.2 (расширенный набор):
  [ ] Special_Cast    — использование способности (уникальна для героя)
  [ ] Armor_Break     — броня разрушается (0.3s)
  [ ] Victory_Pose    — поза победы (2s, для экрана результата)
  [ ] Defeat_Pose     — поза поражения (2s)

🟢 v1.0 (полировка):
  [ ] Idle_Rare       — редкий idle (каждые 10-15с, уникальный для героя)
  [ ] Taunt           — провокация (эмоут)
  [ ] Walk_Fast       — бег (для Speed 2 героев)
  [ ] Spawn           — появление на арене (начало матча)
```

---

## 2. ОКРУЖЕНИЕ / КАРТЫ

### Тайлы сетки

```
🔴 MVP:
  [ ] Tile_Floor         — базовый пол (1×1 unit, плоский quad или low-poly 3D)
  [ ] Tile_Wall          — неразрушимая стена (1×1×1 куб, стилизованный)
  [ ] Tile_Highlight_Move    — подсветка возможного движения (зелёный контур/glow)
  [ ] Tile_Highlight_Shoot   — подсветка линии выстрела (красный контур/glow)
  [ ] Tile_Highlight_Select  — подсветка выбранного тайла (жёлтый)
  [ ] Grid_Lines         — линии сетки (тонкие, полупрозрачные)

🟡 v0.2:
  [ ] Tile_DestructibleWall  — разрушимая стена (трещины, другой материал)
  [ ] Tile_DangerZone        — опасная зона (красное свечение, огонь/энергия)
  [ ] Tile_Pickup_Spot       — точка спавна пикапа (лёгкий glow на полу)

🟢 v1.0:
  [ ] Map_Theme_Arena    — арена (песок/камень, Колизей стиль)
  [ ] Map_Theme_Tech     — техно (металл, неон, киберпанк)
  [ ] Map_Theme_Nature   — природа (трава, деревья, камни)
  [ ] Map_Theme_Ice      — лёд (скользкий визуал, кристаллы)
  [ ] Map_Border         — декоративная граница карты (зрители? пропасть?)
  [ ] Map_Background     — skybox / задний план для каждой темы
```

### Карты (Layout'ы)

```
🔴 MVP:
  [ ] Map_Arena01   — 10×10, симметричная, 6-8 стен, открытый центр

🟡 v0.2:
  [ ] Map_Arena02   — 10×10, больше стен, коридоры, chokepoints
  [ ] Map_Arena03   — 8×8, маленькая, агрессивная

🟢 v1.0:
  [ ] Map_Arena04   — 12×12, большая, много укрытий, для снайперов
  [ ] Map_Arena05   — 10×10, ассиметричная (разные стороны = разная тактика)
  [ ] Map_Arena06   — 10×10, разрушимые стены, динамичная
```

---

## 3. VFX (Визуальные эффекты)

```
🔴 MVP:
  [ ] VFX_Shoot_Projectile  — линия/трассер выстрела (быстрый луч)
  [ ] VFX_Hit_Impact        — попадание (вспышка + частицы)
  [ ] VFX_Death_Explosion   — гибель героя (взрыв/рассыпание)
  [ ] VFX_Armor_Break       — разрушение брони (осколки)
  [ ] VFX_Mutual_Cancel     — взаимная отмена (столкновение двух лучей)
  [ ] VFX_Step_Indicator    — индикатор текущего шага выполнения

🟡 v0.2 (способности):
  [ ] VFX_Ricochet_Bounce   — отскок выстрела от стены
  [ ] VFX_Blink_Teleport    — телепортация (исчезновение + появление)
  [ ] VFX_Push_Force        — волна толчка
  [ ] VFX_Scan_Wave         — волна сканирования
  [ ] VFX_PhaseShot_Ghost   — призрачный выстрел сквозь стену
  [ ] VFX_Bomb_Explosion    — взрыв бомбы 3×3
  [ ] VFX_Barrier_Appear    — появление барьера
  [ ] VFX_Cloak_Fade        — исчезновение/появление призрака
  [ ] VFX_Turret_Spawn      — установка турели
  [ ] VFX_Charge_Trail      — след от рывка берсерка
  [ ] VFX_Pierce_Through    — пробивающий выстрел
  [ ] VFX_Decoy_Spawn       — появление обманки
  [ ] VFX_DangerZone_Fire   — огонь/энергия на опасных тайлах

🟢 v1.0:
  [ ] VFX_Pickup_Collect    — подбор предмета (свечение вверх)
  [ ] VFX_Round_Start       — начало раунда (обратный отсчёт)
  [ ] VFX_Match_Victory     — победа в матче (фейерверк/confetti)
  [ ] VFX_Map_Shrink        — анимация сжатия карты
```

---

## 4. UI ЭЛЕМЕНТЫ

### Иконки

```
🔴 MVP:
  [ ] Icon_Action_Move       — стрелка вперёд
  [ ] Icon_Action_TurnLeft   — стрелка поворота влево
  [ ] Icon_Action_TurnRight  — стрелка поворота вправо
  [ ] Icon_Action_TurnAround — стрелка разворота (U-turn)
  [ ] Icon_Action_Shoot      — прицел / перекрестье
  [ ] Icon_Action_Wait       — часы / пауза
  [ ] Icon_Action_Special    — звезда / молния (для будущих спешлов)
  [ ] Icon_Armor             — щит
  [ ] Icon_Cooldown          — перезарядка (число на затемнении)
  [ ] Icon_Timer             — таймер планирования
  [ ] Icon_Undo              — отмена последнего действия
  [ ] Icon_Confirm           — галочка подтверждения
  [ ] Icon_Direction_Arrow   — стрелка направления героя (на сетке)

🟡 v0.2:
  [ ] Icon_Special_Ricochet  — отскок
  [ ] Icon_Special_Push      — толчок
  [ ] Icon_Special_Blink     — телепорт
  [ ] Icon_Special_Scan      — сканирование
  [ ] Icon_Special_PhaseShot — фазовый выстрел
  [ ] Icon_Special_Bomb      — бомба
  [ ] Icon_Special_Barrier   — барьер
  [ ] Icon_Special_Cloak     — невидимость
  [ ] Icon_Special_Turret    — турель
  [ ] Icon_Special_Charge    — рывок
  [ ] Icon_Special_Pierce    — пробивание
  [ ] Icon_Special_Decoy     — обманка
  [ ] Icon_Pickup_Armor      — осколок брони
  [ ] Icon_Pickup_Intel      — орб разведки
  [ ] Icon_Pickup_Speed      — ускорение
  [ ] Icon_Pickup_Range      — увеличение дальности

🟢 v1.0:
  [ ] Icon_Rank_Bronze       — ранг бронза
  [ ] Icon_Rank_Silver       — ранг серебро
  [ ] Icon_Rank_Gold         — ранг золото
  [ ] Icon_Rank_Diamond      — ранг алмаз
  [ ] Icon_Rank_Master       — ранг мастер
  [ ] Icon_Rank_Legend        — ранг легенда
  [ ] Icon_Currency_Coin     — внутриигровая валюта
  [ ] Icon_Currency_Premium  — премиум валюта
  [ ] Icon_XP                — опыт
  [ ] Icon_BattlePass        — боевой пропуск
  [ ] Icon_Settings          — шестерёнка
  [ ] Icon_Friends           — друзья
  [ ] Icon_Club              — клуб
  [ ] Icon_Replay            — воспроизведение
  [ ] Icon_Share             — поделиться
```

### Портреты героев (2D, для UI)

```
🔴 MVP:
  [ ] Portrait_Archer    — 256×256, стилизованный, bust/face
  [ ] Portrait_Tank
  [ ] Portrait_Shadow
  [ ] Portrait_Scout

🟡 v0.2:
  [ ] Portrait_Mage
  [ ] Portrait_Demo
  [ ] Portrait_Guardian
  [ ] Portrait_Ghost

🟢 v1.0:
  [ ] Portrait_Engineer
  [ ] Portrait_Berserker
  [ ] Portrait_Hawk
  [ ] Portrait_Mirage
```

### Hero Cards (для экрана выбора)

```
🔴 MVP:
  [ ] Card_Hero_Template — шаблон карточки героя (фон, рамка, слоты для статов)
  [ ] Card_Stat_Steps    — иконка + число шагов
  [ ] Card_Stat_Range    — иконка + число дальности
  [ ] Card_Stat_Cooldown — иконка + число перезарядки
  [ ] Card_Stat_Armor    — иконка щита (есть/нет)
  [ ] Card_Stat_Speed    — иконка + число скорости
```

---

## 5. АУДИО

### Музыка

```
🔴 MVP:
  [ ] Music_Menu         — главное меню (лёгкая, tactical vibe, loop ~60-90s)
  [ ] Music_Planning     — фаза планирования (tension, thinking, loop ~30s)
  [ ] Music_Execution    — фаза выполнения (action, dynamic, loop ~20s)
  [ ] Music_Victory      — стингер победы (~5s)
  [ ] Music_Defeat       — стингер поражения (~5s)

🟢 v1.0:
  [ ] Music_HeroSelect   — выбор героя (hype building)
  [ ] Music_Draw         — стингер ничьи (~5s)
  [ ] Music_Menu_Alt     — альтернативная тема меню
```

### SFX (звуковые эффекты)

```
🔴 MVP:
  [ ] SFX_Shoot_Generic     — выстрел (универсальный)
  [ ] SFX_Hit_Impact        — попадание
  [ ] SFX_Death             — гибель героя
  [ ] SFX_Armor_Break       — разрушение брони
  [ ] SFX_Mutual_Cancel     — взаимная отмена (clash звук)
  [ ] SFX_Move_Step         — шаг (движение по тайлу)
  [ ] SFX_Turn              — поворот
  [ ] SFX_UI_Button         — нажатие кнопки
  [ ] SFX_UI_Confirm        — подтверждение действий
  [ ] SFX_UI_Cancel         — отмена
  [ ] SFX_Timer_Tick        — тик таймера (последние 5 секунд)
  [ ] SFX_Timer_End         — конец таймера
  [ ] SFX_Round_Start       — начало раунда
  [ ] SFX_Step_Execute      — шаг выполняется (тик)

🟡 v0.2:
  [ ] SFX_Blink             — телепортация
  [ ] SFX_Bomb_Place        — установка бомбы
  [ ] SFX_Bomb_Explode      — взрыв бомбы
  [ ] SFX_Barrier_Place     — установка барьера
  [ ] SFX_Barrier_Break     — разрушение барьера
  [ ] SFX_Cloak_On          — включение невидимости
  [ ] SFX_Cloak_Off         — выключение невидимости
  [ ] SFX_Turret_Place      — установка турели
  [ ] SFX_Turret_Shoot      — выстрел турели
  [ ] SFX_Charge            — рывок берсерка
  [ ] SFX_Push              — толчок
  [ ] SFX_Scan              — сканирование
  [ ] SFX_Ricochet          — отскок
  [ ] SFX_Pickup_Collect    — подбор предмета
  [ ] SFX_DangerZone_Warn   — предупреждение опасной зоны
  [ ] SFX_Wall_Destroy      — разрушение стены

🟢 v1.0:
  [ ] SFX_Matchmaking       — поиск матча
  [ ] SFX_Match_Found       — матч найден
  [ ] SFX_Rank_Up           — повышение ранга
  [ ] SFX_Rank_Down         — понижение ранга
  [ ] SFX_XP_Gain           — получение опыта
  [ ] SFX_Reward_Open       — открытие награды
```

---

## 6. FONTS / TYPOGRAPHY

```
🔴 MVP:
  [ ] Font_Primary     — основной шрифт UI (bold, readable, Brawl Stars vibe)
                         Рекомендации: Lilita One, Bungee, или кастомный
  [ ] Font_Secondary   — текст описаний, подписи (readable sans-serif)
                         Рекомендации: Nunito, Inter
  [ ] Font_Numbers     — числа статов, таймер, урон (monospace-like, bold)
```

---

## 7. СКИНЫ (Post-launch контент)

```
⚪ Post-launch:
  [ ] Skin system: recolor → texture swap → model swap (3 уровня)
  [ ] 2-3 скина на героя при запуске
  [ ] Seasonal skins (каждый сезон)
  [ ] Анимированные портреты (для premium скинов)
  [ ] Кастомные VFX выстрелов (для premium скинов)
  [ ] Trail effects (для premium скинов)
```

---

## Сводная таблица

| Категория | 🔴 MVP | 🟡 v0.2 | 🟢 v1.0 | ⚪ Post |
|-----------|--------|---------|---------|--------|
| 3D модели героев | 4 | 4 | 4 | скины |
| Анимации (на героя) | 6 | 4 | 3 | — |
| Тайлы окружения | 6 | 3 | 8+ | темы |
| Карты (layout) | 1 | 2 | 3 | сезонные |
| VFX | 6 | 13 | 4 | — |
| UI иконки | 13 | 16 | 15+ | — |
| Портреты | 4 | 4 | 4 | — |
| Музыка | 5 треков | — | 3 трека | сезонная |
| SFX | 14 | 16 | 6 | — |
| Шрифты | 3 | — | — | — |

### Итого для MVP:
- **4** 3D модели героев (с 6 анимациями каждый = 24 анимации)
- **6** тайлов окружения + 1 карта
- **6** VFX эффектов
- **13** UI иконок + 4 портрета + карточка героя
- **5** музыкальных треков + **14** SFX
- **3** шрифта
