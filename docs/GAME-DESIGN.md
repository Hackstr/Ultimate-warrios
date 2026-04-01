# Tactical Duelist — Game Design Summary

## One-line pitch
1v1 tactical game where you program your hero's actions blindly,
then watch them execute simultaneously against your opponent.

## Universe
See **LORE.md** for full story. In short: Rifts between dimensions
opened ancient Arenas where time flows parallel. The Order of Duelists
holds tournaments — two fighters enter, plan simultaneously, and the
winner claims Rift Essence. Each of 12 heroes comes from a unique dimension.

## Core fantasy
"I read my opponent perfectly" — the thrill of prediction and outsmarting.
You are a Duelist from another dimension, reading your opponent in the Rift Arena.

## Core loop (30-90 seconds per round)

```
PLAN → COMMIT → REVEAL → RESOLVE → REACT

1. PLAN (20-30s): Each player secretly programs a sequence of actions
   (Move, Turn, Shoot, Wait, Special) for their hero.
   Number of actions = hero.Steps parameter (3 to 6).

2. COMMIT: Both players lock in. In online play, hash of actions is
   submitted first (anti-cheat). Then actions are revealed.

3. REVEAL: Both players see opponent's full action list.

4. RESOLVE: Actions execute step by step, simultaneously.
   Each step: Movement → Combat → Damage.
   View shows animated execution.

5. REACT: Round ends on elimination or after all steps.
   If no kill → map shrinks → next round (max 3 rounds).
```

## Match structure
- Best of 1 elimination (First Blood)
- Up to 3 rounds per match
- Map shrinks after each round (outermost ring becomes danger zone)
- Standing in danger zone at round end = eliminated
- If 3 rounds with no elimination = draw

## What makes it unique

1. **Simultaneous blind programming**: unlike chess (alternating turns) or
   Clash Royale (real-time), actions are programmed secretly and execute at once.

2. **Reading the opponent**: success = predicting where they'll be.
   Every shot is a bet on opponent's future position.

3. **Asymmetric heroes**: 12 heroes with different step counts, ranges,
   speeds. Archer plays completely different from Shadow or Berserker.

4. **Mutual cancel**: if both players shoot each other on the same step,
   both shots cancel. This prevents "both die" and rewards positioning
   over raw aim.

5. **Short matches**: 2-4 minutes per match. Perfect for mobile.

## Key design rules

- **No randomness in combat.** All outcomes are deterministic given inputs.
  The "randomness" comes from not knowing opponent's plan.

- **Information is power.** Fog of war, Scout's Scan ability, Ghost's Cloak —
  controlling information flow is a core strategic axis.

- **Every hero has a counter.** No hero is universally best.
  Archer dominates at range but dies to Shadow rushing.
  Tank survives one extra hit but can't chase fast heroes.

- **Maps matter.** Same heroes play differently on open vs enclosed maps.
  Walls create cover for Archer, corridors favor Tank, open space favors Hawk.

## Target audience
- Competitive mobile gamers (Clash Royale, Brawl Stars tier)
- Strategy game fans who want quick matches
- 16-35 age range
- Telegram Mini App users (game-first)

## Platform
Unity → WebGL → Telegram Mini App
Blockchain: Solana (match rewards via Anchor program, Rift Essence / SPL tokens)
Wallet: Phantom deep link in TMA
Future: iOS, Android native
