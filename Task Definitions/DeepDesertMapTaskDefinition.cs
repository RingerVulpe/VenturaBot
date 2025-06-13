// TaskDefinitions/DeepDesertMapTaskDefinition.cs
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using VenturaBot.Data;

namespace VenturaBot.TaskDefinitions
{
    public class DeepDesertMapTaskDefinition : ITaskDefinition
    {
        public TaskType Type => TaskType.DeepDesertMap;

        public ModalBuilder BuildModal()
        {
            // You can have up to 5 text inputs (rows 0–4)
            var modal = new ModalBuilder()
                .WithTitle("Create Deep Desert Map Task")
                .WithCustomId("task_modal_DeepDesertMap");

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

            // Row 1: Sections to Chart
            modal.AddTextInput(
                label: "Sections to Chart",
                customId: "sections",
                style: TextInputStyle.Short,
                placeholder: "How many map sections?",
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

            // Row 3: Area Name
            modal.AddTextInput(
                label: "Area Name",
                customId: "areaName",
                style: TextInputStyle.Short,
                placeholder: "e.g. “Maw of the Dunes”",
                minLength: 1,
                maxLength: 50,
                required: true
            );

            // Row 4: Coordinates/Description
            modal.AddTextInput(
                label: "Coordinates/Description",
                customId: "coordinates",
                style: TextInputStyle.Short,
                placeholder: "e.g. “45.3N, 72.1W” or “south ridge of canyon”",
                minLength: 1,
                maxLength: 100,
                required: true
            );

            return modal;
        }

        public Dictionary<string, string> ParseSubmission(SocketModal modal)
        {
            var dict = new Dictionary<string, string>();

            dict["tier"] = modal.Data.Components.First(c => c.CustomId == "tier").Value;
            dict["sections"] = modal.Data.Components.First(c => c.CustomId == "sections").Value;
            dict["tip"] = modal.Data.Components.First(c => c.CustomId == "tip").Value;
            dict["areaName"] = modal.Data.Components.First(c => c.CustomId == "areaName").Value;
            dict["coordinates"] = modal.Data.Components.First(c => c.CustomId == "coordinates").Value;

            return dict;
        }
    }
}
