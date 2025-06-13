using Discord;
using VenturaBot.Data;
using TaskStatus = VenturaBot.Data.TaskStatus;

namespace VenturaBot.Builders
{
    /// <summary>
    /// Builds a row of buttons for a given GuildTask state.
    /// Permission checks (officer/creator) are enforced in your ButtonHandler.
    /// </summary>
    public class TaskComponentBuilder
    {
        public ComponentBuilder Build(GuildTask task, ulong viewerId, bool isOfficer)
        {
            var row = new ComponentBuilder();

            // ─── Community tasks: Deliver → Complete ───────────────
            if (task.IsCommunityTask)
            {
                // Use TotalNeeded, not Quantity
                double percent = task.TotalNeeded >= 0
                    ? (double)task.TotalContributed / task.TotalNeeded
                    : 0;

                if (percent < 1.0)
                {
                    row.WithButton(
                        label: "📦 Deliver",
                        customId: $"community_deliver_{task.Id}",
                        style: ButtonStyle.Secondary
                    );
                }
                else
                {
                    row.WithButton(
                        label: "✅ Complete",
                        customId: $"community_complete_{task.Id}",
                        style: ButtonStyle.Success,
                        disabled: !isOfficer
                    );
                }

                return row;
            }


            // 1) Unapproved → Approve / Decline (officers only)
            if (task.Status == TaskStatus.Unapproved)
            {
                row.WithButton(
                    label: "✅ Approve",
                    customId: $"task_approve_{task.Id}",
                    style: ButtonStyle.Success
                );
                row.WithButton(
                    label: "❌ Decline",
                    customId: $"task_decline_{task.Id}",
                    style: ButtonStyle.Danger
                );
                return row;
            }

            // 2) Approved → Claim & Deliver (any non-creator)
            if (task.Status == TaskStatus.Approved)
            {
                bool canInteract = viewerId != task.CreatedBy;

                row.WithButton(
                    label: "✋ Claim",
                    customId: $"task_claim_{task.Id}",
                    style: ButtonStyle.Primary,
                    disabled: !canInteract
                );
                row.WithButton(
                    label: "📦 Deliver",
                    customId: $"task_deliver_{task.Id}",
                    style: ButtonStyle.Secondary,
                    disabled: !canInteract
                );
                return row;
            }

            // 3) Claimed → Abandon + Complete (claimer only)
            if (task.Status == TaskStatus.Claimed)
            {
                bool isClaimer = task.ClaimedBy.HasValue && task.ClaimedBy.Value == viewerId;
                row.WithButton(
                    label: "🔄 Abandon",
                    customId: $"task_abandon_{task.Id}",
                    style: ButtonStyle.Secondary,
                    disabled: !isClaimer
                );
                row.WithButton(
                    label: "✅ Complete",
                    customId: $"task_complete_{task.Id}",
                    style: ButtonStyle.Success,
                    disabled: !isClaimer
                );
                return row;
            }

            // 4) Pending → Close (creator only)
            if (task.Status == TaskStatus.Pending)
            {
                bool isCreator = viewerId == task.CreatedBy;
                row.WithButton(
                    label: "🔒 Close",
                    customId: $"task_close_{task.Id}",
                    style: ButtonStyle.Secondary,
                    disabled: !isCreator
                );
                return row;
            }

            // 5) Cancel (creator or officer) for non-final states
            if (task.Status == TaskStatus.Approved ||
                task.Status == TaskStatus.Claimed ||
                task.Status == TaskStatus.Pending)
            {
                bool canCancel = isOfficer || viewerId == task.CreatedBy;
                row.WithButton(
                    label: "🗑️ Cancel",
                    customId: $"task_expire_{task.Id}",
                    style: ButtonStyle.Danger,
                    disabled: !canCancel
                );
                return row;
            }

            // 6) Closed or Expired → no actions
            return row;
        }

        /// <summary>
        /// Buttons for delivery approval flow in community tasks.
        /// </summary>
        public ComponentBuilder BuildDeliveryApprovalButtons(int taskId, ulong userId, int amount)
        {
            return new ComponentBuilder()
                .WithButton(
                    label: "✅ Approve",
                    customId: $"community_deliver_approve:{taskId}:{userId}:{amount}",
                    style: ButtonStyle.Success
                )
                .WithButton(
                    label: "❌ Reject",
                    customId: $"community_deliver_reject:{taskId}:{userId}:{amount}",
                    style: ButtonStyle.Danger
                );
        }
    }
}