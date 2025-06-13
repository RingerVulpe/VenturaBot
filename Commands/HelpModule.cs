using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace VenturaBot.Commands
{
    public class HelpModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("vhelp", "List all VenturaBot commands and how to use them.")]
        public async Task VHelpAsync()
        {
            var embed = new EmbedBuilder()
                .WithTitle("VenturaBot Command Reference")
                .WithColor(Color.Blue)
                .WithDescription("Here are the commands you can use with VenturaBot:")
                .AddField("/vregister", "Register yourself with the guild bot to start earning XP and Venturans.", false)
                .AddField("/vprofile", "View your current level, rank, XP, and profile details.", false)
                .AddField("/vroll <dice>", "Roll dice (e.g., `/vroll 1d20+5`) for fun or events.", false)
                .AddField("/vdunefact", "Get a random Dune fact.", false)
                .AddField("/vhelp", "Show this help message.", false)
                .WithFooter("Use /vprofile at any time to check your current rank and XP thresholds.")
                .WithTimestamp(System.DateTimeOffset.UtcNow)
                .Build();

            await RespondAsync(embed: embed, ephemeral: true);
        }
    }
}
