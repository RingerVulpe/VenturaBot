// TaskDefinitions/RepairTaskDefinition.cs
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Generic;
using VenturaBot.Data;

namespace VenturaBot.TaskDefinitions
{
    public class RepairTaskDefinition : ITaskDefinition
    {
        public TaskType Type => TaskType.Repair;

        public ModalBuilder BuildModal()
        {
            // This modal asks for: tier, itemName, damageLevel, notes
            return new ModalBuilder()
                .WithTitle("New Repair Task")
                .WithCustomId($"task_modal_{Type}") // → "task_modal_Repair"
                .AddTextInput(
                    label: "Tier (1–6)",
                    customId: "tier",
                    required: true,
                    maxLength: 1
                )
                .AddTextInput(
                    label: "Item to Repair",
                    customId: "itemName",
                    required: true,
                    maxLength: 50
                )
                .AddTextInput(
                    label: "Additional Notes",
                    customId: "notes",
                    required: false,
                    maxLength: 200
                )
                .AddTextInput(
                    label: "Tip (Optional)",
                    customId: "tip",
                    required: true,
                    maxLength: 3
                );
        }

        public Dictionary<string, string> ParseSubmission(SocketModal modal)
        {
            var dict = new Dictionary<string, string>();
            foreach (var comp in modal.Data.Components)
            {
                dict[comp.CustomId] = comp.Value;
            }
            // We expect "tier", "itemName", "damageLevel", "notes"
            return dict;
        }
    }
}
