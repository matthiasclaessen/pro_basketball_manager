using System;
using System.Collections.Generic;
using System.Linq;

namespace ProBasketballManager.Domain.Players
{
    public static class StarRating
    {
        public const double Minimum = 0.5;

        public const double Maximum = 5.0;

        private static readonly (double Stars, double TopFraction)[] Thresholds =
        {
            (5.0, 0.02),
            (4.5, 0.05),
            (4.0, 0.10),
            (3.5, 0.18),
            (3.0, 0.30),
            (2.5, 0.45),
            (2.0, 0.62),
            (1.5, 0.78),
            (1.0, 0.90)
        };

        public static IReadOnlyList<int> BuildReference(IEnumerable<Player> population)
        {
            if (population == null)
            {
                throw new ArgumentNullException(nameof(population));
            }

            return population.Select(player => player.CurrentAbility).OrderByDescending(ability => ability).ToList();
        }

        public static double FromTopFraction(double topFraction)
        {
            foreach (var threshold in Thresholds)
            {
                if (topFraction <= threshold.TopFraction)
                {
                    return threshold.Stars;
                }
            }

            return Minimum;
        }

        public static double Calculate(int ability, IReadOnlyList<int> descendingReference)
        {
            if (descendingReference == null)
            {
                throw new ArgumentNullException(nameof(descendingReference));
            }

            if (descendingReference.Count == 0)
            {
                return Minimum;
            }

            var better = 0;

            foreach (var entry in descendingReference)
            {
                if (entry <= ability)
                {
                    break;
                }

                better++;
            }

            return FromTopFraction((better + 0.5) / descendingReference.Count);
        }

        public static string Format(double stars)
        {
            return stars.ToString("0.#");
        }
    }
}
