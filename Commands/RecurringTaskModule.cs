using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using VenturaBot.Builders;
using VenturaBot.Data;
using VenturaBot.Services;
using RecurrenceFrequency = VenturaBot.Data.RecurrenceFrequency;

namespace VenturaBot.Modules
{
    [Group("vrecurring", "Manage recurring tasks")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class RecurringTaskModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IRecurringRepo _repo;
        private readonly ITaskService _taskService;
        private readonly DiscordSocketClient _client;
        private readonly TaskEmbedBuilder _embedBuilder;
        private readonly TaskComponentBuilder _compBuilder;

        public RecurringTaskModule(
            IRecurringRepo repo,
            ITaskService taskService,
            DiscordSocketClient client,
            TaskEmbedBuilder embedBuilder,
            TaskComponentBuilder compBuilder)
        {
            _repo = repo;
            _taskService = taskService;
            _client = client;
            _embedBuilder = embedBuilder;
            _compBuilder = compBuilder;
        }

        [SlashCommand("list", "List all recurring task definitions")]
        public async Task ListAsync()
        {
            var defs = _repo.GetAll();
            if (!defs.Any())
            {
                await RespondAsync("No recurring definitions found.", ephemeral: true);
                return;
            }

            var sb = new StringBuilder();
            foreach (var def in defs)
            {
                sb.AppendLine($"**{def.Id}**: {def.Description}");
                sb.AppendLine($"• Type: {def.Type}");
                sb.AppendLine($"• Frequency: {def.Frequency}");
                sb.AppendLine($"• Total Needed: {def.TotalNeeded}");
                sb.AppendLine($"• Drop Location: {def.DropLocation}");
                sb.AppendLine($"• Pot Size: {def.PotSizeVenturans} Venturans");
                sb.AppendLine($"• Expires after: {def.ExpireAfterHours}h");
                sb.AppendLine($"• Channel: <#{def.ChannelId}>");
                sb.AppendLine();
            }

            var embed = new EmbedBuilder()
                .WithTitle("Recurring Task Definitions")
                .WithDescription(sb.ToString())
                .WithColor(Color.Blue)
                .Build();

            await RespondAsync(embed: embed, ephemeral: true);
        }

        [SlashCommand("add", "Add a new recurring task definition")]
        public async Task AddAsync(
            [Summary("id", "Unique identifier")] string id,
            [Summary("type", "Task type")] TaskType type,
            [Summary("totalNeeded", "Total needed for the task")] int totalNeeded,
            [Summary("dropLocation", "Drop-off location")] string dropLocation,
            [Summary("potSizeVenturans", "Pot size (Venturans)")] int potSizeVenturans,
            [Summary("description", "Task description")] string description,
            [Summary("frequency", "Recurrence frequency")] RecurrenceFrequency frequency,
            [Summary("expireAfterHours", "Hours until expiration")] int expireAfterHours,
            [Summary("channel", "Channel to post tasks")] ITextChannel channel)
        {
            if (_repo.GetAll().Any(d => d.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            {
                await RespondAsync($"Definition with ID `{id}` already exists.", ephemeral: true);
                return;
            }

            var def = new RecurringTaskDef
            {
                Id = id,
                Type = type,
                TotalNeeded = totalNeeded,
                DropLocation = dropLocation,
                PotSizeVenturans = potSizeVenturans,
                Description = description,
                Frequency = frequency,
                ExpireAfterHours = expireAfterHours,
                ChannelId = channel.Id
            };

            _repo.AddDefinition(def);
            await RespondAsync($"Added recurring definition `{id}`.", ephemeral: true);
        }

        [SlashCommand("remove", "Remove a recurring definition")]
        public async Task RemoveAsync(
            [Summary("id", "Identifier of the definition to remove")] string id)
        {
            if (_repo.RemoveDefinition(id))
                await RespondAsync($"Removed recurring definition `{id}`.", ephemeral: true);
            else
                await RespondAsync($"Definition `{id}` not found.", ephemeral: true);
        }

        [SlashCommand("update", "Update an existing recurring definition")]
        public async Task UpdateAsync(
            [Summary("id", "ID of the definition to update")] string id,
            [Summary("type", "Task type")] TaskType type,
            [Summary("totalNeeded", "Total needed for the task")] int totalNeeded,
            [Summary("dropLocation", "Drop-off location")] string dropLocation,
            [Summary("potSizeVenturans", "Pot size (Venturans)")] int potSizeVenturans,
            [Summary("description", "Task description")] string description,
            [Summary("frequency", "Recurrence frequency")] RecurrenceFrequency frequency,
            [Summary("expireAfterHours", "Hours until expiration")] int expireAfterHours,
            [Summary("channel", "Channel to post tasks")] ITextChannel channel)
        {
            var existing = _repo.GetAll()
                .FirstOrDefault(d => d.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                await RespondAsync($"Definition `{id}` not found.", ephemeral: true);
                return;
            }

            existing.Type = type;
            existing.TotalNeeded = totalNeeded;
            existing.DropLocation = dropLocation;
            existing.PotSizeVenturans = potSizeVenturans;
            existing.Description = description;
            existing.Frequency = frequency;
            existing.ExpireAfterHours = expireAfterHours;
            existing.ChannelId = channel.Id;

            _repo.UpdateDefinition(existing);
            await RespondAsync($"Updated recurring definition `{id}`.", ephemeral: true);
        }

        [SlashCommand("run", "Trigger a recurring task immediately")]
        public async Task RunAsync(
            [Summary("id", "ID of the definition to trigger")] string id)
        {
            var def = _repo.GetAll()
                .FirstOrDefault(d => d.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (def == null)
            {
                await RespondAsync($"Definition `{id}` not found.", ephemeral: true);
                return;
            }

            await ExecuteDefAsync(def);
            await RespondAsync($"Triggered recurring task `{id}`.", ephemeral: true);
        }

        [SlashCommand("recycle", "Recycle (force-run) all daily or weekly tasks immediately")]
        public async Task RecycleAsync(
            [Summary("frequency", "daily or weekly")] RecurrenceFrequency frequency)
        {
            await DeferAsync(ephemeral: true);

            var defs = _repo.GetAll()
                .Where(d => d.Frequency == frequency)
                .ToList();
            if (!defs.Any())
            {
                await FollowupAsync($"No `{frequency}` definitions found.", ephemeral: true);
                return;
            }

            int limit = frequency switch
            {
                RecurrenceFrequency.Daily => 5,
                RecurrenceFrequency.Weekly => 3,
                _ => defs.Count
            };

            var toRun = defs.OrderBy(_ => Guid.NewGuid()).Take(limit).ToList();
            int count = 0;
            var now = DateTime.UtcNow;

            foreach (var def in toRun)
            {
                var oldTasks = _taskService.GetApprovedTasks()
                    .Where(t => t.RecurringDefinitionId == def.Id)
                    .ToList();
                if (oldTasks.Any() &&
                    _client.GetChannel(def.ChannelId) is IMessageChannel oldChan)
                {
                    foreach (var old in oldTasks)
                    {
                        _taskService.ExpireTask(old.Id, _client.CurrentUser.Id);
                        if (old.BoardMessageId.HasValue)
                            await oldChan.DeleteMessageAsync(old.BoardMessageId.Value);
                    }
                }

                await ExecuteDefAsync(def);
                count++;
            }

            await FollowupAsync($"Recycled and triggered {count} `{frequency}` tasks (limit {limit}).", ephemeral: true);
        }

        [SlashCommand("run-all", "Trigger tasks randomly by frequency")]
        public async Task RunAllAsync(
            [Summary("frequency", "Which frequency to run")] RecurrenceFrequency frequency)
        {
            await DeferAsync(ephemeral: true);

            var candidates = _repo.GetAll()
                .Where(d => d.Frequency == frequency)
                .ToList();
            if (!candidates.Any())
            {
                await FollowupAsync($"No definitions found for frequency `{frequency}`.", ephemeral: true);
                return;
            }

            int limit = frequency switch
            {
                RecurrenceFrequency.Daily => 5,
                RecurrenceFrequency.Weekly => 3,
                _ => candidates.Count
            };

            var selected = candidates.OrderBy(_ => Guid.NewGuid()).Take(limit).ToList();
            int count = 0;
            foreach (var def in selected)
            {
                await ExecuteDefAsync(def);
                count++;
            }

            await FollowupAsync($"Triggered {count} recurring tasks for `{frequency}`.", ephemeral: true);
        }

        /// <summary>
        /// Creates and posts a community task from a definition.
        /// </summary>
        private async Task ExecuteDefAsync(RecurringTaskDef def)
        {
            var now = DateTime.UtcNow;
            var guildTask = _taskService.CreateCommunityTask(
                def.Type,
                def.TotalNeeded,
                def.DropLocation,
                def.PotSizeVenturans,
                def.Description,
                _client.CurrentUser.Id,
                now.AddHours(def.ExpireAfterHours)
            );

            guildTask.RecurringDefinitionId = def.Id;

            if (_client.GetChannel(def.ChannelId) is IMessageChannel chan)
            {
                var embed = _embedBuilder.Build(guildTask, null);
                var comp = _compBuilder.Build(guildTask, Context.User.Id, true).Build();
                var msg = await chan.SendMessageAsync(embed: embed, components: comp);

                guildTask.BoardMessageId = msg.Id;
                guildTask.ChannelId = def.ChannelId;
                _taskService.Save();
                _repo.MarkLastRun(def.Id, now);
            }
        }
    }
}
