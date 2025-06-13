using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;

namespace VenturaBot.Builders
{
    public class HallOfFameEmbedBuilder
    {
        private readonly DiscordSocketClient _client;
        private readonly ulong _guildId = 1377887213764874423; // your guild ID

        // URLs for flair
        private const string TrophyIconUrl = "https://media.discordapp.net/attachments/1379690732704759859/1382686167719350372/HALogo.png?ex=684c0e57&is=684abcd7&hm=387f165f396f5cef420d7a46a235000af328516273a41a6e408c800b0e22e8d9&=&format=webp&quality=lossless";
        private const string ConfettiGifUrl = "https://media.tenor.com/r0fE8NsnnZgAAAAM/dune-dune-part-two.gif";

        public HallOfFameEmbedBuilder(DiscordSocketClient client)
        {
            _client = client;
        }

        public Embed Build(IEnumerable<(ulong UserId, int Earned, int Contributions)> topStats)
        {
            var sb = new StringBuilder();
            int rank = 1;

            // TINY change: order by contributions descending
            var ordered = topStats
                .OrderByDescending(entry => entry.Contributions);

            foreach (var (userId, earned, contributions) in ordered)
            {
                var medal = rank switch
                {
                    1 => "🥇",
                    2 => "🥈",
                    3 => "🥉",
                    _ => "🔹"
                };
                var mention = $"<@{userId}>";

                sb
                  .AppendLine($"{medal} {mention}")
                  .AppendLine($"💰 Venturans Earned: **{earned:N0}**")
                  .AppendLine($"📦 Items Delivered: **{contributions:N0}**")
                  .AppendLine();

                rank++;
            }

            return new EmbedBuilder()
                .WithTitle("🏆 Guild Hall of Fame")
                .WithDescription(sb.ToString().TrimEnd())
                .WithColor(Color.Gold)
                .WithThumbnailUrl(TrophyIconUrl)
                .WithImageUrl(ConfettiGifUrl)
                .WithFooter(footer => footer
                    .WithText("Last updated")
                    .WithIconUrl(TrophyIconUrl))
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();
        }
    }
}
