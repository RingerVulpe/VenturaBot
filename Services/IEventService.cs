using VenturaBot.Services.Models;
using VenturaBot.TaskDefinitions;

public interface IEventService
{
    Task<Event> CreateAsync(Event e);
    Task<Event> GetAsync(string eventId);
    Task<IReadOnlyCollection<Event>> ListUpcomingAsync();
    Task UpdateRsvpAsync(string eventId, ulong userId, RsvpStatus status);
    Task DeleteAsync(string eventId);
    Task<Event> UpdateAsync(Event evt);
}
