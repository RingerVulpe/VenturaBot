// TaskDefinitions/EventHostTaskDefinition.cs
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using VenturaBot.Data;

namespace VenturaBot.TaskDefinitions
{
    public class EventHostTaskDefinition : ITaskDefinition
    {
        public TaskType Type => TaskType.EventHost;

        public ModalBuilder BuildModal()
        {
            // You can have up to 5 text inputs (rows 0–4)
            var modal = new ModalBuilder()
                .WithTitle("Create Event Host Task")
                .WithCustomId("task_modal_EventHost");

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

            // Row 2: Event Name
            modal.AddTextInput(
                label: "Event Name",
                customId: "eventName",
                style: TextInputStyle.Short,
                placeholder: "e.g. “Sandstorm Meet-up”",
                minLength: 1,
                maxLength: 50,
                required: true
            );

            // Row 3: Date & Time
            modal.AddTextInput(
                label: "Date & Time",
                customId: "dateTime",
                style: TextInputStyle.Short,
                placeholder: "e.g. “2025-06-10 18:00 UTC”",
                minLength: 1,
                maxLength: 50,
                required: true
            );

            // Row 4: Location/Description
            modal.AddTextInput(
                label: "Location/Description",
                customId: "location",
                style: TextInputStyle.Short,
                placeholder: "Where or how to host the event?",
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
            dict["eventName"] = modal.Data.Components.First(c => c.CustomId == "eventName").Value;
            dict["dateTime"] = modal.Data.Components.First(c => c.CustomId == "dateTime").Value;
            dict["location"] = modal.Data.Components.First(c => c.CustomId == "location").Value;

            return dict;
        }
    }
}
