using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProBasketballManager.Domain.Demo;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Tactics;

namespace ProBasketballManager.Domain.Tests
{
    /// <summary>
    /// Tests the foul model in isolation. Pure arithmetic, no randomness.
    /// </summary>
    [TestFixture]
    public sealed class FoulModelTests
    {
        [Test]
        public void FiveFouls_Disqualifies()
        {
            Assert.That(FoulModel.IsDisqualified(4), Is.False);
            Assert.That(FoulModel.IsDisqualified(5), Is.True);
            Assert.That(FoulModel.DisqualificationLimit, Is.EqualTo(5),
                "FIBA disqualifies on the fifth personal foul. This is a rule, not a tuning value.");
        }

        [Test]
        public void TheBonus_StartsOnTheFifthTeamFoulOfAPeriod()
        {
            Assert.That(FoulModel.IsInBonus(3), Is.False);
            Assert.That(FoulModel.IsInBonus(4), Is.True,
                "With four team fouls already committed, the next one is the fifth and sends the attacker to the line.");
        }

        [Test]
        public void FoulTrouble_TightensAsThePeriodGetsEarlier()
        {
            // Two fouls in the first period is trouble; two in the third is not.
            Assert.That(FoulModel.GetFoulTroubleLevel(2, 1), Is.GreaterThan(0));
            Assert.That(FoulModel.GetFoulTroubleLevel(2, 3), Is.EqualTo(0));

            Assert.That(FoulModel.GetFoulTroubleLevel(4, 3), Is.GreaterThan(0));
            Assert.That(FoulModel.GetFoulTroubleLevel(4, 4), Is.EqualTo(0),
                "In the closing period there is no later game to save a player for, so the limit relaxes.");
        }

        [Test]
        public void FoulTrouble_DeepensWithEachExtraFoul()
        {
            var two = FoulModel.GetSubstitutionCost(2, 1);
            var three = FoulModel.GetSubstitutionCost(3, 1);
            var four = FoulModel.GetSubstitutionCost(4, 1);

            Assert.That(three, Is.GreaterThan(two));
            Assert.That(four, Is.GreaterThan(three));
        }

        [Test]
        public void ACleanPlayer_PaysNoSubstitutionCost()
        {
            Assert.That(FoulModel.GetSubstitutionCost(0, 1), Is.EqualTo(0.0).Within(1e-9));
            Assert.That(FoulModel.GetSubstitutionCost(1, 2), Is.EqualTo(0.0).Within(1e-9));
        }

        [Test]
        public void Overtime_OnlyCaresAboutOutrightDisqualification()
        {
            Assert.That(FoulModel.GetFoulTroubleLevel(4, 5), Is.EqualTo(0),
                "By overtime every remaining player is needed, so only fouling out removes anyone.");
        }

        [Test]
        public void AggressiveTactics_RaiseTheFoulRate()
        {
            var passive = FoulModel.GetNonShootingFoulChance(10.5, 0.0, new TeamTactics(50, 45, 20, 35, 50, 0, 0));
            var neutral = FoulModel.GetNonShootingFoulChance(10.5, 0.0, TeamTactics.Default);
            var aggressive = FoulModel.GetNonShootingFoulChance(10.5, 0.0, new TeamTactics(50, 45, 20, 35, 50, 100, 100));

            Assert.That(aggressive, Is.GreaterThan(neutral));
            Assert.That(neutral, Is.GreaterThanOrEqualTo(passive),
                "Pressing hard has to cost fouls, otherwise the defensive sliders are free.");
        }

        [Test]
        public void BetterDefenders_FoulLess()
        {
            var disciplined = FoulModel.GetNonShootingFoulChance(18.0, 0.0, TeamTactics.Default);
            var careless = FoulModel.GetNonShootingFoulChance(4.0, 0.0, TeamTactics.Default);

            Assert.That(disciplined, Is.LessThan(careless));
        }

        [Test]
        public void TiredDefenders_FoulMore()
        {
            var fresh = FoulModel.GetNonShootingFoulChance(10.5, 0.0, TeamTactics.Default);
            var tired = FoulModel.GetNonShootingFoulChance(10.5, 1.0, TeamTactics.Default);

            Assert.That(tired, Is.GreaterThan(fresh));
        }

        [Test]
        public void FoulChances_NeverGoNegative()
        {
            var chance = FoulModel.GetNonShootingFoulChance(20.0, 0.0, new TeamTactics(50, 45, 20, 35, 50, 0, 0));

            Assert.That(chance, Is.GreaterThanOrEqualTo(0.0),
                "An elite disciplined defence should foul rarely, never a negative number of times.");
        }
    }

    /// <summary>
    /// Tests what fouls do inside a real match: whether the personal foul count is
    /// realistic, whether the disqualification rule is actually enforced, and
    /// whether aggressive tactics now carry a cost.
    /// </summary>
    [TestFixture]
    [Category("Statistical")]
    public sealed class FoulBehaviourTests
    {
        private const int GameCount = 400;

        private List<MatchResult> _results;

        [OneTimeSetUp]
        public void SimulateSharedBatch()
        {
            _results = SimulationTestHarness.SimulateLeagueBatch(GameCount).ToList();
        }

        private IEnumerable<PlayerBoxScore> AllBoxScores =>
            _results.SelectMany(result => result.HomePlayerStats.Concat(result.AwayPlayerStats));

        [Test]
        public void PersonalFoulsPerGame_AreRealistic()
        {
            var perTeam = _results
                .SelectMany(result => new[] { result.HomePlayerStats, result.AwayPlayerStats })
                .Average(box => box.Sum(player => player.PersonalFouls));

            Assert.That(perTeam, Is.InRange(14.0, 21.0),
                "Team fouls per game should sit in the realistic range. Before non-shooting fouls existed this " +
                "was stuck around 8, because only a fraction of real fouls happen on a shot attempt.");
        }

        [Test]
        public void NoPlayer_EverExceedsTheFoulLimit()
        {
            var worst = AllBoxScores.Max(box => box.PersonalFouls);

            Assert.That(worst, Is.LessThanOrEqualTo(FoulModel.DisqualificationLimit),
                "A disqualified player cannot commit another foul. Several fouls can occur inside one " +
                "possession while the on court list is still the one captured at its start, so the foul " +
                "selection has to skip players who have already fouled out.");
        }

        [Test]
        public void PlayersDoFoulOut_ButNotConstantly()
        {
            var foulOutsPerGame = _results.Average(result =>
                result.HomePlayerStats.Count(box => box.PersonalFouls >= FoulModel.DisqualificationLimit)
                + result.AwayPlayerStats.Count(box => box.PersonalFouls >= FoulModel.DisqualificationLimit));

            Assert.That(foulOutsPerGame, Is.InRange(0.4, 1.5),
                "Under a five foul limit disqualifications should happen most games but stay uncommon. " +
                "A figure near zero means foul trouble is not biting; a high one means the rotation is " +
                "not resting players in trouble.");
        }

        [Test]
        public void DisqualifiedPlayers_StopAccumulatingMinutes()
        {
            // A player who fouls out early cannot have played the whole game.
            var fouledOut = AllBoxScores
                .Where(box => box.PersonalFouls >= FoulModel.DisqualificationLimit)
                .ToList();

            Assert.That(fouledOut, Is.Not.Empty, "The sample should contain some disqualifications.");

            Assert.That(fouledOut.Max(box => box.SecondsPlayed / 60.0), Is.LessThan(40.0),
                "Nobody who fouled out can have been on court for the full forty minutes.");
        }

        [Test]
        public void EveryFoulType_Occurs()
        {
            var events = _results.SelectMany(result => result.Events).ToList();

            foreach (var type in new[]
            {
                MatchEventType.ShootingFoul,
                MatchEventType.PersonalFoul,
                MatchEventType.OffensiveFoul,
                MatchEventType.LooseBallFoul,
                MatchEventType.FoulOut
            })
            {
                Assert.That(events.Count(matchEvent => matchEvent.Type == type), Is.GreaterThan(0),
                    $"No {type} events were produced, so that path is unreachable.");
            }
        }

        [Test]
        public void NonShootingFouls_OutnumberShootingFouls()
        {
            var events = _results.SelectMany(result => result.Events).ToList();

            var shooting = events.Count(matchEvent => matchEvent.Type == MatchEventType.ShootingFoul);

            var other = events.Count(matchEvent =>
                matchEvent.Type == MatchEventType.PersonalFoul
                || matchEvent.Type == MatchEventType.OffensiveFoul
                || matchEvent.Type == MatchEventType.LooseBallFoul);

            Assert.That(other, Is.GreaterThan(shooting),
                "Most real fouls are not committed on a shot attempt. If shooting fouls dominate, the mix " +
                "is wrong even when the total is right.");
        }

        [Test]
        public void OffensiveFouls_CountAsTurnovers()
        {
            var teams = DemoLeagueFactory.Create().Teams;

            var withOffensiveFouls = 0;

            for (var game = 0; game < 60; game++)
            {
                var result = new MatchSimulator(new XorShiftRandom(1u + ((uint)game * 31u))).Simulate(teams[0], teams[1]);

                var offensiveFouls = result.Events.Count(matchEvent => matchEvent.Type == MatchEventType.OffensiveFoul);

                if (offensiveFouls > 0)
                {
                    withOffensiveFouls++;

                    Assert.That(
                        result.HomePlayerStats.Sum(box => box.Turnovers) + result.AwayPlayerStats.Sum(box => box.Turnovers),
                        Is.GreaterThanOrEqualTo(offensiveFouls),
                        "A charge is a turnover, so the box score must record at least as many turnovers as " +
                        "there were offensive fouls.");
                }
            }

            Assert.That(withOffensiveFouls, Is.GreaterThan(0), "The sample should contain offensive fouls.");
        }

        [Test]
        public void AggressiveDefence_CostsFoulsAndGames()
        {
            var teams = DemoLeagueFactory.Create().Teams;

            (double Fouls, double WinRate) Play(TeamTactics homeTactics)
            {
                var fouls = 0.0;
                var wins = 0;
                const int games = 300;

                for (var game = 0; game < games; game++)
                {
                    var result = new MatchSimulator(new XorShiftRandom(1u + ((uint)game * 7919u)))
                        .Simulate(teams[0], teams[1], homeTactics, TeamTactics.Default);

                    fouls += result.HomePlayerStats.Sum(box => box.PersonalFouls);

                    if (result.HomeScore > result.AwayScore)
                    {
                        wins++;
                    }
                }

                return (fouls / games, wins / (double)games);
            }

            var neutral = Play(TeamTactics.Default);
            var aggressive = Play(new TeamTactics(50, 45, 20, 35, 50, 100, 100));

            Assert.That(aggressive.Fouls, Is.GreaterThan(neutral.Fouls + 3.0),
                "Maximum pressure should visibly raise the foul count.");

            Assert.That(aggressive.WinRate, Is.LessThan(neutral.WinRate),
                "Maxing both defensive sliders used to be a free win. Foul trouble is what makes it a " +
                "trade-off, so if this passes again the cost has stopped applying.");
        }

        [Test]
        public void TeamMinutes_StillTotalExactlyTwoHundred()
        {
            foreach (var result in _results.Where(result => result.HomePeriodScores.Count == 4))
            {
                Assert.That(result.HomePlayerStats.Sum(box => box.SecondsPlayed) / 60.0,
                    Is.EqualTo(200.0).Within(0.01),
                    "Disqualifications change who is available, never how much court time exists.");
            }
        }

        [Test]
        public void FiveTeammates_AreAlwaysAvailable()
        {
            // Even with disqualifications, every game must field five players for the
            // full forty minutes, falling back to players outside the rotation if the
            // manager's chosen ten run out.
            foreach (var result in _results.Where(result => result.HomePeriodScores.Count == 4))
            {
                var played = result.HomePlayerStats.Count(box => box.SecondsPlayed > 0.0);

                Assert.That(played, Is.GreaterThanOrEqualTo(5),
                    "A team must always have five players on court.");
            }
        }
    }
}