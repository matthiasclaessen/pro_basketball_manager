using System.Linq;
using NUnit.Framework;
using ProBasketballManager.Domain.Demo;
using ProBasketballManager.Domain.Matches;

namespace ProBasketballManager.Domain.Tests
{
    /// <summary>
    /// Guards the random number generator.
    ///
    /// A raw xorshift32 generator returns heavily biased values for its first
    /// several draws when seeded with a small number, and small sequential numbers
    /// such as fixture IDs are exactly what a game like this uses as seeds. Before
    /// the warm up was added, every match seeded with a low number opened with the
    /// minimum possession count and gave the opening possession to the same side.
    ///
    /// That kind of bug produces no error and no crash. Only a test like this one
    /// makes it visible, which is why these checks are worth keeping permanently.
    /// </summary>
    [TestFixture]
    public sealed class RandomSourceTests
    {
        private const int SeedSampleSize = 20000;

        [Test]
        public void FirstDraw_IsUnbiasedAcrossSequentialSeeds()
        {
            var total = 0.0;

            for (var seed = 1u; seed <= SeedSampleSize; seed++)
            {
                total += new XorShiftRandom(seed).NextDouble();
            }

            Assert.That(total / SeedSampleSize, Is.EqualTo(0.5).Within(0.02),
                "The very first value from a freshly seeded generator averages far from 0.5. The generator " +
                "is not being advanced enough before use, which biases the opening of every simulated match.");
        }

        [Test]
        public void FirstDraw_IsEvenlySpreadAcrossItsRange()
        {
            var buckets = new int[10];

            for (var seed = 1u; seed <= SeedSampleSize; seed++)
            {
                var value = new XorShiftRandom(seed).NextDouble();
                var bucket = (int)(value * 10);

                buckets[bucket == 10 ? 9 : bucket]++;
            }

            var expected = SeedSampleSize / 10.0;

            foreach (var count in buckets)
            {
                Assert.That(count, Is.EqualTo(expected).Within(expected * 0.25),
                    "First draws from sequential seeds are clustering into part of the range instead of " +
                    "spreading evenly across it.");
            }
        }

        [Test]
        public void SequentialSeeds_ProduceIndependentFirstIntegers()
        {
            var values = Enumerable.Range(1, 200)
                .Select(seed => new XorShiftRandom((uint)seed).NextInt(34, 42))
                .ToList();

            Assert.That(values.Distinct().Count(), Is.GreaterThan(4),
                "Sequential seeds are all producing the same first integer. This was the original bug: " +
                "every match began with an identical possession count.");

            Assert.That(values.Average(), Is.EqualTo(37.5).Within(1.0),
                "First integers from sequential seeds are skewed toward one end of the requested range.");
        }

        [Test]
        public void NextDouble_StaysWithinTheUnitInterval()
        {
            var random = new XorShiftRandom(12345u);

            for (var draw = 0; draw < 50000; draw++)
            {
                var value = random.NextDouble();

                Assert.That(value, Is.GreaterThanOrEqualTo(0.0));
                Assert.That(value, Is.LessThan(1.0));
            }
        }

        [Test]
        public void NextInt_StaysWithinTheRequestedRange()
        {
            var random = new XorShiftRandom(6789u);

            for (var draw = 0; draw < 50000; draw++)
            {
                var value = random.NextInt(34, 42);

                Assert.That(value, Is.GreaterThanOrEqualTo(34));
                Assert.That(value, Is.LessThan(42), "The upper bound is exclusive.");
            }
        }

        [Test]
        public void ZeroSeed_DoesNotCollapseTheGenerator()
        {
            var random = new XorShiftRandom(0u);
            var values = Enumerable.Range(0, 100).Select(_ => random.NextDouble()).ToList();

            Assert.That(values.Distinct().Count(), Is.GreaterThan(90),
                "A zero seed must fall back to a valid state. An all zero xorshift state is a fixed point " +
                "that returns the same value forever.");
        }

        [Test]
        public void SameSeed_ProducesAnIdenticalMatch()
        {
            var teams = DemoLeagueFactory.Create().Teams;

            var first = new MatchSimulator(new XorShiftRandom(777u)).Simulate(teams[0], teams[1]);
            var second = new MatchSimulator(new XorShiftRandom(777u)).Simulate(teams[0], teams[1]);

            Assert.That(second.HomeScore, Is.EqualTo(first.HomeScore));
            Assert.That(second.AwayScore, Is.EqualTo(first.AwayScore));
            Assert.That(second.Events.Count, Is.EqualTo(first.Events.Count));

            for (var index = 0; index < first.HomePlayerStats.Count; index++)
            {
                var expected = first.HomePlayerStats[index];
                var actual = second.HomePlayerStats[index];

                Assert.That(actual.Points, Is.EqualTo(expected.Points));
                Assert.That(actual.Assists, Is.EqualTo(expected.Assists));
                Assert.That(actual.SecondsPlayed, Is.EqualTo(expected.SecondsPlayed).Within(1e-9));
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentMatches()
        {
            var teams = DemoLeagueFactory.Create().Teams;

            var scores = Enumerable.Range(1, 20)
                .Select(seed => new MatchSimulator(new XorShiftRandom((uint)seed)).Simulate(teams[0], teams[1]).HomeScore)
                .ToList();

            Assert.That(scores.Distinct().Count(), Is.GreaterThan(8),
                "Twenty different seeds should not collapse into a handful of identical scores.");
        }
    }
}