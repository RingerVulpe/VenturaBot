using Discord.Interactions;
using VenturaBot.Services;

namespace VenturaBot.Commands;

public class SlashGeneralModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IGuildMemberService _memberService;
    private readonly IEconomyService _economy; 
    public SlashGeneralModule(IGuildMemberService memberService, IEconomyService economy)
    {
        _memberService = memberService;
        _economy = economy;
    }

    [SlashCommand("vping", "Test ping response")]
    public async Task Ping()
    {
        await RespondAsync("🏓 Pong!");
    }

    [SlashCommand("vdunefact", "Get a random fun fact about Dune.")]
    public async Task DuneFact()
    {
        string fact = DuneFactService.GetRandomFact();
        await RespondAsync($"📖 **Dune Fact:** {fact}");
    }



    [SlashCommand("vroll", "Roll a D20.")]
    public async Task Roll()
    {
        int roll = new Random().Next(1, 21);
        string result = roll switch
        {
            1 => "🎲 You rolled a **1**... catastrophic failure.",
            20 => "🎯 NATURAL 20! The sand favors you.",
            _ => $"🎲 You rolled a **{roll}**."
        };

        await RespondAsync(result);
    }

    [SlashCommand("vregister", "Register to VenturaBot")]
    public async Task Register()
    {
        var userId = Context.User.Id;

        // Check if the user is already in the dictionary
        if (DataService.Members.ContainsKey(userId))
        {
            await RespondAsync("⚠️ You are already registered.", ephemeral: true);
            return;
        }

        // Not yet registered → create and save
        var member = DataService.GetOrCreate(userId, Context.User.Username);
        DataService.Save();

        await RespondAsync($"✅ Registered as {member.Username}", ephemeral: true);
    }


}
