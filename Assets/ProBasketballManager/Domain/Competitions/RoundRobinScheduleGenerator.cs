using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Competitions
{
    public static class RoundRobinScheduleGenerator
    {
        public static IReadOnlyList<Fixture> Generate(League league, CompetitionRules rules = null)
        {
            return Generate(league, rules, CompetitionCalendar.DefaultFirstSeasonYear);
        }

        public static IReadOnlyList<Fixture> Generate(League league, CompetitionRules rules, int seasonStartYear)
        {
            if (league.Teams.Count < 2)
            {
                throw new ArgumentException("At least two teams are required to generate a schedule.", nameof(league));
            }

            var rotation = league.Teams.ToList();

            if (rotation.Count % 2 != 0)
            {
                rotation.Add(null);
            }

            var firstHalfFixtures = new List<Fixture>();
            var fixtureId = 1;

            var teamSlots = rotation.Count;
            var roundsPerHalf = teamSlots - 1;
            var matchesPerRound = teamSlots / 2;

            for (var roundIndex = 0; roundIndex < roundsPerHalf; roundIndex++)
            {
                var roundNumber = roundIndex + 1;

                for (var matchIndex = 0; matchIndex < matchesPerRound; matchIndex++)
                {
                    var firstTeam = rotation[matchIndex];
                    var secondTeam = rotation[teamSlots - 1 - matchIndex];

                    if (firstTeam == null || secondTeam == null)
                    {
                        continue;
                    }

                    var reverseHomeAndAway = (roundIndex + matchIndex) % 2 != 0;

                    var homeTeam = reverseHomeAndAway ? secondTeam : firstTeam;
                    var awayTeam = reverseHomeAndAway ? firstTeam : secondTeam;

                    firstHalfFixtures.Add(new Fixture(fixtureId, roundNumber, homeTeam, awayTeam, league.Calendar.GetRoundDate(seasonStartYear, roundNumber)));

                    fixtureId++;
                }

                RotateTeams(rotation);
            }

            var fixtures = new List<Fixture>(firstHalfFixtures);

            var passes = (rules ?? CompetitionRules.Fiba).RoundRobinPasses;

            for (var pass = 1; pass < passes; pass++)
            {
                var swapHomeAndAway = pass % 2 == 1;

                foreach (var firstHalfFixture in firstHalfFixtures)
                {
                    var laterRoundNumber = firstHalfFixture.RoundNumber + (roundsPerHalf * pass);

                    fixtures.Add(new Fixture(
                        fixtureId,
                        laterRoundNumber,
                        swapHomeAndAway ? firstHalfFixture.AwayTeam : firstHalfFixture.HomeTeam,
                        swapHomeAndAway ? firstHalfFixture.HomeTeam : firstHalfFixture.AwayTeam,
                        league.Calendar.GetRoundDate(seasonStartYear, laterRoundNumber)
                    ));

                    fixtureId++;
                }
            }

            return fixtures;
        }

        private static void RotateTeams(List<Team> teams)
        {
            if (teams.Count <= 2)
            {
                return;
            }

            var lastTeam = teams[teams.Count - 1];

            teams.RemoveAt(teams.Count - 1);
            teams.Insert(1, lastTeam);
        }
    }
}
