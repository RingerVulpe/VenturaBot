using System;
using System.Linq;
using Discord;
using Discord.WebSocket;
using VenturaBot.Services;

namespace VenturaBot.Builders
{
    /// <summary>
    /// Builds a rich embed to announce player level-ups.
    /// </summary>
    public static class LevelUpEmbedBuilder
    {
        // Mapping for display names (plain text, no emojis)
        private static readonly Dictionary<int, string> RankNames = new()
        {
            { 1, "Initiate"    },
            { 2, "Drudge"      },
            { 3, "Binder"      },
            { 4, "Splicer"     },
            { 5, "Prospect"    },
            { 6, "Forgebearer" },
            { 7, "Vanturian"   },
        };

        /// <summary>
        /// Builds a 10-segment progress bar using custom block characters.
        /// </summary>
        private static string BuildProgressBar(double percent)
        {
            const int barSize = 10;
            int fillCount = (int)Math.Round(percent * barSize);
            fillCount = Math.Max(0, Math.Min(barSize, fillCount));
            int emptyCount = barSize - fillCount;
            return new string('▰', fillCount) + new string('▱', emptyCount);
        }

        /// <summary>
        /// Creates an embed notifying that a user has leveled up.
        /// </summary>
        public static Embed Build(
            SocketGuildUser user,
            int newRank,
            int xpToNext,
            string customAvatarUrl = null)
        {
            var avatarUrl = !string.IsNullOrEmpty(customAvatarUrl)
                ? customAvatarUrl
                : (user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());

            // Get rank labels
            RankNames.TryGetValue(newRank, out var newRankName);
            var prevRankName = newRank > 1 ? RankNames[newRank - 1] : "—";
            var hasNext = RankNames.TryGetValue(newRank + 1, out var nextRankName);

            // Calculate progress
            int lowerXp = XPService.ThresholdForRank(newRank - 1);
            int upperXp = XPService.ThresholdForRank(newRank);
            double percent = (double)(upperXp - xpToNext - lowerXp) / (upperXp - lowerXp);
            var bar = BuildProgressBar(percent);

            var eb = new EmbedBuilder()
                .WithTitle("Level Up!")
                .WithThumbnailUrl(avatarUrl)
                .WithColor(Color.Gold)
                .WithDescription($"**{user.Username}** has been promoted to **{newRankName}**!")
                .AddField("Previous Rank", prevRankName, inline: true)
                .AddField("Current Rank", newRankName ?? newRank.ToString(), inline: true)
                .AddField("Next Rank", hasNext ? nextRankName : "— Max Level —", inline: true)
                .AddField("XP to Next Level", xpToNext, inline: true)
                .AddField("Progress", bar, inline: false)
                .WithFooter("Congratulations on your advancement!")
                .WithTimestamp(DateTimeOffset.UtcNow);

            return eb.Build();
        }
    }
}