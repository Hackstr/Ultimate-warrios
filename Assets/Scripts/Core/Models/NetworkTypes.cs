using UnityEngine;

namespace TacticalDuelist.Core.Models
{
    #region Client → Server

    /// <summary>
    /// Sent during commit phase. Hash = SHA256(JSON(actions) + nonce).
    /// Server stores hash until reveal.
    /// </summary>
    [System.Serializable]
    public class CommitMessage
    {
        public string Hash;
    }

    /// <summary>
    /// Sent during reveal phase. Server verifies SHA256(JSON(Actions) + Nonce) == stored hash.
    /// </summary>
    [System.Serializable]
    public class RevealMessage
    {
        public ActionType[] Actions;
        public string Nonce;
    }

    /// <summary>
    /// Sent to request matchmaking queue entry.
    /// </summary>
    [System.Serializable]
    public class FindMatchMessage
    {
        public string HeroId;
    }

    #endregion

    #region Server → Client

    /// <summary>
    /// Received when matchmaking finds an opponent.
    /// </summary>
    [System.Serializable]
    public class MatchFoundMessage
    {
        public string MatchId;
        public int PlayerSlot;
        public string OpponentName;
        public string OpponentHeroId;
        public string MapId;
        public SerializableVector2Int P1Spawn;
        public SerializableVector2Int P2Spawn;
        public Direction P1Facing;
        public Direction P2Facing;
    }

    /// <summary>
    /// Received at the start of each round's planning phase.
    /// </summary>
    [System.Serializable]
    public class RoundStartMessage
    {
        public int RoundNumber;
        public float PlanningTime;
    }

    /// <summary>
    /// Received after both players reveal. Contains full step-by-step resolution.
    /// </summary>
    [System.Serializable]
    public class RoundResultsMessage
    {
        public StepResultData[] Steps;
    }

    /// <summary>
    /// Received when the match ends.
    /// </summary>
    [System.Serializable]
    public class MatchEndMessage
    {
        public MatchResult Result;
        public int RankDelta;
        public int XpGained;
        public string ReplayId;
    }

    #endregion

    #region Serialization Helpers

    /// <summary>
    /// JSON-serializable substitute for Vector2Int (Unity's Vector2Int
    /// doesn't serialize reliably over network).
    /// </summary>
    [System.Serializable]
    public struct SerializableVector2Int
    {
        public int x;
        public int y;

        public SerializableVector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public SerializableVector2Int(Vector2Int v)
        {
            x = v.x;
            y = v.y;
        }

        public Vector2Int ToVector2Int() => new(x, y);

        public static implicit operator Vector2Int(SerializableVector2Int s) => new(s.x, s.y);
        public static implicit operator SerializableVector2Int(Vector2Int v) => new(v);
    }

    /// <summary>
    /// Network-serializable version of StepResult.
    /// Used in RoundResultsMessage to transmit resolution data from server.
    /// </summary>
    [System.Serializable]
    public class StepResultData
    {
        public int StepIndex;

        public ActionType P1Action;
        public SerializableVector2Int P1StartPos;
        public SerializableVector2Int P1EndPos;
        public Direction P1StartFacing;
        public Direction P1EndFacing;

        public ActionType P2Action;
        public SerializableVector2Int P2StartPos;
        public SerializableVector2Int P2EndPos;
        public Direction P2StartFacing;
        public Direction P2EndFacing;

        public bool P1Fired;
        public bool P2Fired;
        public bool P1Hit;
        public bool P2Hit;
        public bool MutualCancel;
        public bool P1ArmorBroken;
        public bool P2ArmorBroken;
        public bool P1Eliminated;
        public bool P2Eliminated;
    }

    #endregion
}
