// TaskDefinitions/SchematicHuntTaskDefinition.cs
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using VenturaBot.Data;

namespace VenturaBot.TaskDefinitions
{
    public class SchematicHuntTaskDefinition : ITaskDefinition
    {
        public TaskType Type => TaskType.SchematicHunt;

        public ModalBuilder BuildModal()
        {
            // You can have up to 5 text inputs (rows 0–4)
            var modal = new ModalBuilder()
                .WithTitle("Create Schematic Hunt Task")
                .WithCustomId("task_modal_SchematicHunt");

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

            // Row 1: Number of Schematics
            modal.AddTextInput(
                label: "Number of Schematics",
                customId: "quantity",
                style: TextInputStyle.Short,
                placeholder: "How many schematics to locate?",
                minLength: 1,
                maxLength: 2,
                required: true,
                value: "1"
            );

            // Row 2: Tip (optional)
            modal.AddTextInput(
                label: "Tip (optional)",
                customId: "tip",
                style: TextInputStyle.Short,
                placeholder: "Enter tip amount or leave blank",
                required: false
            );

            // Row 3: Schematic Name/Type
            modal.AddTextInput(
                label: "Schematic Name/Type",
                customId: "schematicName",
                style: TextInputStyle.Short,
                placeholder: "e.g. “Advanced Turret Schematic”",
                minLength: 1,
                maxLength: 50,
                required: true
            );

            // Row 4: Search Location
            modal.AddTextInput(
                label: "Search Location",
                customId: "location",
                style: TextInputStyle.Short,
                placeholder: "Where to hunt for schematics?",
                minLength: 1,
                maxLength: 100,
                required: true
            );

            return modal;
        }

        public Dictionary<string, string> ParseSubmission(SocketModal modal)
        {
            // Extract the values exactly as keyed above
            var dict = new Dictionary<string, string>();

            dict["tier"] = modal.Data.Components.First(c => c.CustomId == "tier").Value;
            dict["quantity"] = modal.Data.Components.First(c => c.CustomId == "quantity").Value;
            dict["tip"] = modal.Data.Components.First(c => c.CustomId == "tip").Value;
            dict["schematicName"] = modal.Data.Components.First(c => c.CustomId == "schematicName").Value;
            dict["location"] = modal.Data.Components.First(c => c.CustomId == "location").Value;

            return dict;
        }
    }
}
