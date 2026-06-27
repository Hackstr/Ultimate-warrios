using UnityEngine;

namespace TacticalDuelist.Core.Models
{
    // All field names use camelCase to match NestJS server JSON payloads.
    // Unity's JsonUtility serializes field names as-is, so casing must match exactly.

    #region Client → Server

    /// <summary>
    /// Sent during commit phase. Hash = SHA256(JSON(actions) + nonce).
    /// </summary>
    [System.Serializable]
    public class CommitMessage
    {
        public string hash;
    }

    /// <summary>
    /// Sent during reveal phase. Server verifies SHA256(JSON(actions) + nonce) == stored hash.
    /// </summary>
    [System.Serializable]
    public class RevealMessage
    {
        public int[] actions;
        public string nonce;
    }

    /// <summary>
    /// Sent to request matchmaking queue entry.
    /// </summary>
    [System.Serializable]
    public class FindMatchMessage
    {
        public string heroId;
        public int rankTier;
    }

    /// <summary>
    /// Sent to rejoin an active match after reconnection.
    /// Server finds the match by player auth token — no matchId needed.
    /// </summary>
    [System.Serializable]
    public class RejoinMessage
    {
        // Empty — server identifies match via JWT playerId
    }

    #endregion

    #region Server → Client

    /// <summary>
    /// Received when matchmaking finds an opponent.
    /// Fields are perspective-relative: "your" = this client, "opponent" = the other.
    /// </summary>
    [System.Serializable]
    public class MatchFoundMessage
    {
        public string matchId;
        public string opponentName;
        public string opponentHeroId;
        public string mapId;
        public int mapWidth;
        public int mapHeight;
        public SerializableVector2Int yourSpawn;
        public SerializableVector2Int opponentSpawn;
        public int yourFacing;
        public int opponentFacing;
        public int[][] gridData;
    }

    /// <summary>
    /// Received at the start of each round's planning phase.
    /// </summary>
    [System.Serializable]
    public class RoundStartMessage
    {
        public int roundNumber;
        public float timeLimit;
        public SerializableVector2Int[] shrinkZone;
    }

    /// <summary>
    /// Received after both players reveal. Contains full step-by-step resolution.
    /// </summary>
    [System.Serializable]
    public class RoundResultsMessage
    {
        public StepResultData[] steps;
    }

    /// <summary>
    /// Received when the match ends.
    /// winner: 0 = Player1Win, 1 = Player2Win, 2 = Draw (maps to MatchResult enum).
    /// </summary>
    [System.Serializable]
    public class MatchEndMessage
    {
        public int winner;
        public int ratingDelta;
        public int coinsEarned;
    }

    /// <summary>
    /// Received on match/matchmaking errors.
    /// </summary>
    [System.Serializable]
    public class MatchErrorMessage
    {
        public string code;
        public string message;
    }

    /// <summary>
    /// Received in response to match:rejoin. Contains full match state snapshot.
    /// </summary>
    [System.Serializable]
    public class RejoinAckMessage
    {
        public bool success;
        public string error;
        public RejoinStateData state;
    }

    [System.Serializable]
    public class RejoinStateData
    {
        public string matchId;
        public int currentRound;
        public string phase; // "planning", "committed", "resolving"
        public string yourHeroId;
        public string opponentHeroId;
        public string opponentId;
        public string mapId;
        public int mapWidth;
        public int mapHeight;
        public SerializableVector2Int yourPos;
        public SerializableVector2Int opponentPos;
        public int yourFacing;
        public int opponentFacing;
        public bool yourAlive;
        public bool opponentAlive;
        public bool yourArmor;
        public bool opponentArmor;
        public bool hasCommitted;
        public float timeLimit;
        public int[][] gridData;
    }

    /// <summary>
    /// Received when opponent disconnects during a match.
    /// </summary>
    [System.Serializable]
    public class OpponentDisconnectedMessage
    {
        public string matchId;
        public int gracePeriod;
    }

    /// <summary>
    /// Received when opponent reconnects during grace period.
    /// </summary>
    [System.Serializable]
    public class OpponentReconnectedMessage
    {
        public string matchId;
    }

    #endregion

    #region Serialization Helpers

    /// <summary>
    /// JSON-serializable substitute for Vector2Int.
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
    /// All enum fields are transmitted as ints (ActionType, Direction).
    /// </summary>
    [System.Serializable]
    public class StepResultData
    {
        public int stepIndex;

        public int p1Action;
        public SerializableVector2Int p1StartPos;
        public SerializableVector2Int p1EndPos;
        public int p1StartFacing;
        public int p1EndFacing;

        public int p2Action;
        public SerializableVector2Int p2StartPos;
        public SerializableVector2Int p2EndPos;
        public int p2StartFacing;
        public int p2EndFacing;

        public bool p1Fired;
        public bool p2Fired;
        public bool p1Hit;
        public bool p2Hit;
        public bool mutualCancel;
        public bool p1ArmorBroken;
        public bool p2ArmorBroken;
        public bool p1Eliminated;
        public bool p2Eliminated;
        public bool p1Shielded;
        public bool p2Shielded;

        public string p1PickedUp;
        public string p2PickedUp;
    }

    #endregion

    #region Daily Rewards

    [System.Serializable]
    public class DailyRewardStatusResponse
    {
        public int streak;
        public bool canClaim;
        public int nextReward;
    }

    [System.Serializable]
    public class DailyRewardClaimResponse
    {
        public int coins;
        public int streak;
        public int reward;
        public string unlockedHero;
    }

    #endregion

    #region Match History

    [System.Serializable]
    public class MatchHistoryEntry
    {
        public string matchId;
        public bool isPlayer1;
        public string yourHero;
        public string opponentHero;
        public string opponentName;
        public string result;
        public string mapId;
        public string date;
    }

    [System.Serializable]
    public class MatchHistoryResponse
    {
        public MatchHistoryEntry[] items;
    }

    #endregion
}
