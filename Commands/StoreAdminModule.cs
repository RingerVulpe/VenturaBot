// Commands/StoreAdminModule.cs
using Discord;
using Discord.Interactions;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using VenturaBot.Services;
using VenturaBot.Services.Models;
using VenturaBot.Models;

namespace VenturaBot.Commands
{
    [Group("vadmin", "Guild store commands")]
    [RequireOwner]
    public class StoreAdminModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IStoreService _storeService;

        public StoreAdminModule(IStoreService storeService)
        {
            _storeService = storeService;
        }

        [SlashCommand("add", "Add a brand-new item to the store")]
        public async Task AddItemAsync(
            [Summary("itemId", "Unique ID (lowercase, no spaces)")] string itemId,
            [Summary("name", "Display name")] string name,
            [Summary("price", "Base price in Venturans")] int price,
            [Summary("stock", "Starting stock count")] int stock,
            [Summary("desc", "Optional description")] string description = null
        )
        {
            // 1️⃣ Acknowledge immediately
            await DeferAsync(ephemeral: true);

            var newItem = new StoreItem
            {
                Id = itemId.Trim().ToLowerInvariant(),
                Name = name.Trim(),
                BasePrice = price,
                Stock = stock,
                Description = description?.Trim() ?? ""
            };

            // 2️⃣ Do your work
            var added = _storeService.AddItem(newItem);

            // 3️⃣ Send a follow-up (not RespondAsync)
            if (added)
                await FollowupAsync(
                    $"✅ Added **{newItem.Name}** (`{newItem.Id}`) for {newItem.BasePrice:N0} Venturans, {newItem.Stock:N0} in stock.",
                    ephemeral: true
                );
            else
                await FollowupAsync(
                    $"❌ An item with ID `{newItem.Id}` already exists.",
                    ephemeral: true
                );
        }

        [SlashCommand("stock", "Adjust stock for an existing item")]
        public async Task UpdateStockAsync(
            [Summary("itemId", "ID of the item")] string itemId,
            [Summary("delta", "How many to add / subtract")] int delta
        )
        {
            await DeferAsync(ephemeral: true);

            var (ok, item, err) = _storeService.UpdateStock(itemId.Trim().ToLowerInvariant(), delta);

            if (ok)
                await FollowupAsync(
                    $"✅ Stock for **{item.Name}** (`{item.Id}`) is now {item.Stock:N0}.",
                    ephemeral: true
                );
            else
                await FollowupAsync(
                    $"❌ {err}",
                    ephemeral: true
                );
        }
    }
}
