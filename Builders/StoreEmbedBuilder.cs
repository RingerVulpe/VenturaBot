using Discord;
using Discord.WebSocket;
using VenturaBot.Models;
using VenturaBot.Services;

namespace VenturaBot.Builders
{
    public class StoreEmbedBuilder
    {
        private readonly StoreService _storeService;
        private readonly IEconomyService _economy;

        public StoreEmbedBuilder(StoreService storeService, IEconomyService economy)
        {
            _storeService = storeService;
            _economy = economy;
        }

        public Embed BuildItemEmbed(StoreItem item, ulong userId)
        {
            var price = _storeService.GetPrice(item.Id);
            var balance = _economy.GetBalance(userId);
            var affordText = balance >= price
                ? " **You can afford this item** "
                : "🚫 **Oops! You're short on Venturans!** 🚫";

            var stockText = item.Stock > 0 ? $"**Stock Remaining:** {item.Stock}" : "🔥 **SOLD OUT!** 🔥";

            return new EmbedBuilder()
                .WithTitle($"🛒 {item.Name}")
                .WithDescription($@"
📜 **{item.Description}**

📂 **Category:** {item.Category}  
💰 **Price:** {price} Venturans  
📦 {stockText}

{affordText}")
                .WithThumbnailUrl(item.ImageUrl)
                .WithColor(item.Stock > 0 ? Color.Gold : Color.Red)
                .WithFooter("Powered by VenturaBot Store 🛍️")
                .WithCurrentTimestamp()
                .Build();
        }

        public MessageComponent BuildItemButtons(StoreItem item, ulong userId)
        {
            var price = _storeService.GetPrice(item.Id);
            var canAfford = _economy.GetBalance(userId) >= price;
            var disabled = item.Stock <= 0 || !canAfford;

            var redeemText = item.Stock > 0 ? "Redeem Now 🚀" : "Out of Stock 😔";

            return new ComponentBuilder()
                .WithButton(redeemText, customId: $"store_redeem:{item.Id}",
                            style: ButtonStyle.Success, disabled: disabled)
                .Build();
        }
    }
}
