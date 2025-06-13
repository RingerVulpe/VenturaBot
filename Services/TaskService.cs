using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VenturaBot.Data;
using TaskStatus = VenturaBot.Data.TaskStatus;

namespace VenturaBot.Services
{
    public interface ITaskService
    {
        void Load();
        void Save();

        IReadOnlyList<GuildTask> GetAll();
        IReadOnlyList<GuildTask> GetUnapprovedTasks();
        IReadOnlyList<GuildTask> GetApprovedTasks();
        GuildTask? GetById(int id);

        // Standard task creation
        GuildTask CreateTask(
            TaskType type,
            int tier,
            int quantity,
            string description,
            ulong creatorId,
            string deliveryMethod,
            int tip,
            DateTime? expirationDate = null
        );

        //Community task creation (officer-only)
        GuildTask CreateCommunityTask(
            TaskType type,
            int totalNeeded,
            string dropLocation,
            int potSizeVenturans,
            string description,
            ulong creatorId,
            DateTime? expirationDate = null
        );

        bool ApproveTask(int id, ulong approverId);
        bool DeclineTask(int id, ulong declinerId);
        bool ClaimTask(int id, ulong userId);
        bool CompleteTask(int id, ulong userId);
        bool CloseTask(int id, ulong closerId);
        bool ExpireTask(int id, ulong expirerId);
        bool DeleteTask(int id, ulong requesterId);
        void ClearAllTasks();
        int GenerateNewId();

        // Community contribution operations
        bool RecordContribution(int taskId, ulong userId, int amount);
        List<(ulong UserId, int Contributed)> GetTopContributors(int taskId, int top);
    }

    public class TaskService : ITaskService
    {
        private const string TaskPath = "Storage/tasks.json";
        private readonly List<GuildTask> _tasks = new();

        public void Load()
        {
            if (File.Exists(TaskPath))
            {
                var json = File.ReadAllText(TaskPath);
                var loaded = JsonSerializer.Deserialize<List<GuildTask>>(json);
                if (loaded is not null)
                    _tasks.AddRange(loaded);
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TaskPath)!);
            var json = JsonSerializer.Serialize(_tasks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(TaskPath, json);
        }

        public IReadOnlyList<GuildTask> GetAll() => _tasks;
        public IReadOnlyList<GuildTask> GetUnapprovedTasks() => _tasks.Where(t => t.Status == TaskStatus.Unapproved).ToList();
        public IReadOnlyList<GuildTask> GetApprovedTasks() => _tasks.Where(t => t.Status == TaskStatus.Approved).ToList();
        public GuildTask? GetById(int id) => _tasks.FirstOrDefault(t => t.Id == id);

        public GuildTask CreateTask(
            TaskType type,
            int tier,
            int quantity,
            string description,
            ulong creatorId,
            string deliveryMethod,
            int tip,
            DateTime? expirationDate = null
        )
        {
            var id = GenerateNewId();
            var task = new GuildTask
            {
                Id = id,
                Type = type,
                Tier = tier,
                Quantity = quantity,
                Description = description,
                CreatedBy = creatorId,
                Status = TaskStatus.Unapproved,
                CreatedAt = DateTime.UtcNow,
                DeliveryMethod = deliveryMethod,
                Tip = tip,
                ExpirationDate = expirationDate
            };
            _tasks.Add(task);
            Save();
            return task;
        }

        public GuildTask CreateCommunityTask(
            TaskType type,
            int totalNeeded,
            string dropLocation,
            int potSizeVenturans,
            string description,
            ulong creatorId,
            DateTime? expirationDate = null
        )
        {
            var id = GenerateNewId();
            var task = new GuildTask
            {
                Id = id,
                Type = type,
                Tier = 0,
                Quantity = 0,
                Description = description,
                CreatedBy = creatorId,
                Status = TaskStatus.Approved,      // community tasks go straight to Approved
                CreatedAt = DateTime.UtcNow,
                IsCommunityTask = true,
                TotalNeeded = totalNeeded,
                DropLocation = dropLocation,
                PotSizeVenturans = potSizeVenturans,
                ExpirationDate = expirationDate
            };
            _tasks.Add(task);
            Save();
            return task;
        }

        public bool ApproveTask(int id, ulong approverId)
        {
            var task = GetById(id);
            if (task is null || task.Status != TaskStatus.Unapproved || task.CreatedBy == approverId)
                return false;

            task.Status = TaskStatus.Approved;
            Save();
            return true;
        }

        public bool DeclineTask(int id, ulong declinerId)
        {
            var task = GetById(id);
            if (task is null || task.Status != TaskStatus.Unapproved || task.CreatedBy == declinerId)
                return false;

            task.Status = TaskStatus.Expired;
            task.ExpiredAt = DateTime.UtcNow;
            Save();
            return true;
        }

        public bool ClaimTask(int id, ulong userId)
        {
            var task = GetById(id);
            if (task is null || task.Status != TaskStatus.Approved || task.CreatedBy == userId || task.IsCommunityTask)
                return false;

            task.ClaimedBy = userId;
            task.Status = TaskStatus.Claimed;
            Save();
            return true;
        }

        public bool CompleteTask(int id, ulong userId)
        {
            var task = GetById(id);
            if (task is null || task.Status != TaskStatus.Claimed || task.ClaimedBy != userId)
                return false;

            task.Status = TaskStatus.Pending;
            task.CompletedAt = DateTime.UtcNow;
            Save();
            return true;
        }

        public bool CloseTask(int id, ulong closerId)
        {
            var task = GetById(id);
            if (task is null || task.Status != TaskStatus.Pending || task.CreatedBy != closerId)
                return false;

            task.Status = TaskStatus.Closed;
            task.ClosedAt = DateTime.UtcNow;
            Save();
            return true;
        }

        public bool ExpireTask(int id, ulong expirerId)
        {
            var task = GetById(id);
            if (task is null || task.Status == TaskStatus.Closed)
                return false;

            task.Status = TaskStatus.Expired;
            task.ExpiredAt = DateTime.UtcNow;
            Save();
            return true;
        }

        public bool DeleteTask(int id, ulong requesterId)
        {
            var task = GetById(id);
            if (task is null)
                return false;
            if (task.CreatedBy != requesterId && task.Status != TaskStatus.Unapproved)
                return false;

            _tasks.Remove(task);
            Save();
            return true;
        }

        public void ClearAllTasks()
        {
            _tasks.Clear();
            Save();
        }

        public int GenerateNewId() => _tasks.Any() ? _tasks.Max(t => t.Id) + 1 : 1;

        public bool RecordContribution(int taskId, ulong userId, int amount)
        {
            var task = GetById(taskId);
            if (task is null || !task.IsCommunityTask || amount <= 0)
                return false;

            if (task.Contributions.ContainsKey(userId))
                task.Contributions[userId] += amount;
            else
                task.Contributions[userId] = amount;

            Save();
            return true;
        }

        public List<(ulong UserId, int Contributed)> GetTopContributors(int taskId, int top)
        {
            var task = GetById(taskId);
            if (task is null || !task.IsCommunityTask)
                return new List<(ulong, int)>();

            return task.Contributions
                       .OrderByDescending(kv => kv.Value)
                       .Take(top)
                       .Select(kv => (kv.Key, kv.Value))
                       .ToList();
        }
    }
}
