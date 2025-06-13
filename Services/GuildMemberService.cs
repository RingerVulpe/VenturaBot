// Services/GuildMemberService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using VenturaBot.Data;

namespace VenturaBot.Services
{


    public class GuildMemberService : IGuildMemberService
    {
        private const string MemberPath = "Storage/members.json";

        // Store members in a dictionary keyed by userId
        private readonly Dictionary<ulong, GuildMember> _members = new();

        public void Load()
        {
            if (!File.Exists(MemberPath))
            {
                Console.WriteLine($"[GuildMemberService] {MemberPath} not found. Starting fresh.");
                return;
            }

            var json = File.ReadAllText(MemberPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine($"[GuildMemberService] {MemberPath} is empty. Starting fresh.");
                return;
            }

            try
            {
                var loaded = JsonSerializer.Deserialize<Dictionary<ulong, GuildMember>>(json);
                if (loaded is not null)
                {
                    // Clear existing dictionary and repopulate
                    _members.Clear();
                    foreach (var kv in loaded)
                        _members[kv.Key] = kv.Value;

                    Console.WriteLine($"[GuildMemberService] Loaded {_members.Count} members from {MemberPath}.");
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[GuildMemberService] Warning: failed to parse {MemberPath}. Starting fresh. ({ex.Message})");
                _members.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GuildMemberService] Unexpected error loading {MemberPath}: {ex.Message}");
                _members.Clear();
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(MemberPath)!);
                var json = JsonSerializer.Serialize(_members, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(MemberPath, json);
                Console.WriteLine($"[GuildMemberService] Saved {_members.Count} members to {MemberPath}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GuildMemberService] Error saving {MemberPath}: {ex.Message}");
            }
        }

        public GuildMember GetOrCreate(ulong userId, string username)
        {
            if (!_members.TryGetValue(userId, out var member))
            {
                member = new GuildMember
                {
                    UserId = userId,
                    Username = username,
                    XP = 0,
                    IsVerified = false
                };
                _members[userId] = member;
            }
            else if (member.Username != username)
            {
                // Update username if changed
                member.Username = username;
            }

            return member;
        }

        public IReadOnlyCollection<GuildMember> GetAllMembers() => _members.Values;
    }
}
