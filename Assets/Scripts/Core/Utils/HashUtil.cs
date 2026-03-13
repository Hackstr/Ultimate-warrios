using System;
using System.Security.Cryptography;
using System.Text;
using TacticalDuelist.Core.Models;
using UnityEngine;

namespace TacticalDuelist.Core.Utils
{
    /// <summary>
    /// Commit-reveal hashing for simultaneous action submission.
    /// Hash = SHA256( JSON(actions) + nonce ).
    /// Must produce identical output to the NestJS server implementation.
    /// </summary>
    public static class HashUtil
    {
        /// <summary>
        /// Computes SHA-256 hash for the commit phase.
        /// Format: SHA256( actionsJson + nonce )  where actionsJson is a JSON array.
        /// </summary>
        /// <param name="actions">Ordered action list for the round.</param>
        /// <param name="nonce">Random string known only to this client until reveal.</param>
        /// <returns>Lowercase hex SHA-256 hash string.</returns>
        public static string ComputeActionHash(ActionType[] actions, string nonce)
        {
            if (actions == null) throw new ArgumentNullException(nameof(actions));
            if (string.IsNullOrEmpty(nonce)) throw new ArgumentException("Nonce must not be empty", nameof(nonce));

            string actionsJson = ActionsToJson(actions);
            string preimage = actionsJson + nonce;

            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(preimage));
            return BytesToHex(hashBytes);
        }

        /// <summary>
        /// Generates a cryptographically secure random nonce (32 hex characters).
        /// </summary>
        public static string GenerateNonce()
        {
            byte[] bytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return BytesToHex(bytes);
        }

        /// <summary>
        /// Verifies that a hash matches the given actions + nonce.
        /// Used for local debugging; the authoritative check is server-side.
        /// </summary>
        public static bool VerifyHash(string hash, ActionType[] actions, string nonce)
        {
            return string.Equals(hash, ComputeActionHash(actions, nonce), StringComparison.Ordinal);
        }

        /// <summary>
        /// Serializes actions to a JSON array of integers.
        /// Must match server's JSON.stringify(actions) which emits numeric enum values: [0,4,5]
        /// </summary>
        private static string ActionsToJson(ActionType[] actions)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < actions.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append((int)actions[i]);
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static string BytesToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
