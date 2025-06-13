using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using VenturaBot.Models;

namespace VenturaBot.Services
{
    public class RaffleService
    {
        private const string DataFile = "storage/raffle_items.json";
        private readonly IEconomyService _economy;
        private readonly List<RaffleItem> _items;
        private readonly ConcurrentDictionary<string, List<RaffleEntry>> _entries =
            new ConcurrentDictionary<string, List<RaffleEntry>>();

        public RaffleService(IEconomyService economy)
        {
            _economy = economy;
            if (File.Exists(DataFile))
            {
                var json = File.ReadAllText(DataFile);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                _items = JsonSerializer.Deserialize<List<RaffleItem>>(json, opts) ?? new List<RaffleItem>();
            }
            else
            {
                _items = new List<RaffleItem>();
            }
        }

        public RaffleItem GetItem(string id)
            => _items.FirstOrDefault(i => i.Id == id)
               ?? throw new KeyNotFoundException($"Raffle item '{id}' not found.");

        public Task<bool> EnterRaffleAsync(string raffleId, ulong userId)
        {
            var item = GetItem(raffleId);
            var userEntries = _entries.GetOrAdd(raffleId, _ => new List<RaffleEntry>());

            if (userEntries.Any(e => e.UserId == userId))
                return Task.FromResult(false);

            int cost = item.EntryCost * (userEntries.Count + 1);
            if (!_economy.TryChargeVenturans(userId, cost))
                return Task.FromResult(false);

            userEntries.Add(new RaffleEntry { UserId = userId, Tickets = 1 });
            return Task.FromResult(true);
        }

        public IEnumerable<ulong> DrawWinners(string raffleId)
        {
            var entries = _entries.GetValueOrDefault(raffleId, new List<RaffleEntry>());
            var item = GetItem(raffleId);

            var pool = entries.SelectMany(e => Enumerable.Repeat(e.UserId, e.Tickets)).ToList();
            var rnd = new Random();
            var winners = new HashSet<ulong>();

            while (winners.Count < Math.Min(item.Stock, pool.Count))
            {
                winners.Add(pool[rnd.Next(pool.Count)]);
            }

            return winners;
        }

        public void ClearEntries(string raffleId)
            => _entries.TryRemove(raffleId, out _);
    }
}