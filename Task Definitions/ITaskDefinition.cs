// TaskDefinitions/ITaskDefinition.cs
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Generic;
using VenturaBot.Data;

namespace VenturaBot.TaskDefinitions
{
    /// <summary>
    /// Knows how to build a modal and parse its submission for a specific TaskType.
    /// </summary>
    public interface ITaskDefinition
    {
        /// <summary>The TaskType this definition handles (e.g. Harvest, Repair, etc.).</summary>
        TaskType Type { get; }

        /// <summary>Builds a ModalBuilder with whatever fields are needed for this TaskType.</summary>
        ModalBuilder BuildModal();

        /// <summary>
        /// Parses the submitted modal (SocketModal) into a dictionary of field-id → value.
        /// The keys here must match the IDs used in BuildModal().
        /// </summary>
        Dictionary<string, string> ParseSubmission(SocketModal modal);
    }
}
