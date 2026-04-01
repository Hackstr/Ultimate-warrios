use anchor_lang::prelude::*;
use anchor_lang::system_program;

declare_id!("DEob7xnXNwehZSKFjLecNe68fqQVp1nxBA71AxTH8XMv"); // Replace after first deploy

/// Tactical Duelist — On-chain escrow for staked matches.
///
/// Flow:
/// 1. Player A calls `initialize_match` — creates escrow PDA, deposits stake
/// 2. Player B calls `join_match` — deposits same stake amount
/// 3. Server authority calls `settle_match` — sends 2x stake to winner (minus fee)
/// 4. If timeout, authority calls `cancel_match` — refunds both players
#[program]
pub mod tactical_duelist {
    use super::*;

    /// Create a new match escrow. Player A deposits their stake.
    pub fn initialize_match(
        ctx: Context<InitializeMatch>,
        match_id: String,
        stake_amount: u64,
    ) -> Result<()> {
        require!(stake_amount > 0, ErrorCode::InvalidStakeAmount);
        require!(match_id.len() <= 64, ErrorCode::MatchIdTooLong);

        let escrow = &mut ctx.accounts.escrow;
        escrow.match_id = match_id;
        escrow.player_a = ctx.accounts.player_a.key();
        escrow.player_b = Pubkey::default(); // Set when player B joins
        escrow.stake_amount = stake_amount;
        escrow.total_deposited = stake_amount;
        escrow.status = MatchStatus::WaitingForPlayerB;
        escrow.authority = ctx.accounts.authority.key();
        escrow.created_at = Clock::get()?.unix_timestamp;
        escrow.bump = ctx.bumps.escrow;

        // Transfer SOL from player A to escrow PDA
        system_program::transfer(
            CpiContext::new(
                ctx.accounts.system_program.to_account_info(),
                system_program::Transfer {
                    from: ctx.accounts.player_a.to_account_info(),
                    to: ctx.accounts.escrow.to_account_info(),
                },
            ),
            stake_amount,
        )?;

        msg!("Match {} initialized. Player A staked {} lamports.", escrow.match_id, stake_amount);
        Ok(())
    }

    /// Player B joins the match and deposits their stake.
    pub fn join_match(ctx: Context<JoinMatch>) -> Result<()> {
        let escrow = &mut ctx.accounts.escrow;

        require!(
            escrow.status == MatchStatus::WaitingForPlayerB,
            ErrorCode::MatchNotJoinable
        );

        escrow.player_b = ctx.accounts.player_b.key();
        escrow.total_deposited += escrow.stake_amount;
        escrow.status = MatchStatus::Active;

        // Transfer SOL from player B to escrow PDA
        system_program::transfer(
            CpiContext::new(
                ctx.accounts.system_program.to_account_info(),
                system_program::Transfer {
                    from: ctx.accounts.player_b.to_account_info(),
                    to: ctx.accounts.escrow.to_account_info(),
                },
            ),
            escrow.stake_amount,
        )?;

        msg!("Player B joined match {}. Total staked: {} lamports.", escrow.match_id, escrow.total_deposited);
        Ok(())
    }

    /// Server authority settles the match — winner receives the pot.
    /// 5% platform fee goes to authority.
    pub fn settle_match(
        ctx: Context<SettleMatch>,
        winner: Pubkey,
    ) -> Result<()> {
        let escrow = &ctx.accounts.escrow;

        require!(
            escrow.status == MatchStatus::Active,
            ErrorCode::MatchNotActive
        );
        require!(
            winner == escrow.player_a || winner == escrow.player_b,
            ErrorCode::InvalidWinner
        );

        let total = escrow.total_deposited;
        let fee = total / 20; // 5% platform fee
        let payout = total - fee;

        let seeds = &[
            b"escrow",
            escrow.match_id.as_bytes(),
            &[escrow.bump],
        ];
        let signer_seeds = &[&seeds[..]];

        // Transfer payout to winner
        **ctx.accounts.escrow.to_account_info().try_borrow_mut_lamports()? -= payout;
        **ctx.accounts.winner.to_account_info().try_borrow_mut_lamports()? += payout;

        // Transfer fee to authority
        if fee > 0 {
            **ctx.accounts.escrow.to_account_info().try_borrow_mut_lamports()? -= fee;
            **ctx.accounts.authority.to_account_info().try_borrow_mut_lamports()? += fee;
        }

        msg!("Match {} settled. Winner: {}. Payout: {} lamports. Fee: {} lamports.",
            escrow.match_id, winner, payout, fee);

        Ok(())
    }

    /// Cancel match — refund both players (used for timeout/disconnect).
    pub fn cancel_match(ctx: Context<CancelMatch>) -> Result<()> {
        let escrow = &ctx.accounts.escrow;

        require!(
            escrow.status == MatchStatus::WaitingForPlayerB || escrow.status == MatchStatus::Active,
            ErrorCode::MatchAlreadySettled
        );

        let stake = escrow.stake_amount;

        // Refund player A
        **ctx.accounts.escrow.to_account_info().try_borrow_mut_lamports()? -= stake;
        **ctx.accounts.player_a.to_account_info().try_borrow_mut_lamports()? += stake;

        // Refund player B (if they joined)
        if escrow.status == MatchStatus::Active {
            **ctx.accounts.escrow.to_account_info().try_borrow_mut_lamports()? -= stake;
            **ctx.accounts.player_b.to_account_info().try_borrow_mut_lamports()? += stake;
        }

        msg!("Match {} cancelled. Refunds issued.", escrow.match_id);
        Ok(())
    }
}

// ── Account Structures ──

#[account]
#[derive(InitSpace)]
pub struct MatchEscrow {
    #[max_len(64)]
    pub match_id: String,
    pub player_a: Pubkey,
    pub player_b: Pubkey,
    pub stake_amount: u64,
    pub total_deposited: u64,
    pub status: MatchStatus,
    pub authority: Pubkey,
    pub created_at: i64,
    pub bump: u8,
}

#[derive(AnchorSerialize, AnchorDeserialize, Clone, Copy, PartialEq, Eq, InitSpace)]
pub enum MatchStatus {
    WaitingForPlayerB,
    Active,
    Settled,
    Cancelled,
}

// ── Instruction Contexts ──

#[derive(Accounts)]
#[instruction(match_id: String)]
pub struct InitializeMatch<'info> {
    #[account(
        init,
        payer = player_a,
        space = 8 + MatchEscrow::INIT_SPACE,
        seeds = [b"escrow", match_id.as_bytes()],
        bump
    )]
    pub escrow: Account<'info, MatchEscrow>,

    #[account(mut)]
    pub player_a: Signer<'info>,

    /// Server authority wallet — controls settlement
    /// CHECK: Just stores the pubkey, no data read
    pub authority: UncheckedAccount<'info>,

    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct JoinMatch<'info> {
    #[account(
        mut,
        seeds = [b"escrow", escrow.match_id.as_bytes()],
        bump = escrow.bump,
    )]
    pub escrow: Account<'info, MatchEscrow>,

    #[account(mut)]
    pub player_b: Signer<'info>,

    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct SettleMatch<'info> {
    #[account(
        mut,
        seeds = [b"escrow", escrow.match_id.as_bytes()],
        bump = escrow.bump,
        close = authority, // Reclaim rent to authority
    )]
    pub escrow: Account<'info, MatchEscrow>,

    /// Winner's wallet — receives payout
    /// CHECK: Any valid account can receive SOL
    #[account(mut)]
    pub winner: UncheckedAccount<'info>,

    /// Server authority — must match escrow.authority
    #[account(
        mut,
        constraint = authority.key() == escrow.authority @ ErrorCode::UnauthorizedSettler
    )]
    pub authority: Signer<'info>,
}

#[derive(Accounts)]
pub struct CancelMatch<'info> {
    #[account(
        mut,
        seeds = [b"escrow", escrow.match_id.as_bytes()],
        bump = escrow.bump,
        close = authority,
    )]
    pub escrow: Account<'info, MatchEscrow>,

    /// CHECK: Player A receives refund
    #[account(mut, constraint = player_a.key() == escrow.player_a)]
    pub player_a: UncheckedAccount<'info>,

    /// CHECK: Player B receives refund (if joined)
    #[account(mut)]
    pub player_b: UncheckedAccount<'info>,

    /// Server authority
    #[account(
        mut,
        constraint = authority.key() == escrow.authority @ ErrorCode::UnauthorizedSettler
    )]
    pub authority: Signer<'info>,
}

// ── Errors ──

#[error_code]
pub enum ErrorCode {
    #[msg("Stake amount must be greater than 0")]
    InvalidStakeAmount,
    #[msg("Match ID too long (max 64 chars)")]
    MatchIdTooLong,
    #[msg("Match is not in a joinable state")]
    MatchNotJoinable,
    #[msg("Match is not active")]
    MatchNotActive,
    #[msg("Match is already settled or cancelled")]
    MatchAlreadySettled,
    #[msg("Winner must be player A or player B")]
    InvalidWinner,
    #[msg("Only the authority can settle matches")]
    UnauthorizedSettler,
}
