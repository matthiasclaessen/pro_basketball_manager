using System;
using System.Linq;
using NUnit.Framework;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Demo;
using ProBasketballManager.Domain.Matches;

namespace ProBasketballManager.Domain.Tests
{
    [TestFixture]
    public sealed class MatchInvariantTests
    {
        private const int GameCount = 300;

        [Test]
        public void BoxScorePoints_AlwaysSumToTheFinalScore()
        {
            foreach (var result in SimulationTestHarness.SimulateLeagueBatch(GameCount))
            {
                Assert.That(result.HomePlayerStats.Sum(player => player.Points), Is.EqualTo(result.HomeScore),
                    "Home box score points must equal the home team's final score.");

                Assert.That(result.AwayPlayerStats.Sum(player => player.Points), Is.EqualTo(result.AwayScore),
                    "Away box score points must equal the away team's final score.");
            }
        }

        [Test]
        public void PeriodScores_AlwaysSumToTheFinalScore()
        {
            foreach (var result in SimulationTestHarness.SimulateLeagueBatch(GameCount))
            {
                Assert.That(result.HomePeriodScores.Sum(), Is.EqualTo(result.HomeScore));
                Assert.That(result.AwayPeriodScores.Sum(), Is.EqualTo(result.AwayScore));
            }
        }

        [Test]
        public void MadeShots_NeverExceedAttempts()
        {
            foreach (var result in SimulationTestHarness.SimulateLeagueBatch(GameCount))
            {
                var players = result.HomePlayerStats.Concat(result.AwayPlayerStats);

                foreach (var player in players)
                {
                    Assert.That(player.FieldGoalsMade, Is.LessThanOrEqualTo(player.FieldGoalsAttempted));
                    Assert.That(player.ThreePointsMade, Is.LessThanOrEqualTo(player.ThreePointsAttempted));
                    Assert.That(player.FreeThrowsMade, Is.LessThanOrEqualTo(player.FreeThrowsAttempted));

                    Assert.That(player.ThreePointsAttempted, Is.LessThanOrEqualTo(player.FieldGoalsAttempted),
                        "A three point attempt is also a field goal attempt, so it can never be the larger number.");

                    Assert.That(player.SecondsPlayed, Is.GreaterThanOrEqualTo(0.0));
                }
            }
        }

        [Test]
        public void RegulationGames_DistributeTheRulesetsPlayerMinutes()
        {
            foreach (var result in SimulationTestHarness.SimulateLeagueBatch(GameCount))
            {
                var wentToOvertime = CompetitionRules.Fiba.IsOvertimePeriod(result.HomePeriodScores.Count);

                if (wentToOvertime)
                {
                    continue;
                }

                var homeMinutes = result.HomePlayerStats.Sum(player => player.SecondsPlayed) / 60.0;
                var awayMinutes = result.AwayPlayerStats.Sum(player => player.SecondsPlayed) / 60.0;

                var expectedMinutes = CompetitionRules.Fiba.TotalPlayerMinutesPerGame;

                Assert.That(homeMinutes, Is.EqualTo(expectedMinutes).Within(0.01),
                    "Players on court for the whole game is exactly PlayersOnCourt x RegulationMinutes of " +
                    "player time. A different total means the substitution logic is dropping or double " +
                    "counting time on court.");

                Assert.That(awayMinutes, Is.EqualTo(expectedMinutes).Within(0.01));
            }
        }

        [Test]
        public void OvertimeGames_AwardMoreThanTwoHundredMinutes()
        {
            var overtimeGames = SimulationTestHarness.SimulateLeagueBatch(GameCount)
                .Where(result => CompetitionRules.Fiba.IsOvertimePeriod(result.HomePeriodScores.Count))
                .ToList();

            Assert.That(overtimeGames, Is.Not.Empty,
                "No overtime games appeared in the sample, so this invariant could not be checked. " +
                "If overtime has been removed, delete this test.");

            foreach (var result in overtimeGames)
            {
                var minutes = result.HomePlayerStats.Sum(player => player.SecondsPlayed) / 60.0;

                Assert.That(minutes, Is.GreaterThan(CompetitionRules.Fiba.TotalPlayerMinutesPerGame),
                    "An overtime game must award more than the regulation 200 player minutes.");
            }
        }

        [Test]
        public void Matches_NeverFinishTied()
        {
            foreach (var result in SimulationTestHarness.SimulateLeagueBatch(GameCount))
            {
                Assert.That(result.HomeScore, Is.Not.EqualTo(result.AwayScore),
                    "Basketball has no draws, and Fixture.Complete rejects a tied result. The simulator must " +
                    "keep playing overtime periods until the tie is broken.");
            }
        }

        [Test]
        public void EveryRosterPlayer_AppearsInTheBoxScore()
        {
            var teams = DemoLeagueFactory.Create().Teams;
            var result = new MatchSimulator(new XorShiftRandom(4242u)).Simulate(teams[0], teams[1]);

            Assert.That(result.HomePlayerStats.Count, Is.EqualTo(teams[0].Players.Count),
                "Every player on the roster needs a box score row, even one who did not play.");

            Assert.That(result.AwayPlayerStats.Count, Is.EqualTo(teams[1].Players.Count));

            var homeIds = result.HomePlayerStats.Select(player => player.Player.Id).ToList();

            Assert.That(homeIds.Distinct().Count(), Is.EqualTo(homeIds.Count),
                "A player must not appear twice in the same box score.");
        }

        [Test]
        public void StartingFive_IsFlaggedInTheBoxScore()
        {
            var teams = DemoLeagueFactory.Create().Teams;
            var result = new MatchSimulator(new XorShiftRandom(909u)).Simulate(teams[0], teams[1]);

            Assert.That(result.HomePlayerStats.Count(player => player.IsStarter), Is.EqualTo(CompetitionRules.Fiba.PlayersOnCourt),
                "The starting lineup is however many players the rules put on court.");

            Assert.That(result.AwayPlayerStats.Count(player => player.IsStarter), Is.EqualTo(CompetitionRules.Fiba.PlayersOnCourt));
        }

        [Test]
        public void MatchEvents_AreRecordedInSensiblePeriods()
        {
            var teams = DemoLeagueFactory.Create().Teams;
            var result = new MatchSimulator(new XorShiftRandom(31337u)).Simulate(teams[0], teams[1]);

            Assert.That(result.Events, Is.Not.Empty, "A completed match must produce play by play events.");

            var periodCount = Math.Max(result.HomePeriodScores.Count, 4);

            foreach (var matchEvent in result.Events)
            {
                Assert.That(matchEvent.PeriodNumber, Is.InRange(1, periodCount));
                Assert.That(matchEvent.SecondsRemaining, Is.GreaterThanOrEqualTo(0));
            }
        }

        [Test]
        public void SimulatingATeamAgainstItself_IsRejected()
        {
            var teams = DemoLeagueFactory.Create().Teams;
            var simulator = new MatchSimulator(new XorShiftRandom(1u));

            Assert.Throws<ArgumentException>(() => simulator.Simulate(teams[0], teams[0]),
                "A team cannot play itself, and the simulator should say so rather than produce nonsense.");
        }
    }
}