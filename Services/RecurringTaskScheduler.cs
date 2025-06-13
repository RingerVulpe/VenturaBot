using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VenturaBot.Builders;
using VenturaBot.Data;
using RecurrenceFrequency = VenturaBot.Data.RecurrenceFrequency;

namespace VenturaBot.Services
{
    public class RecurringTaskScheduler : IHostedService, IDisposable
    {
        private readonly IServiceProvider _sp;
        private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(1));

        public RecurringTaskScheduler(IServiceProvider sp)
            => _sp = sp;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // fire-and-forget loop
            _ = Task.Run(async () =>
            {
                while (await _timer.WaitForNextTickAsync(cancellationToken))
                    await DoWorkAsync();
            }, cancellationToken);

            return Task.CompletedTask;
        }

        private async Task DoWorkAsync()
        {
            using var scope = _sp.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRecurringRepo>();
            var defs = repo.GetAll();
            var taskSvc = scope.ServiceProvider.GetRequiredService<ITaskService>();
            var client = scope.ServiceProvider.GetRequiredService<DiscordSocketClient>();
            var embedBuilder = scope.ServiceProvider.GetRequiredService<TaskEmbedBuilder>();
            var compBuilder = scope.ServiceProvider.GetRequiredService<TaskComponentBuilder>();
            var now = DateTime.UtcNow;

            // 1) Fire any due recurring definitions
            foreach (var def in defs)
            {
                if (!IsDue(def, now))
                    continue;

                // Create the community task
                var guildTask = taskSvc.CreateCommunityTask(
                    def.Type,
                    def.TotalNeeded,
                    def.DropLocation,
                    def.PotSizeVenturans,
                    def.Description,
                    client.CurrentUser.Id,
                    now.AddHours(def.ExpireAfterHours)
                );

                // Post embed + buttons
                if (client.GetChannel(def.ChannelId) is IMessageChannel chan)
                {
                    var embed = embedBuilder.Build(guildTask, null);

                    var component = compBuilder
                        .Build(guildTask, client.CurrentUser.Id, isOfficer: true)
                        .Build();

                    var msg = await chan.SendMessageAsync(
                        embed: embed,
                        components: component
                    );

                    guildTask.BoardMessageId = msg.Id;
                    taskSvc.Save();
                }

                // record that we just ran it
                repo.MarkLastRun(def.Id, now);
            }


            // 2) Expire any community tasks whose expirationDate <= now
            var toExpire = taskSvc.GetApprovedTasks()
                                  .Where(t => t.ExpirationDate <= now)
                                  .ToList();

            foreach (var old in toExpire)
            {
                taskSvc.ExpireTask(old.Id, client.CurrentUser.Id);
                if (old.BoardMessageId.HasValue &&
                    client.GetChannel(old.ChannelId) is IMessageChannel ch)
                {
                    await ch.DeleteMessageAsync(old.BoardMessageId.Value);
                }
            }
        }

        private bool IsDue(RecurringTaskDef def, DateTime now)
        {
            if (def.LastRunUtc.HasValue)
            {
                var elapsed = now - def.LastRunUtc.Value;
                return def.Frequency switch
                {
                    RecurrenceFrequency.Daily => elapsed >= TimeSpan.FromDays(1),
                    RecurrenceFrequency.Weekly => elapsed >= TimeSpan.FromDays(7),
                    _ => false
                };
            }
            // never run before
            return true;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public void Dispose() => _timer.Dispose();
    }
}
