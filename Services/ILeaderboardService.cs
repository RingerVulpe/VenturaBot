// ─── LeaderboardService.cs ─────────────────────────────────────────────────────────
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace VenturaBot.Services
{
    public interface ILeaderboardService
    {
        /// <summary>
        /// Returns top N users by Venturans earned, then contributions.
        /// </summary>
        Task<List<(ulong userId, int earned, int contributions)>> GetTopStatsAsync(int topN);
    }

    public class LeaderboardService : ILeaderboardService
    {
        private readonly IEconomyService _economyService;

        public LeaderboardService(IEconomyService economyService)
            => _economyService = economyService;

        public Task<List<(ulong userId, int earned, int contributions)>> GetTopStatsAsync(int topN)
        {
            var stats = _economyService.GetAllStats();
            var top = stats
                .OrderByDescending(kv => kv.Value.earned)
                .ThenByDescending(kv => kv.Value.contributions)
                .Take(topN)
                .Select(kv => (kv.Key, kv.Value.earned, kv.Value.contributions))
                .ToList();

            return Task.FromResult(top);
        }
    }
}