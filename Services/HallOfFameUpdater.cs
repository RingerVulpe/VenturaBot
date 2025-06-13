using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using VenturaBot.Builders;

namespace VenturaBot.Services
{
    /// <summary>
    /// Background service that keeps the “Guild Hall of Fame” updated
    /// by editing the existing pinned message instead of reposting.
    /// </summary>
    public class HallOfFameUpdater : BackgroundService
    {
        private readonly ulong _channelId = 1382674140493774869;   // ← your channel ID
        private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(5);

        private readonly DiscordSocketClient _client;
        private readonly ILeaderboardService _leaderboardService;
        private readonly HallOfFameEmbedBuilder _embedBuilder;

        // Holds the ID of the pinned Hall of Fame message (once discovered or created)
        private ulong? _messageId;

        public HallOfFameUpdater(
            DiscordSocketClient client,
            ILeaderboardService leaderboardService,
            HallOfFameEmbedBuilder embedBuilder)
        {
            _client = client;
            _leaderboardService = leaderboardService;
            _embedBuilder = embedBuilder;
        }

        /// <summary>
        /// Force an immediate update (e.g. from Bot.OnReadyAsync).
        /// </summary>
        public Task ForceUpdateAsync() => UpdateLeaderboardAsync();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Immediately perform the first update
            await UpdateLeaderboardAsync();

            // Then refresh at the configured interval
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_updateInterval, stoppingToken);
                await UpdateLeaderboardAsync();
            }
        }

        private async Task UpdateLeaderboardAsync()
        {
            // 1) Grab the text channel
            if (!(_client.GetChannel(_channelId) is SocketTextChannel textChannel))
                return;

            // 2) If we haven't yet located the pinned message, try to find it
            if (!_messageId.HasValue)
            {
                var pinned = await textChannel.GetPinnedMessagesAsync();
                var existing = pinned.FirstOrDefault(msg =>
                    msg.Author.Id == _client.CurrentUser.Id &&
                    msg.Embeds.Any(e => e.Title == "🏆 Guild Hall of Fame")
                );
                if (existing != null)
                    _messageId = existing.Id;
            }

            // 3) Build the latest embed
            var topStats = await _leaderboardService.GetTopStatsAsync(10);
            var embed = _embedBuilder.Build(topStats);

            // 4) If we have an existing message, try to edit it
            if (_messageId.HasValue)
            {
                try
                {
                    await textChannel.ModifyMessageAsync(_messageId.Value, props => props.Embed = embed);
                    return;
                }
                catch (Discord.Net.HttpException ex)
                    when (ex.DiscordCode == DiscordErrorCode.CannotEditOtherUsersMessage)
                {
                    // If somehow it’s not editable, we'll fall through and recreate below
                }
            }

            // 5) Otherwise, send a fresh message and pin it
            var sent = await textChannel.SendMessageAsync(
                text: null,
                embed: embed,
                allowedMentions: AllowedMentions.None
            );
            await sent.PinAsync();
            _messageId = sent.Id;
        }
    }
}
