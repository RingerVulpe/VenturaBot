using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using VenturaBot.Data;
using VenturaBot.Services;
using TaskStatus = VenturaBot.Data.TaskStatus;

namespace VenturaBot.Commands
{
    [Group("vtask", "Ventura Task System")]
    public class SlashTaskModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ITaskService _taskService;

        public SlashTaskModule(ITaskService taskService)
        {
            _taskService = taskService;
        }

        private bool UserIsOfficer()
        {
            var user = Context.User as SocketGuildUser;
            return user?.Roles.Any(r => string.Equals(r.Name, "Officer", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(r.Name, "GM", StringComparison.OrdinalIgnoreCase))
                   == true;
        }

        //// ─── Create ──────────────────────────────────────────────────────────────────

        //[SlashCommand("create", "Create a new task")]
        //[RequireOwner]
        //public async Task CreateTask(
        //    TaskType type,
        //    int tier,
        //    int quantity,
        //    int tip,
        //    [Choice("P2P Trade", "P2P Trade")]
        //    [Choice("Drop-off Box", "Drop-off Box")]
        //    string deliveryMethod,
        //    [Summary("description", "Describe the task")] string description)
        //{
        //    // CreateTask now requires deliveryMethod and tip; no expiration by default
        //    var task = _taskService.CreateTask(
        //        type,
        //        tier,
        //        quantity,
        //        description,
        //        Context.User.Id,
        //        deliveryMethod,
        //        tip,
        //        expirationDate: null
        //    );

        //    await RespondAsync(
        //        $"✅ Created task #{task.Id} (Unapproved) — {type} T{tier} x{quantity}:\n" +
        //        $"{description}\n" +
        //        $"• Tip: {(tip > 0 ? $"{tip} Sol" : "None")}\n" +
        //        $"• Delivery Method: {deliveryMethod}"
        //    );
        //}

        [SlashCommand("setup-task-button", "Post the task creation button")]
        [RequireOwner]
        public async Task SetupTaskButton()
        {
            var builder = new ComponentBuilder()
                .WithButton("📝 Create New Task", customId: "create_task");

            await RespondAsync(
                "Use the button below to create a new guild task (it will enter the Unapproved queue):",
                components: builder.Build()
            );
        }

        // ─── Approve / Decline ──────────────────────────────────────────────────────

        //[SlashCommand("approve", "Approve an unapproved task (officer only)")]
        //[RequireOwner]
        //public async Task ApproveTask(int id)
        //{
        //    var task = _taskService.GetById(id);
        //    if (task is null)
        //    {
        //        await RespondAsync($"❌ Task #{id} not found.");
        //        return;
        //    }

        //    if (task.Status != TaskStatus.Unapproved)
        //    {
        //        await RespondAsync($"⚠️ Task #{id} is not in Unapproved status.");
        //        return;
        //    }

        //    if (task.CreatedBy == Context.User.Id)
        //    {
        //        await RespondAsync("🚫 You cannot approve your own task.");
        //        return;
        //    }

        //    bool isOfficer = (Context.User as SocketGuildUser)?
        //        .Roles.Any(r => r.Name.Equals("Officer", StringComparison.OrdinalIgnoreCase)
        //                     || r.Name.Equals("GM", StringComparison.OrdinalIgnoreCase))
        //        == true;

        //    if (!isOfficer)
        //    {
        //        await RespondAsync("🚫 You do not have permission to approve tasks.");
        //        return;
        //    }

        //    var ok = _taskService.ApproveTask(id, Context.User.Id);
        //    if (!ok)
        //    {
        //        await RespondAsync($"❌ Failed to approve task #{id}. Make sure it is Unapproved and you are not the creator.");
        //        return;
        //    }

        //    await RespondAsync($"✅ Task #{id} has been approved and is now available on the Task Board.");
        //}

        //[SlashCommand("decline", "Decline (expire) an unapproved task (officer only)")]
        //[RequireOwner]
        //public async Task DeclineTask(int id)
        //{
        //    var task = _taskService.GetById(id);
        //    if (task is null)
        //    {
        //        await RespondAsync($"❌ Task #{id} not found.");
        //        return;
        //    }

        //    if (task.Status != TaskStatus.Unapproved)
        //    {
        //        await RespondAsync($"⚠️ Task #{id} is not in Unapproved status.");
        //        return;
        //    }

        //    if (task.CreatedBy == Context.User.Id)
        //    {
        //        await RespondAsync("🚫 You cannot decline your own task.");
        //        return;
        //    }

        //    bool isOfficer = (Context.User as SocketGuildUser)?
        //        .Roles.Any(r => r.Name.Equals("Officer", StringComparison.OrdinalIgnoreCase)
        //                     || r.Name.Equals("GM", StringComparison.OrdinalIgnoreCase))
        //        == true;

        //    if (!isOfficer)
        //    {
        //        await RespondAsync("🚫 You do not have permission to decline tasks.");
        //        return;
        //    }

        //    var ok = _taskService.DeclineTask(id, Context.User.Id);
        //    if (!ok)
        //    {
        //        await RespondAsync($"❌ Failed to decline task #{id}. Make sure it is Unapproved and you are not the creator.");
        //        return;
        //    }

        //    await RespondAsync($"❌ Task #{id} has been declined and marked as Expired.");
        //}

        // ─── View Approved Tasks (Open for Claiming) ─────────────────────────────────

        //[SlashCommand("open", "View all approved tasks available for claiming")]
        //[RequireOwner]
        //public async Task OpenTasks()
        //{
        //    var tasks = _taskService.GetApprovedTasks();
        //    if (!tasks.Any())
        //    {
        //        await RespondAsync("📭 No approved tasks available for claiming.");
        //        return;
        //    }

        //    var lines = tasks
        //        .OrderBy(t => t.Id)
        //        .Select(t =>
        //            $"🟢 `#{t.Id}` **{t.Type}** T{t.Tier} x{t.Quantity} — {t.Description}\n" +
        //            $"• Tip: {(t.Tip > 0 ? $"{t.Tip} Sol" : "None")} | Delivery: {t.DeliveryMethod}"
        //        );

        //    var message = string.Join("\n\n", lines);
        //    await RespondAsync($"📋 **Approved Tasks:**\n{message}");
        //}

        // ─── Claim ───────────────────────────────────────────────────────────────────

        //[SlashCommand("claim", "Claim an approved task")]
        //[RequireOwner]
        //public async Task ClaimTask(int id)
        //{
        //    var task = _taskService.GetById(id);
        //    if (task is null)
        //    {
        //        await RespondAsync($"❌ Task #{id} not found.");
        //        return;
        //    }

        //    if (task.Status != TaskStatus.Approved)
        //    {
        //        await RespondAsync($"⚠️ Task #{id} is not available to claim (Status: {task.Status}).");
        //        return;
        //    }

        //    if (task.CreatedBy == Context.User.Id)
        //    {
        //        await RespondAsync("🚫 You cannot claim your own task.");
        //        return;
        //    }

        //    var ok = _taskService.ClaimTask(id, Context.User.Id);
        //    if (!ok)
        //    {
        //        await RespondAsync($"❌ Failed to claim task #{id}. Make sure it is still Approved and you are not the creator.");
        //        return;
        //    }

        //    await RespondAsync($"✋ You claimed task #{id}. Good luck! Now its status is Claimed (Pending completion).");
        //}

        // ─── Complete ────────────────────────────────────────────────────────────────

        //[SlashCommand("complete", "Mark a claimed task as completed (moves to Pending)")]
        //[RequireOwner]
        //public async Task CompleteTask(int id)
        //{
        //    var task = _taskService.GetById(id);
        //    if (task is null)
        //    {
        //        await RespondAsync($"❌ Task #{id} not found.");
        //        return;
        //    }

        //    if (task.Status != TaskStatus.Claimed)
        //    {
        //        await RespondAsync($"⚠️ Task #{id} is not currently claimed.");
        //        return;
        //    }

        //    if (task.ClaimedBy != Context.User.Id)
        //    {
        //        await RespondAsync("🚫 You can only complete tasks you have claimed.");
        //        return;
        //    }

        //    var ok = _taskService.CompleteTask(id, Context.User.Id);
        //    if (!ok)
        //    {
        //        await RespondAsync($"❌ Failed to mark task #{id} as completed.");
        //        return;
        //    }

        //    await RespondAsync($"✅ Task #{id} marked as Completed and is now Pending Verification.");
        //}

        // ─── Verify ──────────────────────────────────────────────────────────────────

        //[SlashCommand("verify", "Verify a completed task (officer only)")]
        //[RequireOwner]
        //public async Task VerifyTask(int id)
        //{
        //    var task = _taskService.GetById(id);
        //    if (task is null)
        //    {
        //        await RespondAsync($"❌ Task #{id} not found.");
        //        return;
        //    }

        //    if (task.Status != TaskStatus.Pending)
        //    {
        //        await RespondAsync($"⚠️ Task #{id} is not pending verification.");
        //        return;
        //    }

        //    bool isOfficer = (Context.User as SocketGuildUser)?
        //        .Roles.Any(r => r.Name.Equals("Officer", StringComparison.OrdinalIgnoreCase)
        //                     || r.Name.Equals("GM", StringComparison.OrdinalIgnoreCase))
        //        == true;

        //    if (!isOfficer && task.CreatedBy != Context.User.Id)
        //    {
        //        await RespondAsync("🚫 You do not have permission to verify this task.");
        //        return;
        //    }

        //    // Award XP to the claimer upon verification
        //    int xpGained = 0;
        //    if (task.ClaimedBy.HasValue)
        //    {
        //        // Context.Guild is a SocketGuild (at runtime), but its compile‐time type is IGuild.
        //        // Cast to SocketGuild so we can call GetUser(...) synchronously:
        //        var socketGuild = Context.Guild as SocketGuild;
        //        SocketGuildUser socketUser = null;
        //        string usernameDiscr = "Unknown#0000";

        //        if (socketGuild != null)
        //        {
        //            socketUser = socketGuild.GetUser(task.ClaimedBy.Value);
        //            if (socketUser != null)
        //                usernameDiscr = $"{socketUser.Username}#{socketUser.Discriminator}";
        //        }

        //        var member = DataService.GetOrCreate(
        //            task.ClaimedBy.Value,
        //            usernameDiscr
        //        );

        //        xpGained = XPService.CalculateXP(task);
        //        member.XP += xpGained;
        //        DataService.Save();
        //    }

        //    await RespondAsync(
        //        $"✅ Task #{id} has been verified and is now Verified.\n" +
        //        $"<@{task.ClaimedBy}> earned **{xpGained} XP**!"
        //    );
        //}

        // ─── Close ───────────────────────────────────────────────────────────────────

        //[SlashCommand("close", "Close a verified task (creator only)")]
        //[RequireOwner]
        //public async Task CloseTask(int id)
        //{
        //    var task = _taskService.GetById(id);
        //    if (task is null)
        //    {
        //        await RespondAsync($"❌ Task #{id} not found.");
        //        return;
        //    }

        //    if (task.Status != TaskStatus.Verified)
        //    {
        //        await RespondAsync($"⚠️ Task #{id} is not verified and cannot be closed.");
        //        return;
        //    }

        //    if (task.CreatedBy != Context.User.Id)
        //    {
        //        await RespondAsync("🚫 Only the task creator can close this task.");
        //        return;
        //    }

        //    var ok = _taskService.CloseTask(id, Context.User.Id);
        //    if (!ok)
        //    {
        //        await RespondAsync($"❌ Failed to close task #{id}. Make sure it is Verified and you are the creator.");
        //        return;
        //    }

        //    await RespondAsync($"🔒 Task #{id} has been closed successfully.");
        //}

        // ─── Cancel (Expire) ─────────────────────────────────────────────────────────

        [SlashCommand("cancel", "Expire (cancel) a task you created or as an officer")]
        [RequireOwner]
        public async Task CancelTask(int id)
        {
            var task = _taskService.GetById(id);
            if (task is null)
            {
                await RespondAsync($"❌ Task #{id} not found.");
                return;
            }

            bool isOwner = task.CreatedBy == Context.User.Id;
            bool isOfficer = (Context.User as SocketGuildUser)?
                .Roles.Any(r => r.Name.Equals("Officer", StringComparison.OrdinalIgnoreCase)
                             || r.Name.Equals("GM", StringComparison.OrdinalIgnoreCase))
                == true;

            if (!isOwner && !isOfficer)
            {
                await RespondAsync("🚫 You do not have permission to cancel this task.");
                return;
            }

            var ok = _taskService.ExpireTask(id, Context.User.Id);
            if (!ok)
            {
                await RespondAsync($"❌ Failed to cancel (expire) task #{id}. It may already be finalized.");
                return;
            }

            await RespondAsync($"🗑️ Task #{id} has been canceled and marked as Expired.");
        }

        // ─── Delete ───────────────────────────────────────────────────────────────────

        [SlashCommand("delete", "Permanently delete a task (creator or officer)")]
        [RequireOwner]
        public async Task DeleteTask(int id)
        {
            var task = _taskService.GetById(id);
            if (task is null)
            {
                await RespondAsync($"❌ Task #{id} not found.");
                return;
            }

            bool isOwner = task.CreatedBy == Context.User.Id;
            bool isOfficer = (Context.User as SocketGuildUser)?
                .Roles.Any(r => r.Name.Equals("Officer", StringComparison.OrdinalIgnoreCase)
                             || r.Name.Equals("GM", StringComparison.OrdinalIgnoreCase))
                == true;

            if (!isOwner && !isOfficer)
            {
                await RespondAsync("🚫 You do not have permission to delete this task.");
                return;
            }

            var ok = _taskService.DeleteTask(id, Context.User.Id);
            if (!ok)
            {
                await RespondAsync($"❌ Failed to delete task #{id}. Make sure you have permissions.");
                return;
            }

            await RespondAsync($"🗑️ Task #{id} has been permanently deleted.");
        }

        //// ─── Abandon (Unclaim) ───────────────────────────────────────────────────────

        //[SlashCommand("abandon", "Unclaim a task you've previously claimed")]
        //[RequireOwner]
        //public async Task AbandonTask(int id)
        //{
        //    var task = _taskService.GetById(id);
        //    if (task is null)
        //    {
        //        await RespondAsync($"❌ Task #{id} not found.");
        //        return;
        //    }

        //    if (task.ClaimedBy != Context.User.Id)
        //    {
        //        await RespondAsync("🚫 You can only abandon tasks you have claimed.");
        //        return;
        //    }

        //    // Transition back to Approved
        //    task.ClaimedBy = null;
        //    task.Status = TaskStatus.Approved;
        //    _taskService.Save();

        //    await RespondAsync($"🔄 You have abandoned task #{id}. It is now Approved again.");
        //}

        // ─── “My” Tasks ─────────────────────────────────────────────────────────────

        //[SlashCommand("my", "List your personal tasks (created & claimed)")]
        //public async Task TaskMy()
        //{
        //    var userId = Context.User.Id;
        //    var allTasks = _taskService.GetAll();

        //    var myCreated = allTasks.Where(t => t.CreatedBy == userId).OrderBy(t => t.Id).ToList();
        //    var myClaimed = allTasks.Where(t => t.ClaimedBy == userId).OrderBy(t => t.Id).ToList();

        //    var embed = new EmbedBuilder()
        //        .WithTitle($"{Context.User.Username}'s Tasks")
        //        .WithColor(Color.DarkBlue);

        //    embed.AddField("Created Tasks", myCreated.Any()
        //        ? string.Join("\n", myCreated.Select(t => $"`#{t.Id}` - {t.Description} ({t.Status})"))
        //        : "None");

        //    embed.AddField("Claimed Tasks", myClaimed.Any()
        //        ? string.Join("\n", myClaimed.Select(t => $"`#{t.Id}` - {t.Description} ({t.Status})"))
        //        : "None");

        //    await RespondAsync(embed: embed.Build());
        //}

        //// ─── History ─────────────────────────────────────────────────────────────────

        //[SlashCommand("history", "View task history counts for a member")]
        //public async Task TaskHistory(SocketGuildUser? user = null)
        //{
        //    var targetUser = user ?? Context.User;
        //    var allTasks = _taskService.GetAll();

        //    var createdCount = allTasks.Count(t => t.CreatedBy == targetUser.Id);
        //    var claimedCount = allTasks.Count(t => t.ClaimedBy == targetUser.Id);
        //    var verifiedCount = allTasks.Count(t => t.Status == TaskStatus.Verified && t.ClaimedBy == targetUser.Id);
        //    var closedCount = allTasks.Count(t => t.Status == TaskStatus.Closed && t.CreatedBy == targetUser.Id);

        //    var embed = new EmbedBuilder()
        //        .WithTitle($"Task History for {targetUser.Username}")
        //        .WithColor(Color.Teal);

        //    embed.AddField("Created", createdCount, true);
        //    embed.AddField("Claimed", claimedCount, true);
        //    embed.AddField("Verified (by them)", verifiedCount, true);
        //    embed.AddField("Closed (they created)", closedCount, true);

        //    await RespondAsync(embed: embed.Build());
        //}

        // ─── Clear All ───────────────────────────────────────────────────────────────

        [SlashCommand("clear", "Clear all tasks (admin only)")]
        [RequireOwner]
        public async Task ClearTasks()
        {
            _taskService.ClearAllTasks();
            await RespondAsync("🗑️ All tasks have been cleared.");
        }

        //// ─── Community Task Creation ────────────────────────────────────────────────────────────────────────────
        //[SlashCommand("create-community", "Create a new guild community task (officer only)")]
        //public async Task CreateCommunityTask(
        //    int totalNeeded,
        //    string dropLocation,
        //    int potSizeVenturans,
        //    [Summary("description", "Describe the community task")] string description)
        //{
        //    if (!UserIsOfficer())
        //    {
        //        await RespondAsync("🚫 You do not have permission to create community tasks.");
        //        return;
        //    }

        //    var task = _taskService.CreateCommunityTask(
        //        type: TaskType.Community,
        //        totalNeeded: totalNeeded,
        //        dropLocation: dropLocation,
        //        potSizeVenturans: potSizeVenturans,
        //        description: description,
        //        creatorId: Context.User.Id,
        //        expirationDate: null
        //    );

        //    await RespondAsync($"🛡️ Community Task #{task.Id} created: **{description}**\n" +
        //                       $"• Total Needed: {totalNeeded}\n" +
        //                       $"• Drop Location: {dropLocation}\n" +
        //                       $"• Pot: {potSizeVenturans} Venturans\n" +
        //                       $"Members can submit their contributions with `/vtask submit-delivery {task.Id} <amount> <screenshotUrl>`");
        //}

        //// ─── Submit Delivery ──────────────────────────────────────────────────────────────────────────────────
        //[SlashCommand("submit-delivery", "Submit a contribution to a community task")]
        //public async Task SubmitDelivery(
        //    int taskId,
        //    int amount,
        //    [Summary("screenshotUrl", "URL of proof image")] string screenshotUrl)
        //{
        //    var task = _taskService.GetById(taskId);
        //    if (task == null || !task.IsCommunityTask)
        //    {
        //        await RespondAsync($"❌ Task #{taskId} not found or is not a community task.");
        //        return;
        //    }
        //    if (amount <= 0)
        //    {
        //        await RespondAsync("🚫 Contribution amount must be greater than zero.");
        //        return;
        //    }

        //    var ok = _taskService.RecordContribution(taskId, Context.User.Id, amount);
        //    if (!ok)
        //    {
        //        await RespondAsync($"❌ Failed to record contribution for Task #{taskId}.");
        //        return;
        //    }

        //    await RespondAsync($"✅ Recorded your contribution of {amount} units for Task #{taskId}. " +
        //                       $"Proof: {screenshotUrl}");
        //}

        //// ─── Verify Delivery ─────────────────────────────────────────────────────────────────────────────────
        //[SlashCommand("verify-delivery", "Verify a member's contribution to a community task (officer only)")]
        //public async Task VerifyDelivery(
        //    int taskId,
        //    IUser user)
        //{
        //    if (!UserIsOfficer())
        //    {
        //        await RespondAsync("🚫 You do not have permission to verify contributions.");
        //        return;
        //    }

        //    var task = _taskService.GetById(taskId);
        //    if (task == null || !task.IsCommunityTask)
        //    {
        //        await RespondAsync($"❌ Task #{taskId} not found or is not a community task.");
        //        return;
        //    }
        //    if (!task.Contributions.ContainsKey(user.Id))
        //    {
        //        await RespondAsync($"⚠️ {user.Mention} has not contributed to Task #{taskId}.");
        //        return;
        //    }

        //    // Calculate payout share
        //    int contributed = task.Contributions[user.Id];
        //    int total = task.TotalContributed;
        //    int payout = (int)Math.Floor(task.PotSizeVenturans * (double)contributed / total);

        //    // Award Venturans to user
        //    var member = DataService.GetOrCreate(user.Id, user.Username + "#" + user.Discriminator);
        //    member.GainVenturans(payout);
        //    DataService.Save();

        //    await RespondAsync($"✅ Verified {user.Mention}'s contribution of {contributed} units." +
        //                       $" They receive **{payout} Venturans**.");
        //}


    }
}
