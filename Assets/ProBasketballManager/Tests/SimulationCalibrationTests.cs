using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProBasketballManager.Domain.Matches;

namespace ProBasketballManager.Domain.Tests
{
    /// <summary>
    /// Checks that simulated output resembles real basketball.
    ///
    /// These are calibration tests, not correctness tests. The simulator can be
    /// completely free of bugs and still fail every one of them: they exist to
    /// catch the moment a new feature quietly drags scoring, shooting or pace away
    /// from realistic values.
    ///
    /// Reference ranges are FIBA / European league values, because the simulator
    /// plays four ten-minute periods with five-minute overtimes. If the game is
    /// ever switched to NBA rules, every band here has to move as well.
    ///
    /// Tolerances are deliberately wide. Measured variation between independent
    /// batches of this size is well under one point, so a failure means the
    /// simulator genuinely changed, not that the sample was unlucky.
    /// </summary>
    [TestFixture]
    [Category("Statistical")]
    public sealed class SimulationCalibrationTests
    {
        private IReadOnlyList<IReadOnlyList<PlayerBoxScore>> _boxScores;
        private List<int> _teamScores;

        private double _fieldGoalsMade;
        private double _fieldGoalsAttempted;
        private double _threePointsMade;
        private double _threePointsAttempted;
        private double _freeThrowsMade;
        private double _freeThrowsAttempted;
        private double _offensiveRebounds;
        private double _defensiveRebounds;

        /// <summary>
        /// Runs the batch once and shares it across every test in this fixture.
        /// Simulating separately per test would multiply the runtime by the number
        /// of assertions for no benefit.
        /// </summary>
        [OneTimeSetUp]
        public void SimulateSharedBatch()
        {
            var results = SimulationTestHarness.SimulateLeagueBatch(SimulationTestHarness.StatisticalSampleSize);

            _boxScores = SimulationTestHarness.ToTeamBoxScores(results);

            _teamScores = new List<int>();

            foreach (var result in results)
            {
                _teamScores.Add(result.HomeScore);
                _teamScores.Add(result.AwayScore);
            }

            _fieldGoalsMade = SimulationTestHarness.PerGame(_boxScores, box => box.FieldGoalsMade);
            _fieldGoalsAttempted = SimulationTestHarness.PerGame(_boxScores, box => box.FieldGoalsAttempted);
            _threePointsMade = SimulationTestHarness.PerGame(_boxScores, box => box.ThreePointsMade);
            _threePointsAttempted = SimulationTestHarness.PerGame(_boxScores, box => box.ThreePointsAttempted);
            _freeThrowsMade = SimulationTestHarness.PerGame(_boxScores, box => box.FreeThrowsMade);
            _freeThrowsAttempted = SimulationTestHarness.PerGame(_boxScores, box => box.FreeThrowsAttempted);
            _offensiveRebounds = SimulationTestHarness.PerGame(_boxScores, box => box.OffensiveRebounds);
            _defensiveRebounds = SimulationTestHarness.PerGame(_boxScores, box => box.DefensiveRebounds);
        }

        [Test]
        public void MeanTeamScore_MatchesFibaScoringLevels()
        {
            Assert.That(_teamScores.Average(), Is.InRange(76.0, 83.0),
                "Mean team score has drifted away from FIBA scoring levels. The usual cause is a change to " +
                "possession count or to the per-zone base shot chances in MatchSimulator.");
        }

        [Test]
        public void TeamScoreSpread_ProducesBothCloseAndLopsidedGames()
        {
            var mean = _teamScores.Average();
            var standardDeviation = System.Math.Sqrt(_teamScores.Select(score => (score - mean) * (score - mean)).Average());

            Assert.That(standardDeviation, Is.InRange(8.0, 14.0),
                "Score variance has drifted. Too low and every game is a coin flip decided by ratings alone; " +
                "too high and results stop reflecting team quality.");
        }

        [Test]
        public void PossessionsPerGame_FitInsideFortyMinutes()
        {
            Assert.That(SimulationTestHarness.EstimatePossessions(_boxScores), Is.InRange(72.0, 78.0),
                "Possessions per team are outside the range that fits in 40 minutes of basketball. " +
                "Check MinimumPeriodPossessions and MaximumPeriodPossessions in MatchSimulator.");
        }

        [Test]
        public void OffensiveRating_IsRealistic()
        {
            var possessions = SimulationTestHarness.EstimatePossessions(_boxScores);
            var offensiveRating = _teamScores.Average() / possessions * 100.0;

            Assert.That(offensiveRating, Is.InRange(101.0, 112.0),
                "Points per 100 possessions is outside the realistic range. This separates a pace problem " +
                "from an efficiency problem: if possessions pass but this fails, the shot chances are wrong.");
        }

        [Test]
        public void FieldGoalPercentage_IsRealistic()
        {
            Assert.That(_fieldGoalsMade / _fieldGoalsAttempted, Is.InRange(0.440, 0.475),
                "Field goal percentage has drifted. Check the per-zone base chances in CalculateShotChance.");
        }

        [Test]
        public void ThreePointPercentage_IsRealistic()
        {
            Assert.That(_threePointsMade / _threePointsAttempted, Is.InRange(0.325, 0.360),
                "Three point percentage has drifted. Check CornerThreeBaseChance and AboveBreakThreeBaseChance.");
        }

        [Test]
        public void FreeThrowPercentage_IsRealistic()
        {
            Assert.That(_freeThrowsMade / _freeThrowsAttempted, Is.InRange(0.745, 0.805),
                "Free throw percentage has drifted. Check the make chance in SimulateShootingFoul.");
        }

        [Test]
        public void FreeThrowRate_ShowsSensibleFoulVolume()
        {
            Assert.That(_freeThrowsAttempted / _fieldGoalsAttempted, Is.InRange(0.22, 0.32),
                "Free throw attempts relative to field goal attempts have drifted. " +
                "Check the per-zone foul chances in SimulateShootingFoul.");
        }

        [Test]
        public void ReboundTotals_AreRealistic()
        {
            Assert.That(_offensiveRebounds + _defensiveRebounds, Is.InRange(32.0, 39.0),
                "Total rebounds per team have drifted. This tracks missed shots, so it usually moves " +
                "when shooting percentages or possession counts change.");
        }

        [Test]
        public void OffensiveReboundShare_IsRealistic()
        {
            var share = _offensiveRebounds / (_offensiveRebounds + _defensiveRebounds);

            Assert.That(share, Is.InRange(0.20, 0.30),
                "Offensive rebound share has drifted. Check the base chance in SimulateRebound.");
        }

        [Test]
        public void AssistsPerGame_AreRealistic()
        {
            Assert.That(SimulationTestHarness.PerGame(_boxScores, box => box.Assists), Is.InRange(14.5, 19.0),
                "Assists per game have drifted. Check the base chances in TryCreateAssist.");
        }

        [Test]
        public void AssistedFieldGoalShare_IsRealistic()
        {
            var assists = SimulationTestHarness.PerGame(_boxScores, box => box.Assists);

            Assert.That(assists / _fieldGoalsMade, Is.InRange(0.50, 0.66),
                "The share of made field goals that are assisted has drifted. This is a better guide than raw " +
                "assist counts because it stays meaningful when pace changes.");
        }

        [Test]
        public void TurnoversPerGame_AreRealistic()
        {
            Assert.That(SimulationTestHarness.PerGame(_boxScores, box => box.Turnovers), Is.InRange(9.0, 13.0),
                "Turnovers per game have drifted. Check the base chance in SimulateTurnover.");
        }

        [Test]
        public void StealsPerGame_AreRealistic()
        {
            Assert.That(SimulationTestHarness.PerGame(_boxScores, box => box.Steals), Is.InRange(5.5, 8.0),
                "Steals per game have drifted. Steals are a share of turnovers, so check SimulateTurnover.");
        }
    }
}