// TaskDefinitions/ScoutReportTaskDefinition.cs
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using VenturaBot.Data;

namespace VenturaBot.TaskDefinitions
{
    public class ScoutReportTaskDefinition : ITaskDefinition
    {
        public TaskType Type => TaskType.ScoutReport;

        public ModalBuilder BuildModal()
        {
            // You can have up to 5 text inputs (rows 0–4)
            var modal = new ModalBuilder()
                .WithTitle("Create Scout Report Task")
                .WithCustomId("task_modal_ScoutReport");

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

            // Row 1: Tip (optional)
            modal.AddTextInput(
                label: "Tip (optional)",
                customId: "tip",
                style: TextInputStyle.Short,
                placeholder: "Enter tip amount or leave blank",
                required: false
            );

            // Row 2: Area Name
            modal.AddTextInput(
                label: "Area Name",
                customId: "areaName",
                style: TextInputStyle.Short,
                placeholder: "e.g. “Northern Dune Ridge”",
                minLength: 1,
                maxLength: 50,
                required: true
            );

            // Row 3: Enemy Sightings / Observations
            modal.AddTextInput(
                label: "Enemy Sightings / Observations",
                customId: "observations",
                style: TextInputStyle.Paragraph,
                placeholder: "Describe any hostiles or points of interest",
                minLength: 1,
                maxLength: 300,
                required: true
            );

            // Row 4: Coordinates / Notes
            modal.AddTextInput(
                label: "Coordinates / Notes",
                customId: "coordinates",
                style: TextInputStyle.Short,
                placeholder: "e.g. “23.5N, 45.2E” or “near abandoned outpost”",
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
            dict["tip"] = modal.Data.Components.First(c => c.CustomId == "tip").Value;
            dict["areaName"] = modal.Data.Components.First(c => c.CustomId == "areaName").Value;
            dict["observations"] = modal.Data.Components.First(c => c.CustomId == "observations").Value;
            dict["coordinates"] = modal.Data.Components.First(c => c.CustomId == "coordinates").Value;

            return dict;
        }
    }
}
