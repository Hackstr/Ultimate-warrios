import { HeroConfig } from '../models/game-types';

/**
 * Canonical hero configuration — single source of truth for the server.
 * Must match Unity ScriptableObject values in Assets/ScriptableObjects/Heroes/.
 */
export const HERO_CONFIGS: Readonly<Record<string, HeroConfig>> = {
  archer:    { heroId: 'archer',    heroName: 'Archer',    steps: 4, range: 8,  cooldown: 2, armor: 0, speed: 1, specialName: 'Ricochet' },
  tank:      { heroId: 'tank',      heroName: 'Tank',      steps: 4, range: 4,  cooldown: 1, armor: 1, speed: 1, specialName: 'Push' },
  shadow:    { heroId: 'shadow',    heroName: 'Shadow',    steps: 6, range: 3,  cooldown: 1, armor: 0, speed: 2, specialName: 'Blink' },
  scout:     { heroId: 'scout',     heroName: 'Scout',     steps: 5, range: 5,  cooldown: 1, armor: 0, speed: 2, specialName: 'Scan' },
  mage:      { heroId: 'mage',      heroName: 'Mage',      steps: 4, range: 6,  cooldown: 2, armor: 0, speed: 1, specialName: 'PhaseShot' },
  demo:      { heroId: 'demo',      heroName: 'Demo',      steps: 4, range: 5,  cooldown: 2, armor: 0, speed: 1, specialName: 'Bomb' },
  guardian:  { heroId: 'guardian',   heroName: 'Guardian',  steps: 4, range: 5,  cooldown: 2, armor: 1, speed: 1, specialName: 'Barrier' },
  ghost:     { heroId: 'ghost',     heroName: 'Ghost',     steps: 5, range: 4,  cooldown: 1, armor: 0, speed: 1, specialName: 'Cloak' },
  engineer:  { heroId: 'engineer',  heroName: 'Engineer',  steps: 4, range: 5,  cooldown: 2, armor: 0, speed: 1, specialName: 'Turret' },
  berserker: { heroId: 'berserker', heroName: 'Berserker', steps: 6, range: 2,  cooldown: 0, armor: 0, speed: 1, specialName: 'Charge' },
  hawk:      { heroId: 'hawk',      heroName: 'Hawk',      steps: 3, range: 10, cooldown: 3, armor: 0, speed: 1, specialName: 'Pierce' },
  mirage:    { heroId: 'mirage',    heroName: 'Mirage',    steps: 5, range: 4,  cooldown: 1, armor: 0, speed: 1, specialName: 'Decoy' },
};

export const VALID_HERO_IDS = Object.keys(HERO_CONFIGS);

export function getHeroConfig(heroId: string): HeroConfig {
  const config = HERO_CONFIGS[heroId];
  if (!config) {
    throw new Error(`Unknown heroId: "${heroId}". Valid: ${VALID_HERO_IDS.join(', ')}`);
  }
  return config;
}
