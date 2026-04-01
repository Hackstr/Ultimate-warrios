import * as anchor from "@coral-xyz/anchor";
import { Program } from "@coral-xyz/anchor";
import { TacticalDuelist } from "../target/types/tactical_duelist";
import { assert } from "chai";

describe("tactical-duelist", () => {
  const provider = anchor.AnchorProvider.env();
  anchor.setProvider(provider);

  const program = anchor.workspace.TacticalDuelist as Program<TacticalDuelist>;
  const authority = provider.wallet;

  it("Creates a staked match escrow", async () => {
    const matchId = "test-match-001";
    const stakeAmount = new anchor.BN(100_000_000); // 0.1 SOL

    const playerA = anchor.web3.Keypair.generate();

    // Airdrop SOL to player A for testing
    const sig = await provider.connection.requestAirdrop(
      playerA.publicKey,
      1_000_000_000 // 1 SOL
    );
    await provider.connection.confirmTransaction(sig);

    // Derive escrow PDA
    const [escrowPda] = anchor.web3.PublicKey.findProgramAddressSync(
      [Buffer.from("escrow"), Buffer.from(matchId)],
      program.programId
    );

    await program.methods
      .initializeMatch(matchId, stakeAmount)
      .accounts({
        escrow: escrowPda,
        playerA: playerA.publicKey,
        authority: authority.publicKey,
        systemProgram: anchor.web3.SystemProgram.programId,
      })
      .signers([playerA])
      .rpc();

    const escrow = await program.account.matchEscrow.fetch(escrowPda);
    assert.equal(escrow.matchId, matchId);
    assert.equal(escrow.stakeAmount.toNumber(), 100_000_000);
    assert.equal(escrow.status.waitingForPlayerB !== undefined, true);

    console.log("✅ Match created:", matchId);
    console.log("   Escrow PDA:", escrowPda.toString());
    console.log("   Stake:", stakeAmount.toNumber() / 1e9, "SOL");
  });

  it("Player B joins the match", async () => {
    const matchId = "test-match-002";
    const stakeAmount = new anchor.BN(50_000_000); // 0.05 SOL

    const playerA = anchor.web3.Keypair.generate();
    const playerB = anchor.web3.Keypair.generate();

    // Airdrop
    await provider.connection.requestAirdrop(playerA.publicKey, 1e9);
    await provider.connection.requestAirdrop(playerB.publicKey, 1e9);
    await new Promise(r => setTimeout(r, 1000)); // Wait for airdrop

    const [escrowPda] = anchor.web3.PublicKey.findProgramAddressSync(
      [Buffer.from("escrow"), Buffer.from(matchId)],
      program.programId
    );

    // Player A creates match
    await program.methods
      .initializeMatch(matchId, stakeAmount)
      .accounts({
        escrow: escrowPda,
        playerA: playerA.publicKey,
        authority: authority.publicKey,
        systemProgram: anchor.web3.SystemProgram.programId,
      })
      .signers([playerA])
      .rpc();

    // Player B joins
    await program.methods
      .joinMatch()
      .accounts({
        escrow: escrowPda,
        playerB: playerB.publicKey,
        systemProgram: anchor.web3.SystemProgram.programId,
      })
      .signers([playerB])
      .rpc();

    const escrow = await program.account.matchEscrow.fetch(escrowPda);
    assert.equal(escrow.totalDeposited.toNumber(), 100_000_000); // 2x stake
    assert.equal(escrow.status.active !== undefined, true);

    console.log("✅ Player B joined. Total staked:", escrow.totalDeposited.toNumber() / 1e9, "SOL");
  });

  it("Authority settles match — winner gets payout", async () => {
    const matchId = "test-match-003";
    const stakeAmount = new anchor.BN(100_000_000); // 0.1 SOL

    const playerA = anchor.web3.Keypair.generate();
    const playerB = anchor.web3.Keypair.generate();

    await provider.connection.requestAirdrop(playerA.publicKey, 1e9);
    await provider.connection.requestAirdrop(playerB.publicKey, 1e9);
    await new Promise(r => setTimeout(r, 1000));

    const [escrowPda] = anchor.web3.PublicKey.findProgramAddressSync(
      [Buffer.from("escrow"), Buffer.from(matchId)],
      program.programId
    );

    // Create + Join
    await program.methods
      .initializeMatch(matchId, stakeAmount)
      .accounts({
        escrow: escrowPda,
        playerA: playerA.publicKey,
        authority: authority.publicKey,
        systemProgram: anchor.web3.SystemProgram.programId,
      })
      .signers([playerA])
      .rpc();

    await program.methods
      .joinMatch()
      .accounts({
        escrow: escrowPda,
        playerB: playerB.publicKey,
        systemProgram: anchor.web3.SystemProgram.programId,
      })
      .signers([playerB])
      .rpc();

    // Record winner balance before
    const winnerBalanceBefore = await provider.connection.getBalance(playerA.publicKey);

    // Settle — Player A wins
    await program.methods
      .settleMatch(playerA.publicKey)
      .accounts({
        escrow: escrowPda,
        winner: playerA.publicKey,
        authority: authority.publicKey,
      })
      .rpc();

    const winnerBalanceAfter = await provider.connection.getBalance(playerA.publicKey);
    const payout = winnerBalanceAfter - winnerBalanceBefore;

    // Expected: 200_000_000 * 0.95 = 190_000_000 (minus 5% fee)
    assert.approximately(payout, 190_000_000, 10_000); // Allow small variance for rent

    console.log("✅ Match settled. Winner payout:", payout / 1e9, "SOL");
    console.log("   Platform fee: 5%");
  });
});
