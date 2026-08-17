using System;
using System.Linq;
using NUnit.Framework;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Persistence;
using ProBasketballManager.Presentation.State;

namespace ProBasketballManager.Domain.Tests
{
    [TestFixture]
    public sealed class SaveGameRoundTripTests
    {
        private GameSessionSnapshot _before;
        private GameSessionSnapshot _after;

        [OneTimeSetUp]
        public void SaveAndReload()
        {
            var session = GameSession.CreateDemo();

            // Play a few rounds so there are results, rotations, box scores and
            // play by play events to persist.
            for (var round = 0; round < 3; round++)
            {
                session.SimulateCurrentFixture();
                session.CompleteCurrentRound();
            }

            _before = session.CreateSnapshot();
            _after = SaveGameMapper.FromDto(SaveGameMapper.ToDto(_before, "test"));
        }

        [Test]
        public void TheLeagueSurvives()
        {
            Assert.That(_after.Season.League.Teams.Count, Is.EqualTo(_before.Season.League.Teams.Count));
            Assert.That(_after.UserTeam.Id, Is.EqualTo(_before.UserTeam.Id));

            foreach (var team in _before.Season.League.Teams)
            {
                var restored = _after.Season.League.Teams.Single(entry => entry.Id == team.Id);

                Assert.That(restored.Name, Is.EqualTo(team.Name));
                Assert.That(restored.Players.Count, Is.EqualTo(team.Players.Count));
            }
        }

        [Test]
        public void PlayerAttributesSurvive()
        {
            var original = _before.UserTeam.Players[0];
            var restored = _after.UserTeam.Players.Single(player => player.Id == original.Id);

            Assert.That(restored.FullName, Is.EqualTo(original.FullName));
            Assert.That(restored.Position, Is.EqualTo(original.Position));
            Assert.That(restored.Attributes.Finishing, Is.EqualTo(original.Attributes.Finishing));
            Assert.That(restored.Attributes.Stamina, Is.EqualTo(original.Attributes.Stamina));
            Assert.That(restored.Attributes.BasketballIq, Is.EqualTo(original.Attributes.BasketballIq));
        }

        [Test]
        public void TheLeagueTableIsUnchanged()
        {
            var before = _before.Season.GetStandings();
            var after = _after.Season.GetStandings();

            Assert.That(after.Count, Is.EqualTo(before.Count));

            for (var position = 0; position < before.Count; position++)
            {
                Assert.That(after[position].Team.Id, Is.EqualTo(before[position].Team.Id),
                    "Teams must come back in the same order.");

                Assert.That(after[position].Wins, Is.EqualTo(before[position].Wins));
                Assert.That(after[position].Losses, Is.EqualTo(before[position].Losses));
                Assert.That(after[position].PointsFor, Is.EqualTo(before[position].PointsFor));
                Assert.That(after[position].PointsAgainst, Is.EqualTo(before[position].PointsAgainst));
            }
        }

        [Test]
        public void RestoredPlayersAreTheSameObjectsAsTheirTeamsHold()
        {
            var team = _after.Season.League.Teams[0];

            var fixture = _after.Season.Fixtures.First(entry =>
                entry.IsPlayed && entry.HomeTeam.Id == team.Id);

            Assert.That(ReferenceEquals(fixture.HomeTeam, team), Is.True,
                "A fixture must hold the league's Team instance, not a copy of it.");

            Assert.That(ReferenceEquals(fixture.Result.HomeTeam, team), Is.True,
                "A result must hold the league's Team instance too.");

            foreach (var box in fixture.Result.HomePlayerStats)
            {
                var rosterPlayer = team.Players.Single(player => player.Id == box.Player.Id);

                Assert.That(ReferenceEquals(box.Player, rosterPlayer), Is.True,
                    "A box score must reference the roster Player, not an equal copy. If this fails, " +
                    "the save has duplicated the player and statistics will diverge from the squad.");
            }

            foreach (var assignment in fixture.Result.HomeRotation.Assignments)
            {
                var rosterPlayer = team.Players.Single(player => player.Id == assignment.Player.Id);

                Assert.That(ReferenceEquals(assignment.Player, rosterPlayer), Is.True);
            }
        }

        [Test]
        public void PlayByPlayEventsSurviveInFull()
        {
            var before = _before.Season.Fixtures.First(fixture => fixture.IsPlayed).Result;
            var after = _after.Season.Fixtures.First(fixture => fixture.IsPlayed).Result;

            Assert.That(after.Events.Count, Is.EqualTo(before.Events.Count));
            Assert.That(after.Events.Count, Is.GreaterThan(0), "The sample should contain events.");

            for (var index = 0; index < before.Events.Count; index++)
            {
                var expected = before.Events[index];
                var actual = after.Events[index];

                Assert.That(actual.Type, Is.EqualTo(expected.Type));
                Assert.That(actual.PeriodNumber, Is.EqualTo(expected.PeriodNumber));
                Assert.That(actual.SecondsRemaining, Is.EqualTo(expected.SecondsRemaining));
                Assert.That(actual.Player.Id, Is.EqualTo(expected.Player.Id));
                Assert.That(actual.HomeScore, Is.EqualTo(expected.HomeScore));
                Assert.That(actual.AwayScore, Is.EqualTo(expected.AwayScore));

                Assert.That(actual.SecondaryPlayer?.Id ?? -1, Is.EqualTo(expected.SecondaryPlayer?.Id ?? -1),
                    "An event with no second player must come back with none, not with a wrong one.");

                Assert.That(actual.OffensiveAction, Is.EqualTo(expected.OffensiveAction));
                Assert.That(actual.ShotZone, Is.EqualTo(expected.ShotZone));
            }
        }

        [Test]
        public void BoxScoresSurviveIncludingFatigue()
        {
            var before = _before.Season.Fixtures.First(fixture => fixture.IsPlayed).Result;
            var after = _after.Season.Fixtures.First(fixture => fixture.IsPlayed).Result;

            Assert.That(after.HomeScore, Is.EqualTo(before.HomeScore));
            Assert.That(after.AwayScore, Is.EqualTo(before.AwayScore));
            Assert.That(after.HomePeriodScores, Is.EqualTo(before.HomePeriodScores).AsCollection);

            for (var index = 0; index < before.HomePlayerStats.Count; index++)
            {
                var expected = before.HomePlayerStats[index];
                var actual = after.HomePlayerStats[index];

                Assert.That(actual.Player.Id, Is.EqualTo(expected.Player.Id));
                Assert.That(actual.IsStarter, Is.EqualTo(expected.IsStarter));
                Assert.That(actual.Points, Is.EqualTo(expected.Points));
                Assert.That(actual.PersonalFouls, Is.EqualTo(expected.PersonalFouls));
                Assert.That(actual.SecondsPlayed, Is.EqualTo(expected.SecondsPlayed).Within(1e-9));
                Assert.That(actual.PeakFatigue, Is.EqualTo(expected.PeakFatigue).Within(1e-9));
            }
        }

        [Test]
        public void TacticsAndRotationSurvive()
        {
            Assert.That(_after.UserTactics.Pace, Is.EqualTo(_before.UserTactics.Pace));
            Assert.That(_after.UserTactics.RimWeight, Is.EqualTo(_before.UserTactics.RimWeight));
            Assert.That(_after.UserTactics.ProtectPaint, Is.EqualTo(_before.UserTactics.ProtectPaint));

            Assert.That(_after.UserRotation.PrimaryBallHandler.Id,
                Is.EqualTo(_before.UserRotation.PrimaryBallHandler.Id));

            foreach (var assignment in _before.UserRotation.Assignments)
            {
                var restored = _after.UserRotation.GetAssignment(assignment.Player.Id);

                Assert.That(restored.TargetMinutes, Is.EqualTo(assignment.TargetMinutes));
                Assert.That(restored.RotationOrder, Is.EqualTo(assignment.RotationOrder));
            }
        }

        [Test]
        public void AReloadedGameContinuesTheSameSequence()
        {
            var original = GameSession.Restore(_before);
            var reloaded = GameSession.Restore(_after);

            Assert.That(reloaded.CurrentFixture.Id, Is.EqualTo(original.CurrentFixture.Id));

            var originalResult = original.SimulateCurrentFixture();
            var reloadedResult = reloaded.SimulateCurrentFixture();

            Assert.That(reloadedResult.HomeScore, Is.EqualTo(originalResult.HomeScore));
            Assert.That(reloadedResult.AwayScore, Is.EqualTo(originalResult.AwayScore));
            Assert.That(reloadedResult.Events.Count, Is.EqualTo(originalResult.Events.Count));
        }

        [Test]
        public void AnUnplayedFixtureStaysUnplayed()
        {
            var unplayed = _after.Season.Fixtures.Where(fixture => !fixture.IsPlayed).ToList();

            Assert.That(unplayed, Is.Not.Empty, "Three rounds of six should leave fixtures outstanding.");

            foreach (var fixture in unplayed)
            {
                Assert.That(fixture.Result, Is.Null);
            }
        }
    }

    [TestFixture]
    public sealed class SaveGameValidationTests
    {
        private static SaveGameDto CreateValidDto()
        {
            return SaveGameMapper.ToDto(GameSession.CreateDemo().CreateSnapshot(), "test");
        }

        [Test]
        public void AFileFromAnotherSchemaVersionIsRejected()
        {
            var dto = CreateValidDto();
            dto.SchemaVersion = SaveGameMapper.CurrentSchemaVersion + 1;

            var exception = Assert.Throws<SaveGameException>(() => SaveGameMapper.FromDto(dto));

            Assert.That(exception.Message.Contains("version"), Is.True,
                "The message should tell the player the save is from a different build.");
        }

        [Test]
        public void AnEmptyFileIsRejected()
        {
            Assert.Throws<SaveGameException>(() => SaveGameMapper.FromDto(null));
        }

        [Test]
        public void AnUnknownTeamReferenceIsRejected()
        {
            var dto = CreateValidDto();
            dto.UserTeamId = 999999;

            Assert.Throws<SaveGameException>(() => SaveGameMapper.FromDto(dto),
                "A save pointing at a team that is not in the league must fail loudly, not load a null team.");
        }

        [Test]
        public void AnUnrecognisedEnumValueIsRejected()
        {
            var dto = CreateValidDto();
            dto.League.Teams[0].Players[0].Position = "GoalKeeper";

            Assert.Throws<SaveGameException>(() => SaveGameMapper.FromDto(dto));
        }

        [Test]
        public void AMissingSeasonIsRejected()
        {
            var dto = CreateValidDto();
            dto.Season = null;

            Assert.Throws<SaveGameException>(() => SaveGameMapper.FromDto(dto));
        }
    }
}