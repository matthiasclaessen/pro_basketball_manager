using NUnit.Framework;

namespace ProBasketballManager.Domain.Tests
{
    /// <summary>
    /// Guards how decisively roster quality settles a game.
    ///
    /// This is the most important property in a management game and the easiest to
    /// break by accident. If a small rating edge becomes a near certain win, the
    /// league table is decided the moment rosters are generated and no management
    /// decision can affect it. If ratings barely matter, squad building is
    /// pointless. Both failures are invisible when watching a single match.
    ///
    /// Each test plays two rosters where every player has identical attributes, so
    /// one number describes the whole team and nothing else differs between sides.
    ///
    /// Note that roster quality reaches the scoreboard through three separate
    /// channels: shot chances, turnovers, and offensive rebounds. Retuning only one
    /// of them moves these numbers far less than expected.
    /// </summary>
    [TestFixture]
    [Category("Statistical")]
    public sealed class CompetitiveBalanceTests
    {
        [Test]
        public void EvenlyMatchedTeams_ProduceCloseToACoinFlip()
        {
            var (winRate, margin) = SimulationTestHarness.PlayTalentCurveSeries(10, 10);

            Assert.That(winRate, Is.InRange(0.45, 0.62),
                "Identical rosters should be near even, allowing for a modest home court edge.");

            Assert.That(margin, Is.InRange(-1.5, 3.5),
                "Identical rosters should produce a small average margin. A large value means something " +
                "favours the home or away side beyond the intended home court bonus.");
        }

        [Test]
        public void SmallTalentEdge_IsAnAdvantageNotACertainty()
        {
            var (winRate, margin) = SimulationTestHarness.PlayTalentCurveSeries(12, 10);

            Assert.That(winRate, Is.InRange(0.55, 0.72),
                "A two point rating edge should make a team a favourite, not a lock. If this climbs toward " +
                "90 percent, upsets vanish and the season stops being competitive.");

            Assert.That(margin, Is.InRange(2.0, 7.5),
                "A two point rating edge should be worth a few points a game, not a blowout.");
        }

        [Test]
        public void ModerateTalentEdge_ProducesRegularButNotCertainWins()
        {
            var (winRate, margin) = SimulationTestHarness.PlayTalentCurveSeries(14, 10);

            Assert.That(winRate, Is.InRange(0.62, 0.83));
            Assert.That(margin, Is.InRange(4.5, 11.0));
        }

        [Test]
        public void LargeTalentEdge_StillLeavesRoomForUpsets()
        {
            var (winRate, _) = SimulationTestHarness.PlayTalentCurveSeries(18, 10);

            Assert.That(winRate, Is.InRange(0.78, 0.94),
                "Even a very large rating gap should lose occasionally. Real leagues see the best team beaten " +
                "by the worst roughly one game in ten.");
        }

        [Test]
        public void WinRate_IncreasesWithTalentGap()
        {
            var even = SimulationTestHarness.PlayTalentCurveSeries(10, 10).WinRate;
            var small = SimulationTestHarness.PlayTalentCurveSeries(12, 10).WinRate;
            var moderate = SimulationTestHarness.PlayTalentCurveSeries(14, 10).WinRate;
            var large = SimulationTestHarness.PlayTalentCurveSeries(18, 10).WinRate;

            Assert.That(small, Is.GreaterThan(even), "A better roster must win more often than an even one.");
            Assert.That(moderate, Is.GreaterThan(small), "Win rate must keep rising as the talent gap widens.");
            Assert.That(large, Is.GreaterThan(moderate), "Win rate must keep rising as the talent gap widens.");
        }

        [Test]
        public void BetterRoster_ScoresMoreAndConcedesLess()
        {
            var (_, margin) = SimulationTestHarness.PlayTalentCurveSeries(16, 8);

            Assert.That(margin, Is.GreaterThan(0.0),
                "A clearly stronger roster must outscore a clearly weaker one on average. Failing this means " +
                "an attribute is wired backwards somewhere in the simulation.");
        }
    }
}