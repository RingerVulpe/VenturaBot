// Builders/EventComponentBuilder.cs
using Discord;

public class EventComponentBuilder
{
    public MessageComponent Build(string eventId)
    {
        return new ComponentBuilder()
            .WithButton("✅ Going", $"event:{eventId}:Going", ButtonStyle.Success)
            .WithButton("📌 Interested", $"event:{eventId}:Interested", ButtonStyle.Primary)
            .WithButton("❓ Maybe", $"event:{eventId}:Maybe", ButtonStyle.Secondary)
            .WithButton("🚫 Not Going", $"event:{eventId}:NotGoing", ButtonStyle.Danger)
            .Build();
    }
}
