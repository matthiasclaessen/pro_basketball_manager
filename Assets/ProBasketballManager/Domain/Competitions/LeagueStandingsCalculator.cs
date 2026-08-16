using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Teams;
using static Codice.Client.Common.Connection.AskCredentialsToUser;

namespace ProBasketballManager.Domain.Competitions
{
    public static class LeagueStandingsCalculator
    {
        public static IReadOnlyList<LeagueStanding> Calculate(Season season)
        {
            var accumulators = season.League.Teams.ToDictionary(team => team.Id, team => new StandingAccumulator(team));

            foreach (var fixture in season.Fixtures.Where(fixture => fixture.IsPlayed))
            {
                var home = accumulators[fixture.HomeTeam.Id];
                var away = accumulators[fixture.AwayTeam.Id];

                home.Played++;
                away.Played++;

                home.PointsFor += fixture.Result.HomeScore;
                home.PointsAgainst += fixture.Result.AwayScore;

                away.PointsFor += fixture.Result.AwayScore;
                away.PointsAgainst += fixture.Result.HomeScore;

                if (fixture.Result.HomeScore > fixture.Result.AwayScore)
                {
                    home.Wins++;
                    away.Losses++;
                }
                else
                {
                    away.Wins++;
                    home.Losses++;
                }
            }

            var ordered = accumulators.Values
                .OrderByDescending(standing => standing.Wins)
                .ThenByDescending(standing => standing.PointDifference)
                .ThenByDescending(standing => standing.PointsFor)
                .ThenBy(standing => standing.Team.Name)
                .ToList();

            return ordered
                .Select((standing, index) => new LeagueStanding(
                    index + 1,
                    standing.Team,
                    standing.Played,
                    standing.Wins,
                    standing.Losses,
                    standing.PointsFor,
                    standing.PointsAgainst
                ))
                .ToList();
        }

        private sealed class StandingAccumulator
        {
            public Team Team { get; }

            public int Played { get; set; }

            public int Wins { get; set; }

            public int Losses { get; set; }

            public int PointsFor { get; set; }

            public int PointsAgainst { get; set; }

            public int PointDifference => PointsFor - PointsAgainst;

            public StandingAccumulator(Team team)
            {
                Team = team;
            }
        }
    }
}