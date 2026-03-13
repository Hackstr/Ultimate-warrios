# Tactical Duelist — Colosseum Eternal Sprint Plan

## Overview

4-week development sprint for Colosseum Eternal (Solana ecosystem).
Goal: deliver a playable, online, Solana-integrated tactical game.

**Deliverable:** Working 1v1 tactical game as Telegram Mini App with Solana staked matches.

---

## Week 1 — Playable Prototype (Setup & Planning)

**Goal:** Two people can play on one device and say "this is fun."

**Deliverables:**
- [ ] Unity WebGL build running in browser
- [ ] 3D isometric grid (10×10) with placeholder visuals
- [ ] Hero selection (4 heroes: Archer, Tank, Shadow, Scout)
- [ ] Planning phase: program 4-5 actions, timer, confirm
- [ ] Execution phase: step-by-step animated playback
- [ ] One-shot kill victory condition
- [ ] Pass-and-play mode (P1 plans → hand device → P2 plans → watch together)
- [ ] Basic result screen (winner/loser/draw + rematch)

**What's NOT needed this week:**
- Online multiplayer
- Sound/music
- Final 3D models (use placeholder capsules)
- Solana integration
- Special abilities

**Video update focus:** Show the core loop working. Two heroes on a grid, planning actions, watching execution, someone wins.

---

## Week 2 — Online Multiplayer + Telegram Mini App (Core Development)

**Goal:** Two players can match and play from their own phones via Telegram.

**Deliverables:**
- [ ] NestJS backend deployed (WebSocket via Socket.IO)
- [ ] Matchmaking: find opponent, pair, start match
- [ ] Commit-reveal anti-cheat (SHA-256 hash of actions)
- [ ] Server-authoritative resolution (TypeScript ActionResolver mirrors C#)
- [ ] Telegram Mini App wrapper (WebGL build inside TMA)
- [ ] Online match flow: matchmaking → hero select → plan → execute → result
- [ ] Basic error handling (disconnect → reconnect → resume)

**Infrastructure:**
- [ ] Server: Railway / Render / Fly.io (quick deploy)
- [ ] Database: PostgreSQL (player profiles, match history)
- [ ] Redis: matchmaking queue, session cache

**Video update focus:** Show two phones playing against each other in real-time through Telegram.

---

## Week 3 — Polish + 3D Art (Core Development)

**Goal:** Game looks and feels good. Playtesting feedback incorporated.

**Deliverables:**
- [ ] 3D hero models replacing placeholders (at least 2-4 heroes)
- [ ] VFX: shoot trails, hit impacts, elimination effects
- [ ] Sound effects: shoot, hit, timer, UI interactions
- [ ] UI polish: hero select cards, planning screen, result screen
- [ ] Balance iteration based on playtesting
- [ ] Camera improvements (dynamic follow, zoom on kills)
- [ ] Performance optimization for mobile WebGL (60fps target)
- [ ] Playtesting with 10+ external players

**Video update focus:** Show the visual transformation — before/after. Show real players reacting.

---

## Week 4 — Solana Integration + Launch (Final Touches)

**Goal:** Working staked matches on Solana devnet. Public beta.

**Deliverables:**
- [ ] Anchor smart contract: match escrow program
  - `create_match`: both players deposit SOL/SPL stake into escrow
  - `settle_match`: winner receives both stakes (minus platform fee)
  - `cancel_match`: refund if opponent doesn't show
- [ ] Wallet connection in TMA (Phantom/Solflare deep link or embedded wallet)
- [ ] Staked match flow: deposit → play → settle (automatic on match end)
- [ ] On-chain match result verification
- [ ] SPL token rewards for winners (optional: $DUEL token)
- [ ] Devnet deployment + public beta
- [ ] Final submission with gameplay demo video

**Solana specifics:**
- Network: Devnet for beta, Mainnet for launch
- Wallet: Phantom mobile (deep link from TMA) or Privy embedded wallet
- Transactions per match: 2-3 (deposit + settle)
- Cost: ~$0.00025 per transaction (negligible)

**Video update focus:** Show a real staked match — deposit SOL, play, winner gets payout. Show the on-chain transaction.

---

## Architecture Summary for Sprint

```
┌─────────────────────────────────┐
│  Telegram Mini App (WebView)    │
│  ┌───────────────────────────┐  │
│  │  Unity WebGL Build        │  │
│  │  - 3D Grid + Heroes       │  │
│  │  - Planning/Execution UI  │  │
│  │  - @solana/web3.js        │  │
│  └────────────┬──────────────┘  │
│               │ WebSocket        │
└───────────────┼─────────────────┘
                │
┌───────────────▼─────────────────┐
│  NestJS Server                  │
│  - Matchmaking (Redis queue)    │
│  - Match resolution (authorit.) │
│  - Commit-reveal verification   │
│  - Solana settlement trigger    │
│  - PostgreSQL (profiles/history)│
└───────────────┬─────────────────┘
                │ RPC
┌───────────────▼─────────────────┐
│  Solana (Devnet → Mainnet)      │
│  - Anchor escrow program        │
│  - Match result on-chain        │
│  - SPL token rewards            │
└─────────────────────────────────┘
```

---

## Success Criteria

- [ ] Two strangers can find each other, play a match, and have fun
- [ ] Match takes 2-4 minutes
- [ ] Staked match settles automatically on Solana
- [ ] 60fps on mobile browser (Telegram)
- [ ] Zero cheating possible (commit-reveal + server authority)
- [ ] Compelling 1-minute demo video for final submission
