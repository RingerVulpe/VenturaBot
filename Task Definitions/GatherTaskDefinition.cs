// TaskDefinitions/GatherTaskDefinition.cs
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Generic;
using VenturaBot.Data;

namespace VenturaBot.TaskDefinitions
{
    public class GatherTaskDefinition : ITaskDefinition
    {
        public TaskType Type => TaskType.Gather;

        public ModalBuilder BuildModal()
        {
            // You can only have up to 5 text inputs (rows 0–4)
            var modal = new ModalBuilder()
                .WithTitle("Create Gather Task")
                .WithCustomId("task_modal_Gather");

            // Row 0: Tier
            modal.AddTextInput(
                label: "Tier (1–6)",
                customId: "tier",
                style: TextInputStyle.Short,
                placeholder: "Enter a number between 1 and 6",
                minLength: 1,
                maxLength: 1,
                required: true,
                value: "1"
            );

            // Row 1: Quantity
            modal.AddTextInput(
                label: "Quantity",
                customId: "quantity",
                style: TextInputStyle.Short,
                placeholder: "How many units?",
                minLength: 1,
                maxLength: 5,
                required: true,
                value: "1"
            );

            // Row 2: Tip
            modal.AddTextInput(
                label: "Tip (optional)",
                customId: "tip",
                style: TextInputStyle.Short,
                placeholder: "Enter tip amount or leave blank",
                required: false
            );

            // Row 3: Item Name
            modal.AddTextInput(
                label: "Material Name",
                customId: "itemName",
                style: TextInputStyle.Short,
                placeholder: "e.g. “Wood”",
                minLength: 1,
                maxLength: 50,
                required: true
            );

            // Row 4: Location
            modal.AddTextInput(
                label: "Location",
                customId: "location",
                style: TextInputStyle.Short,
                placeholder: "Where to gather?",
                minLength: 1,
                maxLength: 100,
                required: true
            );

            // If you need “notes,” you must remove one of the above or collect notes separately
            return modal;
        }

        public Dictionary<string, string> ParseSubmission(SocketModal modal)
        {
            // Extract the values exactly as keyed above
            var dict = new Dictionary<string, string>();

            dict["tier"] = modal.Data.Components.First(c => c.CustomId == "tier").Value;
            dict["quantity"] = modal.Data.Components.First(c => c.CustomId == "quantity").Value;
            dict["tip"] = modal.Data.Components.First(c => c.CustomId == "tip").Value;
            dict["itemName"] = modal.Data.Components.First(c => c.CustomId == "itemName").Value;
            dict["location"] = modal.Data.Components.First(c => c.CustomId == "location").Value;

            return dict;
        }
    }
}
