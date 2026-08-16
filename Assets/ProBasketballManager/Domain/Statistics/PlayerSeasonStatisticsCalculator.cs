using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Statistics
{
    public static class PlayerSeasonStatisticsCalculator
    {
        public static IReadOnlyList<PlayerSeasonStatistics> Calculate(Season season, Team team)
        {
            if (season == null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            if (team == null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            if (!season.League.Teams.Any(leagueTeam => leagueTeam.Id == team.Id))
            {
                throw new ArgumentException("The team does not belong to this season's league.", nameof(team));
            }

            var builders = team.Players.ToDictionary(player => player.Id, player => new StatisticsBuilder(player));

            foreach (var fixture in season.Fixtures.Where(fixture => fixture.IsPlayed && ContainsTeam(fixture, team)))
            {
                var playerStats = fixture.HomeTeam.Id == team.Id ? fixture.Result.HomePlayerStats : fixture.Result.AwayPlayerStats;

                foreach (var boxScore in playerStats)
                {
                    if (builders.TryGetValue(boxScore.Player.Id, out var builder))
                    {
                        builder.Add(boxScore);
                    }
                }
            }

            return team.Players.Select(player => builders[player.Id].Build()).ToList();
        }

        private static bool ContainsTeam(Fixture fixture, Team team)
        {
            return fixture.HomeTeam.Id == team.Id || fixture.AwayTeam.Id == team.Id;
        }

        private sealed class StatisticsBuilder
        {
            private Player Player { get; }

            private int GamesPlayed { get; set; }

            private int GamesStarted { get; set; }

            private double TotalMinutes { get; set; }

            private int Points { get; set; }

            private int FieldGoalsMade { get; set; }

            private int FieldGoalsAttempted { get; set; }

            private int ThreePointsMade { get; set; }

            private int ThreePointsAttempted { get; set; }

            private int FreeThrowsMade { get; set; }

            private int FreeThrowsAttempted { get; set; }

            private int OffensiveRebounds { get; set; }

            private int DefensiveRebounds { get; set; }

            private int Assists { get; set; }

            private int Steals { get; set; }

            private int PersonalFouls { get; set; }

            private int Turnovers { get; set; }

            public StatisticsBuilder(Player player)
            {
                Player = player;
            }

            public void Add(PlayerBoxScore boxScore)
            {
                if (boxScore.MinutesPlayed <= 0)
                {
                    return;
                }

                GamesPlayed++;

                if (boxScore.IsStarter)
                {
                    GamesStarted++;
                }

                TotalMinutes += boxScore.MinutesPlayed;
                Points += boxScore.Points;
                FieldGoalsMade += boxScore.FieldGoalsMade;
                FieldGoalsAttempted += boxScore.FieldGoalsAttempted;
                ThreePointsMade += boxScore.ThreePointsMade;
                ThreePointsAttempted += boxScore.ThreePointsAttempted;
                FreeThrowsMade += boxScore.FreeThrowsMade;
                FreeThrowsAttempted += boxScore.FreeThrowsAttempted;
                OffensiveRebounds += boxScore.OffensiveRebounds;
                DefensiveRebounds += boxScore.DefensiveRebounds;
                Assists += boxScore.Assists;
                Steals += boxScore.Steals;
                PersonalFouls += boxScore.PersonalFouls;
                Turnovers += boxScore.Turnovers;
            }

            public PlayerSeasonStatistics Build()
            {
                return new PlayerSeasonStatistics(Player, GamesPlayed, GamesStarted, TotalMinutes, Points, FieldGoalsMade, FieldGoalsAttempted, ThreePointsMade, ThreePointsAttempted, FreeThrowsMade, FreeThrowsAttempted, OffensiveRebounds, DefensiveRebounds, Assists, Steals, PersonalFouls, Turnovers);
            }
        }
    }
}