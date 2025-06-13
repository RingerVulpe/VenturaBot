using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;

namespace VenturaBot.Services
{
    public class EconomyService : IEconomyService
    {
        private const string BalanceFile = "storage/economy.json";
        private const string EarnedFile = "storage/earned.json";
        private const string ContributionFile = "storage/contributions.json";

        private readonly ConcurrentDictionary<ulong, int> _balances;
        private readonly ConcurrentDictionary<ulong, int> _earned;
        private readonly ConcurrentDictionary<ulong, int> _contributions;

        public EconomyService()
        {
            _balances = Load<ConcurrentDictionary<ulong, int>>(BalanceFile)
                        ?? new ConcurrentDictionary<ulong, int>();
            _earned = Load<ConcurrentDictionary<ulong, int>>(EarnedFile)
                      ?? new ConcurrentDictionary<ulong, int>();
            _contributions = Load<ConcurrentDictionary<ulong, int>>(ContributionFile)
                             ?? new ConcurrentDictionary<ulong, int>();
        }

        public void AwardVenturans(ulong userId, int amount)
        {
            _balances.AddOrUpdate(userId, amount, (_, old) => old + amount);
            _earned.AddOrUpdate(userId, amount, (_, old) => old + amount);
            Save(BalanceFile, _balances);
            Save(EarnedFile, _earned);
        }

        public bool TryChargeVenturans(ulong userId, int amount)
        {
            bool success = false;
            _balances.AddOrUpdate(userId, _ =>
            {
                success = false;
                return 0;
            }, (_, old) =>
            {
                if (old >= amount)
                {
                    success = true;
                    return old - amount;
                }
                return old;
            });

            if (success)
            {
                Save(BalanceFile, _balances);
            }

            return success;
        }

        public int GetBalance(ulong userId)
            => _balances.TryGetValue(userId, out var bal) ? bal : 0;

        public int GetTotalEarned(ulong userId)
            => _earned.TryGetValue(userId, out var earned) ? earned : 0;

        public void AddContribution(ulong userId, int amount)
        {
            _contributions.AddOrUpdate(userId, amount, (_, old) => old + amount);
            Save(ContributionFile, _contributions);
        }

        public int GetTotalContributions(ulong userId)
            => _contributions.TryGetValue(userId, out var contrib) ? contrib : 0;

        public Dictionary<ulong, (int earned, int contributions)> GetAllStats()
            => _earned.Keys
               .Union(_contributions.Keys)
               .ToDictionary(
                   id => id,
                   id => (
                       _earned.TryGetValue(id, out var e) ? e : 0,
                       _contributions.TryGetValue(id, out var c) ? c : 0
                   )
               );

        private T Load<T>(string file) where T : class
        {
            if (!File.Exists(file)) return null;
            var json = File.ReadAllText(file);
            return JsonSerializer.Deserialize<T>(json);
        }

        private void Save<T>(string file, T data)
        {
            var json = JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions { WriteIndented = true }
            );
            File.WriteAllText(file, json);
        }
    }
}