# Tactical Duelist — Roadmap

> Обновлено: 2026-06-16

## Текущее состояние: MVP+ (v0.6.0)

**Stack:** Unity 6 (URP, WebGL) + NestJS + PostgreSQL + Redis
**Platform:** Telegram Mini App
**Deploy:** https://tacticalduelist.duckdns.org (Caddy + SSL)

---

## ✅ Завершено

### Core Gameplay
- [x] 12 героев с уникальными способностями (Ricochet, Blink, Charge, Bomb, etc.)
- [x] 3 карты (Arena01 10×10, Arena02 8×8, Arena03 10×8)
- [x] ActionResolver — детерминистическая 3-фазная система (Movement → Combat → Damage)
- [x] Commit-Reveal античит (SHA256)
- [x] Smart Bot AI (6 уровней приоритетов)
- [x] Shrink system (danger zone с 3 раунда)
- [x] Макс. 3 раунда (Draw если нет kill)

### UI (17 экранов UI Toolkit)
- [x] MainMenu, HeroSelect, Planning, Execution, Result
- [x] Matchmaking, PreMatch, HUD, Reveal, RoundTransition
- [x] Settings, Profile, Leaderboard, HeroesCollection
- [x] Tutorial, Reconnecting, Splash, DailyReward, Toast

### Networking
- [x] Socket.IO WebSocket (match:find, round:commit, round:reveal, match:rejoin, match:surrender)
- [x] 60s reconnection grace period
- [x] Rate limiting (@nestjs/throttler)
- [x] JWT auth (handshake.auth only)

### Server Hardening
- [x] CORS whitelist из env
- [x] Redis: SCAN, match checkpointing, auto-restore
- [x] Race conditions: mutex на _endMatch, Prisma $transaction
- [x] Hero configs в shared/config (не hardcode)

### Meta-Game
- [x] Coin economy (Win=50, Loss=10, Draw=25)
- [x] Hero unlock system (3 starter + 9 locked, 200-600 coins)
- [x] Hero mastery XP (5 levels)
- [x] Daily rewards (7-day streak, hero ticket на день 7)
- [x] Rank tiers (Bronze → Grandmaster) с цветами
- [x] Match history на профиле
- [x] ELO рейтинг (K=32)
- [x] Leaderboard (top 20)

### Локализация
- [x] EN / RU / KZ (~175 ключей)
- [x] Language selector в Settings
- [x] Auto-detect по системному языку

### VFX
- [x] Particle-based shoot/hit/elimination VFX
- [x] Camera shake (Perlin noise, quadratic decay)
- [x] Hit pause на elimination
- [x] 12 special ability VFX

### Tutorial
- [x] Интерактивный 3-раундовый guided match
- [x] Button highlights + dimming
- [x] Per-round bot behavior
- [x] Post-round feedback

### Deployment
- [x] Docker Compose (Postgres + Redis + NestJS + Nginx + Caddy)
- [x] Auto-SSL (Let's Encrypt)
- [x] VPS: 93.170.73.119

---

## 🔧 В работе / Следующие приоритеты

### UI Polish (HIGH)
- [ ] Заменить hardcoded цвета в UXML на USS tokens
- [ ] Добавить USS классы для динамических элементов (leaderboard rows, match history, stat badges)
- [ ] Улучшить визуальное качество экранов
- [ ] Consistent component styling (toggles, badges, cards)

### Unity Build
- [ ] Собрать WebGL build v0.6.0 с VFX, Tutorial, DailyRewards, Localization, Rank, MatchHistory
- [ ] Deploy на сервер

---

## 📋 Backlog

### Высокий приоритет
- [ ] Invite friend через Telegram (deep links)
- [ ] Battle Pass / сезонные задания
- [ ] Push Notifications (Telegram Bot API)

### Средний приоритет
- [ ] Achievements ("First Kill", "Win Streak 5", etc.)
- [ ] Аниматоры для 5 героев (Guardian, Berserker, Mage, Hawk, Mirage)
- [ ] In-match emotes через socket
- [ ] Seasonal content (ротация карт/событий)

### Низкий приоритет
- [ ] Cosmetics / Skins
- [ ] Replay System
- [ ] Performance (object pooling VFX, UI list recycling)
- [ ] Android/iOS native platform support

---

## Архитектура (кратко)

```
Client (Unity WebGL)          Server (NestJS)
├── Core/Systems/             ├── auth/ (Telegram JWT)
├── Gameplay/ (GameManager)   ├── match/ (Gateway + Service)
├── UI/Toolkit/ (17 screens)  ├── player/ (Profile + Daily)
├── Networking/ (Socket.IO)   ├── blockchain/ (Solana)
├── Platform/ (WebGL/Editor)  └── shared/ (Redis, Configs)
└── Localization/ (EN/RU/KZ)
    Database: PostgreSQL + Redis
```
