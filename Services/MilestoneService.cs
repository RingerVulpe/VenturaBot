using VenturaBot.Data;

namespace VenturaBot.Services;

public static class MilestoneService
{
    private static readonly List<Milestone> _milestones = new()
    {
        new("create_5", "🎖️ You’ve created 5 tasks! Leadership potential detected.", m => m.TasksCreated >= 5),
        new("complete_3", "💼 You’ve completed 3 tasks! House Ventura salutes you.", m => m.TasksCompleted >= 3),
        new("complete_10", "🏅 10 task completions! Promotion eligibility increased.", m => m.TasksCompleted >= 10),
        new("create_10", "📦 10 tasks created! You’re building our future.", m => m.TasksCreated >= 10),
        new("claimed_5", "🛠️ You’ve claimed 5 tasks. You're a dependable operative.", m => m.TasksClaimed >= 5)
    };

    public static List<string> CheckMilestones(GuildMember member)
    {
        List<string> unlocked = new();

        foreach (var milestone in _milestones)
        {
            if (!member.MilestonesReached.Contains(milestone.Id) && milestone.Condition(member))
            {
                member.MilestonesReached.Add(milestone.Id);
                unlocked.Add(milestone.Message);
            }
        }

        return unlocked;
    }
}

public record Milestone(string Id, string Message, Func<GuildMember, bool> Condition);
