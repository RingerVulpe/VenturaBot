// TaskDefinitions/ResourceDeliveryTaskDefinition.cs
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using VenturaBot.Data;

namespace VenturaBot.TaskDefinitions
{
    public class ResourceDeliveryTaskDefinition : ITaskDefinition
    {
        public TaskType Type => TaskType.ResourceDelivery;

        public ModalBuilder BuildModal()
        {
            // You can have up to 5 text inputs (rows 0–4)
            var modal = new ModalBuilder()
                .WithTitle("Create Resource Delivery Task")
                .WithCustomId("task_modal_ResourceDelivery");

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
                placeholder: "How many units to deliver?",
                minLength: 1,
                maxLength: 4,
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

            // Row 3: Resource Name
            modal.AddTextInput(
                label: "Resource Name",
                customId: "resourceName",
                style: TextInputStyle.Short,
                placeholder: "e.g. “Water” or “Food Rations”",
                minLength: 1,
                maxLength: 50,
                required: true
            );

            // Row 4: Delivery Location
            modal.AddTextInput(
                label: "Delivery Location",
                customId: "deliveryLocation",
                style: TextInputStyle.Short,
                placeholder: "Where should it be delivered?",
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
            dict["resourceName"] = modal.Data.Components.First(c => c.CustomId == "resourceName").Value;
            dict["deliveryLocation"] = modal.Data.Components.First(c => c.CustomId == "deliveryLocation").Value;

            return dict;
        }
    }
}
