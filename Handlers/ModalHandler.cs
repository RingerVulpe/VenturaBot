using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using VenturaBot.Services;
using VenturaBot.Data;
using VenturaBot.Builders;
using VenturaBot.TaskDefinitions;
using VenturaBot.Services.Models;

namespace VenturaBot.Handlers
{
    public class ModalHandler
    {
        private readonly ITaskService _taskService;
        private readonly TaskEmbedBuilder _taskEmbedBuilder;
        private readonly TaskComponentBuilder _taskComponentBuilder;
        private readonly IGuildMemberService _memberService;
        private readonly IEventService _eventService;
        private readonly EventEmbedBuilder _eventEmbedBuilder;
        private readonly EventComponentBuilder _eventComponentBuilder;

        // Replace with your actual channel IDs:
        private const ulong UNAPPROVED_CHANNEL_ID = 1382091966766121070;
        private const ulong TASKBOARD_CHANNEL_ID = 1379731733930446928;

        public ModalHandler(
            ITaskService taskService,
            TaskEmbedBuilder taskEmbedBuilder,
            TaskComponentBuilder taskComponentBuilder,
            IGuildMemberService memberService,
            IEventService eventService,
            EventEmbedBuilder eventEmbedBuilder,
            EventComponentBuilder eventComponentBuilder)
        {
            _taskService = taskService;
            _taskEmbedBuilder = taskEmbedBuilder;
            _taskComponentBuilder = taskComponentBuilder;
            _memberService = memberService;
            _eventService = eventService;
            _eventEmbedBuilder = eventEmbedBuilder;
            _eventComponentBuilder = eventComponentBuilder;
        }

        public async Task HandleModalAsync(SocketModal modal)
        {
            var customId = modal.Data.CustomId;
            Console.WriteLine($"[ModalHandler] Received modal: {customId}");

            var guildUser = modal.User as SocketGuildUser;
            bool isOfficer = guildUser != null && (
                guildUser.GuildPermissions.Administrator ||
                guildUser.Roles.Any(r =>
                    r.Name.Equals("Officer", StringComparison.OrdinalIgnoreCase) ||
                    r.Name.Equals("GM", StringComparison.OrdinalIgnoreCase)) ||
                guildUser.Id == guildUser.Guild.OwnerId
            );

            try
            {
                // ----- Event Creation -----
                if (customId == "event-create-modal")
                {
                    // Extract input values
                    var inputs = modal.Data.Components
                        .ToDictionary(c => c.CustomId, c => c.Value ?? string.Empty);

                    inputs.TryGetValue("title", out var title);
                    inputs.TryGetValue("datetime", out var datetimeRaw);
                    inputs.TryGetValue("imageUrl", out var imageUrl);
                    inputs.TryGetValue("channelId", out var channelRaw);
                    inputs.TryGetValue("recurrence", out var recurrence);

                    if (!DateTimeOffset.TryParse(datetimeRaw, out var scheduledFor))
                    {
                        await modal.RespondAsync(
                            "❌ Invalid date/time format. Use YYYY-MM-DD HH:mm.",
                            ephemeral: true
                        );
                        return;
                    }

                    if (!ulong.TryParse(channelRaw, out var channelId))
                    {
                        await modal.RespondAsync(
                            "❌ Invalid channel ID.",
                            ephemeral: true
                        );
                        return;
                    }

                    // Create and persist the event
                    var gamEvent = new Event
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = title,
                        ScheduledFor = scheduledFor,
                        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl,
                        ChannelId = channelId,
                        Recurrence = string.IsNullOrWhiteSpace(recurrence) ? null : recurrence
                    };
                    gamEvent = await _eventService.CreateAsync(gamEvent);

                    // Build embed and components
                    var embed = _eventEmbedBuilder.Build(gamEvent);
                    var components = _eventComponentBuilder.Build(gamEvent.Id);

                    // Post to channel
                    var channel = guildUser?.Guild.GetTextChannel(channelId);
                    if (channel == null)
                    {
                        await modal.RespondAsync(
                            "❌ Could not find the target channel.",
                            ephemeral: true
                        );
                        return;
                    }

                    var msg = await channel.SendMessageAsync(
                        embed: embed,
                        components: components
                    );

                    // Persist message ID using UpdateAsync
                    gamEvent.MessageId = msg.Id;
                    await _eventService.UpdateAsync(gamEvent);

                    await modal.RespondAsync("✅ Event posted!", ephemeral: true);
                    return;
                }

                // ----- Community Task Creation -----
                if (customId.StartsWith("community_modal_"))
                {
                    if (!isOfficer)
                    {
                        await modal.RespondAsync(
                            "🚫 Only officers can create community tasks.",
                            ephemeral: true
                        );
                        return;
                    }

                    var typePart = customId.Substring("community_modal_".Length);
                    Enum.TryParse<TaskType>(typePart, true, out var communityType);

                    var inputs = modal.Data.Components
                        .ToDictionary(c => c.CustomId, c => c.Value ?? string.Empty);

                    inputs.TryGetValue("community_totalNeeded", out var totalRaw);
                    var cleanedTotal = totalRaw.Replace(",", string.Empty).Trim();
                    if (!int.TryParse(cleanedTotal, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var totalNeeded) || totalNeeded <= 0)
                    {
                        await modal.RespondAsync(
                            "❌ Please enter a valid total needed.",
                            ephemeral: true
                        );
                        return;
                    }

                    inputs.TryGetValue("community_dropLocation", out var dropLoc);
                    if (string.IsNullOrWhiteSpace(dropLoc))
                        dropLoc = "Unknown Location";

                    inputs.TryGetValue("community_potSize", out var potRaw);
                    var cleanedPot = potRaw.Replace(",", string.Empty).Trim();
                    int potSize = int.TryParse(cleanedPot, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var p) && p >= 0
                        ? p : 0;

                    inputs.TryGetValue("community_description", out var description);
                    description ??= string.Empty;

                    var communityTask = _taskService.CreateCommunityTask(
                        communityType,
                        totalNeeded,
                        dropLoc,
                        potSize,
                        description,
                        modal.User.Id,
                        expirationDate: null
                    );
                    _taskService.Save();

                    var boardChan = guildUser?.Guild.GetTextChannel(TASKBOARD_CHANNEL_ID);
                    if (boardChan != null)
                    {
                        var embed = _taskEmbedBuilder.Build(communityTask, viewer: null);
                        var comp = _taskComponentBuilder.Build(communityTask, modal.User.Id, isOfficer);
                        var sent = await boardChan.SendMessageAsync(embed: embed, components: comp.Build());

                        communityTask.BoardMessageId = sent.Id;
                        _taskService.Save();
                    }

                    await modal.RespondAsync(
                        "✅ Community task created and posted to the Task Board.",
                        ephemeral: true
                    );
                    return;
                }

                // ----- Community Delivery Submission -----
                if (customId.StartsWith("community_deliver_modal_"))
                {
                    var parts = customId.Split('_');
                    if (parts.Length != 4 || !int.TryParse(parts[3], out var commTaskId))
                    {
                        await modal.RespondAsync(
                            "❌ Invalid community task ID.",
                            ephemeral: true
                        );
                        return;
                    }

                    var inputs = modal.Data.Components
                        .ToDictionary(c => c.CustomId, c => c.Value ?? string.Empty);
                    inputs.TryGetValue("deliver_amount", out var amtRaw);
                    var cleanedAmt = amtRaw.Replace(",", string.Empty).Trim();
                    if (!int.TryParse(cleanedAmt, out var amount) || amount <= 0)
                    {
                        await modal.RespondAsync(
                            "❌ Please enter a valid amount.",
                            ephemeral: true
                        );
                        return;
                    }
                    inputs.TryGetValue("deliver_screenshot", out var screenshotUrl);

                    var pendingEmbed = new EmbedBuilder()
                        .WithTitle($"📦 Delivery Pending for Community Task #{commTaskId}")
                        .AddField("👤 Delivered by", $"<@{modal.User.Id}>", false)
                        .AddField("💵 Amount", $"{amount:N0}", false)
                        .WithFooter("Officers: please Approve or Reject below.");
                    if (!string.IsNullOrWhiteSpace(screenshotUrl))
                        pendingEmbed.WithImageUrl(screenshotUrl);

                    var unappChan = guildUser?.Guild.GetTextChannel(UNAPPROVED_CHANNEL_ID);
                    if (unappChan != null)
                    {
                        var buttons = _taskComponentBuilder.BuildDeliveryApprovalButtons(commTaskId, modal.User.Id, amount);
                        await unappChan.SendMessageAsync(
                            embed: pendingEmbed.Build(),
                            components: buttons.Build()
                        );
                    }

                    await modal.RespondAsync(
                        $"✅ You submitted **{amount}** units for community task #{commTaskId}. Waiting for officer approval.",
                        ephemeral: true
                    );
                    return;
                }

                // ----- Regular Task Delivery Submission -----
                if (customId.StartsWith("task_deliver_modal_"))
                {
                    var parts = customId.Split('_');
                    if (parts.Length != 4 || !int.TryParse(parts[3], out var taskId))
                    {
                        await modal.RespondAsync(
                            "❌ Invalid task ID.",
                            ephemeral: true
                        );
                        return;
                    }

                    var inputs = modal.Data.Components
                        .ToDictionary(c => c.CustomId, c => c.Value ?? string.Empty);
                    inputs.TryGetValue("deliver_amount", out var amtRaw2);
                    var cleanedAmt2 = amtRaw2.Replace(",", string.Empty).Trim();
                    if (!int.TryParse(cleanedAmt2, out var amt2) || amt2 <= 0)
                    {
                        await modal.RespondAsync(
                            "❌ Please enter a valid amount.",
                            ephemeral: true
                        );
                        return;
                    }

                    _taskService.RecordContribution(taskId, modal.User.Id, amt2);
                    _taskService.Save();

                    await modal.RespondAsync(
                        $"✅ You submitted **{amt2}** units for task #{taskId}.",
                        ephemeral: true
                    );
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModalHandler] EXCEPTION: {ex}");
                try { await modal.RespondAsync($"⚠️ Error: {ex.Message}", ephemeral: true); } catch { }
            }
        }
    }
}
