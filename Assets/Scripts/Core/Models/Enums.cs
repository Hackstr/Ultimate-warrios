namespace TacticalDuelist.Core.Models
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    public enum ActionType
    {
        Move,
        TurnLeft,
        TurnRight,
        TurnAround,
        Shoot,
        Wait,
        Special
    }

    public enum GamePhase
    {
        Matchmaking,
        HeroSelect,
        Planning,
        Execution,
        PostRound,
        PostMatch
    }

    public enum MatchResult
    {
        Player1Win,
        Player2Win,
        Draw
    }

    public enum RoundResult
    {
        Player1Kill,
        Player2Kill,
        MutualCancel,
        NoKill
    }

    public enum TileType
    {
        Empty,
        Wall,
        DestructibleWall,
        DangerZone,
        OutOfBounds
    }

    public enum PickupType
    {
        None = -1,
        ArmorShard,
        IntelOrb,
        SpeedBoost,
        RangeBoost
    }

    public enum SpecialAbility
    {
        Ricochet,
        Push,
        Blink,
        Scan,
        PhaseShot,
        Bomb,
        Barrier,
        Cloak,
        Turret,
        Charge,
        Pierce,
        Decoy
    }
}
