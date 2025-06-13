using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using VenturaBot.Data;

namespace VenturaBot.Services
{
    public class FileBasedRecurringRepo : IRecurringRepo
    {
        private const string RecurringPath = "Storage/recurringTasks.json";
        private readonly List<RecurringTaskDef> _defs;

        public FileBasedRecurringRepo()
        {
            if (File.Exists(RecurringPath))
            {
                var json = File.ReadAllText(RecurringPath);
                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                opts.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                _defs = JsonSerializer.Deserialize<List<RecurringTaskDef>>(json, opts)
                        ?? new List<RecurringTaskDef>();
            }
            else
            {
                _defs = new List<RecurringTaskDef>();
            }
        }

        public IReadOnlyList<RecurringTaskDef> GetAll() => _defs;

        public void AddDefinition(RecurringTaskDef def)
        {
            if (_defs.Any(d => d.Id.Equals(def.Id, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Recurring definition with ID '{def.Id}' already exists.");

            _defs.Add(def);
            Save();
        }

        public bool RemoveDefinition(string id)
        {
            var def = _defs.FirstOrDefault(d => d.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (def == null)
                return false;

            _defs.Remove(def);
            Save();
            return true;
        }

        public bool UpdateDefinition(RecurringTaskDef def)
        {
            var index = _defs.FindIndex(d => d.Id.Equals(def.Id, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                return false;

            def.LastRunUtc = _defs[index].LastRunUtc;
            _defs[index] = def;
            Save();
            return true;
        }

        public void MarkLastRun(string id, DateTime runTime)
        {
            var def = _defs.FirstOrDefault(d => d.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (def != null)
            {
                def.LastRunUtc = runTime;
                Save();
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RecurringPath)!);
            var opts = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            opts.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            var json = JsonSerializer.Serialize(_defs, opts);
            File.WriteAllText(RecurringPath, json);
        }
    }
}
