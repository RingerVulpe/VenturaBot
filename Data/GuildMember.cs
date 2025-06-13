// -----------------------------
// 1) Data model update
// File: GuildMember.cs (VenturaBot.Data)
// -----------------------------
namespace VenturaBot.Data
{
    public class GuildMember
    {
        public ulong UserId { get; set; }
        public string Username { get; set; } = "";
        public int XP { get; set; } = 0;
        public bool IsVerified { get; set; } = false;
        public int VenturansBalance { get; set; } = 0;
        public int Rank => Services.XPService.CalculateRank(XP);
        public int TasksCreated { get; set; } = 0;
        public int TasksClaimed { get; set; } = 0;
        public int TasksCompleted { get; set; } = 0;
        public HashSet<string> MilestonesReached { get; set; } = new();

        // — New fields —
        public string AvatarUrl { get; set; } = "";
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public void GainXP(int amount)
        {
            XP = Math.Max(0, XP + amount);
        }

        public void GainVenturans(int amount)
        {
            VenturansBalance = Math.Max(0, VenturansBalance + amount);
        }

        public bool SpendVenturans(int amount)
        {
            if (amount <= 0 || amount > VenturansBalance)
                return false;
            VenturansBalance -= amount;
            return true;
        }
    }
}
