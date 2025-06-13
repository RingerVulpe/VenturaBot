// File: Services/IRecurringRepo.cs
using System;
using System.Collections.Generic;
using VenturaBot.Data;

namespace VenturaBot.Services
{
    public interface IRecurringRepo
    {
        IReadOnlyList<RecurringTaskDef> GetAll();
        void MarkLastRun(string id, DateTime runTime);
        void Save();

        // Manage definitions
        void AddDefinition(RecurringTaskDef def);
        bool RemoveDefinition(string id);
        bool UpdateDefinition(RecurringTaskDef def);
    }
}
