using System;
using System.Linq;
using NUnit.Framework;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Persistence;
using ProBasketballManager.Presentation.State;

namespace ProBasketballManager.Domain.Tests
{
    [TestFixture]
    public sealed class SeasonRolloverTests
    {
        private static GameSession PlayASeason()
        {
            var session = GameSession.CreateDemo();

            PlayToTheEnd(session);

            return session;
        }

        private static void PlayToTheEnd(GameSession session)
        {
            var guard = 0;

            while (!session.Season.IsComplete && guard++ < 200)
            {
                session.SimulateCurrentFixture();
                session.CompleteCurrentRound();
            }
        }

        [Test]
        public void ASeasonInProgressCannotBeAdvanced()
        {
            var session = GameSession.CreateDemo();

            Assert.That(session.CanAdvanceSeason, Is.False);

            Assert.Throws<InvalidOperationException>(() => session.AdvanceToNextSeason(),
                "Advancing early would archive a half played table, so it fails loudly rather than quietly.");
        }

        [Test]
        public void AFinishedSeasonCanBeAdvanced()
        {
            var session = PlayASeason();

            Assert.That(session.Season.IsComplete, Is.True);
            Assert.That(session.CanAdvanceSeason, Is.True);
        }

        [Test]
        public void TheNextSeasonStartsFresh()
        {
            var session = PlayASeason();

            var fixtureCount = session.Season.Fixtures.Count;

            session.AdvanceToNextSeason();

            Assert.That(session.Season.IsComplete, Is.False);
            Assert.That(session.Season.Fixtures.Count, Is.EqualTo(fixtureCount));

            Assert.That(session.Season.Fixtures.All(fixture => !fixture.IsPlayed), Is.True,
                "A new season must generate its own fixtures, not carry last season's results forward.");

            Assert.That(session.CurrentFixture.RoundNumber, Is.EqualTo(1));
            Assert.That(session.CurrentMatchResult, Is.Null);
        }

        [Test]
        public void TheSeasonNameAdvancesByAYear()
        {
            Assert.That(Career.GetNextSeasonName("2026 / 27"), Is.EqualTo("2027 / 28"));
            Assert.That(Career.GetNextSeasonName("2029 / 30"), Is.EqualTo("2030 / 31"));

            Assert.That(Career.GetNextSeasonName("1999 / 00"), Is.EqualTo("2000 / 01"),
                "The two digit end year has to wrap rather than reaching 100.");
        }

        [Test]
        public void AnOddlyNamedSeasonStillAdvances()
        {
            Assert.That(Career.GetNextSeasonName("Preseason"), Is.Not.EqualTo("Preseason"));
        }

        [Test]
        public void TheFinishedSeasonIsArchivedIntact()
        {
            var session = PlayASeason();

            var expected = session.Season.GetStandings()
                .Select(standing => (standing.Team.Id, standing.Wins, standing.Losses, standing.PointsFor))
                .ToList();

            var archived = session.AdvanceToNextSeason();

            var actual = archived.FinalStandings
                .Select(standing => (standing.Team.Id, standing.Wins, standing.Losses, standing.PointsFor))
                .ToList();

            Assert.That(actual, Is.EqualTo(expected).AsCollection,
                "The archived table must match the table that was on screen when the season ended.");

            Assert.That(archived.Champion.Position, Is.EqualTo(1));
        }

        [Test]
        public void EveryPlayerInTheLeagueIsArchived()
        {
            var session = PlayASeason();

            var expectedPlayers = session.Career.League.Teams.Sum(team => team.Players.Count);

            var archived = session.AdvanceToNextSeason();

            Assert.That(archived.PlayerStatistics.Count, Is.EqualTo(expectedPlayers));
            Assert.That(archived.GetLeadingScorer(), Is.Not.Null);

            Assert.That(archived.GetLeadingScorer().PointsPerGame, Is.GreaterThan(0.0));
        }

        [Test]
        public void HistoryAccumulatesAcrossSeasons()
        {
            var session = GameSession.CreateDemo();

            for (var season = 0; season < 3; season++)
            {
                PlayToTheEnd(session);
                session.AdvanceToNextSeason();
            }

            Assert.That(session.Career.SeasonsCompleted, Is.EqualTo(3));

            var names = session.Career.CompletedSeasons.Select(entry => entry.Name).ToList();

            Assert.That(names.Distinct().Count(), Is.EqualTo(3),
                "Each archived season needs its own name, or history collapses into one entry.");

            var titles = session.Career.League.Teams.Sum(team => session.Career.GetTitleCount(team.Id));

            Assert.That(titles, Is.EqualTo(3), "Three completed seasons means exactly three champions.");
        }

        [Test]
        public void RostersCarryOverUnchanged()
        {
            var session = PlayASeason();

            var before = session.UserTeam.Players.Select(player => player.Id).ToList();

            session.AdvanceToNextSeason();

            var after = session.UserTeam.Players.Select(player => player.Id).ToList();

            Assert.That(after, Is.EqualTo(before).AsCollection,
                "Rollover does not change squads yet. When ageing and development arrive this test should " +
                "change rather than be deleted.");
        }

        [Test]
        public void TheRotationIsRebuiltForTheNewSeason()
        {
            var session = PlayASeason();

            session.AdvanceToNextSeason();

            var totalMinutes = session.UserRotation.Assignments.Sum(assignment => assignment.TargetMinutes);

            Assert.That(totalMinutes, Is.EqualTo(200),
                "A new season starts from a valid default rotation.");
        }
    }

    [TestFixture]
    public sealed class CareerPersistenceTests
    {
        private static GameSession PlaySeasons(int count)
        {
            var session = GameSession.CreateDemo();

            for (var season = 0; season < count; season++)
            {
                var guard = 0;

                while (!session.Season.IsComplete && guard++ < 200)
                {
                    session.SimulateCurrentFixture();
                    session.CompleteCurrentRound();
                }

                session.AdvanceToNextSeason();
            }

            return session;
        }

        [Test]
        public void ACareerSurvivesASaveAndLoad()
        {
            var session = PlaySeasons(2);

            var before = session.CreateSnapshot();
            var after = SaveGameMapper.FromDto(SaveGameMapper.ToDto(before, "career"));

            Assert.That(after.Career.SeasonsCompleted, Is.EqualTo(2));
            Assert.That(after.Career.CurrentSeason.Name, Is.EqualTo(before.Career.CurrentSeason.Name));

            var expected = before.Career.CompletedSeasons[0];
            var actual = after.Career.CompletedSeasons[0];

            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.Champion.Team.Id, Is.EqualTo(expected.Champion.Team.Id));
            Assert.That(actual.GetLeadingScorer().Player.Id, Is.EqualTo(expected.GetLeadingScorer().Player.Id));
            Assert.That(actual.PlayerStatistics.Count, Is.EqualTo(expected.PlayerStatistics.Count));
        }

        [Test]
        public void ArchivedRecordsPointAtTheLiveRosterObjects()
        {
            var session = PlaySeasons(1);

            var after = SaveGameMapper.FromDto(SaveGameMapper.ToDto(session.CreateSnapshot(), "career"));

            var archived = after.Career.CompletedSeasons[0];

            var rosterTeam = after.Career.League.Teams
                .Single(team => team.Id == archived.FinalStandings[0].Team.Id);

            Assert.That(ReferenceEquals(archived.FinalStandings[0].Team, rosterTeam), Is.True,
                "An archived standing must reference the league's Team, not a copy frozen at season end.");

            var archivedPlayer = archived.PlayerStatistics[0].Player;

            var rosterPlayer = after.Career.League.Teams
                .SelectMany(team => team.Players)
                .Single(player => player.Id == archivedPlayer.Id);

            Assert.That(ReferenceEquals(archivedPlayer, rosterPlayer), Is.True);
        }

        [Test]
        public void SavesWrittenBeforeCareersExistedStillLoad()
        {
            var dto = SaveGameMapper.ToDto(GameSession.CreateDemo().CreateSnapshot(), "old");

            dto.SchemaVersion = 1;
            dto.CompletedSeasons = null;

            var loaded = SaveGameMapper.FromDto(dto);

            Assert.That(loaded.Career.SeasonsCompleted, Is.EqualTo(0));
            Assert.That(loaded.Career.CurrentSeason.Fixtures.Count, Is.GreaterThan(0));
        }

        [Test]
        public void SavesFromAFutureVersionAreStillRejected()
        {
            var dto = SaveGameMapper.ToDto(GameSession.CreateDemo().CreateSnapshot(), "future");

            dto.SchemaVersion = SaveGameMapper.CurrentSchemaVersion + 1;

            Assert.Throws<SaveGameException>(() => SaveGameMapper.FromDto(dto),
                "Reading a newer format would silently drop whatever it added.");
        }
    }
}