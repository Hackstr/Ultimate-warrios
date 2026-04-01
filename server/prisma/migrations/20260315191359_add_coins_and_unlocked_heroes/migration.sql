-- AlterTable
ALTER TABLE "Player" ADD COLUMN     "coins" INTEGER NOT NULL DEFAULT 0,
ADD COLUMN     "unlockedHeroes" TEXT[] DEFAULT ARRAY['archer', 'tank', 'scout']::TEXT[];
