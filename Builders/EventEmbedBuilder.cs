// Builders/EventEmbedBuilder.cs
using Discord;
using VenturaBot.TaskDefinitions;
using VenturaBot.Services.Models;
using System.Linq;
using System.Collections.Generic;
//using for image url 

public class EventEmbedBuilder
{
    public Embed Build(Event e)
    {
        var going = e.Rsvps.Where(x => x.Value == RsvpStatus.Going).Select(x => $"<@{x.Key}>");
        var interested = e.Rsvps.Where(x => x.Value == RsvpStatus.Interested).Select(x => $"<@{x.Key}>");
        var maybe = e.Rsvps.Where(x => x.Value == RsvpStatus.Maybe).Select(x => $"<@{x.Key}>");
        var notGoing = e.Rsvps.Where(x => x.Value == RsvpStatus.NotGoing).Select(x => $"<@{x.Key}>");

        return new EmbedBuilder()
            .WithTitle(e.Title)
            .WithImageUrl(e.ImageUrl)
            .AddField("When", e.ScheduledFor.ToString("f"), inline: false)
            .AddField("Going", going.Any() ? string.Join(" ", going) : "—", inline: true)
            .AddField("Interested", interested.Any() ? string.Join(" ", interested) : "—", inline: true)
            .AddField("Maybe", maybe.Any() ? string.Join(" ", maybe) : "—", inline: true)
            .AddField("Not Going", notGoing.Any() ? string.Join(" ", notGoing) : "—", inline: true)
            .WithColor(Color.DarkGreen)
            .Build();
    }
}

