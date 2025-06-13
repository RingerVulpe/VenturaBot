// TaskDefinitions/ExchangeRevenueTaskDefinition.cs
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using VenturaBot.Data;

namespace VenturaBot.TaskDefinitions
{
    public class ExchangeRevenueTaskDefinition : ITaskDefinition
    {
        public TaskType Type => TaskType.ExchangeRevenue;

        public ModalBuilder BuildModal()
        {
            // You can have up to 5 text inputs (rows 0–4)
            var modal = new ModalBuilder()
                .WithTitle("Create Exchange Revenue Task")
                .WithCustomId("task_modal_ExchangeRevenue");

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

            // Row 1: Revenue Amount
            modal.AddTextInput(
                label: "Revenue Amount",
                customId: "amount",
                style: TextInputStyle.Short,
                placeholder: "Enter total revenue (e.g. 500)",
                minLength: 1,
                maxLength: 10,
                required: true,
                value: "0"
            );

            // Row 2: Tip (optional)
            modal.AddTextInput(
                label: "Tip (optional)",
                customId: "tip",
                style: TextInputStyle.Short,
                placeholder: "Enter tip amount or leave blank",
                required: false
            );

            // Row 3: Item Traded
            modal.AddTextInput(
                label: "Item Traded",
                customId: "itemName",
                style: TextInputStyle.Short,
                placeholder: "e.g. “Spice Bundle”",
                minLength: 1,
                maxLength: 50,
                required: true
            );

            // Row 4: Exchange Location
            modal.AddTextInput(
                label: "Exchange Location",
                customId: "location",
                style: TextInputStyle.Short,
                placeholder: "Where did the trade occur?",
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
            dict["amount"] = modal.Data.Components.First(c => c.CustomId == "amount").Value;
            dict["tip"] = modal.Data.Components.First(c => c.CustomId == "tip").Value;
            dict["itemName"] = modal.Data.Components.First(c => c.CustomId == "itemName").Value;
            dict["location"] = modal.Data.Components.First(c => c.CustomId == "location").Value;

            return dict;
        }
    }
}
