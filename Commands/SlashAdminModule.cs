using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using VenturaBot.Services;

namespace VenturaBot.Commands;

public class SlashAdminModule : InteractionModuleBase<SocketInteractionContext>
{
    // Only allow server owner or bot owner to run this
    [SlashCommand("clear-user", "Delete a registered user from VenturaBot")]
    [RequireOwner]
    public async Task ClearUser(string username)
    {
        var user = DataService.Members.Values
            .FirstOrDefault(m => m.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        if (user == null)
        {
            await RespondAsync("❌ User not found.");
            return;
        }

        DataService.Members.Remove(user.UserId);
        DataService.Save();
        await RespondAsync($"🧹 Cleared data for **{username}**.");
    }

    [SlashCommand("clear-all", "Delete all registered users (DANGER)")]
    [RequireOwner]
    public async Task ClearAll()
    {
        DataService.Members.Clear();
        DataService.Save();
        await RespondAsync("💥 All VenturaBot member data cleared.");
    }

    [SlashCommand("reload-data", "Reload members.json from disk")]
    [RequireOwner]
    public async Task Reload()
    {
        DataService.Load();
        await RespondAsync("🔄 Member data reloaded from disk.");
    }

    [SlashCommand("unregister", "Remove yourself from VenturaBot")]
    public async Task Unregister()
    {
        if (DataService.Members.Remove(Context.User.Id))
        {
            DataService.Save();
            await RespondAsync("🗑️ Your data has been deleted.");
        }
        else
        {
            await RespondAsync("❌ You weren’t registered anyway.");
        }
    }

    // ─── Clear Channel ──────────────────────────────────────────────────────────────

    [SlashCommand("vclear", "Bulk delete recent messages from a text channel (admin only)")]
    [RequireOwner]
    public async Task Vclear(
        [Summary("channel", "Select the text channel to clear")] ITextChannel channel,
        [Summary("limit", "How many recent messages to delete (1-100)")] int limit = 100)
    {
        // Clamp the limit to Discord's bulk-delete max
        limit = Math.Clamp(limit, 1, 100);

        // Fetch and delete messages
        var messages = await channel.GetMessagesAsync(limit).FlattenAsync();
        await channel.DeleteMessagesAsync(messages);

        await RespondAsync($"✅ Cleared {messages.Count()} messages in {channel.Mention}.", ephemeral: true);
    }
}
