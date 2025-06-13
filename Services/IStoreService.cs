namespace VenturaBot.Services
{
    using System.Collections.Generic;
    using VenturaBot.Models;

    public interface IStoreService
    {
        // --- Read / Browse ---
        /// <summary>All items, including hidden ones.</summary>
        IReadOnlyList<StoreItem> GetAllItems();

        /// <summary>Only items with IsHidden == false.</summary>
        IReadOnlyList<StoreItem> GetVisibleItems();

        /// <summary>Compute purchase price based on base price, demand, and stock.</summary>
        int GetPrice(string itemId);

        // --- Purchase / Redeem ---
        /// <summary>Attempts to charge and decrement stock. Returns true if successful.</summary>
        bool TryRedeem(string itemId, ulong userId);

        // --- Admin / Stock Management ---
        /// <summary>Hide an item (IsHidden = true). Returns (success, item, errorMsg).</summary>
        (bool Success, StoreItem? Item, string? ErrorMessage) HideItem(string itemId);

        /// <summary>Un-hide an item (IsHidden = false). Returns (success, item, errorMsg).</summary>
        (bool Success, StoreItem? Item, string? ErrorMessage) ShowItem(string itemId);

        /// <summary>Adjust stock by delta (can be positive or negative). Returns (success, item, errorMsg).</summary>
        (bool Success, StoreItem? Item, string? ErrorMessage) UpdateStock(string itemId, int delta);

        /// <summary>Add a completely new item to the catalog. Returns false if ID already exists.</summary>
        bool AddItem(StoreItem item);
    }
}
