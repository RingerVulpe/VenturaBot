using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using VenturaBot.Services;
using VenturaBot.Data;
using VenturaBot.Builders;
using TaskStatus = VenturaBot.Data.TaskStatus;
using VenturaBot.TaskDefinitions;
using VenturaBot.Services.Models;

namespace VenturaBot.Handlers
{
    public class ButtonHandler
    {
        private readonly ITaskService _taskService;
        private readonly IGuildMemberService _memberService;
        private readonly IEconomyService _economyService;
        private readonly TaskEmbedBuilder _embedBuilder;
        private readonly TaskComponentBuilder _componentBuilder;
        private readonly IEventService _eventService;
        private readonly EventComponentBuilder _eventComponentBuilder;
        private readonly EventEmbedBuilder _eventEmbedBuilder;
        private readonly IStoreService _storeService;
        private readonly StoreEmbedBuilder _storeEmbedBuilder;
        private readonly RaffleService _raffleService;
        private readonly RaffleEmbedBuilder _raffleEmbedBuilder;

        // ←– Replace with your actual channel IDs:
        private const ulong UNAPPROVED_CHANNEL_ID = 1382091966766121070;
        private const ulong TASKBOARD_CHANNEL_ID = 1379731733930446928;

        // in your constructor (add as last parameter):
        public ButtonHandler(
            ITaskService taskService,
            TaskEmbedBuilder embedBuilder,
            TaskComponentBuilder componentBuilder,
            IEconomyService economyService,
            IGuildMemberService memberService,
            IEventService eventService,
            EventComponentBuilder eventComponentBuilder, 
            EventEmbedBuilder eventEmbedBuilder,
            StoreEmbedBuilder storeEmbedBuilder,
            IStoreService storeService,
            RaffleService raffleService,
            RaffleEmbedBuilder raffleEmbedBuilder
        )
        {
            _taskService = taskService;
            _embedBuilder = embedBuilder;
            _componentBuilder = componentBuilder;
            _memberService = memberService;
            _economyService = economyService;
            _eventService = eventService;
            _eventComponentBuilder = eventComponentBuilder;
            _eventEmbedBuilder = eventEmbedBuilder;
            _storeService = storeService;
            _storeEmbedBuilder = storeEmbedBuilder;
            _raffleService = raffleService;
            _raffleEmbedBuilder = raffleEmbedBuilder;
        }

        public async Task HandleButtonAsync(SocketMessageComponent component)
        {
            var customId = component.Data.CustomId;
            SocketGuildUser guildUser = component.User as SocketGuildUser;
            var parts = customId.Split('_');

            // ─── Community Complete ───────────────────────────────────
            if (customId.StartsWith("community_complete_"))
            {
                // 1) Parse out taskId
                parts = customId.Split('_');
                int taskId = int.Parse(parts[2]);
                var communityTask = _taskService.GetById(taskId);

                // 2) Mark task closed
                communityTask.Status = TaskStatus.Closed;
                _taskService.Save();

                // 3) Pot distribution
                var potSize = communityTask.PotSizeVenturans;
                var contributions = communityTask.Contributions;
                var totalContrib = contributions.Values.Sum();

                var distributed = new Dictionary<ulong, int>();
                if (totalContrib > 0 && potSize > 0)
                {
                    int sumAllocated = 0;
                    // compute each player’s share
                    foreach (var (userId, amount) in contributions)
                    {
                        double exact = (double)amount / totalContrib * potSize;
                        int share = (int)Math.Floor(exact);
                        distributed[userId] = share;
                        sumAllocated += share;
                    }

                    // give any leftover to the top contributor
                    int remainder = potSize - sumAllocated;
                    if (remainder > 0)
                    {
                        var topUser = contributions
                                      .OrderByDescending(kv => kv.Value)
                                      .First().Key;
                        distributed[topUser] += remainder;
                    }

                    // award each share
                    foreach (var (userId, share) in distributed)
                    {
                        if (share > 0)
                            _economyService.AwardVenturans(userId, share);
                    }


                    // award XP to each contributor
                    int xpGain = XPService.CalculateXP(communityTask);
                    foreach (var userId in distributed.Keys)
                    {
                        // use the userId string as a fallback tag
                        var member = _memberService.GetOrCreate(userId, userId.ToString());
                        member.GainXP(xpGain);
                    }
                    _memberService.Save();
                }

                // build a “payout” description
                var payoutLines = distributed.Select(kv =>
                {
                    var userId = kv.Key;
                    var got = kv.Value;
                    var gave = contributions[userId];
                    return $"<@{userId}> earned **{got}** Venturans based on their contribution of **{gave}**.";
                });
                string payoutDescription = string.Join("\n", payoutLines);

                var payoutEmbed = new EmbedBuilder()
                    .WithTitle($"🎉 Task #{taskId} Complete — Payout")
                    .WithDescription(payoutDescription)
                    .WithColor(Color.Green)
                    .Build();

                // 4) Update the board message embed + components
                var board = (component.Channel as SocketGuildChannel)
                             ?.Guild.GetTextChannel(TASKBOARD_CHANNEL_ID);
                if (board != null && communityTask.BoardMessageId.HasValue)
                {
                    if (await board.GetMessageAsync(communityTask.BoardMessageId.Value)
                        is IUserMessage origMsg)
                    {
                        var embed = _embedBuilder.Build(communityTask, viewer: null);
                        await origMsg.ModifyAsync(m =>
                        {
                            m.Embed = embed;
                            m.Components = new ComponentBuilder().Build();
                        });
                    }
                }

                // 5) Acknowledge the “Complete” button press with payout embed
                await component.UpdateAsync(props =>
                {
                    props.Content = null;
                    props.Embed = payoutEmbed;
                    props.Components = new ComponentBuilder().Build();
                });

                return;
            }

            // inside HandleButtonAsync, after your store_redeem block:
            if (customId.StartsWith("raffle_enter:"))
            {
                var raffleId = customId["raffle_enter:".Length..];

                // try to enter
                var success = await _raffleService.EnterRaffleAsync(raffleId, component.User.Id);
                if (!success)
                {
                    await component.RespondAsync(
                        "❌ You either already entered or don't have enough venturans.",
                        ephemeral: true
                    );
                    return;
                }

                // update the public raffle embed so everyone sees the new entry (optional)
                var item = _raffleService.GetItem(raffleId);
                var newEmbed = _raffleEmbedBuilder.BuildRaffleEmbed(item);
                var newComponents = _raffleEmbedBuilder.BuildEntryButton(raffleId);

                await component.UpdateAsync(props =>
                {
                    props.Embed = newEmbed;
                    props.Components = newComponents;
                });

                // confirm to the user
                await component.FollowupAsync(
                    "✅ You’ve entered the raffle!",
                    ephemeral: true
                );

                return;
            }

            //
            // 1) Community Deliver click → open modal
            //
            if (customId.StartsWith("community_deliver_")
                && !customId.StartsWith("community_deliver_approve:")
                && !customId.StartsWith("community_deliver_reject:"))
            {
                parts = customId.Split('_');
                if (parts.Length == 3 && int.TryParse(parts[2], out int commTaskId))
                {
                    var modal = new ModalBuilder()
                        .WithTitle($"Deliver for Community Task #{commTaskId}")
                        .WithCustomId($"community_deliver_modal_{commTaskId}")
                        .AddTextInput("Amount Delivered", "deliver_amount", TextInputStyle.Short, placeholder: "How many units?", required: true)
                        .AddTextInput("Screenshot URL", "deliver_screenshot", TextInputStyle.Short, placeholder: "Link to a screenshot", required: true)
                        .Build();

                    await component.RespondWithModalAsync(modal);
                }
                else
                {
                    await component.RespondAsync("❌ Invalid community task ID.", ephemeral: true);
                }
                return;
            }

            // 2) Community Delivery Approval / Rejection
            if (customId.StartsWith("community_deliver_approve:")
             || customId.StartsWith("community_deliver_reject:"))
            {
                // split the customId payload
                var deliverParts = customId.Split(':');
                bool isApprove = deliverParts[0].EndsWith("approve");

                // validate payload
                if (deliverParts.Length != 4
                    || !int.TryParse(deliverParts[1], out int deliveryTaskId)
                    || !ulong.TryParse(deliverParts[2], out ulong contributorId)
                    || !int.TryParse(deliverParts[3], out int deliveredAmount))
                {
                    await component.RespondAsync("❌ Invalid delivery action.", ephemeral: true);
                    return;
                }

                // check permissions
                var approver = component.User as SocketGuildUser;
                bool canApprove = approver != null && (
                    approver.GuildPermissions.Administrator ||
                    approver.Roles.Any(r =>
                        r.Name.Equals("Officer", StringComparison.OrdinalIgnoreCase) ||
                        r.Name.Equals("GM", StringComparison.OrdinalIgnoreCase)
                    ) ||
                    approver.Guild.OwnerId == approver.Id
                );
                if (!canApprove)
                {
                    await component.RespondAsync("🚫 Only officers/Admins can approve deliveries.", ephemeral: true);
                    return;
                }

                // fetch the task
                var communityTask = _taskService.GetById(deliveryTaskId);
                if (communityTask == null || !communityTask.IsCommunityTask)
                {
                    await component.RespondAsync("❌ Task not found or not a community task.", ephemeral: true);
                    return;
                }

                if (isApprove)
                {
                    // record the approved contribution
                    _taskService.RecordContribution(deliveryTaskId, contributorId, deliveredAmount);
                    _taskService.Save();
                    _economyService.AddContribution(
                        contributorId,
                        deliveredAmount
                    );

                    var guild = (component.Channel as SocketGuildChannel)?.Guild;
                    if (guild != null)
                    {
                        // re-build embed + buttons
                        var embed = _embedBuilder.Build(communityTask, viewer: null);
                        var comps = _componentBuilder
                                        .Build(communityTask, viewerId: 0, isOfficer: true)
                                        .Build();

                        // 1) update the board post
                        var boardChannel = guild.GetTextChannel(communityTask.ChannelId);
                        if (boardChannel != null)
                        {
                            if (communityTask.BoardMessageId.HasValue
                             && await boardChannel.GetMessageAsync(communityTask.BoardMessageId.Value)
                                    is IUserMessage boardMsg)
                            {
                                await boardMsg.ModifyAsync(m =>
                                {
                                    m.Embed = embed;
                                    m.Components = comps;
                                });
                            }
                            else
                            {
                                var posted = await boardChannel.SendMessageAsync(embed: embed, components: comps);
                                communityTask.BoardMessageId = posted.Id;
                                _taskService.Save();
                            }
                        }

                        // 2) update the original post
                        if (communityTask.OriginChannelId.HasValue)
                        {
                            var originChannel = guild.GetTextChannel(communityTask.OriginChannelId.Value);
                            if (originChannel != null)
                            {
                                if (communityTask.OriginMessageId.HasValue
                                 && await originChannel.GetMessageAsync(communityTask.OriginMessageId.Value)
                                        is IUserMessage originMsg)
                                {
                                    await originMsg.ModifyAsync(m =>
                                    {
                                        m.Embed = embed;
                                        m.Components = comps;
                                    });
                                }
                                else
                                {
                                    var posted = await originChannel.SendMessageAsync(embed: embed, components: comps);
                                    communityTask.OriginMessageId = posted.Id;
                                    _taskService.Save();
                                }
                            }
                        }
                    }

                    // finally, update the interaction response
                    await component.UpdateAsync(props =>
                    {
                        props.Content = "✅ Delivery approved and both messages updated.";
                        props.Components = new ComponentBuilder().Build();
                    });
                }
                else
                {
                    // rejection path
                    await component.UpdateAsync(props =>
                    {
                        props.Content = "❌ Delivery rejected.";
                        props.Components = new ComponentBuilder().Build();
                    });
                }

                return;
            }

            if (customId.StartsWith("store_redeem:"))
            {
                var itemId = customId["store_redeem:".Length..];

                // Attempt redeem
                var ok = _storeService.TryRedeem(itemId, component.User.Id);

                if (!ok)
                {
                    // Couldn’t redeem: just reply ephemerally
                    await component.RespondAsync(
                        $"❌ Could not redeem **{itemId}** (out of stock or insufficient funds).",
                        ephemeral: true
                    );
                    return;
                }

                // ✅ Redeemed: now update the *public* message in-place
                //    so the embed shows new stock and button disabled if needed.
                var item = _storeService
                               .GetAllItems()
                               .First(i => i.Id == itemId);

                var newEmbed = _storeEmbedBuilder.BuildItemEmbed(item, component.User.Id);
                var newButtons = _storeEmbedBuilder.BuildItemButtons(item, component.User.Id);

                await component.UpdateAsync(props =>
                {
                    props.Embed = newEmbed;
                    props.Components = newButtons;
                });

                // and optionally send an ephemeral confirmation
                await component.FollowupAsync(
                    $"✅ You redeemed **{item.Name}**!",
                    ephemeral: true
                );

                return;
            }
            //
            // 3) Create Task Button
            //
            if (customId == "create_task")
            {
                var typeMenu = new SelectMenuBuilder()
                    .WithCustomId("select_task_type")
                    .WithPlaceholder("Choose a task type…");
                foreach (TaskType t in Enum.GetValues<TaskType>())
                {
                    if (t == TaskType.Unknown) continue;
                    typeMenu.AddOption(label: t.ToString(), value: t.ToString(), description: $"Create a {t} task");
                }

                var deliveryMenu = new SelectMenuBuilder()
                    .WithCustomId("select_delivery_method")
                    .WithPlaceholder("Choose delivery method…")
                    .AddOption(label: "P2P Trade", value: "P2P Trade")
                    .AddOption(label: "Drop-off Box", value: "Drop-off Box");

                var compBuilder = new ComponentBuilder()
                    .WithSelectMenu(typeMenu)
                    .WithSelectMenu(deliveryMenu);

                await component.RespondAsync(
                    "Step 1: Select both **Task Type** and **Delivery Method**:",
                    components: compBuilder.Build(),
                    ephemeral: true
                );
                return;
            }

            //
            // 4) Resolve guild context
            //
            var guildChannel = component.Channel as SocketGuildChannel;
            var guildContext = guildChannel?.Guild;
            guildUser = null;
            if (guildContext != null)
                guildUser = guildContext.GetUser(component.User.Id);


            // Handlers/ButtonHandler.cs → inside HandleButtonAsync
            parts = component.Data.CustomId.Split(':');
            if (parts[0] == "event")
            {
                var eventId = parts[1];
                var status = Enum.Parse<RsvpStatus>(parts[2]);
                await _eventService.UpdateRsvpAsync(eventId, component.User.Id, status);

                // rebuild embed
                var evt = await _eventService.GetAsync(eventId);
                var embed = _eventEmbedBuilder.Build(evt);

                // build buttons via your dedicated builder
                MessageComponent components = _eventComponentBuilder.Build(eventId);

                await component.UpdateAsync(props =>
                {
                    props.Embed = embed;
                    props.Components = components;
                });

                return;
            }

            //
            // 5) Regular "task_" button handling
            //
            if (!customId.StartsWith("task_"))
                return;

            var idParts = customId.Split('_');
            if (idParts.Length != 3 || !int.TryParse(idParts[2], out int parsedTaskId))
            {
                await component.RespondAsync("❌ Invalid task ID.", ephemeral: true);
                return;
            }

            string action = idParts[1];
            var task = _taskService.GetById(parsedTaskId);
            if (task == null)
            {
                await component.RespondAsync($"❌ Task #{parsedTaskId} not found.", ephemeral: true);
                return;
            }

            bool isCreator = task.CreatedBy == component.User.Id;
            bool hasGMRoleTC = guildUser?.Roles.Any(r => r.Name.Equals("GM", StringComparison.OrdinalIgnoreCase)) == true;
            bool isOfficerTC = hasGMRoleTC || guildUser?.Roles.Any(r => r.Name.Equals("Officer", StringComparison.OrdinalIgnoreCase)) == true;
            bool isAdmin = guildUser?.GuildPermissions.Administrator == true;
            bool isOwner = (guildUser != null && guildContext.OwnerId == guildUser.Id) || isAdmin;
            bool isClaimer = task.ClaimedBy.HasValue && task.ClaimedBy.Value == component.User.Id;

            switch (action)
            {
                case "approve":
                    if (task.Status != TaskStatus.Unapproved)
                    {
                        await component.RespondAsync($"⚠️ Task #{parsedTaskId} is not Unapproved.", ephemeral: true);
                        return;
                    }
                    if (isOwner)
                    {
                        if (!_taskService.ApproveTask(parsedTaskId, component.User.Id))
                        {
                            await component.RespondAsync($"❌ Failed to approve task #{parsedTaskId}.", ephemeral: true);
                            return;
                        }
                    }
                    else
                    {
                        if (!isOfficerTC)
                        {
                            await component.RespondAsync("🚫 Only officers or Admin can approve tasks.", ephemeral: true);
                            return;
                        }
                        if (isCreator)
                        {
                            await component.RespondAsync("🚫 You cannot approve your own task.", ephemeral: true);
                            return;
                        }
                        if (!_taskService.ApproveTask(parsedTaskId, component.User.Id))
                        {
                            await component.RespondAsync($"❌ Failed to approve task #{parsedTaskId}.", ephemeral: true);
                            return;
                        }
                    }
                    if (component.Message.Channel is SocketTextChannel ua && ua.Id == UNAPPROVED_CHANNEL_ID)
                        await component.Message.DeleteAsync();

                    var boardCh = guildContext?.GetTextChannel(TASKBOARD_CHANNEL_ID);
                    if (boardCh != null)
                    {
                        var embedApp = _embedBuilder.Build(task, viewer: null);
                        var buttonsApp = _componentBuilder.Build(task, viewerId: 0, isOfficer: false).Build();
                        await boardCh.SendMessageAsync(embed: embedApp, components: buttonsApp);
                    }
                    await component.RespondAsync($"✅ Task #{parsedTaskId} approved and moved to Task Board.", ephemeral: true);
                    return;

                case "decline":
                    if (task.Status != TaskStatus.Unapproved)
                    {
                        await component.RespondAsync($"⚠️ Task #{parsedTaskId} is not Unapproved.", ephemeral: true);
                        return;
                    }
                    if (!(isOfficerTC || isOwner))
                    {
                        await component.RespondAsync("🚫 Only officers or Admin can decline tasks.", ephemeral: true);
                        return;
                    }
                    if (isCreator)
                    {
                        await component.RespondAsync("🚫 You cannot decline your own task.", ephemeral: true);
                        return;
                    }
                    if (!_taskService.DeclineTask(parsedTaskId, component.User.Id))
                    {
                        await component.RespondAsync($"❌ Failed to decline task #{parsedTaskId}.", ephemeral: true);
                        return;
                    }
                    if (component.Message.Channel is SocketTextChannel dc && dc.Id == UNAPPROVED_CHANNEL_ID)
                        await component.Message.DeleteAsync();
                    await component.RespondAsync($"❌ Task #{parsedTaskId} declined and removed.", ephemeral: true);
                    return;

                case "claim":
                    if (task.Status != TaskStatus.Approved)
                    {
                        await component.RespondAsync($"⚠️ Task #{parsedTaskId} not available to claim.", ephemeral: true);
                        return;
                    }
                    if (isCreator)
                    {
                        await component.RespondAsync("🚫 You cannot claim your own task.", ephemeral: true);
                        return;
                    }
                    if (!_taskService.ClaimTask(parsedTaskId, component.User.Id))
                    {
                        await component.RespondAsync($"❌ Failed to claim task #{parsedTaskId}.", ephemeral: true);
                        return;
                    }
                    break;

                case "deliver":
                    if (task.Status != TaskStatus.Approved)
                    {
                        await component.RespondAsync("🚫 You can only deliver on Approved tasks.", ephemeral: true);
                        return;
                    }
                    var rm = new ModalBuilder()
                        .WithTitle($"Deliver for Task #{parsedTaskId}")
                        .WithCustomId($"task_deliver_modal_{parsedTaskId}")
                        .AddTextInput("Amount Delivered", "deliver_amount", TextInputStyle.Short, placeholder: "How many units?", required: true)
                        .AddTextInput("Screenshot URL", "deliver_screenshot", TextInputStyle.Short, placeholder: "Link to a screenshot", required: true)
                        .Build();
                    await component.RespondWithModalAsync(rm);
                    return;

                case "abandon":
                    if (!(task.Status == TaskStatus.Claimed && isClaimer))
                    {
                        await component.RespondAsync("🚫 You have not claimed that task.", ephemeral: true);
                        return;
                    }
                    task.ClaimedBy = null;
                    task.Status = TaskStatus.Approved;
                    _taskService.Save();
                    break;

                case "expire":
                    if (!(isCreator || isOfficerTC || isOwner))
                    {
                        await component.RespondAsync("🚫 You do not have permission to expire this task.", ephemeral: true);
                        return;
                    }
                    if (task.Status == TaskStatus.Closed || task.Status == TaskStatus.Expired)
                    {
                        await component.RespondAsync($"⚠️ Task #{parsedTaskId} already finalized.", ephemeral: true);
                        return;
                    }
                    if (!_taskService.ExpireTask(parsedTaskId, component.User.Id))
                    {
                        await component.RespondAsync($"❌ Failed to expire task #{parsedTaskId}.", ephemeral: true);
                        return;
                    }
                    if (component.Message.Channel is SocketTextChannel ec && ec.Id == TASKBOARD_CHANNEL_ID)
                        await component.Message.DeleteAsync();
                    await component.RespondAsync($"🗑️ Task #{parsedTaskId} expired and removed.", ephemeral: true);
                    break;

                case "complete":
                    if (!(task.Status == TaskStatus.Claimed && isClaimer))
                    {
                        await component.RespondAsync("🚫 You can only complete tasks you claimed.", ephemeral: true);
                        return;
                    }
                    if (!_taskService.CompleteTask(parsedTaskId, component.User.Id))
                    {
                        await component.RespondAsync($"❌ Failed to complete task #{parsedTaskId}.", ephemeral: true);
                        return;
                    }
                    task.Status = TaskStatus.Pending;
                    _taskService.Save();
                    break;

                case "close":
                    if (!(task.Status == TaskStatus.Pending && (isCreator || isOwner)))
                    {
                        await component.RespondAsync("🚫 Only creator or Admin can close a pending task.", ephemeral: true);
                        return;
                    }
                    if (!_taskService.CloseTask(parsedTaskId, component.User.Id))
                    {
                        await component.RespondAsync($"❌ Failed to close task #{parsedTaskId}.", ephemeral: true);
                        return;
                    }
                    if (task.ClaimedBy.HasValue)
                    {
                        var userObj = guildContext?.GetUser(task.ClaimedBy.Value);
                        string userTag = userObj != null ? $"{userObj.Username}#{userObj.Discriminator}" : "Unknown#0000";
                        var member = _memberService.GetOrCreate(task.ClaimedBy.Value, userTag);
                        int xpGain = XPService.CalculateXP(task);
                        member.GainXP(xpGain);
                        _memberService.Save();
                        await component.RespondAsync($"🔒 Task #{parsedTaskId} closed. <@{task.ClaimedBy}> earned **{xpGain} XP**!", ephemeral: true);
                    }
                    else
                    {
                        await component.RespondAsync($"🔒 Task #{parsedTaskId} closed and removed.", ephemeral: true);
                    }
                    if (component.Message.Channel is SocketTextChannel cc && cc.Id == TASKBOARD_CHANNEL_ID)
                        await component.Message.DeleteAsync();
                    return;

                default:
                    await component.RespondAsync("❌ Unknown task action.", ephemeral: true);
                    return;
            }

            //
            // 6) Update original embed + buttons in-place
            //
            if (component.Message is IUserMessage originalMessage)
            {
                var updatedEmbed = _embedBuilder.Build(task, guildUser);
                ulong viewerId = (task.Status == TaskStatus.Pending) ? task.CreatedBy : component.User.Id;
                var updatedComponents = _componentBuilder.Build(task, viewerId, isOfficerTC).Build();
                if (task.Status == TaskStatus.Closed || task.Status == TaskStatus.Expired)
                    updatedComponents = new ComponentBuilder().Build();

                await originalMessage.ModifyAsync(m =>
                {
                    m.Embed = updatedEmbed;
                    m.Components = updatedComponents;
                });
            }

            //
            // 7) Ephemeral ack for claim/abandon
            //
            string reply = action switch
            {
                "claim" => $"✋ You claimed task #{parsedTaskId}.",
                "abandon" => $"🔄 You abandoned task #{parsedTaskId}.",
                _ => string.Empty
            };
            if (!string.IsNullOrEmpty(reply))
                await component.RespondAsync(reply, ephemeral: true);
        }
    }
}
