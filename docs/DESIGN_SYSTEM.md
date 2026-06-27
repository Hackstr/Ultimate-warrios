# Tactical Duelist — Design System

> Источник: `Assets/Scripts/UI/Toolkit/Styles/tokens.uss`
> Референс: 1080×1920 HD (PanelSettings)

---

## Цветовая палитра

### Фоны
| Token | Hex | Использование |
|-------|-----|---------------|
| --color-bg | `#0F0F1A` | Основной фон |
| --color-bg-panel | `#1A1A28` | Панели, карточки, секции |
| --color-bg-secondary | `#14142A` | Вторичный фон |
| --color-bg-card | `#1E1E3A` | Карточки, модалки |

### Акценты
| Token | Hex | Использование |
|-------|-----|---------------|
| --color-primary | `#FF6B35` | Кнопки, ссылки, активные элементы |
| --color-secondary | `#4073F3` | Player 2, информационные |
| --color-accent-gold | `#FFD933` | Награды, золото, premium |

### Текст
| Token | Hex | Использование |
|-------|-----|---------------|
| --color-text-primary | `#EAEAF4` | Основной текст |
| --color-text-secondary | `#9898B0` | Вторичный, описания |
| --color-text-muted | `rgba(152,152,176,0.4)` | Disabled, версия |

### Ранги
| Ранг | Цвет | ELO |
|------|------|-----|
| Bronze | `#CD7F32` | < 1000 |
| Silver | `#C0C0C0` | 1000-1199 |
| Gold | `#FFD700` | 1200-1399 |
| Platinum | `#00CED1` | 1400-1599 |
| Diamond | `#B9F2FF` | 1600-1799 |
| Master | `#9B59B6` | 1800-1999 |
| Grandmaster | `#FF4444` | 2000+ |

### Результаты
| Статус | Цвет |
|--------|------|
| Win / Success | `#33CC66` |
| Loss / Error | `#FF4444` |
| Draw / Warning | `#FFD933` |

---

## Типографика

| Класс | Шрифт | Размер | Использование |
|-------|-------|--------|---------------|
| `.font-display` | Lilita One | 48-96px | Заголовки экранов, VICTORY/DEFEAT |
| body | Inter | 26-32px | Основной текст |
| small | Inter | 20-24px | Подписи, секции |
| nano | Inter | 16-18px | Версия, мелкие данные |

---

## Spacing Scale

| Token | Значение |
|-------|----------|
| --sp-xs | 8px |
| --sp-sm | 16px |
| --sp-md | 24px |
| --sp-lg | 32px |
| --sp-xl | 48px |

---

## Компоненты (USS классы)

### Кнопки
- `.btn` — базовый стиль (height: 80px, border-radius: 16px)
- `.btn--primary` — оранжевый фон (#FF6B35), белый текст
- `.btn--secondary` — прозрачный, оранжевая рамка
- `.btn--tab` — таб в navigation (rounded pill)
- `.btn--ghost` — прозрачный, только текст
- `.btn--disabled` — opacity 0.4, no pointer events

### Карточки
- `.panel` — фон #1A1A28, border-radius 24px, padding 20px 24px
- `.card` — фон #1E1E3A, border-radius 32px
- `.hero-card` — 220×155px, панель героя в коллекции

### Layout
- `.row` — flex-direction: row
- `.col` — flex-direction: column
- `.center` — align-items + justify-content: center
- `.flex-1` — flex-grow: 1
- `.w-full` — width: 100%

### Action Buttons (Planning)
- `.action-btn` — 120×100px, border-radius 16px
- `.action-btn--move` — зелёный акцент
- `.action-btn--turn` — синий акцент
- `.action-btn--shoot` — красный акцент
- `.action-btn--wait` — серый
- `.action-btn--shield` — голубой
- `.action-btn--special` — золотой

### Overlays
- `.overlay` — absolute, полный экран, чёрный 85% opacity
- `.overlay--visible` — display: flex

### Tutorial
- `.tutorial-dimmed` — opacity: 0.15
- `.tutorial-highlight` — золотая рамка 4px, border-color: #FFD933

### Toast
- `.toast` — плавающее уведомление
- `.toast--success` — зелёная полоса
- `.toast--error` — красная полоса
- `.toast--warning` — жёлтая полоса
- `.toast--info` — синяя полоса

---

## Экраны (18)

| # | Экран | Файл | Назначение |
|---|-------|------|------------|
| 1 | Splash | SplashScreen.uxml | Загрузка |
| 2 | Tutorial | TutorialScreen.uxml | 3-slide онбординг |
| 3 | MainMenu | MainMenuScreen.uxml | Главное меню + навигация |
| 4 | HeroSelect | HeroSelectScreen.uxml | Выбор героя |
| 5 | PreMatch | PreMatchScreen.uxml | Настройки матча |
| 6 | Matchmaking | MatchmakingScreen.uxml | Поиск противника |
| 7 | Planning | PlanningScreen.uxml | Программирование ходов |
| 8 | Reveal | RevealScreen.uxml | Показ планов |
| 9 | HUD | HUDScreen.uxml | Overlay во время execution |
| 10 | RoundTransition | RoundTransitionOverlay.uxml | Между раундами |
| 11 | Result | ResultScreen.uxml | Победа/поражение |
| 12 | Profile | ProfileScreen.uxml | Профиль + match history |
| 13 | Leaderboard | LeaderboardScreen.uxml | Таблица лидеров |
| 14 | HeroesCollection | HeroesCollectionScreen.uxml | Коллекция героев |
| 15 | Settings | SettingsScreen.uxml | Настройки + язык |
| 16 | DailyReward | DailyRewardScreen.uxml | Ежедневная награда |
| 17 | Reconnecting | ReconnectingScreen.uxml | Переподключение |
| 18 | Toast | ToastContainer.uxml | Уведомления |
