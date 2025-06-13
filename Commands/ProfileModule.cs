using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using VenturaBot.Builders;
using VenturaBot.Data;
using VenturaBot.Services;

namespace VenturaBot.Modules
{
    public class ProfileModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IGuildMemberService _memberService;
        private readonly IEconomyService _economyService;
        private readonly ProfileEmbedBuilder _profileBuilder;

        public ProfileModule(
            IGuildMemberService memberService,
            IEconomyService economyService,
            ProfileEmbedBuilder profileBuilder)
        {
            _memberService = memberService;
            _economyService = economyService;
            _profileBuilder = profileBuilder;
        }

        [SlashCommand("vprofile", "View your guild profile")]
        public async Task ViewAsync()
        {
            var userId = Context.User.Id;
            var member = _memberService
                .GetAllMembers()
                .FirstOrDefault(m => m.UserId == userId);

            if (member is null)
            {
                await RespondAsync("You’re not registered yet – use `/vregister` first.", ephemeral: true);
                return;
            }

            if (Context.User is not SocketGuildUser guildUser)
            {
                await RespondAsync("Couldn’t resolve your guild membership.", ephemeral: true);
                return;
            }

            var embed = _profileBuilder.Build(guildUser, member);
            await RespondAsync(embed: embed, ephemeral: true);
        }

        [SlashCommand("vprofile-avatar", "Set or update your profile avatar URL")]
        public async Task AvatarAsync(
            [Summary("url", "Image link for your avatar")] string url)
        {
            var userId = Context.User.Id;
            var member = _memberService
                .GetAllMembers()
                .FirstOrDefault(m => m.UserId == userId);

            if (member is null)
            {
                await RespondAsync("You need to register first (`/vregister`).", ephemeral: true);
                return;
            }

            member.AvatarUrl = url;
            _memberService.Save();

            await RespondAsync("✅ Avatar URL updated!", ephemeral: true);
        }

        [SlashCommand("vprofiles", "View all registered guild profiles")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ViewAllAsync()
        {
            var allMembers = _memberService.GetAllMembers().ToList();
            if (!allMembers.Any())
            {
                await RespondAsync("No registered members found.", ephemeral: true);
                return;
            }

            // Acknowledge the command
            await RespondAsync("Registered profiles:", ephemeral: true);

            // Send each profile as a follow-up embed
            foreach (var member in allMembers)
            {
                var guildUser = Context.Guild.GetUser(member.UserId) as SocketGuildUser;
                if (guildUser is null)
                    continue;

                var embed = _profileBuilder.Build(guildUser, member);
                await FollowupAsync(embed: embed, ephemeral: true);
            }
        }
    }
}
