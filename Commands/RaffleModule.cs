// File: Modules/RaffleModule.cs
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using VenturaBot.Services;
using VenturaBot.Builders;
using VenturaBot.Models;

namespace VenturaBot.Modules
{
    public class RaffleModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly RaffleService _raffleService;
        private readonly RaffleEmbedBuilder _embedBuilder;

        // Channel where raffles are posted:
        private const ulong RAFFLE_CHANNEL_ID = 1382932899929657475; // set to your channel

        public RaffleModule(RaffleService raffleService, RaffleEmbedBuilder embedBuilder)
        {
            _raffleService = raffleService;
            _embedBuilder = embedBuilder;
        }

        [SlashCommand("vraffle-start", "Post a new raffle")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task StartAsync([Summary("raffleId")] string raffleId)
        {
            var item = _raffleService.GetItem(raffleId);
            var embed = _embedBuilder.BuildRaffleEmbed(item);
            var components = _embedBuilder.BuildEntryButton(raffleId);

            // Send to dedicated raffle channel
            var channel = Context.Guild.GetTextChannel(RAFFLE_CHANNEL_ID);
            if (channel == null)
            {
                await RespondAsync("❌ Raffle channel not found. Check configuration.", ephemeral: true);
                return;
            }

            var msg = await channel.SendMessageAsync(embed: embed, components: components);

            // Acknowledge to command user
            await RespondAsync($"✅ Posted raffle **{item.Name}** in {channel.Mention} (message ID {msg.Id}).", ephemeral: true);
        }

        [SlashCommand("raffle-draw", "Draw winners for a raffle")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DrawAsync([Summary("raffleId")] string raffleId)
        {
            var winners = _raffleService.DrawWinners(raffleId);
            var mentions = winners.Select(id => MentionUtils.MentionUser(id));
            await RespondAsync($"🏆 Winners: {string.Join(", ", mentions)}");
            _raffleService.ClearEntries(raffleId);
        }
    }
}