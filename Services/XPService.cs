using System;
using VenturaBot.Data;

namespace VenturaBot.Services
{
    /// <summary>
    /// Static helper for calculating ranks, XP thresholds, and rank rewards.
    /// Uses a quadratic progression for rank thresholds (threshold ∝ rank²),
    /// and a “diminishing‐returns” formula for per‐task XP so that large quantities
    /// do not translate directly into massive XP gains.
    /// </summary>
    public static class XPService
    {
        /// <summary>
        /// Given a total XP, returns the user’s current rank.
        /// Quadratic threshold: threshold(n) = 100 * n².
        /// Invert via rank = floor( sqrt(xp / 100) ).
        /// </summary>
        public static int CalculateRank(int xp)
        {
            if (xp < 0)
                return 0;

            double raw = Math.Sqrt(xp / 100.0);
            return (int)Math.Floor(raw);
        }

        /// <summary>
        /// Given a rank, returns the XP threshold required to reach that rank.
        /// Quadratic formula: 100 × rank².
        /// e.g.
        ///   rank 0 → 0 XP
        ///   rank 1 → 100 XP
        ///   rank 2 → 400 XP
        ///   rank 3 → 900 XP
        /// </summary>
        public static int ThresholdForRank(int rank)
        {
            if (rank < 0)
                return 0;

            return 100 * rank * rank;
        }

        /// <summary>
        /// Given a total XP, returns how many more XP are needed to hit the next rank.
        /// </summary>
        public static int XPToNextRank(int xp)
        {
            int currentRank = CalculateRank(xp);
            int nextThreshold = ThresholdForRank(currentRank + 1);
            return nextThreshold - xp;
        }

        /// <summary>
        /// Small rewards table: description of what the user “gets” at a given rank.
        /// </summary>
        public static string GetRewardForRank(int rank)
        {
            return rank switch
            {
                1 => "Access to the \"Level 1\" role",
                2 => "Access to the \"Level 2\" role + a special color",
                3 => "Access to the \"Level 3\" role + a custom nickname badge",
                4 => "Access to the \"Level 4\" role + 5 free server boosts",
                // …and so on…
                _ => "No reward at this rank"
            };
        }

        /// <summary>
        /// Given a completed task, returns how much XP it should award.
        /// We now divide quantity by a “scaleFactor” before sqrt, so that
        /// dumping 50 000 ore won’t blow past your next few ranks.
        /// </summary>
        public static int CalculateXP(GuildTask task)
        {
            const double scaleFactor = 100.0;  // tune this up or down to taste
                                               // 1) Compute sqrt of (quantity ÷ scaleFactor), then multiply by tier
            double quantityFactor = Math.Sqrt(task.Quantity / scaleFactor);
            int scaledXp = (int)Math.Floor(task.Tier * quantityFactor);

            // 2) Add tip on top
            int baseXp = scaledXp + task.Tip;

            // 3) If verified, double it
            return task.Verified
                ? baseXp * 2
                : baseXp;
        }
    }
}
