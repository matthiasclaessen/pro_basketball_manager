using ProBasketballManager.Domain.Players;

namespace ProBasketballManager.Domain.Statistics
{
    public sealed class PlayerSeasonStatistics
    {
        public Player Player { get; }

        public int GamesPlayed { get; }

        public int GamesStarted { get; }

        public double TotalMinutes { get; }

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

        public double MinutesPerGame => PerGame(TotalMinutes);

        public double PointsPerGame => PerGame(Points);

        public double ReboundsPerGame => PerGame(Rebounds);

        public double AssistsPerGame => PerGame(Assists);

        public double StealsPerGame => PerGame(Steals);

        public double PersonalFoulsPerGame => PerGame(PersonalFouls);

        public double TurnoversPerGame => PerGame(Turnovers);

        public double FieldGoalPercentage => Percentage(FieldGoalsMade, FieldGoalsAttempted);

        public double ThreePointPercentage => Percentage(ThreePointsMade, ThreePointsAttempted);

        public double FreeThrowPercentage => Percentage(FreeThrowsMade, FreeThrowsAttempted);

        public PlayerSeasonStatistics(Player player, int gamesPlayed, int gamesStarted, double totalMinutes, int points, int fieldGoalsMade, int fieldGoalsAttempted, int threePointsMade, int threePointsAttempted, int freeThrowsMade, int freeThrowsAttempted, int offensiveRebounds, int defensiveRebounds, int assists, int steals, int personalFouls, int turnovers)
        {
            Player = player;
            GamesPlayed = gamesPlayed;
            GamesStarted = gamesStarted;
            TotalMinutes = totalMinutes;
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

        private double PerGame(double total)
        {
            return GamesPlayed == 0 ? 0 : total / GamesPlayed;
        }

        private static double Percentage(int made, int attempted)
        {
            return attempted == 0 ? 0 : made / (double)attempted * 100.0;
        }
    }
}