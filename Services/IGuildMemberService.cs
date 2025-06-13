// Services/IGuildMemberService.cs
using System.Collections.Generic;
using VenturaBot.Data;

namespace VenturaBot.Services
{
    public interface IGuildMemberService
    {
        void Load();
        void Save();
        GuildMember GetOrCreate(ulong userId, string username);
        IReadOnlyCollection<GuildMember> GetAllMembers();
    }
}
