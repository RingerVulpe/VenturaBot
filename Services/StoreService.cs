using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VenturaBot.Models;

namespace VenturaBot.Services
{
    public class StoreService : IStoreService
    {
        private const string DataFile = "storage/store.json";
        private readonly Dictionary<string, StoreItem> _items;
        private readonly IEconomyService _economyService;
        private readonly object _lock = new();

        public StoreService(IEconomyService economyService)
        {
            _economyService = economyService;
            List<StoreItem> list;

            if (!File.Exists(DataFile))
            {
                list = new List<StoreItem>();
            }
            else
            {
                try
                {
                    var json = File.ReadAllText(DataFile);
                    list = JsonSerializer.Deserialize<List<StoreItem>>(json)
                           ?? new List<StoreItem>();
                }
                catch (JsonException)
                {
                    // invalid JSON on disk: reset to empty catalog
                    list = new List<StoreItem>();
                    File.WriteAllText(DataFile, "[]");
                }
            }

            _items = list.ToDictionary(i => i.Id, i => i);
        }

        #region Read
        public IReadOnlyList<StoreItem> GetAllItems()
            => _items.Values.ToList();

        public IReadOnlyList<StoreItem> GetVisibleItems()
            => _items.Values.Where(i => !i.IsHidden).ToList();

        public int GetPrice(string itemId)
        {
            if (!_items.TryGetValue(itemId, out var item))
                throw new ArgumentException($"Unknown item '{itemId}'.");
            var factor = 1.0 + (double)item.Demand / (item.Stock + 1);
            return (int)Math.Ceiling(item.BasePrice * factor);
        }
        #endregion

        #region Purchase
        public bool TryRedeem(string itemId, ulong userId)
        {
            if (!_items.TryGetValue(itemId, out var item) || item.Stock <= 0)
                return false;
            var price = GetPrice(itemId);
            if (!_economyService.TryChargeVenturans(userId, price))
                return false;

            lock (_lock)
            {
                item.Stock--;
                item.Demand++;
                Save();
            }
            return true;
        }
        #endregion

        #region Admin / Stock Management
        public bool AddItem(StoreItem item)
        {
            lock (_lock)
            {
                if (_items.ContainsKey(item.Id))
                    return false;

                _items[item.Id] = item;
                Save();
                return true;
            }
        }

        public (bool Success, StoreItem? Item, string? ErrorMessage) UpdateStock(string itemId, int delta)
        {
            lock (_lock)
            {
                if (!_items.TryGetValue(itemId, out var item))
                    return (false, null, $"No item with ID `{itemId}` found.");

                var newStock = item.Stock + delta;
                if (newStock < 0)
                    return (false, item, "Cannot drop below 0 stock.");

                item.Stock = newStock;
                Save();
                return (true, item, null);
            }
        }

        public (bool Success, StoreItem? Item, string? ErrorMessage) HideItem(string itemId)
        {
            lock (_lock)
            {
                if (!_items.TryGetValue(itemId, out var item))
                    return (false, null, $"No item with ID `{itemId}` found.");

                item.IsHidden = true;
                Save();
                return (true, item, null);
            }
        }

        public (bool Success, StoreItem? Item, string? ErrorMessage) ShowItem(string itemId)
        {
            lock (_lock)
            {
                if (!_items.TryGetValue(itemId, out var item))
                    return (false, null, $"No item with ID `{itemId}` found.");

                item.IsHidden = false;
                Save();
                return (true, item, null);
            }
        }
        #endregion

        private void Save()
        {
            var json = JsonSerializer.Serialize(
                _items.Values.ToList(),
                new JsonSerializerOptions { WriteIndented = true }
            );
            File.WriteAllText(DataFile, json);
        }
    }
}
