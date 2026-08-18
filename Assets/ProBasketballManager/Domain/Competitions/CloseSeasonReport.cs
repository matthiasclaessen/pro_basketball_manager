using System.Collections.Generic;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Competitions
{
    public sealed class CloseSeasonReport
    {
        public List<RetirementRecord> Retirements { get; } = new List<RetirementRecord>();

        public List<DevelopmentRecord> Improvements { get; } = new List<DevelopmentRecord>();

        public List<DevelopmentRecord> Declines { get; } = new List<DevelopmentRecord>();

        public int TotalRetirements => Retirements.Count;
    }

    public sealed class RetirementRecord
    {
        public Team Team { get; }

        public Player Retired { get; }

        public int RetiredAtAge { get; }

        public Player Replacement { get; }

        public RetirementRecord(Team team, Player retired, int retiredAtAge, Player replacement)
        {
            Team = team;
            Retired = retired;
            RetiredAtAge = retiredAtAge;
            Replacement = replacement;
        }
    }

    public sealed class DevelopmentRecord
    {
        public Team Team { get; }

        public Player Player { get; }

        public int OverallBefore { get; }

        public int OverallAfter { get; }

        public int Change => OverallAfter - OverallBefore;

        public DevelopmentRecord(Team team, Player player, int overallBefore, int overallAfter)
        {
            Team = team;
            Player = player;
            OverallBefore = overallBefore;
            OverallAfter = overallAfter;
        }
    }
}