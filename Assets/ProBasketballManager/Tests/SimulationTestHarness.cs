using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Demo;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Tactics;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Tests
{
    /// <summary>
    /// Shared helpers for the simulation tests.
    ///
    /// Every method here takes an explicit seed, so the tests are fully
    /// deterministic: a given build of the simulator always produces exactly the
    /// same numbers. That means a failure is always a real change in behaviour
    /// and never bad luck.
    /// </summary>
    public static class SimulationTestHarness
    {
        /// <summary>
        /// Games used by the statistical tests. A single basketball game is far
        /// too noisy to judge a tuning constant by, so the aggregate tests average
        /// over a large batch. Measured spread across independent batches of this
        /// size is under one point of scoring, which is comfortably inside the
        /// tolerance bands the tests assert.
        /// </summary>
        public const int StatisticalSampleSize = 1200;

        /// <summary>Games used per matchup in the competitive balance tests.</summary>
        public const int TalentCurveSampleSize = 400;

        /// <summary>
        /// Simulates a batch of games between every pair of teams in the demo
        /// league, so no single roster dominates the aggregate.
        /// </summary>
        public static IReadOnlyList<MatchResult> SimulateLeagueBatch(int gameCount, uint startingSeed = 1u)
        {
            var teams = DemoLeagueFactory.Create().Teams;
            var results = new List<MatchResult>(gameCount);

            var seedOffset = 0u;

            while (results.Count < gameCount)
            {
                for (var home = 0; home < teams.Count && results.Count < gameCount; home++)
                {
                    for (var away = 0; away < teams.Count && results.Count < gameCount; away++)
                    {
                        if (home == away)
                        {
                            continue;
                        }

                        var seed = startingSeed + (seedOffset * 31u) + (uint)((home * 4) + away);
                        var simulator = new MatchSimulator(new XorShiftRandom(seed));

                        results.Add(simulator.Simulate(teams[home], teams[away]));
                    }
                }

                seedOffset++;
            }

            return results;
        }

        /// <summary>
        /// Flattens a batch of results into one box score per team per game, which
        /// is the unit the per-game averages are computed over.
        /// </summary>
        public static IReadOnlyList<IReadOnlyList<PlayerBoxScore>> ToTeamBoxScores(IEnumerable<MatchResult> results)
        {
            var boxScores = new List<IReadOnlyList<PlayerBoxScore>>();

            foreach (var result in results)
            {
                boxScores.Add(result.HomePlayerStats);
                boxScores.Add(result.AwayPlayerStats);
            }

            return boxScores;
        }

        /// <summary>Average team total of a counting stat, per game.</summary>
        public static double PerGame(IEnumerable<IReadOnlyList<PlayerBoxScore>> boxScores, Func<PlayerBoxScore, int> selector)
        {
            return boxScores.Average(box => box.Sum(selector));
        }

        /// <summary>
        /// Standard possession estimate. Free throws are counted at 0.44 because
        /// only some trips to the line end a possession.
        /// </summary>
        public static double EstimatePossessions(IEnumerable<IReadOnlyList<PlayerBoxScore>> boxScores)
        {
            var list = boxScores.ToList();

            return PerGame(list, box => box.FieldGoalsAttempted)
                + PerGame(list, box => box.Turnovers)
                + (0.44 * PerGame(list, box => box.FreeThrowsAttempted))
                - PerGame(list, box => box.OffensiveRebounds);
        }

        /// <summary>
        /// Builds a roster where every player has identical attributes. Used by the
        /// competitive balance tests so that a single number describes the whole
        /// team's quality and nothing else varies between the two sides.
        /// </summary>
        public static Team CreateUniformTeam(int teamId, string name, int rating)
        {
            var positions = (PlayerPosition[])Enum.GetValues(typeof(PlayerPosition));
            var players = new List<Player>();

            for (var index = 0; index < 10; index++)
            {
                var attributes = new PlayerAttributes(
                    rating, rating, rating, rating,
                    rating, rating,
                    rating, rating,
                    rating, rating,
                    rating, rating, rating,
                    rating
                );

                players.Add(new Player(
                    (teamId * 1000) + index,
                    "Player" + index,
                    "Team" + teamId,
                    positions[index % positions.Length],
                    attributes
                ));
            }

            return new Team(teamId, name, players);
        }

        /// <summary>
        /// Plays two uniformly rated teams against each other and reports how often
        /// the stronger side wins, plus the average points margin.
        /// </summary>
        public static (double WinRate, double AverageMargin) PlayTalentCurveSeries(int homeRating, int awayRating, int gameCount = TalentCurveSampleSize)
        {
            var home = CreateUniformTeam(91, "Home Reference", homeRating);
            var away = CreateUniformTeam(92, "Away Reference", awayRating);

            var wins = 0;
            var marginTotal = 0;

            for (var game = 0; game < gameCount; game++)
            {
                var seed = 1u + ((uint)game * 7919u);
                var result = new MatchSimulator(new XorShiftRandom(seed)).Simulate(home, away, TeamTactics.Default, TeamTactics.Default);

                if (result.HomeScore > result.AwayScore)
                {
                    wins++;
                }

                marginTotal += result.HomeScore - result.AwayScore;
            }

            return (wins / (double)gameCount, marginTotal / (double)gameCount);
        }
    }
}