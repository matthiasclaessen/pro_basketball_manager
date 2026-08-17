using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Statistics;

namespace ProBasketballManager.Domain.Competitions
{
    public sealed class CompletedSeason
    {
        public int Id { get; }

        public string Name { get; }

        public IReadOnlyList<LeagueStanding> FinalStandings { get; }

        public IReadOnlyList<PlayerSeasonStatistics> PlayerStatistics { get; }

        public LeagueStanding Champion => FinalStandings.Count == 0 ? null : FinalStandings[0];

        public CompletedSeason(
            int id,
            string name,
            IReadOnlyList<LeagueStanding> finalStandings,
            IReadOnlyList<PlayerSeasonStatistics> playerStatistics)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A completed season needs a name.", nameof(name));
            }

            Id = id;
            Name = name;
            FinalStandings = finalStandings ?? throw new ArgumentNullException(nameof(finalStandings));
            PlayerStatistics = playerStatistics ?? throw new ArgumentNullException(nameof(playerStatistics));
        }

        public LeagueStanding GetStandingFor(int teamId)
        {
            return FinalStandings.FirstOrDefault(standing => standing.Team.Id == teamId);
        }

        public PlayerSeasonStatistics GetLeadingScorer()
        {
            return PlayerStatistics
                .Where(statistics => statistics.GamesPlayed > 0)
                .OrderByDescending(statistics => statistics.PointsPerGame)
                .ThenByDescending(statistics => statistics.Points)
                .FirstOrDefault();
        }

        public PlayerSeasonStatistics GetStatisticsFor(int playerId)
        {
            return PlayerStatistics.FirstOrDefault(statistics => statistics.Player.Id == playerId);
        }
    }
}