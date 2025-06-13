// Services/Models/Event.cs
using System;
using System.Collections.Generic;
using VenturaBot.TaskDefinitions;

namespace VenturaBot.Services.Models
{
    public class Event
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public ulong ChannelId { get; set; }

        /// <summary>
        /// When the event is scheduled to occur.
        /// </summary>
        public DateTimeOffset ScheduledFor { get; set; }

        /// <summary>
        /// Optional recurrence rule string (e.g., "Weekly on Friday at 20:00").
        /// </summary>
        public string Recurrence { get; set; } = string.Empty;

        /// <summary>
        /// The Discord message ID where the event was announced.
        /// </summary>
        public ulong? MessageId { get; set; }

        /// <summary>
        /// RSVP statuses keyed by user ID.
        /// </summary>
        public Dictionary<ulong, RsvpStatus> Rsvps { get; }
            = new Dictionary<ulong, RsvpStatus>();
    }
}
