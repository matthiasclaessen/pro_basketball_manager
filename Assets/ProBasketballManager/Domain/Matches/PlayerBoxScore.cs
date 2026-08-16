using ProBasketballManager.Domain.Players;

namespace ProBasketballManager.Domain.Matches
{
    public sealed class PlayerBoxScore
    {
        public Player Player { get; }

        public bool IsStarter { get; }

        public double SecondsPlayed { get; }

        public double MinutesPlayed => SecondsPlayed / 60.0;

        public int Points { get; }

        public int FieldGoalsMade { get; }

        public int FieldGoalsAttempted { get; }

        public int ThreePointsMade { get; }

        public int ThreePointsAttempted { get; }

        public int FreeThrowsMade { get; }

        public int FreeThrowsAttempted { get; }

        public int OffensiveRebounds { get; }

        public int DefensiveRebounds { get; }

        public int Rebounds => OffensiveRebounds + DefensiveRebounds;

        public int Assists { get; }

        public int Steals { get; }

        public int PersonalFouls { get; }

        public int Turnovers { get; }

        public PlayerBoxScore(Player player, bool isStarter, double secondsPlayed, int points, int fieldGoalsMade, int fieldGoalsAttempted, int threePointsMade, int threePointsAttempted, int freeThrowsMade, int freeThrowsAttempted, int offensiveRebounds, int defensiveRebounds, int assists, int steals, int personalFouls, int turnovers)
        {
            Player = player;
            IsStarter = isStarter;
            SecondsPlayed = secondsPlayed;
            Points = points;
            FieldGoalsMade = fieldGoalsMade;
            FieldGoalsAttempted = fieldGoalsAttempted;
            ThreePointsMade = threePointsMade;
            ThreePointsAttempted = threePointsAttempted;
            FreeThrowsMade = freeThrowsMade;
            FreeThrowsAttempted = freeThrowsAttempted;
            OffensiveRebounds = offensiveRebounds;
            DefensiveRebounds = defensiveRebounds;
            Assists = assists;
            Steals = steals;
            PersonalFouls = personalFouls;
            Turnovers = turnovers;
        }
    }
}