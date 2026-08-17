using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProBasketballManager.Domain.Demo;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Tests
{
    /// <summary>
    /// Tests the fatigue model in isolation. These are pure arithmetic with no
    /// randomness, so they run instantly and their results are exact.
    /// </summary>
    [TestFixture]
    public sealed class FatigueModelTests
    {
        private static Player CreatePlayer(int stamina)
        {
            var attributes = new PlayerAttributes(11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, stamina, 11);

            return new Player(1, "Test", "Player", PlayerPosition.Center, attributes);
        }

        [Test]
        public void PlayingTime_IncreasesFatigue()
        {
            var player = CreatePlayer(11);
            var fatigue = FatigueModel.ApplyExertion(FatigueModel.Fresh, player, 600.0, 1.0, 1.0);

            Assert.That(fatigue, Is.GreaterThan(FatigueModel.Fresh));
            Assert.That(fatigue, Is.InRange(0.35, 0.75),
                "A full period of continuous play should leave an average player meaningfully tired " +
                "but not exhausted.");
        }

        [Test]
        public void BenchTime_ReducesFatigue()
        {
            var player = CreatePlayer(11);
            var tired = FatigueModel.ApplyExertion(FatigueModel.Fresh, player, 600.0, 1.0, 1.0);
            var rested = FatigueModel.ApplyRecovery(tired, player, 240.0);

            Assert.That(rested, Is.LessThan(tired), "Sitting down must make a player fresher, not more tired.");
        }

        [Test]
        public void Fatigue_NeverLeavesItsBounds()
        {
            var player = CreatePlayer(2);

            Assert.That(FatigueModel.ApplyExertion(FatigueModel.Exhausted, player, 6000.0, 1.5, 1.5),
                Is.EqualTo(FatigueModel.Exhausted).Within(1e-9),
                "Fatigue must not rise above 1.0, or the performance penalties would exceed their stated maximums.");

            Assert.That(FatigueModel.ApplyRecovery(FatigueModel.Fresh, player, 6000.0),
                Is.EqualTo(FatigueModel.Fresh).Within(1e-9),
                "Fatigue must not fall below 0.0, or resting would make a player better than fresh.");
        }

        [Test]
        public void HigherStamina_TiresMoreSlowly()
        {
            var strong = FatigueModel.ApplyExertion(FatigueModel.Fresh, CreatePlayer(20), 600.0, 1.0, 1.0);
            var average = FatigueModel.ApplyExertion(FatigueModel.Fresh, CreatePlayer(11), 600.0, 1.0, 1.0);
            var weak = FatigueModel.ApplyExertion(FatigueModel.Fresh, CreatePlayer(2), 600.0, 1.0, 1.0);

            Assert.That(strong, Is.LessThan(average));
            Assert.That(average, Is.LessThan(weak));
        }

        [Test]
        public void HigherStamina_RecoversMoreQuickly()
        {
            const double startingFatigue = 0.6;

            var strong = FatigueModel.ApplyRecovery(startingFatigue, CreatePlayer(20), 120.0);
            var weak = FatigueModel.ApplyRecovery(startingFatigue, CreatePlayer(2), 120.0);

            Assert.That(strong, Is.LessThan(weak));
        }

        [Test]
        public void FasterPace_TiresPlayersMoreQuickly()
        {
            var player = CreatePlayer(11);

            var slow = FatigueModel.ApplyExertion(FatigueModel.Fresh, player, 600.0, 0.85, 1.0);
            var fast = FatigueModel.ApplyExertion(FatigueModel.Fresh, player, 600.0, 1.15, 1.0);

            Assert.That(fast, Is.GreaterThan(slow),
                "Running a fast tempo has to cost something, or the pace slider is a free source of possessions.");
        }

        [Test]
        public void AFreshPlayer_SuffersNoPenalties()
        {
            Assert.That(FatigueModel.GetShootingPenalty(FatigueModel.Fresh), Is.EqualTo(0.0).Within(1e-9));
            Assert.That(FatigueModel.GetTurnoverPenalty(FatigueModel.Fresh), Is.EqualTo(0.0).Within(1e-9));
            Assert.That(FatigueModel.GetDefensiveMultiplier(FatigueModel.Fresh), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(FatigueModel.GetReboundingMultiplier(FatigueModel.Fresh), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(FatigueModel.GetFoulMultiplier(FatigueModel.Fresh), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(FatigueModel.GetSubstitutionCost(FatigueModel.Fresh), Is.EqualTo(0.0).Within(1e-9));
        }

        [Test]
        public void AnExhaustedPlayer_SuffersTheFullPenalty()
        {
            Assert.That(FatigueModel.GetShootingPenalty(FatigueModel.Exhausted),
                Is.EqualTo(FatigueModel.MaximumShootingPenalty).Within(1e-9));

            Assert.That(FatigueModel.GetDefensiveMultiplier(FatigueModel.Exhausted),
                Is.EqualTo(1.0 - FatigueModel.MaximumDefensivePenalty).Within(1e-9));

            Assert.That(FatigueModel.GetFoulMultiplier(FatigueModel.Exhausted),
                Is.EqualTo(1.0 + FatigueModel.MaximumFoulPenalty).Within(1e-9));
        }

        [Test]
        public void PenaltiesNeverInvertAPlayersContribution()
        {
            Assert.That(FatigueModel.GetDefensiveMultiplier(FatigueModel.Exhausted), Is.GreaterThan(0.0),
                "An exhausted defender should be worse, never actively helping the opponent.");

            Assert.That(FatigueModel.GetReboundingMultiplier(FatigueModel.Exhausted), Is.GreaterThan(0.0));
        }

        [Test]
        public void ConditionFactor_StaysInsideItsBand()
        {
            var player = CreatePlayer(11);

            for (var step = 0; step <= 20; step++)
            {
                var factor = FatigueModel.RollConditionFactor(player, step / 20.0);

                Assert.That(factor, Is.InRange(FatigueModel.MinimumConditionFactor, FatigueModel.MaximumConditionFactor));
            }
        }
    }

    /// <summary>
    /// Tests what fatigue does once it is wired into a real match: whether it
    /// reshapes the rotation, whether Stamina affects results, and whether the
    /// calibration survived. These simulate games, so they are slower.
    /// </summary>
    [TestFixture]
    [Category("Statistical")]
    public sealed class FatigueBehaviourTests
    {
        private const int GameCount = 250;

        private List<MatchResult> _results;
        private TeamRotation _homeRotation;

        [OneTimeSetUp]
        public void SimulateSharedBatch()
        {
            var teams = DemoLeagueFactory.Create().Teams;

            _homeRotation = TeamRotation.CreateDefault(teams[0]);
            _results = new List<MatchResult>();

            for (var game = 0; game < GameCount; game++)
            {
                var seed = 1u + ((uint)game * 13u);

                _results.Add(new MatchSimulator(new XorShiftRandom(seed)).Simulate(teams[0], teams[1]));
            }
        }

        private IEnumerable<PlayerBoxScore> HomeBoxScores => _results.SelectMany(result => result.HomePlayerStats);

        [Test]
        public void Starters_FallShortOfTheirMinutesTarget()
        {
            var starterMinutes = HomeBoxScores
                .Where(box => box.IsStarter)
                .Select(box => box.SecondsPlayed / 60.0)
                .Average();

            Assert.That(starterMinutes, Is.LessThan(28.0),
                "Fatigue should pull starters off before they reach their full target, otherwise it is not " +
                "affecting the rotation at all.");

            Assert.That(starterMinutes, Is.GreaterThan(23.0),
                "Starters should still carry the bulk of the minutes. If this drops far below target, " +
                "SubstitutionSecondsPerFatiguePoint is overpowering the manager's rotation plan.");
        }

        [Test]
        public void BenchPlayers_ReceiveMoreThanTheirMinutesTarget()
        {
            var benchMinutes = HomeBoxScores
                .Where(box => !box.IsStarter && box.SecondsPlayed > 0.0)
                .Select(box => box.SecondsPlayed / 60.0)
                .Average();

            Assert.That(benchMinutes, Is.GreaterThan(12.5),
                "Minutes taken from tired starters have to go somewhere, so the bench should clearly exceed its " +
                "12 minute target. With fatigue disabled this sits at almost exactly 12.0, so the threshold is " +
                "set above that to make the test meaningful.");
        }

        [Test]
        public void MinutesVary_BetweenGames()
        {
            var starterId = _results[0].HomePlayerStats.First(box => box.IsStarter).Player.Id;

            var perGame = _results
                .Select(result => result.HomePlayerStats.First(box => box.Player.Id == starterId).SecondsPlayed / 60.0)
                .ToList();

            var mean = perGame.Average();
            var standardDeviation = Math.Sqrt(perGame.Select(value => (value - mean) * (value - mean)).Average());

            Assert.That(standardDeviation, Is.GreaterThan(0.80),
                "One player's minutes should differ meaningfully from night to night. Possession count alone " +
                "produces about 0.6; the per match condition factor lifts it past 1.0. If this drops back toward " +
                "0.6, RollConditionFactor has stopped having an effect.");
        }

        [Test]
        public void PlayersWithNoTargetMinutes_StillDoNotPlay()
        {
            var excluded = _homeRotation.Assignments
                .Where(assignment => assignment.TargetMinutes == 0)
                .Select(assignment => assignment.Player.Id)
                .ToHashSet();

            Assert.That(excluded, Is.Not.Empty, "The default rotation should leave some players out.");

            foreach (var box in HomeBoxScores.Where(box => excluded.Contains(box.Player.Id)))
            {
                Assert.That(box.SecondsPlayed, Is.EqualTo(0.0).Within(1e-9),
                    "Fatigue must not promote a player the manager left out of the rotation entirely.");
            }
        }

        [Test]
        public void TeamMinutes_StillTotalExactlyTwoHundred()
        {
            foreach (var result in _results.Where(result => result.HomePeriodScores.Count == 4))
            {
                var minutes = result.HomePlayerStats.Sum(box => box.SecondsPlayed) / 60.0;

                Assert.That(minutes, Is.EqualTo(200.0).Within(0.01),
                    "Substitutions change who is on court, never how much court time exists.");
            }
        }

        [Test]
        public void ScoringDoesNotCollapse_InTheFourthQuarter()
        {
            var regulation = _results.Where(result => result.HomePeriodScores.Count == 4).ToList();

            var firstQuarter = regulation.Average(result => result.HomePeriodScores[0]);
            var fourthQuarter = regulation.Average(result => result.HomePeriodScores[3]);

            Assert.That(fourthQuarter, Is.GreaterThan(firstQuarter * 0.85),
                "Some late game decline is realistic, but a collapse means the fatigue penalties are too " +
                "harsh or players are not being rested enough.");
        }

        [Test]
        public void Stamina_AffectsResults()
        {
            var fit = SimulationTestHarness.CreateTeamWithStamina(91, "Well Conditioned", 11, 17);
            var unfit = SimulationTestHarness.CreateTeamWithStamina(92, "Poorly Conditioned", 11, 5);

            // The fit team plays each fixture once at home and once away, so home
            // court advantage cancels out entirely. Without this the test would pass
            // on home advantage alone even if Stamina did nothing.
            var fitWins = 0;
            const int fixtures = 200;

            for (var game = 0; game < fixtures; game++)
            {
                var seed = 1u + ((uint)game * 7919u);

                var atHome = new MatchSimulator(new XorShiftRandom(seed)).Simulate(fit, unfit);
                var away = new MatchSimulator(new XorShiftRandom(seed)).Simulate(unfit, fit);

                if (atHome.HomeScore > atHome.AwayScore)
                {
                    fitWins++;
                }

                if (away.AwayScore > away.HomeScore)
                {
                    fitWins++;
                }
            }

            Assert.That(fitWins / (double)(fixtures * 2), Is.InRange(0.53, 0.70),
                "With every other attribute identical and home advantage cancelled, the fitter team should win " +
                "noticeably more often. With fatigue disabled this sits at almost exactly 50 percent, which is " +
                "what a failure here would mean: the Stamina attribute is not reaching the simulation.");
        }
    }
}