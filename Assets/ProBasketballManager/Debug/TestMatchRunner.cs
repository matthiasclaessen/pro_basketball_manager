using ProBasketballManager.Domain.Demo;
using ProBasketballManager.Domain.Matches;
using UnityEngine;

namespace ProBasketballManager.Debugging
{
    public sealed class TestMatchRunner : MonoBehaviour
    {
        [SerializeField]
        private int seed = 12345;

        private void Start()
        {
            var league = DemoLeagueFactory.Create();

            var homeTeam = league.Teams[0];

            var awayTeam = league.Teams[1];

            var random = new XorShiftRandom((uint)seed);

            var simulator = new MatchSimulator(random);

            var result = simulator.Simulate(homeTeam, awayTeam);

            Debug.Log(
                $"{result.HomeTeam.Name} " +
                $"{result.HomeScore} - " +
                $"{result.AwayScore} " +
                $"{result.AwayTeam.Name}"
            );

            for (var period = 0; period < result.HomePeriodScores.Count; period++)
            {
                var periodName = period < 4 ? $"Q{period + 1}" : $"OT{period - 3}";

                Debug.Log(
                    $"{periodName}: " +
                    $"{result.HomePeriodScores[period]} - " +
                    $"{result.AwayPeriodScores[period]}"
                );
            }
        }
    }
}