using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using VenturaBot.Builders;
using VenturaBot.Data;

namespace VenturaBot.Services
{
    public class LevelUpService
    {
        // ——— YOUR RANK→ROLE MAPPING ———
        private static readonly Dictionary<int, string> RankRoleNames = new()
        {
            { 1, "Initiate"    },
            { 2, "Drudge"      },
            { 3, "Binder"      },
            { 4, "Splicer"     },
            { 5, "Prospect"    },
            { 6, "Forgebearer" },
            { 7, "Vanturian"   },
        };

        private readonly IGuildMemberService _memberService;
        private readonly DiscordSocketClient _client;
        private readonly ulong _levelUpChannelId;

        public LevelUpService(
            IGuildMemberService memberService,
            DiscordSocketClient client,
            IConfiguration configuration)
        {
            _memberService = memberService;
            _client = client;
            _levelUpChannelId = 1383025917529690232;
        }

        public async Task CheckAndHandleLevelUpAsync(ulong userId, int oldXp)
        {
            // Retrieve member data
            var memberData = _memberService
                .GetAllMembers()
                .FirstOrDefault(m => m.UserId == userId);
            if (memberData == null)
                return;

            int newXp = memberData.XP;
            int oldRank = XPService.CalculateRank(oldXp);
            int newRank = XPService.CalculateRank(newXp);

            if (newRank <= oldRank)
                return;

            // Process each guild the bot is in
            foreach (var guild in _client.Guilds)
            {
                if (!(guild.GetUser(userId) is SocketGuildUser guildUser))
                    continue;

                var channel = guild.GetTextChannel(_levelUpChannelId);
                if (channel == null)
                    continue;

                // For each rank gained, assign role and announce
                for (int rank = oldRank + 1; rank <= newRank; rank++)
                {
                    int xpToNext = XPService.XPToNextRank(memberData.XP);
                    var embed = LevelUpEmbedBuilder.Build(guildUser, rank, xpToNext, memberData.AvatarUrl);

                    await AssignRankRoleAsync(guildUser, rank);
                    await channel.SendMessageAsync(embed: embed);
                }
            }
        }

        private async Task AssignRankRoleAsync(SocketGuildUser user, int rank)
        {
            // 1) Look up the configured role-name
            if (!RankRoleNames.TryGetValue(rank, out var roleName))
                return;

            // 2) Find it on the server
            var role = user.Guild.Roles
                         .FirstOrDefault(r =>
                             string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase));
            if (role == null)
                return;

            // 3) Drop any other rank roles
            var oldRoles = user.Roles
                .Where(r => RankRoleNames.Values.Contains(r.Name) && r.Id != role.Id);
            if (oldRoles.Any())
                await user.RemoveRolesAsync(oldRoles);

            // 4) Give them their new rank
            await user.AddRoleAsync(role);
        }
    }
}
