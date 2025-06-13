using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using VenturaBot.Builders;
using VenturaBot.Models;
using VenturaBot.Services;

namespace VenturaBot.Modules
{
    [Group("vstore", "Browse & manage the guild store")]
    public class StoreModule : InteractionModuleBase<SocketInteractionContext>
    {
        private const ulong STORE_CHANNEL_ID = 1382846269323612224;  // ← your channel

        private readonly IStoreService _storeService;
        private readonly IEconomyService _economy;
        private readonly StoreEmbedBuilder _embedBuilder;

        public StoreModule(
            IStoreService storeService,
            IEconomyService economy,
            StoreEmbedBuilder embedBuilder)
        {
            _storeService = storeService;
            _economy = economy;
            _embedBuilder = embedBuilder;
        }

        [SlashCommand("browse", "Show the current catalog here")]
        public async Task BrowseAsync(
            [Summary("category", "Optional filter")] string category = null)
        {
            var items = _storeService.GetVisibleItems()
                .Where(i => category == null
                         || i.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!items.Any())
            {
                await RespondAsync("❌ No items found.", ephemeral: true);
                return;
            }

            await DeferAsync(ephemeral: false);

            var channel = Context.Guild.GetTextChannel(STORE_CHANNEL_ID);
            if (channel == null)
            {
                await FollowupAsync("❌ Store channel not found.", ephemeral: true);
                return;
            }

            foreach (var item in items)
            {
                var embed = _embedBuilder.BuildItemEmbed(item, Context.User.Id);
                var components = _embedBuilder.BuildItemButtons(item, Context.User.Id);
                await FollowupAsync(embed: embed, components: components);
            }
        }
        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("set-stock", "Set an item’s stock to an exact value and update its message")]
        public async Task SetStockAsync(
    [Summary("itemId", "ID of the item")] string itemId,
    [Summary("stock", "New absolute stock")] int newStock
)
        {
            // 1) Lookup existing item
            var item = _storeService.GetAllItems()
                        .FirstOrDefault(i => i.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                await RespondAsync($"❌ No item with ID `{itemId}` found.", ephemeral: true);
                return;
            }

            // 2) Compute delta and update
            var delta = newStock - item.Stock;
            var (success, updated, err) = _storeService.UpdateStock(itemId, delta);
            if (!success)
            {
                await RespondAsync($"❌ {err}", ephemeral: true);
                return;
            }

            // 3) Find and update the live message in the store channel
            var guild = Context.Guild as SocketGuild;
            var storeChan = guild?.GetTextChannel(STORE_CHANNEL_ID);
            if (storeChan != null)
            {
                var msgs = await storeChan.GetMessagesAsync(100).FlattenAsync();
                var msg = msgs
                    .OfType<IUserMessage>()
                    .FirstOrDefault(m =>
                        m.Embeds.FirstOrDefault()?.Title?.Equals(updated.Name, StringComparison.OrdinalIgnoreCase) == true
                    );

                if (msg != null)
                {
                    var newEmbed = _embedBuilder.BuildItemEmbed(updated, Context.User.Id);
                    var newComponents = _embedBuilder.BuildItemButtons(updated, Context.User.Id);
                    await msg.ModifyAsync(m =>
                    {
                        m.Embed = newEmbed;
                        m.Components = newComponents;
                    });

                    await RespondAsync($"✅ **{updated.Name}** stock set to **{updated.Stock}**.", ephemeral: true);
                    return;
                }
            }

            // 4) Fallback if we couldn’t find the message
            await RespondAsync(
                $"⚠️ Stock set to **{updated.Stock}**, but I couldn’t locate its message. Run `/store refresh` to rebuild.",
                ephemeral: true
            );
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("refresh", "Force‐refresh the catalog display")]
        public async Task RefreshAsync(
            [Summary("category", "Optional filter")] string category = null)
        {
            // 1) Acknowledge immediately
            await RespondAsync("🔄 Refreshing catalog…", ephemeral: true);

            // 2) Fetch the store channel
            var channel = (Context.Guild as SocketGuild)?
                              .GetTextChannel(STORE_CHANNEL_ID);
            if (channel == null)
            {
                await FollowupAsync("❌ Store channel not found.", ephemeral: true);
                return;
            }

            // 3) Delete recent messages (up to 100)
            var toDelete = await channel.GetMessagesAsync(100)
                                        .FlattenAsync();
            await channel.DeleteMessagesAsync(toDelete);

            // 4) Re‐post the catalog by directly calling BrowseAsync
            //    We need to simulate a new interaction in that channel,
            //    so we'll just invoke the logic inline here:

            var items = _storeService.GetVisibleItems()
                .Where(i => category == null
                         || i.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var item in items)
            {
                var embed = _embedBuilder.BuildItemEmbed(item, Context.User.Id);
                var components = _embedBuilder.BuildItemButtons(item, Context.User.Id);
                await channel.SendMessageAsync(embed: embed, components: components);
            }

            // 5) Confirm completion
            await FollowupAsync($"✅ Catalog refreshed in {channel.Mention}.", ephemeral: true);
        }


        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("add", "Add a new item to the store catalog")]
        public async Task AddItemAsync(
            [Summary("id", "Unique item ID")] string id,
            [Summary("name", "Display name")] string name,
            [Summary("category", "Category")] string category,
            [Summary("desc", "Description")] string description,
            [Summary("price", "Base price")] int basePrice,
            [Summary("stock", "Starting stock")] int stock,
            [Summary("imageUrl", "Image URL")] string imageUrl
        )
        {
            var item = new StoreItem
            {
                Id = id,
                Name = name,
                Category = category,
                Description = description,
                BasePrice = basePrice,
                Stock = stock,
                Demand = 0,
                ImageUrl = imageUrl,
                IsHidden = false
            };

            if (_storeService.AddItem(item))
                await RespondAsync($"✅ Added **{name}** to the catalog.", ephemeral: true);
            else
                await RespondAsync($"❌ An item with ID `{id}` already exists.", ephemeral: true);
        }

        [ComponentInteraction("store_redeem:*")]
        public async Task RedeemItemAsync(string itemId)
        {
            if (_storeService.TryRedeem(itemId, Context.User.Id))
                await RespondAsync($"✅ You redeemed **{itemId}**!", ephemeral: true);
            else
                await RespondAsync($"❌ Could not redeem **{itemId}** (out of stock or insufficient funds).", ephemeral: true);
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("hide", "Hide an item from the catalog")]
        public async Task HideItemAsync([Summary("itemId", "Item ID")] string itemId)
        {
            var (ok, item, err) = _storeService.HideItem(itemId);
            if (ok)
                await RespondAsync($"🛑 **{item!.Name}** is now hidden.", ephemeral: true);
            else
                await RespondAsync(err, ephemeral: true);
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("show", "Un-hide an item from the catalog")]
        public async Task ShowItemAsync([Summary("itemId", "Item ID")] string itemId)
        {
            var (ok, item, err) = _storeService.ShowItem(itemId);
            if (ok)
                await RespondAsync($"✅ **{item!.Name}** is now visible again.", ephemeral: true);
            else
                await RespondAsync(err, ephemeral: true);
        }
    }
}
