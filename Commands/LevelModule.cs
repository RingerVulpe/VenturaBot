using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using VenturaBot.Services;

namespace VenturaBot.Commands
{
    public class LevelModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly LevelUpService _levelUpService;
        private readonly IGuildMemberService _memberService;

        public LevelModule(LevelUpService levelUpService, IGuildMemberService memberService)
        {
            _levelUpService = levelUpService;
            _memberService = memberService;
        }

        [SlashCommand("testlevel", "Add XP and trigger a level-up embed for testing.")]
        [RequireOwner]  // Only allow bot owner to use this command
        public async Task TestLevelUpAsync(
            [Summary("xp", "Amount of XP to add (e.g. 1000)")] int xpAmount)
        {
            var userId = Context.User.Id;
            var member = _memberService
                .GetAllMembers()
                .FirstOrDefault(m => m.UserId == userId);

            if (member == null)
            {
                await RespondAsync("⚠️ You are not registered yet. Please run `/vregister` first.", ephemeral: true);
                return;
            }

            // Capture old XP and add new XP
            int oldXp = member.XP;
            member.XP += xpAmount;
            _memberService.Save();  // Persist changes

            // Trigger level-up processing
            await _levelUpService.CheckAndHandleLevelUpAsync(userId, oldXp);

            await RespondAsync(
                $"✅ Added {xpAmount:N0} XP (old: {oldXp:N0}, new: {member.XP:N0})." +
                " If you leveled up, you should see the embed in the designated channel.",
                ephemeral: true
            );
        }
    }
}
