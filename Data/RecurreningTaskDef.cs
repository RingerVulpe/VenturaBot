using System;

namespace VenturaBot.Data
{
    public enum RecurrenceFrequency
    {
        Daily,
        Weekly,
        // you can add more (e.g. GuildObjective, Monthly, etc.)
    }

    public class RecurringTaskDef
    {
        public string Id { get; set; } = "";                  // unique key, e.g. "daily-gather-wood"
        public TaskType Type { get; set; }                    // reuse your existing TaskType
        public int TotalNeeded { get; set; }                  // for community tasks
        public string DropLocation { get; set; } = "";
        public int PotSizeVenturans { get; set; }
        public string Description { get; set; } = "";
        public RecurrenceFrequency Frequency { get; set; }
        public int ExpireAfterHours { get; set; }             // how long until this task auto-expires
        public ulong ChannelId { get; set; }                  // Discord channel to post into

        // tracks when we last fired this definition
        public DateTime? LastRunUtc { get; set; }
    }
}
