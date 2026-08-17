using System;
using System.Linq;
using NUnit.Framework;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Demo;
using ProBasketballManager.Domain.Matches;

namespace ProBasketballManager.Domain.Tests
{
    /// <summary>
    /// Checks the structure of a generated season.
    ///
    /// These are pure logic tests with no randomness at all, so they run instantly
    /// and their results are exact. A double round robin has properties that are
    /// true by definition: everyone plays everyone twice, once at home and once
    /// away, and nobody plays twice in the same round.
    /// </summary>
    [TestFixture]
    public sealed class SeasonScheduleTests
    {
        [Test]
        public void EveryTeam_PlaysEveryOpponentHomeAndAway()
        {
            var season = DemoSeasonFactory.Create();
            var teams = season.League.Teams;

            foreach (var team in teams)
            {
                foreach (var opponent in teams)
                {
                    if (team.Id == opponent.Id)
                    {
                        continue;
                    }

                    var atHome = season.Fixtures.Count(fixture =>
                        fixture.HomeTeam.Id == team.Id && fixture.AwayTeam.Id == opponent.Id);

                    Assert.That(atHome, Is.EqualTo(1),
                        $"{team.Name} should host {opponent.Name} exactly once in a double round robin.");
                }
            }
        }

        [Test]
        public void EveryTeam_HasABalancedHomeAndAwayCount()
        {
            var season = DemoSeasonFactory.Create();
            var teams = season.League.Teams;

            foreach (var team in teams)
            {
                var home = season.Fixtures.Count(fixture => fixture.HomeTeam.Id == team.Id);
                var away = season.Fixtures.Count(fixture => fixture.AwayTeam.Id == team.Id);

                Assert.That(home, Is.EqualTo(away),
                    $"{team.Name} has an unbalanced schedule, which hands out an unearned home court advantage.");

                Assert.That(home + away, Is.EqualTo((teams.Count - 1) * 2),
                    $"{team.Name} should play every opponent twice.");
            }
        }

        [Test]
        public void NoTeam_PlaysTwiceInTheSameRound()
        {
            var season = DemoSeasonFactory.Create();

            foreach (var round in season.Fixtures.GroupBy(fixture => fixture.RoundNumber))
            {
                var appearances = round
                    .SelectMany(fixture => new[] { fixture.HomeTeam.Id, fixture.AwayTeam.Id })
                    .ToList();

                Assert.That(appearances.Distinct().Count(), Is.EqualTo(appearances.Count),
                    $"A team appears more than once in round {round.Key}.");
            }
        }

        [Test]
        public void FixtureIds_AreUnique()
        {
            var season = DemoSeasonFactory.Create();
            var ids = season.Fixtures.Select(fixture => fixture.Id).ToList();

            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count),
                "Season.RecordResult looks fixtures up by ID, so duplicates would write results to the wrong game.");
        }

        [Test]
        public void RoundCount_MatchesADoubleRoundRobin()
        {
            var season = DemoSeasonFactory.Create();
            var teamCount = season.League.Teams.Count;

            Assert.That(season.TotalRounds, Is.EqualTo((teamCount - 1) * 2));
            Assert.That(season.Fixtures.Count, Is.EqualTo(teamCount * (teamCount - 1)));
        }

        [Test]
        public void ANewSeason_StartsUnplayed()
        {
            var season = DemoSeasonFactory.Create();

            Assert.That(season.IsComplete, Is.False);
            Assert.That(season.Fixtures.All(fixture => !fixture.IsPlayed), Is.True);
            Assert.That(season.CurrentRoundNumber, Is.EqualTo(1));
        }

        [Test]
        public void PlayingEveryFixture_CompletesTheSeasonAndFillsTheTable()
        {
            var season = DemoSeasonFactory.Create();
            var simulator = new MatchSimulator(new XorShiftRandom(20260817u));

            foreach (var fixture in season.Fixtures.ToList())
            {
                season.RecordResult(fixture.Id, simulator.Simulate(fixture.HomeTeam, fixture.AwayTeam));
            }

            Assert.That(season.IsComplete, Is.True);

            var standings = season.GetStandings();
            var expectedGames = (season.League.Teams.Count - 1) * 2;

            Assert.That(standings.Count, Is.EqualTo(season.League.Teams.Count));

            foreach (var standing in standings)
            {
                Assert.That(standing.Played, Is.EqualTo(expectedGames));
                Assert.That(standing.Wins + standing.Losses, Is.EqualTo(expectedGames),
                    "Every game must be either a win or a loss, because basketball has no draws.");
            }

            Assert.That(standings.Sum(standing => standing.Wins),
                Is.EqualTo(standings.Sum(standing => standing.Losses)),
                "Across the whole league every win is somebody else's loss.");

            Assert.That(standings.Sum(standing => standing.PointsFor),
                Is.EqualTo(standings.Sum(standing => standing.PointsAgainst)),
                "Points scored league wide must equal points conceded league wide.");
        }

        [Test]
        public void Standings_AreOrderedByRecord()
        {
            var season = DemoSeasonFactory.Create();
            var simulator = new MatchSimulator(new XorShiftRandom(555u));

            foreach (var fixture in season.Fixtures.ToList())
            {
                season.RecordResult(fixture.Id, simulator.Simulate(fixture.HomeTeam, fixture.AwayTeam));
            }

            var standings = season.GetStandings();

            for (var index = 1; index < standings.Count; index++)
            {
                Assert.That(standings[index - 1].Wins, Is.GreaterThanOrEqualTo(standings[index].Wins),
                    "A team placed above another must not have fewer wins.");
            }
        }

        [Test]
        public void RecordingAResultTwice_IsRejected()
        {
            var season = DemoSeasonFactory.Create();
            var simulator = new MatchSimulator(new XorShiftRandom(88u));
            var fixture = season.Fixtures.First();

            season.RecordResult(fixture.Id, simulator.Simulate(fixture.HomeTeam, fixture.AwayTeam));

            Assert.Throws<InvalidOperationException>(
                () => season.RecordResult(fixture.Id, simulator.Simulate(fixture.HomeTeam, fixture.AwayTeam)),
                "Replaying a fixture would corrupt the league table.");
        }

        [Test]
        public void RecordingAResultForTheWrongFixture_IsRejected()
        {
            var season = DemoSeasonFactory.Create();
            var simulator = new MatchSimulator(new XorShiftRandom(99u));

            var target = season.Fixtures.First();
            var mismatched = season.Fixtures.First(fixture =>
                fixture.HomeTeam.Id != target.HomeTeam.Id || fixture.AwayTeam.Id != target.AwayTeam.Id);

            var result = simulator.Simulate(mismatched.HomeTeam, mismatched.AwayTeam);

            Assert.Throws<ArgumentException>(() => season.RecordResult(target.Id, result),
                "A result must belong to the fixture it is recorded against.");
        }
    }
}