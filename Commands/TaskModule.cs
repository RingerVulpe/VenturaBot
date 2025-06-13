// Modules/TaskModule.cs
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using VenturaBot.Services;
using VenturaBot.Data;
using VenturaBot.Builders;

namespace VenturaBot.Modules
{
    public class TaskModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ITaskService _taskService;
        private readonly TaskEmbedBuilder _embedBuilder;
        private readonly TaskComponentBuilder _componentBuilder;

        // ←– Replace with your actual “Unapproved Tasks” channel ID below:
        private const ulong UNAPPROVED_CHANNEL_ID = 1379731733930446928;

        public TaskModule(
            ITaskService taskService,
            TaskEmbedBuilder embedBuilder,
            TaskComponentBuilder componentBuilder)
        {
            _taskService = taskService;
            _embedBuilder = embedBuilder;
            _componentBuilder = componentBuilder;
        }

        [SlashCommand("createtask", "Create a new guild task.")]
        public async Task CreateTaskAsync(
            [Summary("type", "Select the task type.")] TaskType type,
            [Summary("tier", "Tier (1–6).")][MinValue(1)][MaxValue(6)] int tier,
            [Summary("count", "Quantity required.")][MinValue(1)] int count,
            [Summary("desc", "Describe the task.")] string desc,
            [Summary("tip", "Optional Solaris tip amount.")] int tip = 0,
            [Choice("P2P Trade", "P2P Trade")]
            [Choice("Drop-off Box", "Drop-off Box")]
            [Summary("delivery", "Delivery method.")] string deliveryMethod = "P2P Trade"
        )
        {
            try
            {
                Console.WriteLine($"[TaskModule] /createtask by {Context.User.Id}: " +
                                  $"type={type}, tier={tier}, count={count}, desc=\"{desc}\", " +
                                  $"tip={tip}, delivery={deliveryMethod}");

                // 1) Create the new task in Unapproved state:
                var newTask = _taskService.CreateTask(
                    type,
                    tier,
                    count,
                    desc,
                    Context.User.Id,
                    deliveryMethod,
                    tip,
                    expirationDate: null
                );
                Console.WriteLine($"[TaskModule] Created Task ID={newTask.Id}");

                // 2) Build its embed (TaskEmbedBuilder will show “⚠️ Unapproved”):
                var embed = _embedBuilder.Build(newTask, viewer: null);

                // 3) Build its buttons for Unapproved (Approve/Decline)
                //    We pass viewerId=0 and isOfficer=false—permission is checked later.
                var buttons = _componentBuilder
                                .Build(newTask, viewerId: 0, isOfficer: false)
                                .Build();

                // 4) Post in the Unapproved channel:
                var guildSocket = Context.Guild as SocketGuild;
                var unapprovedChannel = guildSocket?.GetTextChannel(UNAPPROVED_CHANNEL_ID);
                if (unapprovedChannel != null)
                {
                    Console.WriteLine($"[TaskModule] Posting Task#{newTask.Id} to Unapproved channel {UNAPPROVED_CHANNEL_ID}");
                    await unapprovedChannel.SendMessageAsync(embed: embed, components: buttons);
                }
                else
                {
                    Console.WriteLine("[TaskModule] ERROR: Unapproved channel not found.");
                }

                // 5) Acknowledge to the user:
                await RespondAsync(
                    $"✅ Task #{newTask.Id} created (Unapproved). An officer or the server owner will review it shortly.",
                    ephemeral: true
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TaskModule] EXCEPTION: {ex}");
                try
                {
                    await RespondAsync(
                        $"⚠️ An error occurred: {ex.GetType().Name}",
                        ephemeral: true
                    );
                }
                catch
                {
                    Console.WriteLine("[TaskModule] Failed to send fallback reply.");
                }
            }
        }
    }
}
