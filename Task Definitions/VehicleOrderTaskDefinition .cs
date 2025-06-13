// TaskDefinitions/VehicleOrderTaskDefinition.cs
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using VenturaBot.Data;

namespace VenturaBot.TaskDefinitions
{
    public class VehicleOrderTaskDefinition : ITaskDefinition
    {
        public TaskType Type => TaskType.VehicleOrder;

        public ModalBuilder BuildModal()
        {
            // You can have up to 5 text inputs (rows 0–4)
            var modal = new ModalBuilder()
                .WithTitle("Create Vehicle Order Task")
                .WithCustomId("task_modal_VehicleOrder");

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
                placeholder: "How many vehicles to deliver?",
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

            // Row 3: Vehicle Model
            modal.AddTextInput(
                label: "Vehicle Model",
                customId: "vehicleModel",
                style: TextInputStyle.Short,
                placeholder: "e.g. “Sandbike”",
                minLength: 1,
                maxLength: 50,
                required: true
            );

            // Row 4: Destination
            modal.AddTextInput(
                label: "Destination",
                customId: "destination",
                style: TextInputStyle.Short,
                placeholder: "Where to deliver?",
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
            dict["vehicleModel"] = modal.Data.Components.First(c => c.CustomId == "vehicleModel").Value;
            dict["destination"] = modal.Data.Components.First(c => c.CustomId == "destination").Value;

            return dict;
        }
    }
}
