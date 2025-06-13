using Discord;
using Discord.WebSocket;
using VenturaBot.Data;
using VenturaBot.Services;
using System;
using System.Linq;
using System.Globalization;
using TaskStatus = VenturaBot.Data.TaskStatus;

namespace VenturaBot.Builders
{
    /// <summary>
    /// Responsible for constructing an Embed for a given GuildTask,
    /// including extra fields for both standard and community tasks.
    /// </summary>
    public class TaskEmbedBuilder
    {
        public TaskEmbedBuilder()
        {
            // No DI for XPService needed; it’s static
        }

        public Embed Build(GuildTask task, SocketGuildUser? viewer)
        {
            // Community task override
            if (task.IsCommunityTask)
            {
                var eb = new EmbedBuilder()
                    // 1) Title = the task description (so it shows as your “material type”)
                    .WithTitle($"{task.Description}")
                    .WithColor(Color.Gold)

                    // 2) Big Total Needed up top
                    .WithDescription($""" 🏁 **Total Needed: {task.TotalNeeded:N0}** """);

                // 3) If you still want the longer description/details…
                eb.AddField("📝 Details",
                    // you can truncate if it’s super long
                    task.Description.Length > 200
                      ? task.Description.Substring(0, 200) + "…"
                      : task.Description,
                    inline: false);

                // 4) Inline: drop-off location + pot size
                eb.AddField("📍 Drop Location",
                    string.IsNullOrWhiteSpace(task.DropLocation)
                      ? "`Not specified`"
                      : $"`{task.DropLocation}`",
                    inline: true);

                eb.AddField("💰 Pot",
                    $"{task.PotSizeVenturans:N0} Venturans",
                    inline: true);

                // 5) Progress bar section
                var contributed = task.Contributions?.Values.Sum() ?? 0;
                var percent = task.TotalNeeded > 0
                                  ? (double)contributed / task.TotalNeeded
                                  : 0;
                var progressBar = BuildProgressBar(percent);

                eb.AddField("📊 Progress",
                    $"""
                    {progressBar}

                    **{contributed:N0}/{task.TotalNeeded:N0}** ({percent:P0})
                    """,
                    inline: false);

                // 6) Leaderboard
                if (task.Contributions != null && task.Contributions.Any())
                {
                    var lines = task.Contributions
                        .OrderByDescending(kv => kv.Value)
                        .Select((kv, idx) => $"#{idx + 1} • <@{kv.Key}> — {kv.Value:N0}");
                    eb.AddField("📋 Leaderboard",
                        string.Join("\n", lines),
                        inline: false);
                }
                else
                {
                    eb.AddField("📋 Leaderboard",
                        "`No contributions yet`",
                        inline: false);
                }

                // 7) Footer: posted by + timestamp (smaller, at very bottom)
                eb.WithFooter($"Posted by <@{task.CreatedBy}> • Created: {task.CreatedAt:yyyy-MM-dd HH:mm} UTC");

                return eb.Build();


            }

            // Standard task embed
            string statusText = task.Status switch
            {
                TaskStatus.Unapproved => "⚠️ Unapproved",
                TaskStatus.Approved => "🟢 Approved",
                TaskStatus.Claimed => "✋ Claimed",
                TaskStatus.Pending => "⏳ Pending Verification",
                TaskStatus.Verified => "✅ Verified",
                TaskStatus.Closed => "🔒 Closed",
                TaskStatus.Expired => "🗑️ Expired",
                _ => "❓ Unknown"
            };

            var builder = new EmbedBuilder()
                .WithTitle($"🛠️ Task #{task.Id}")
                .WithDescription($"**{task.Description}**")
                .WithColor(Color.Orange)
                .WithFooter($"Created: {task.CreatedAt:yyyy-MM-dd HH:mm} UTC");

            // Core fields
            builder.AddField("👤 Posted By", $"<@{task.CreatedBy}>", inline: false);
            builder.AddField("📂 Type", $"`{task.Type}`", inline: false);
            builder.AddField("🏅 Tier", $"`T{task.Tier}`", inline: false);
            builder.AddField("🔢 Quantity", $"`{task.Quantity}`", inline: false);
            builder.AddField("🔍 Status", $"`{statusText}`", inline: false);

            // XP Reward
            var xpReward = XPService.CalculateXP(task);
            builder.AddField("⭐ XP Reward", $"`{xpReward}`", inline: false);

            // Claimer
            if (task.Status == TaskStatus.Claimed && task.ClaimedBy.HasValue)
                builder.AddField("🤝 Claimer", $"<@{task.ClaimedBy.Value}>", inline: false);

            // Verified
            builder.AddField("✅ Verified", task.Verified ? "`Yes`" : "`No`", inline: false);

            // Delivery & Tip
            builder.AddField("🚚 Delivery Method",
                !string.IsNullOrWhiteSpace(task.DeliveryMethod) ? $"`{task.DeliveryMethod}`" : "`Not specified`",
                inline: false);
            builder.AddField("💰 Tip",
                task.Tip > 0 ? $"`{task.Tip} Sol`" : "`None`",
                inline: false);

            // Timestamps
            if (task.CompletedAt.HasValue)
                builder.AddField("✅ Completed At", $"{task.CompletedAt:yyyy-MM-dd HH:mm} UTC", inline: false);
            if (task.VerifiedAt.HasValue)
                builder.AddField("🔎 Verified At", $"{task.VerifiedAt:yyyy-MM-dd HH:mm} UTC", inline: false);
            if (task.ClosedAt.HasValue)
                builder.AddField("🔒 Closed At", $"{task.ClosedAt:yyyy-MM-dd HH:mm} UTC", inline: false);
            if (task.ExpiredAt.HasValue)
                builder.AddField("⏰ Expired At", $"{task.ExpiredAt:yyyy-MM-dd HH:mm} UTC", inline: false);

            // Type-specific sections
            switch (task.Type)
            {
                case TaskType.Gather:
                    if (!string.IsNullOrWhiteSpace(task.Location))
                        builder.AddField("📍 Location", $"`{task.Location}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.ItemName))
                        builder.AddField("🪵 Material Name", $"`{task.ItemName}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.Notes))
                        builder.AddField("📝 Notes", $"`{task.Notes}`", inline: false);
                    break;
                case TaskType.Repair:
                    if (!string.IsNullOrWhiteSpace(task.ItemName))
                        builder.AddField("🔧 Item to Repair", $"`{task.ItemName}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.Location))
                        builder.AddField("📍 Repair Location", $"`{task.Location}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.Notes))
                        builder.AddField("📝 Notes", $"`{task.Notes}`", inline: false);
                    break;
                case TaskType.CraftingOrder:
                    if (!string.IsNullOrWhiteSpace(task.RecipeName))
                        builder.AddField("📜 Recipe Name", $"`{task.RecipeName}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.MaterialsList))
                        builder.AddField("📦 Materials List", $"`{task.MaterialsList}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.DeliveryLocation))
                        builder.AddField("🚚 Delivery Location", $"`{task.DeliveryLocation}`", inline: false);
                    break;
                case TaskType.VehicleOrder:
                    if (!string.IsNullOrWhiteSpace(task.VehicleModel))
                        builder.AddField("🚗 Vehicle Model", $"`{task.VehicleModel}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.DeliveryLocation))
                        builder.AddField("🚚 Delivery Destination", $"`{task.DeliveryLocation}`", inline: false);
                    break;
                case TaskType.ConstructionOrder:
                    if (!string.IsNullOrWhiteSpace(task.StructureName))
                        builder.AddField("🏗️ Structure Name", $"`{task.StructureName}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.Location))
                        builder.AddField("📍 Build Location", $"`{task.Location}`", inline: false);
                    break;
                case TaskType.ResourceDelivery:
                    if (!string.IsNullOrWhiteSpace(task.ResourceName))
                        builder.AddField("📦 Resource Name", $"`{task.ResourceName}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.DeliveryLocation))
                        builder.AddField("🚚 Delivery Location", $"`{task.DeliveryLocation}`", inline: false);
                    break;
                case TaskType.DeepDesertMap:
                    if (task.Sections.HasValue && task.Sections.Value > 0)
                        builder.AddField("🗺️ Sections to Chart", $"`{task.Sections}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.AreaName))
                        builder.AddField("📛 Area Name", $"`{task.AreaName}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.Coordinates))
                        builder.AddField("📍 Coordinates/Description", $"`{task.Coordinates}`", inline: false);
                    break;
                case TaskType.ExchangeRevenue:
                    if (task.Amount.HasValue && task.Amount.Value > 0)
                        builder.AddField("💵 Revenue Amount", $"`{task.Amount}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.ItemName))
                        builder.AddField("📦 Item Traded", $"`{task.ItemName}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.Location))
                        builder.AddField("📍 Exchange Location", $"`{task.Location}`", inline: false);
                    break;
                case TaskType.SchematicHunt:
                    if (!string.IsNullOrWhiteSpace(task.SchematicName))
                        builder.AddField("📑 Schematic Name", $"`{task.SchematicName}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.Location))
                        builder.AddField("📍 Search Location", $"`{task.Location}`", inline: false);
                    break;
                case TaskType.EventHost:
                    if (!string.IsNullOrWhiteSpace(task.EventName))
                        builder.AddField("🎉 Event Name", $"`{task.EventName}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.EventDateTime))
                        builder.AddField("🕒 Date & Time", $"`{task.EventDateTime}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.Location))
                        builder.AddField("📍 Event<Location/Description", $"`{task.Location}`", inline: false);
                    break;
                case TaskType.GroupExpedition:
                    if (!string.IsNullOrWhiteSpace(task.Objective))
                        builder.AddField("🗻 Expedition Objective", $"`{task.Objective}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.Location))
                        builder.AddField("🚩 Start → Destination", $"`{task.Location}`", inline: false);
                    break;
                case TaskType.ScoutReport:
                    if (!string.IsNullOrWhiteSpace(task.AreaName))
                        builder.AddField("📛 Area Name", $"`{task.AreaName}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.Observations))
                        builder.AddField("🔭 Observations", $"`{task.Observations}`", inline: false);
                    if (!string.IsNullOrWhiteSpace(task.Coordinates))
                        builder.AddField("📍 Coordinates/Notes", $"`{task.Coordinates}`", inline: false);
                    break;
                default:
                    break;
            }

            return builder.Build();
        }

        private static string BuildProgressBar(double percent)
        {
            const int barSize = 10;

            // round and clamp so we never go below 0 or above barSize
            int fillCount = (int)Math.Round(percent * barSize);
            fillCount = Math.Max(0, Math.Min(barSize, fillCount));

            int emptyCount = barSize - fillCount;

            return new string('▰', fillCount) + new string('▱', emptyCount);
        }
    }
}
