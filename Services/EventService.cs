using System.Collections.Concurrent;
using VenturaBot.Services.Models;
using VenturaBot.TaskDefinitions;

public class EventService : IEventService
{
    private readonly ConcurrentDictionary<string, Event> _events
        = new ConcurrentDictionary<string, Event>(StringComparer.OrdinalIgnoreCase);

    public Task<Event> CreateAsync(Event e)
    {
        if (e == null) throw new ArgumentNullException(nameof(e));
        if (string.IsNullOrWhiteSpace(e.Id))
            throw new ArgumentException("Event must have a non-empty Id", nameof(e));

        if (!_events.TryAdd(e.Id, e))
            throw new InvalidOperationException($"An event with Id '{e.Id}' already exists.");

        return Task.FromResult(e);
    }

    public Task<Event> UpdateAsync(Event evt)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));

        // Overwrite or add
        _events[evt.Id] = evt;
        return Task.FromResult(evt);
    }

    public Task<Event> GetAsync(string eventId)
    {
        if (eventId == null) throw new ArgumentNullException(nameof(eventId));
        if (_events.TryGetValue(eventId, out var e))
            return Task.FromResult(e);
        throw new KeyNotFoundException($"Event '{eventId}' not found.");
    }

    public Task<IReadOnlyCollection<Event>> ListUpcomingAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var upcoming = _events.Values
            .Where(e => e.ScheduledFor >= now)
            .OrderBy(e => e.ScheduledFor)
            .ToList()
            .AsReadOnly();
        return Task.FromResult<IReadOnlyCollection<Event>>(upcoming);
    }

    public Task UpdateRsvpAsync(string eventId, ulong userId, RsvpStatus status)
    {
        if (eventId == null) throw new ArgumentNullException(nameof(eventId));
        var e = GetAsync(eventId).GetAwaiter().GetResult();  // throws if not found

        lock (e.Rsvps)
        {
            e.Rsvps[userId] = status;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string eventId)
    {
        if (eventId == null) throw new ArgumentNullException(nameof(eventId));
        _events.TryRemove(eventId, out _);
        return Task.CompletedTask;
    }
}
