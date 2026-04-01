-- CreateTable
CREATE TABLE "Season" (
    "id" SERIAL NOT NULL,
    "name" TEXT NOT NULL DEFAULT 'Season 1',
    "startDate" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "endDate" TIMESTAMP(3),
    "isActive" BOOLEAN NOT NULL DEFAULT true,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "Season_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "SeasonRating" (
    "id" TEXT NOT NULL,
    "playerId" TEXT NOT NULL,
    "seasonId" INTEGER NOT NULL,
    "rating" INTEGER NOT NULL DEFAULT 1000,
    "peakRating" INTEGER NOT NULL DEFAULT 1000,
    "wins" INTEGER NOT NULL DEFAULT 0,
    "losses" INTEGER NOT NULL DEFAULT 0,

    CONSTRAINT "SeasonRating_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE INDEX "SeasonRating_seasonId_rating_idx" ON "SeasonRating"("seasonId", "rating" DESC);

-- CreateIndex
CREATE UNIQUE INDEX "SeasonRating_playerId_seasonId_key" ON "SeasonRating"("playerId", "seasonId");
