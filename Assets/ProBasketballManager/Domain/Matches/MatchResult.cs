using System.Collections.Generic;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Matches
{
    public sealed class MatchResult
    {
        public Team HomeTeam { get; }

        public Team AwayTeam { get; }

        public TeamRotation HomeRotation { get; }

        public TeamRotation AwayRotation { get; }

        public IReadOnlyList<int> HomePeriodScores { get; }

        public IReadOnlyList<int> AwayPeriodScores { get; }

        public IReadOnlyList<MatchEvent> Events { get; }

        public IReadOnlyList<PlayerBoxScore> HomePlayerStats { get; }

        public IReadOnlyList<PlayerBoxScore> AwayPlayerStats { get; }

        public int HomeScore { get; }

        public int AwayScore { get; }

        public MatchResult(Team homeTeam, Team awayTeam, TeamRotation homeRotation, TeamRotation awayRotation, IReadOnlyList<int> homePeriodScores, IReadOnlyList<int> awayPeriodScores, IReadOnlyList<MatchEvent> events, IReadOnlyList<PlayerBoxScore> homePlayerStats, IReadOnlyList<PlayerBoxScore> awayPlayerStats)
        {
            HomeTeam = homeTeam;
            AwayTeam = awayTeam;
            HomeRotation = homeRotation;
            AwayRotation = awayRotation;
            HomePeriodScores = homePeriodScores;
            AwayPeriodScores = awayPeriodScores;
            Events = events;
            HomePlayerStats = homePlayerStats;
            AwayPlayerStats = awayPlayerStats;

            HomeScore = CalculateTotal(homePeriodScores);
            AwayScore = CalculateTotal(awayPeriodScores);
        }

        private static int CalculateTotal(IReadOnlyList<int> scores)
        {
            var total = 0;

            for (var i = 0; i < scores.Count; i++)
            {
                total += scores[i];
            }

            return total;
        }
    }
}