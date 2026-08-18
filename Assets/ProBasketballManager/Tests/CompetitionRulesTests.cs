using System;
using System.Linq;
using NUnit.Framework;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Demo;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Tactics;
using ProBasketballManager.Domain.Teams;
using ProBasketballManager.Persistence;
using ProBasketballManager.Presentation.State;

namespace ProBasketballManager.Domain.Tests
{
    [TestFixture]
    public sealed class CompetitionRulesTests
    {
        [Test]
        public void FibaRulesDescribeFortyMinuteBasketball()
        {
            var rules = CompetitionRules.Fiba;

            Assert.That(rules.RegulationMinutes, Is.EqualTo(40.0).Within(1e-9));
            Assert.That(rules.TotalPlayerMinutesPerGame, Is.EqualTo(200.0).Within(1e-9));
            Assert.That(rules.PersonalFoulsToDisqualify, Is.EqualTo(5));
        }

        [Test]
        public void NbaRulesDescribeFortyEightMinuteBasketball()
        {
            var rules = CompetitionRules.Nba;

            Assert.That(rules.RegulationMinutes, Is.EqualTo(48.0).Within(1e-9));
            Assert.That(rules.TotalPlayerMinutesPerGame, Is.EqualTo(240.0).Within(1e-9),
                "The 200 minute figure that used to be hardcoded is arithmetic, not a rule.");

            Assert.That(rules.PersonalFoulsToDisqualify, Is.EqualTo(6));
        }

        [Test]
        public void OvertimePeriodsAreRecognisedFromTheRules()
        {
            var rules = CompetitionRules.Fiba;

            Assert.That(rules.IsOvertimePeriod(4), Is.False);
            Assert.That(rules.IsOvertimePeriod(5), Is.True);

            Assert.That(rules.GetPeriodLengthSeconds(4), Is.EqualTo(rules.PeriodLengthSeconds));
            Assert.That(rules.GetPeriodLengthSeconds(5), Is.EqualTo(rules.OvertimeLengthSeconds));
        }

        [Test]
        public void NonsenseRulesAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Build(periodCount: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Build(playersOnCourt: 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Build(periodLengthSeconds: 0));
            Assert.Throws<ArgumentException>(() => Build(name: " "));
        }

        [Test]
        public void ARosterSmallerThanTheLineupIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Build(rosterSize: 3),
                "A squad that cannot field a starting five is not a squad.");
        }

        private static CompetitionRules Build(
            string name = "Test",
            int periodCount = 4,
            int periodLengthSeconds = 600,
            int overtimeLengthSeconds = 300,
            int playersOnCourt = 5,
            int personalFoulsToDisqualify = 5,
            int teamFoulsBeforeBonus = 4,
            int bonusFreeThrows = 2,
            int rosterSize = 12,
            int roundRobinPasses = 2)
        {
            return new CompetitionRules(
                name, periodCount, periodLengthSeconds, overtimeLengthSeconds, playersOnCourt,
                personalFoulsToDisqualify, teamFoulsBeforeBonus, bonusFreeThrows, rosterSize, roundRobinPasses);
        }
    }

    [TestFixture]
    [Category("Statistical")]
    public sealed class RulesAffectSimulationTests
    {
        private const int GameCount = 300;

        private static (double Points, double Minutes, int MaximumFouls, int Periods) Simulate(CompetitionRules rules)
        {
            var teams = DemoLeagueFactory.Create().Teams;

            var homeRotation = TeamRotation.CreateDefault(teams[0], rules);
            var awayRotation = TeamRotation.CreateDefault(teams[1], rules);

            var points = 0.0;
            var minutes = 0.0;
            var regulationGames = 0;
            var maximumFouls = 0;
            var periods = 0;

            for (var game = 0; game < GameCount; game++)
            {
                var result = new MatchSimulator(new XorShiftRandom(1u + ((uint)game * 7919u)), rules)
                    .Simulate(teams[0], teams[1], homeRotation, awayRotation, TeamTactics.Default, TeamTactics.Default);

                points += result.HomeScore + result.AwayScore;

                maximumFouls = Math.Max(maximumFouls,
                    result.HomePlayerStats.Concat(result.AwayPlayerStats).Max(box => box.PersonalFouls));

                if (result.HomePeriodScores.Count == rules.PeriodCount)
                {
                    minutes += result.HomePlayerStats.Sum(box => box.SecondsPlayed) / 60.0;
                    regulationGames++;
                    periods = result.HomePeriodScores.Count;
                }
            }

            return (points / (GameCount * 2), minutes / regulationGames, maximumFouls, periods);
        }

        [Test]
        public void LongerPeriodsProduceMoreScoring()
        {
            var fiba = Simulate(CompetitionRules.Fiba);
            var nba = Simulate(CompetitionRules.Nba);

            Assert.That(nba.Points, Is.GreaterThan(fiba.Points * 1.1),
                "Forty eight minutes of basketball should produce noticeably more points than forty. " +
                "If these match, period length is not reaching the possession count.");
        }

        [Test]
        public void PlayerMinutesFollowTheRuleset()
        {
            Assert.That(Simulate(CompetitionRules.Fiba).Minutes, Is.EqualTo(200.0).Within(0.05));
            Assert.That(Simulate(CompetitionRules.Nba).Minutes, Is.EqualTo(240.0).Within(0.05));
        }

        [Test]
        public void TheFoulLimitFollowsTheRuleset()
        {
            Assert.That(Simulate(CompetitionRules.Fiba).MaximumFouls,
                Is.LessThanOrEqualTo(CompetitionRules.Fiba.PersonalFoulsToDisqualify));

            Assert.That(Simulate(CompetitionRules.Nba).MaximumFouls,
                Is.LessThanOrEqualTo(CompetitionRules.Nba.PersonalFoulsToDisqualify));
        }

        [Test]
        public void PeriodCountFollowsTheRuleset()
        {
            var threePeriods = new CompetitionRules("Three", 3, 600, 300, 5, 5, 4, 2, 12, 2);

            var result = new MatchSimulator(new XorShiftRandom(42u), threePeriods)
                .Simulate(
                    DemoLeagueFactory.Create().Teams[0],
                    DemoLeagueFactory.Create().Teams[1],
                    TeamRotation.CreateDefault(DemoLeagueFactory.Create().Teams[0], threePeriods),
                    TeamRotation.CreateDefault(DemoLeagueFactory.Create().Teams[1], threePeriods),
                    TeamTactics.Default,
                    TeamTactics.Default);

            Assert.That(result.HomePeriodScores.Count, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void TheDefaultRotationIsValidUnderAnyRuleset()
        {
            var team = DemoLeagueFactory.Create().Teams[0];

            foreach (var rules in new[] { CompetitionRules.Fiba, CompetitionRules.Nba })
            {
                var rotation = TeamRotation.CreateDefault(team, rules);

                Assert.That(rotation.Assignments.Sum(assignment => assignment.TargetMinutes),
                    Is.EqualTo(rotation.RequiredTotalMinutes),
                    $"The default rotation must satisfy its own validator under {rules.Name} rules.");
            }
        }

        [Test]
        public void TheScheduleFollowsTheRoundRobinPasses()
        {
            var league = DemoLeagueFactory.Create();

            int Fixtures(int passes) => RoundRobinScheduleGenerator
                .Generate(league, new CompetitionRules("Test", 4, 600, 300, 5, 5, 4, 2, 12, passes))
                .Count;

            var single = Fixtures(1);

            Assert.That(Fixtures(2), Is.EqualTo(single * 2));
            Assert.That(Fixtures(3), Is.EqualTo(single * 3));
        }

        [Test]
        public void ASingleRoundRobinPlaysEveryOpponentOnce()
        {
            var league = DemoLeagueFactory.Create();

            var rules = new CompetitionRules("Single", 4, 600, 300, 5, 5, 4, 2, 12, 1);

            var fixtures = RoundRobinScheduleGenerator.Generate(league, rules);

            foreach (var team in league.Teams)
            {
                var played = fixtures.Count(fixture =>
                    fixture.HomeTeam.Id == team.Id || fixture.AwayTeam.Id == team.Id);

                Assert.That(played, Is.EqualTo(league.Teams.Count - 1));
            }
        }
    }

    [TestFixture]
    public sealed class RulesPersistenceTests
    {
        [Test]
        public void TheRulesetSurvivesASaveAndLoad()
        {
            var session = GameSession.CreateDemo();

            var dto = SaveGameMapper.ToDto(session.CreateSnapshot(), "rules");

            Assert.That(dto.SchemaVersion, Is.EqualTo(4));
            Assert.That(dto.Season.Rules, Is.Not.Null);

            var restored = SaveGameMapper.FromDto(dto);

            Assert.That(restored.Career.CurrentSeason.Rules.Name, Is.EqualTo(CompetitionRules.Fiba.Name));
            Assert.That(restored.Career.CurrentSeason.Rules.PeriodLengthSeconds, Is.EqualTo(600));
            Assert.That(restored.Career.CurrentSeason.Rules.PersonalFoulsToDisqualify, Is.EqualTo(5));
        }

        [Test]
        public void ANonFibaRulesetSurvivesASaveAndLoad()
        {
            var season = DemoSeasonFactory.Create(CompetitionRules.Nba);

            var snapshot = new GameSessionSnapshot
            {
                Career = new Career(season.League, season),
                UserTeam = season.League.Teams[0],
                UserTactics = TeamTactics.Default,
                UserRotation = TeamRotation.CreateDefault(season.League.Teams[0], CompetitionRules.Nba),
                NextSeed = 1u
            };

            var restored = SaveGameMapper.FromDto(SaveGameMapper.ToDto(snapshot, "nba"));

            Assert.That(restored.Career.CurrentSeason.Rules.Name, Is.EqualTo("NBA"));
            Assert.That(restored.Career.CurrentSeason.Rules.PeriodLengthSeconds, Is.EqualTo(720));
            Assert.That(restored.Career.CurrentSeason.Rules.PersonalFoulsToDisqualify, Is.EqualTo(6));
            Assert.That(restored.Career.CurrentSeason.Rules.TotalPlayerMinutesPerGame, Is.EqualTo(240.0).Within(1e-9));
        }

        [Test]
        public void SavesWrittenBeforeRulesExistedLoadAsFiba()
        {
            var dto = SaveGameMapper.ToDto(GameSession.CreateDemo().CreateSnapshot(), "old");

            dto.SchemaVersion = 3;
            dto.Season.Rules = null;

            var restored = SaveGameMapper.FromDto(dto);

            Assert.That(restored.Career.CurrentSeason.Rules.Name, Is.EqualTo("FIBA"),
                "Every save written before this change assumed FIBA, so that is what a missing ruleset means.");
        }

        [Test]
        public void AnInvalidRulesetInASaveIsRejectedClearly()
        {
            var dto = SaveGameMapper.ToDto(GameSession.CreateDemo().CreateSnapshot(), "broken");

            dto.Season.Rules.PeriodCount = 0;

            var exception = Assert.Throws<SaveGameException>(() => SaveGameMapper.FromDto(dto));

            Assert.That(exception.Message.Contains("ruleset"), Is.True,
                "A modded database with a broken ruleset should say so, not crash mid match.");
        }

        [Test]
        public void RulesCarryForwardIntoTheNextSeason()
        {
            var season = DemoSeasonFactory.Create(CompetitionRules.Nba);
            var career = new Career(season.League, season);

            foreach (var fixture in season.Fixtures.ToList())
            {
                var simulator = new MatchSimulator(new XorShiftRandom((uint)(fixture.Id + 1)), season.Rules);

                season.RecordResult(fixture.Id, simulator.Simulate(fixture.HomeTeam, fixture.AwayTeam));
            }

            career.AdvanceToNextSeason();

            Assert.That(career.CurrentSeason.Rules.Name, Is.EqualTo("NBA"),
                "A new season inherits the competition's rules rather than reverting to the default.");
        }
    }
}