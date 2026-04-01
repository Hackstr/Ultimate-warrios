/*
  Warnings:

  - You are about to drop the column `tonWallet` on the `Player` table. All the data in the column will be lost.

*/
-- AlterTable
ALTER TABLE "Player" DROP COLUMN "tonWallet",
ADD COLUMN     "walletAddress" TEXT,
ADD COLUMN     "walletChain" TEXT;
