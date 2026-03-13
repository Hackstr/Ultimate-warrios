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
    }

    /// <summary>
    /// Received at the start of each round's planning phase.
    /// </summary>
    [System.Serializable]
    public class RoundStartMessage
    {
        public int roundNumber;
        public float timeLimit;
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

        public string p1PickedUp;
        public string p2PickedUp;
    }

    #endregion
}
