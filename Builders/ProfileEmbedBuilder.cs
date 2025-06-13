using System;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using VenturaBot.Data;
using VenturaBot.Services;

namespace VenturaBot.Builders
{
    public class ProfileEmbedBuilder
    {
        private readonly IEconomyService _economyService;
        private readonly string _guildLogoUrl;

        private static readonly string[] RankRoleNames = new[]
        {
            "Initiate",
            "Drudge",
            "Binder",
            "Splicer",
            "Prospect",
            "Forgebearer",
            "Vanturian"
        };

        public ProfileEmbedBuilder(IEconomyService economyService, IConfiguration configuration)
        {
            _economyService = economyService;
            // Read the guild logo URL from config (fallback to a placeholder)
            _guildLogoUrl = configuration["GuildLogoUrl"]
                ?? "https://media.discordapp.net/attachments/1379690732704759859/1382686167719350372/HALogo.png?ex=684c0e57&is=684abcd7&hm=387f165f396f5cef420d7a46a235000af328516273a41a6e408c800b0e22e8d9&=&format=webp&quality=lossless";
        }

        public Embed Build(SocketGuildUser guildUser, GuildMember member)
        {
            // Fetch economy and XP stats
            int venturans = _economyService.GetBalance(member.UserId);
            int contributions = _economyService.GetTotalContributions(member.UserId);
            int xpCurrent = member.XP;
            int nextThreshold = XPService.ThresholdForRank(member.Rank + 1);
            int xpToNext = nextThreshold - xpCurrent;

            // Calculate percent of XP towards next rank
            double percent = nextThreshold > 0
                ? xpCurrent / (double)nextThreshold
                : 0;
            string progressBar = BuildProgressBar(percent);

            // Determine user's current rank role
            var rankRole = guildUser.Roles
                .Where(r => RankRoleNames.Contains(r.Name, StringComparer.OrdinalIgnoreCase))
                .OrderBy(r => Array.IndexOf(RankRoleNames, r.Name))
                .FirstOrDefault();
            string currentRank = rankRole?.Name ?? "Unranked";

            // Colors
            var duneColor = new Color(194, 178, 128);

            // Avatar: prefer custom, else placeholder
            const string placeholderAvatarUrl = "https://cdn.mos.cms.futurecdn.net/wb3hrNDpy2t9fkbgGXQjqG.jpg";
            bool hasCustomAvatar = !string.IsNullOrWhiteSpace(member.AvatarUrl);
            string avatarUrl = hasCustomAvatar
                ? member.AvatarUrl
                : placeholderAvatarUrl;

            // Build the embed
            var eb = new EmbedBuilder()
                // Use the guild nickname (or username) instead of raw username
                .WithTitle($"House Ventura — {guildUser.DisplayName}")
                .WithColor(duneColor)

                // User's avatar (or placeholder) as the large image
                .WithImageUrl(avatarUrl)

                // Guild logo as the thumbnail
                .WithThumbnailUrl(_guildLogoUrl)

                // Spacer
                .WithDescription("\u200b");

            // Warning if no custom avatar
            if (!hasCustomAvatar)
            {
                eb.AddField(
                    ":warning: No custom avatar set!",
                    "Use `/vprofile-avatar <url>` to upload your own profile image.",
                    false);
            }

            // Main stats fields
            eb.AddField(
                    "💰 Venturans & Contributions",
                    $"• Venturans: {venturans:N0}\n• Contributions: {contributions:N0}",
                    false)
              .AddField("🎖️ Rank", currentRank, true)
              .AddField("📅 Joined", member.RegisteredAt.ToString("MMMM dd, yyyy"), true)
              .AddField("\u200b", "\u200b", true)
              .AddField(
                    "📊 XP Progress",
                    $"{progressBar}\n{xpCurrent:N0} / {nextThreshold:N0} (+{xpToNext:N0} to go)",
                    false)

              .WithFooter(f => f
                  .WithText($"ID: {member.UserId}  |  ☀️ May the sands guide you"));

            return eb.Build();
        }




        // Builds a 10-segment progress bar using custom block characters
        private static string BuildProgressBar(double percent)
        {
            const int barSize = 10;

            // Round and clamp
            int fillCount = (int)Math.Round(percent * barSize);
            fillCount = Math.Max(0, Math.Min(barSize, fillCount));
            int emptyCount = barSize - fillCount;

            return new string('▰', fillCount) + new string('▱', emptyCount);
        }
    }
}