using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using VenturaBot.Data;

namespace VenturaBot.Services
{
    public static class DataService
    {
        private const string MemberPath = "Storage/members.json";

        public static Dictionary<ulong, GuildMember> Members = new();

        public static void Load()
        {
            if (!File.Exists(MemberPath))
            {
                // No file yet → start with an empty dictionary
                Console.WriteLine($"[DataService] {MemberPath} not found. Starting with empty member list.");
                return;
            }

            var json = File.ReadAllText(MemberPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                // Empty file → treat as no members
                Console.WriteLine($"[DataService] {MemberPath} is empty. Starting with empty member list.");
                return;
            }

            try
            {
                Members = JsonSerializer.Deserialize<Dictionary<ulong, GuildMember>>(json)
                          ?? new Dictionary<ulong, GuildMember>();
                Console.WriteLine($"[DataService] Loaded {Members.Count} members from {MemberPath}.");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[DataService] Warning: failed to parse {MemberPath}. Starting fresh. ({ex.Message})");
                Members = new Dictionary<ulong, GuildMember>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DataService] Unexpected error loading {MemberPath}: {ex.Message}");
                Members = new Dictionary<ulong, GuildMember>();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(MemberPath)!);
                var json = JsonSerializer.Serialize(Members, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(MemberPath, json);
                Console.WriteLine($"[DataService] Saved {Members.Count} members to {MemberPath}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DataService] Error saving {MemberPath}: {ex.Message}");
            }
        }

        public static GuildMember GetOrCreate(ulong userId, string username)
        {
            if (!Members.ContainsKey(userId))
            {
                Members[userId] = new GuildMember
                {
                    UserId = userId,
                    Username = username
                };
            }

            return Members[userId];
        }
    }
}
