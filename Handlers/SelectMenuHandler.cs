// Handlers/SelectMenuHandler.cs
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VenturaBot.Data;
using VenturaBot.TaskDefinitions;

namespace VenturaBot.Handlers
{
    public class SelectMenuHandler
    {
        private readonly IServiceProvider _services;

        // Tracks partial selections: (TaskType, DeliveryMethod)
        private static readonly ConcurrentDictionary<ulong, (TaskType Type, string Delivery)> _pending
            = new();

        public SelectMenuHandler(IServiceProvider services)
        {
            _services = services;
        }

        public async Task HandleSelectMenuAsync(SocketMessageComponent component)
        {
            var customId = component.Data.CustomId;
            var userId = component.User.Id;

            // ─── Step 1: Task Type selection ─────────────────────────────────────────
            if (customId == "select_task_type")
            {
                var chosen = component.Data.Values.FirstOrDefault();
                if (!Enum.TryParse<TaskType>(chosen, true, out var selectedType))
                    selectedType = TaskType.Unknown;

                // Save type (delivery may update later)
                _pending.AddOrUpdate(
                    userId,
                    (selectedType, delivery: string.Empty),
                    (_, old) => (selectedType, old.Delivery)
                );

                // If Community, skip Delivery step and pop up community modal
                if (selectedType == TaskType.Community)
                {
                    var modal = new ModalBuilder()
                        .WithTitle("Create Community Task")
                        .WithCustomId($"community_modal_{selectedType}")
                        .AddTextInput(
                            label: "Total Needed",
                            customId: "community_totalNeeded",
                            style: TextInputStyle.Short,
                            placeholder: "Enter total needed",
                            required: true
                        )
                        .AddTextInput(
                            label: "Drop Location",
                            customId: "community_dropLocation",
                            style: TextInputStyle.Short,
                            placeholder: "Where to drop",
                            required: false
                        )
                        .AddTextInput(
                            label: "Pot Size (Venturans)",
                            customId: "community_potSize",
                            style: TextInputStyle.Short,
                            placeholder: "Enter pot amount",
                            required: true
                        )
                        .AddTextInput(
                            label: "Description",
                            customId: "community_description",
                            style: TextInputStyle.Paragraph,
                            placeholder: "Optional details",
                            required: false
                        );

                    await component.RespondWithModalAsync(modal.Build());
                    _pending.TryRemove(userId, out _);
                    return;
                }

                // Otherwise, ask for Delivery Method next
                await component.RespondAsync(
                    $"✅ Task Type set to **{selectedType}**. Now select the delivery method.",
                    ephemeral: true
                );
                return;
            }

            // ─── Step 2: Delivery Method selection ────────────────────────────────────
            if (customId == "select_delivery_method")
            {
                var chosenDelivery = component.Data.Values.FirstOrDefault() ?? "P2P Trade";

                _pending.AddOrUpdate(
                    userId,
                    (Type: TaskType.Unknown, Delivery: chosenDelivery),
                    (_, old) => (old.Type, chosenDelivery)
                );

                // If both Type and Delivery are set, fire the standard modal
                if (_pending.TryGetValue(userId, out var tuple)
                    && tuple.Type != TaskType.Unknown
                    && !string.IsNullOrWhiteSpace(tuple.Delivery))
                {
                    var definitions = _services.GetServices<ITaskDefinition>();
                    var definition = definitions.FirstOrDefault(d => d.Type == tuple.Type);
                    if (definition == null)
                    {
                        await component.RespondAsync(
                            "❌ No task definition found for that type.",
                            ephemeral: true
                        );
                        return;
                    }

                    // Build and show the task creation modal
                    var modalBuilder = definition.BuildModal();
                    modalBuilder.CustomId = $"task_modal_{tuple.Type}:{tuple.Delivery}";
                    await component.RespondWithModalAsync(modalBuilder.Build());
                    _pending.TryRemove(userId, out _);
                    return;
                }

                // Otherwise, still need task type
                await component.RespondAsync(
                    $"✅ Delivery method set to **{chosenDelivery}**. Now select the task type.",
                    ephemeral: true
                );
                return;
            }

            // ─── Other selects: ignore ───────────────────────────────────────────────
        }
    }
}
