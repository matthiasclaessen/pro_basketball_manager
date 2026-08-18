using System;
using System.Linq;
using NUnit.Framework;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Persistence;
using ProBasketballManager.Presentation.State;

namespace ProBasketballManager.Domain.Tests
{
    [TestFixture]
    public sealed class DevelopmentModelTests
    {
        [Test]
        public void YoungPlayersWithRoomToGrowImproveFastest()
        {
            var young = DevelopmentModel.GetGrowth(19, 6, AttributeCategory.Skill);
            var prime = DevelopmentModel.GetGrowth(25, 6, AttributeCategory.Skill);
            var old = DevelopmentModel.GetGrowth(31, 6, AttributeCategory.Skill);

            Assert.That(young, Is.GreaterThan(prime));
            Assert.That(prime, Is.GreaterThan(old));
            Assert.That(old, Is.EqualTo(0.0).Within(1e-9), "Improvement stops entirely with age.");
        }

        [Test]
        public void APlayerAtHisCeilingStopsImproving()
        {
            Assert.That(DevelopmentModel.GetGrowth(20, 0, AttributeCategory.Skill), Is.EqualTo(0.0).Within(1e-9),
                "Potential is a ceiling, so no headroom means no growth however young the player is.");
        }

        [Test]
        public void PhysicalAttributesDeclineBeforeSkills()
        {
            const int age = 30;

            var physical = DevelopmentModel.GetDecline(age, AttributeCategory.Physical);
            var skill = DevelopmentModel.GetDecline(age, AttributeCategory.Skill);

            Assert.That(physical, Is.GreaterThan(0.0), "Legs go first.");
            Assert.That(skill, Is.EqualTo(0.0).Within(1e-9), "Shooting holds up long after speed has gone.");
        }

        [Test]
        public void DeclineDeepensWithAge()
        {
            var early = DevelopmentModel.GetDecline(31, AttributeCategory.Physical);
            var late = DevelopmentModel.GetDecline(36, AttributeCategory.Physical);

            Assert.That(late, Is.GreaterThan(early));
        }

        [Test]
        public void BasketballIqNeverDeclines()
        {
            foreach (var age in new[] { 20, 28, 34, 39 })
            {
                Assert.That(DevelopmentModel.GetDecline(age, AttributeCategory.Mental), Is.EqualTo(0.0).Within(1e-9),
                    "Reading the game is the one thing that does not wear out.");
            }

            Assert.That(DevelopmentModel.GetGrowth(36, 0, AttributeCategory.Mental), Is.GreaterThan(0.0),
                "A veteran keeps learning even with no headroom left elsewhere.");
        }

        [Test]
        public void FractionalGrowthIsNotLostToRounding()
        {
            var random = new XorShiftRandom(4242u);

            var total = 0;

            for (var trial = 0; trial < 4000; trial++)
            {
                total += DevelopmentModel.RoundStochastically(0.25, random);
            }

            Assert.That(total / 4000.0, Is.EqualTo(0.25).Within(0.03));
        }

        [Test]
        public void StochasticRoundingHandlesLosses()
        {
            var random = new XorShiftRandom(99u);

            var total = 0;

            for (var trial = 0; trial < 4000; trial++)
            {
                total += DevelopmentModel.RoundStochastically(-0.4, random);
            }

            Assert.That(total / 4000.0, Is.EqualTo(-0.4).Within(0.03));
        }

        [Test]
        public void DevelopmentNeverLeavesTheLegalRange()
        {
            var random = new XorShiftRandom(7u);

            var maxed = new PlayerAttributes(20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20);
            var player = new Player(1, "Max", "Rated", PlayerPosition.Center, maxed, 20, 20, 20);

            for (var season = 0; season < 25; season++)
            {
                player.ApplyDevelopment(DevelopmentModel.Develop(player, random));
                player.AdvanceAge();

                Assert.That(player.Attributes.Finishing, Is.InRange(PlayerAttributes.Minimum, PlayerAttributes.Maximum));
                Assert.That(player.Attributes.Speed, Is.InRange(PlayerAttributes.Minimum, PlayerAttributes.Maximum));
                Assert.That(player.Attributes.BasketballIq, Is.InRange(PlayerAttributes.Minimum, PlayerAttributes.Maximum));
            }
        }
    }

    [TestFixture]
    public sealed class RetirementAndProspectTests
    {
        [Test]
        public void YoungPlayersNeverRetire()
        {
            Assert.That(RetirementModel.GetRetirementChance(24, 12), Is.EqualTo(0.0).Within(1e-9));
            Assert.That(RetirementModel.GetRetirementChance(31, 8), Is.EqualTo(0.0).Within(1e-9));
        }

        [Test]
        public void RetirementBecomesMoreLikelyWithAge()
        {
            var early = RetirementModel.GetRetirementChance(33, 11);
            var later = RetirementModel.GetRetirementChance(36, 11);

            Assert.That(later, Is.GreaterThan(early));
        }

        [Test]
        public void GoodPlayersHangOnLonger()
        {
            var star = RetirementModel.GetRetirementChance(34, 18);
            var journeyman = RetirementModel.GetRetirementChance(34, 7);

            Assert.That(star, Is.LessThan(journeyman));
        }

        [Test]
        public void EverybodyRetiresEventually()
        {
            Assert.That(RetirementModel.GetRetirementChance(RetirementModel.MandatoryRetirementAge, 20),
                Is.EqualTo(1.0).Within(1e-9),
                "Without a hard limit a good enough player could pass the dice roll forever.");
        }

        [Test]
        public void ProspectsArriveYoungAndUnfinished()
        {
            var random = new XorShiftRandom(31337u);

            for (var trial = 0; trial < 200; trial++)
            {
                var prospect = ProspectGenerator.Create(trial, PlayerPosition.SmallForward, random);

                Assert.That(prospect.Age, Is.InRange(ProspectGenerator.MinimumEntryAge, ProspectGenerator.MaximumEntryAge));

                Assert.That(prospect.Potential, Is.GreaterThan(prospect.Overall),
                    "A prospect whose ceiling equals his current level has nothing to offer but what he already is.");

                Assert.That(prospect.Attributes.Speed, Is.InRange(PlayerAttributes.Minimum, PlayerAttributes.Maximum));
            }
        }

        [Test]
        public void ScoutingIsImperfectButNotWild()
        {
            var random = new XorShiftRandom(555u);

            var anyWrong = false;

            for (var trial = 0; trial < 200; trial++)
            {
                var prospect = ProspectGenerator.Create(trial, PlayerPosition.PointGuard, random);

                var error = Math.Abs(prospect.ScoutedPotential - prospect.Potential);

                Assert.That(error, Is.LessThanOrEqualTo(ProspectGenerator.MaximumScoutingError));

                if (error > 0)
                {
                    anyWrong = true;
                }
            }

            Assert.That(anyWrong, Is.True,
                "If scouting is never wrong then the hidden potential rating is not hidden at all.");
        }

        [Test]
        public void GeneratedBigsRebound()
        {
            var random = new XorShiftRandom(808u);

            var centreRebounding = 0;
            var guardRebounding = 0;

            for (var trial = 0; trial < 150; trial++)
            {
                centreRebounding += ProspectGenerator.Create(trial, PlayerPosition.Center, random)
                    .Attributes.DefensiveRebounding;

                guardRebounding += ProspectGenerator.Create(trial, PlayerPosition.PointGuard, random)
                    .Attributes.DefensiveRebounding;
            }

            Assert.That(centreRebounding, Is.GreaterThan(guardRebounding),
                "Position should shape a generated player, or every prospect is interchangeable.");
        }
    }

    [TestFixture]
    [Category("Statistical")]
    public sealed class CareerProgressionTests
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
        public void EveryoneAgesByAYearEachSeason()
        {
            var session = GameSession.CreateDemo();

            var before = session.UserTeam.Players.ToDictionary(player => player.Id, player => player.Age);

            var guard = 0;

            while (!session.Season.IsComplete && guard++ < 200)
            {
                session.SimulateCurrentFixture();
                session.CompleteCurrentRound();
            }

            session.AdvanceToNextSeason();

            foreach (var player in session.UserTeam.Players.Where(player => before.ContainsKey(player.Id)))
            {
                Assert.That(player.Age, Is.EqualTo(before[player.Id] + 1));
            }
        }

        [Test]
        public void SquadsStayFullAsPlayersRetire()
        {
            var session = PlaySeasons(12);

            foreach (var team in session.Career.League.Teams)
            {
                Assert.That(team.Players.Count, Is.EqualTo(12),
                    "Every retirement must bring in a replacement, or squads drain away over a career.");

                Assert.That(team.Players.Select(player => player.Id).Distinct().Count(), Is.EqualTo(12),
                    "A generated prospect must not reuse an existing player's id.");
            }
        }

        [Test]
        public void PlayersDoRetireOverALongCareer()
        {
            var session = PlaySeasons(12);

            Assert.That(session.Career.RetiredPlayers, Is.Not.Empty);

            Assert.That(session.Career.RetiredPlayers.All(player => player.Age >= RetirementModel.EarliestRetirementAge),
                Is.True);
        }

        [Test]
        public void TheLeagueContainsAMixOfAges()
        {
            var session = PlaySeasons(10);

            var ages = session.Career.League.Teams
                .SelectMany(team => team.Players)
                .Select(player => player.Age)
                .ToList();

            Assert.That(ages.Min(), Is.LessThan(24), "There should be youth coming through.");
            Assert.That(ages.Max(), Is.GreaterThan(30), "And veterans still going.");

            Assert.That(ages.Max(), Is.LessThan(RetirementModel.MandatoryRetirementAge));
        }

        [Test]
        public void LeagueTalentStaysBroadlyStable()
        {
            var session = GameSession.CreateDemo();

            double AverageOverall() => session.Career.League.Teams
                .SelectMany(team => team.Players)
                .Average(player => player.Overall);

            var start = AverageOverall();

            for (var season = 0; season < 20; season++)
            {
                var guard = 0;

                while (!session.Season.IsComplete && guard++ < 200)
                {
                    session.SimulateCurrentFixture();
                    session.CompleteCurrentRound();
                }

                session.AdvanceToNextSeason();
            }

            var end = AverageOverall();

            Assert.That(Math.Abs(end - start), Is.LessThan(1.5),
                "Over twenty seasons the league should neither drain away nor inflate. Drift here means " +
                "prospects are arriving systematically weaker or stronger than the players they replace.");
        }

        [Test]
        public void TheCloseSeasonReportsWhatChanged()
        {
            var session = PlaySeasons(6);

            var report = session.Career.LastCloseSeason;

            Assert.That(report, Is.Not.Null);

            Assert.That(report.Improvements.Count + report.Declines.Count, Is.GreaterThan(0),
                "Something has to move each close season, or development is not running.");

            foreach (var retirement in report.Retirements)
            {
                Assert.That(retirement.Replacement, Is.Not.Null);
                Assert.That(retirement.Replacement.Id, Is.Not.EqualTo(retirement.Retired.Id));
            }
        }

        [Test]
        public void ACareerWithRetirementsSurvivesASave()
        {
            var session = PlaySeasons(8);

            Assert.That(session.Career.RetiredPlayers, Is.Not.Empty, "The sample needs retirements to be meaningful.");

            var before = session.CreateSnapshot();
            var after = SaveGameMapper.FromDto(SaveGameMapper.ToDto(before, "career"));

            Assert.That(after.Career.RetiredPlayers.Count, Is.EqualTo(before.Career.RetiredPlayers.Count),
                "Retired players must be saved. Archived seasons hold their statistics, so a save that " +
                "forgets them cannot resolve its own history and fails to load.");

            var archivedPlayers = after.Career.CompletedSeasons
                .SelectMany(season => season.PlayerStatistics)
                .Select(statistics => statistics.Player.Id)
                .Distinct();

            var known = after.Career.League.Teams
                .SelectMany(team => team.Players)
                .Concat(after.Career.RetiredPlayers)
                .Select(player => player.Id)
                .ToHashSet();

            Assert.That(archivedPlayers.All(id => known.Contains(id)), Is.True);
        }

        [Test]
        public void AgeAndPotentialSurviveASave()
        {
            var session = PlaySeasons(3);

            var before = session.CreateSnapshot();
            var after = SaveGameMapper.FromDto(SaveGameMapper.ToDto(before, "aged"));

            var originals = before.Career.League.Teams.SelectMany(team => team.Players).OrderBy(player => player.Id).ToList();
            var restored = after.Career.League.Teams.SelectMany(team => team.Players).OrderBy(player => player.Id).ToList();

            Assert.That(restored.Count, Is.EqualTo(originals.Count));

            for (var index = 0; index < originals.Count; index++)
            {
                Assert.That(restored[index].Age, Is.EqualTo(originals[index].Age));
                Assert.That(restored[index].Potential, Is.EqualTo(originals[index].Potential));
                Assert.That(restored[index].ScoutedPotential, Is.EqualTo(originals[index].ScoutedPotential));
            }
        }

        [Test]
        public void SavesWithoutAgesStillLoad()
        {
            var dto = SaveGameMapper.ToDto(GameSession.CreateDemo().CreateSnapshot(), "old");

            dto.SchemaVersion = 2;
            dto.CompletedSeasons = null;
            dto.RetiredPlayers = null;

            foreach (var player in dto.League.Teams.SelectMany(team => team.Players))
            {
                player.Age = 0;
                player.Potential = 0;
                player.ScoutedPotential = 0;
            }

            var loaded = SaveGameMapper.FromDto(dto);

            var players = loaded.Career.League.Teams.SelectMany(team => team.Players).ToList();

            Assert.That(players.All(player => player.Age == Player.DefaultAge), Is.True);
            Assert.That(players.All(player => player.Potential == player.Overall), Is.True);
        }
    }
}