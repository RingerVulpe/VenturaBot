// TaskDefinitions/GroupExpeditionTaskDefinition.cs
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using VenturaBot.Data;

namespace VenturaBot.TaskDefinitions
{
    public class GroupExpeditionTaskDefinition : ITaskDefinition
    {
        public TaskType Type => TaskType.GroupExpedition;

        public ModalBuilder BuildModal()
        {
            // You can have up to 5 text inputs (rows 0–4)
            var modal = new ModalBuilder()
                .WithTitle("Create Group Expedition Task")
                .WithCustomId("task_modal_GroupExpedition");

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

            // Row 1: Group Size
            modal.AddTextInput(
                label: "Group Size",
                customId: "quantity",
                style: TextInputStyle.Short,
                placeholder: "Number of participants?",
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

            // Row 3: Expedition Name/Objectives
            modal.AddTextInput(
                label: "Expedition Objective",
                customId: "objective",
                style: TextInputStyle.Short,
                placeholder: "e.g. “Dune Supply Run”",
                minLength: 1,
                maxLength: 50,
                required: true
            );

            // Row 4: Start & End Locations
            modal.AddTextInput(
                label: "Start → Destination",
                customId: "location",
                style: TextInputStyle.Short,
                placeholder: "e.g. “Base Camp → Oasis Outpost”",
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
            dict["quantity"] = modal.Data.Components.First(c => c.CustomId == "quantity").Value;
            dict["tip"] = modal.Data.Components.First(c => c.CustomId == "tip").Value;
            dict["objective"] = modal.Data.Components.First(c => c.CustomId == "objective").Value;
            dict["location"] = modal.Data.Components.First(c => c.CustomId == "location").Value;

            return dict;
        }
    }
}
