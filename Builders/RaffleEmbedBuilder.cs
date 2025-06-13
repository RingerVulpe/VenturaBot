using Discord;
using Discord.Rest;
using Discord.WebSocket;
using VenturaBot.Models;

namespace VenturaBot.Builders
{
    public class RaffleEmbedBuilder
    {
        public Embed BuildRaffleEmbed(RaffleItem item)
        {
            return new EmbedBuilder()
                .WithTitle($"🎟️ Raffle: {item.Name}")
                .WithDescription($@"
{item.Description}

**Entry cost:** {item.EntryCost} venturans
**Remaining stock:** {item.Stock}
Click the button below to enter!")
                .WithImageUrl(item.ImageUrl)
                .WithColor(Color.Gold)
                .Build();
        }

        public MessageComponent BuildEntryButton(string raffleId)
        {
            return new ComponentBuilder()
                .WithButton("Enter Raffle", customId: $"raffle_enter:{raffleId}", style: ButtonStyle.Primary)
                .Build();
        }
    }
}