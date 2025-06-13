using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using VenturaBot.Services;
using VenturaBot.Services.Models;
using VenturaBot.Builders;
using System.Runtime.InteropServices;

namespace VenturaBot.Modules
{
    [Group("event", "Event management")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class SlashEventModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IEventService _eventService;
        private readonly EventEmbedBuilder _embedBuilder;
        private readonly EventComponentBuilder _componentBuilder;

        public SlashEventModule(
            IEventService eventService,
            EventEmbedBuilder embedBuilder,
            EventComponentBuilder componentBuilder)
        {
            _eventService = eventService;
            _embedBuilder = embedBuilder;
            _componentBuilder = componentBuilder;
        }

        [SlashCommand("create", "Create a new event via a structured modal")]
        public async Task CreateAsync()
        {
            var modal = new ModalBuilder()
                .WithTitle("New Event")
                .WithCustomId("event-create-modal")
                // ——— Basic Info ———
                .AddTextInput("Title", "title", TextInputStyle.Short, placeholder: "Deep Desert Run", required: true)
                .AddTextInput("Image URL", "imageUrl", TextInputStyle.Short, placeholder: "https://...jpg", required: false)
                // ——— Schedule Settings ———
                .AddTextInput("When (YYYY-MM-DD HH:mm)", "datetime", TextInputStyle.Short, placeholder: "2025-06-15 18:00", required: true)
                .AddTextInput("Recurrence (optional)", "recurrence", TextInputStyle.Short, placeholder: "Weekly on Friday at 20:00", required: false)
                // ——— Deployment ———
                .AddTextInput("Channel ID", "channelId", TextInputStyle.Short, placeholder: "1379731733930446928", required: true)
                .Build();

            await Context.Interaction.RespondWithModalAsync(modal);
        }

        //[SlashCommand("quick", "Quick-create an event inline")]
        //public async Task QuickAsync(
        //    [Summary("Date (YYYY-MM-DD)")] string date,
        //    [Summary("Time (HH:mm)")] string time,
        //    [Summary("Target channel")] ITextChannel channel,
        //    [Summary("Image URL")] string imageUrl = null,
        //    [Summary("Recurrence rule (optional)")] string recurrence = null
        //)
        //{
        //    if (!DateTimeOffset.TryParse($"{date} {time}", out var scheduled))
        //    {
        //        await RespondAsync("❌ Invalid date/time format. Use YYYY-MM-DD and HH:mm.", ephemeral: true);
        //        return;
        //    }

        //    var evt = new Event
        //    {
        //        Id = Guid.NewGuid().ToString(),
        //        //Title = title,
        //        ImageUrl = imageUrl,
        //        ChannelId = channel.Id,
        //        ScheduledFor = scheduled,
        //        Recurrence = recurrence
        //    };

        //    // 1) persist
        //    evt = await _eventService.CreateAsync(evt);

        //    // 2) post to channel
        //    var sent = await channel.SendMessageAsync(
        //        embed: _embedBuilder.Build(evt),
        //        components: _componentBuilder.Build(evt.Id)
        //    );

        //    // 3) save the MessageId
        //    evt.MessageId = sent.Id;
        //    await _eventService.UpdateAsync(evt);

        //    // 4) confirmation
        //    await RespondAsync("✅ Event created successfully!", ephemeral: true);
        //}
    }
}
