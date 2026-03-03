# Tactical Duelist — Hero Specifications

## Parameter Definitions

| Parameter | Description | Range |
|-----------|-------------|-------|
| Steps     | Actions per round (plan this many moves) | 3–6 |
| Range     | Shoot distance in tiles | 2–10 |
| Cooldown  | Steps to wait between shots (0 = shoot every step) | 0–3 |
| Armor     | Absorbs 1 hit then breaks. Does not regenerate. | 0–1 |
| Speed     | Tiles moved per Move action | 1–2 |
| Special   | Unique ability, max 1 use per round | — |

## Balance Philosophy

Heroes are NOT balanced by total power points. They are balanced through
**compensating weaknesses**: a hero with high Range has high Cooldown.
A hero with high Speed has low Range. Armor compensates for low mobility.

---

## ARCHER (Лучник)
- Steps: 4 | Range: 8 | CD: 2 | Armor: 0 | Speed: 1
- Special: **Ricochet** — shot bounces off first wall it hits, continues in reflected direction for remaining range
- Playstyle: Long-range sniper. Finds firing lines and punishes predictable movement. Ricochet enables trick shots around corners.
- Weakness: Low mobility (Speed 1), high cooldown means every shot must count. Vulnerable to rushdown heroes (Shadow, Berserker).
- Matchup notes: Strong vs slow heroes (Tank, Guardian), weak vs fast closers (Shadow, Scout).

## TANK (Танк)
- Steps: 4 | Range: 4 | CD: 1 | Armor: 1 | Speed: 1
- Special: **Push** — pushes any entity (player, turret, barrier, decoy) 2 tiles in Tank's facing direction. If entity hits wall, it stops. If pushed into danger zone, entity is trapped there.
- Playstyle: Aggressive brawler. Armor gives free mistake. Low cooldown = constant pressure at close range.
- Weakness: Short range, low speed. Must close distance before becoming effective.
- Matchup notes: Strong vs low-DPS heroes (high CD), weak vs kite heroes who maintain distance.

## SHADOW (Тень)
- Steps: 6 | Range: 3 | CD: 1 | Armor: 0 | Speed: 2
- Special: **Blink** — teleport to any empty tile within 4 tiles (line of sight NOT required). Counts as movement only, cannot shoot same step.
- Playstyle: Hyper-mobile assassin. 6 steps + Speed 2 = covers enormous ground. Blink makes position unpredictable.
- Weakness: Extremely short range (3 tiles). Must get very close to kill. No armor = one mistake is death.
- Matchup notes: Strong vs slow heroes (Archer, Mage, Hawk), weak vs area denial (Demo, Engineer).

## SCOUT (Разведчик)
- Steps: 5 | Range: 5 | CD: 1 | Armor: 0 | Speed: 2
- Special: **Scan** — reveals opponent's FIRST action of the current round before you finalize your plan. Does not consume a step; activates during planning phase.
- Playstyle: Intel-based duelist. Balanced stats with information advantage. Scan turns 50/50 reads into educated guesses.
- Weakness: No defensive abilities, no escape. Information is only useful if you can act on it.
- Matchup notes: Equally effective against all heroes. Slight edge vs predictable heroes, slight weakness vs heroes with multiple viable patterns.

## MAGE (Маг)
- Steps: 4 | Range: 6 | CD: 2 | Armor: 0 | Speed: 1
- Special: **Phase Shot** — next shot passes through exactly 1 wall (but not 2+). Must be used on same step as Shoot or the step before.
- Playstyle: Wall-ignoring control mage. Forces opponents out of cover. Phase Shot punishes hiding behind single walls.
- Weakness: Same mobility issues as Archer. Useless in open areas where Phase Shot gives no advantage.
- Matchup notes: Strong on maps with many single walls. Weak on open maps.

## DEMO (Подрывник)
- Steps: 4 | Range: 5 | CD: 2 | Armor: 0 | Speed: 1
- Special: **Bomb** — places a bomb on any tile within range 3 (LOS required). Bomb detonates at the END of the NEXT step, dealing damage in a 3×3 area. Destroys destructible walls. Only 1 bomb active at a time.
- Playstyle: Area denial. Forces movement by threatening zones. Bomb + Shoot creates lose-lose situations.
- Weakness: Slow, no armor, bomb is telegraphed (opponent sees placement). Useless in tight corridors.
- Matchup notes: Strong vs low-mobility heroes. Weak vs high-Speed heroes who dodge easily.

## GUARDIAN (Страж)
- Steps: 4 | Range: 5 | CD: 2 | Armor: 1 | Speed: 1
- Special: **Barrier** — places temporary wall on any adjacent tile (1 of 4 directions). Barrier lasts 3 steps then disappears. Only 1 barrier active at a time. Barrier blocks ALL shots (both players).
- Playstyle: Defensive tactician. Armor + Barrier = extremely hard to kill. Controls space by placing walls.
- Weakness: Low offensive pressure (CD 2, average range). Can accidentally block own shots with Barrier.
- Matchup notes: Strong vs sniper heroes. Weak vs heroes that can destroy/bypass barriers (Demo, Mage).

## GHOST (Призрак)
- Steps: 5 | Range: 4 | CD: 1 | Armor: 0 | Speed: 1
- Special: **Cloak** — become invisible for 2 steps. Opponent cannot see Ghost's position during cloaked steps. Ghost CAN still be hit by shots that cross the cloaked tile (opponent just doesn't know where to aim). Attacking while cloaked reveals position.
- Playstyle: Mind games. Cloak creates massive uncertainty. Opponent must guess where Ghost moved.
- Weakness: Average stats, no defensive ability. If opponent correctly predicts position, Ghost dies.
- Matchup notes: Strong vs prediction-based heroes (Archer). Weak vs area-effect heroes (Demo).

## ENGINEER (Инженер)
- Steps: 4 | Range: 5 | CD: 2 | Armor: 0 | Speed: 1
- Special: **Turret** — places auto-turret on current tile. Turret has Range 4, shoots at nearest player in LOS at the start of each step (before other actions). Turret has 1 HP (destroyed by any hit). Only 1 turret active at a time.
- Playstyle: Zone control. Turret provides persistent threat. Engineer + Turret = two firing angles.
- Weakness: Turret is fragile (1 HP). Placing turret requires standing still = vulnerable. Slow.
- Matchup notes: Strong in narrow maps (turret controls choke). Weak vs long-range heroes who snipe turret.

## BERSERKER (Берсерк)
- Steps: 6 | Range: 2 | CD: 0 | Armor: 0 | Speed: 1
- Special: **Charge** — move 3 tiles in facing direction + shoot at arrival tile. Counts as Move AND Shoot in 1 action step. Ignores normal Speed (always 3 tiles). Blocked by walls (stops at wall).
- Playstyle: Pure aggression. 6 steps + CD 0 = shoots almost every step at point-blank. Charge closes distance explosively.
- Weakness: Range 2 = must be adjacent to hit. Extremely vulnerable at distance. No armor, no escape.
- Matchup notes: Strong vs any hero at close range. Weak vs heroes that maintain distance (Archer, Hawk).

## HAWK (Ястреб)
- Steps: 3 | Range: 10 | CD: 3 | Armor: 0 | Speed: 1
- Special: **Pierce** — next shot passes through ALL obstacles (walls, barriers, turrets). Must be used on same step as Shoot.
- Playstyle: Ultra-long-range glass cannon. Fewest steps = least flexible. Every shot opportunity is critical. Pierce is a guaranteed threat.
- Weakness: Only 3 steps, CD 3 = shoots once per round at most. If the one shot misses, Hawk is defenseless for entire round.
- Matchup notes: Strong on open/large maps. Weak on small maps where Hawk can't use range advantage.

## MIRAGE (Мираж)
- Steps: 5 | Range: 4 | CD: 1 | Armor: 0 | Speed: 1
- Special: **Decoy** — places visual clone at any empty tile within range 5 (LOS required). Clone looks identical to Mirage on opponent's screen. Clone does not move or shoot. Destroyed by any hit. Only 1 decoy active. Lasts until destroyed or end of round.
- Playstyle: Deception. Opponent must decide which target is real. Mirage attacks while opponent shoots decoy.
- Weakness: Decoy doesn't move (predictable over time). Once identified, provides no benefit. Average combat stats.
- Matchup notes: Strong vs heroes with high cooldown (wasted shots on decoy). Weak vs low-CD heroes who can shoot both.

---

## Matchup Matrix (Advantage)

```
         ARC TAN SHA SCO MAG DEM GUA GHO ENG BER HAW MIR
ARCHER    =   +   -   =   =   +   -   -   +   +   =   +
TANK      -   =   -   =   +   =   -   =   +   -   +   =
SHADOW    +   +   =   =   +   -   =   +   -   =   +   =
SCOUT     =   =   =   =   =   =   =   =   =   =   =   =
MAGE      =   -   -   =   =   =   +   =   +   +   =   =
DEMO      -   =   +   =   =   =   -   +   =   +   -   +
GUARDIAN   +   +   =   =   -   +   =   =   -   +   -   =
GHOST     +   =   -   =   =   -   =   =   =   =   =   -
ENGINEER  -   -   +   =   -   =   +   =   =   +   -   =
BERSERKER -   +   =   =   -   -   -   =   -   =   -   =
HAWK      =   -   -   =   =   +   +   =   +   +   =   +
MIRAGE    -   =   =   =   =   -   =   +   =   =   -   =

+ = advantage, - = disadvantage, = = even
```
