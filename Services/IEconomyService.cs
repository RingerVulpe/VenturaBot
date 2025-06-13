using System.Collections.Generic;

namespace VenturaBot.Services
{
    public interface IEconomyService
    {
        /// <summary>
        /// Add Venturans to a user’s balance and record total earned.
        /// </summary>
        void AwardVenturans(ulong userId, int amount);

        /// <summary>
        /// Attempt to deduct Venturans from a user’s balance.
        /// Returns true on success, false if insufficient funds.
        /// </summary>
        bool TryChargeVenturans(ulong userId, int amount);

        /// <summary>
        /// Get a user’s current Venturans balance.
        /// </summary>
        int GetBalance(ulong userId);

        /// <summary>
        /// Get a user’s total lifetime Venturans earned.
        /// </summary>
        int GetTotalEarned(ulong userId);

        /// <summary>
        /// Record material contribution units for a user.
        /// </summary>
        void AddContribution(ulong userId, int amount);

        /// <summary>
        /// Get a user’s total material contribution units.
        /// </summary>
        int GetTotalContributions(ulong userId);

        /// <summary>
        /// Get stats (earned Venturans and contributions) for all users.
        /// </summary>
        Dictionary<ulong, (int earned, int contributions)> GetAllStats();
    }
}