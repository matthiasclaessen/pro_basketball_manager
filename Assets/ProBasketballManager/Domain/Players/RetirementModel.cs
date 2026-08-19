using System;
using ProBasketballManager.Domain.Matches;

namespace ProBasketballManager.Domain.Players
{
    public static class RetirementModel
    {
        public const int EarliestRetirementAge = 32;

        public const int MandatoryRetirementAge = 39;

        public const double BaseRetirementChance = 0.06;

        public const double RetirementChanceGrowth = 0.09;

        public const double QualityReprieve = 0.020;

        public static bool ShouldRetire(Player player, IRandomSource random)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (player.Age >= MandatoryRetirementAge)
            {
                return true;
            }

            if (player.Age < EarliestRetirementAge)
            {
                return false;
            }

            return random.NextDouble() < GetRetirementChance(player.Age, player.CurrentAbility);
        }

        public static double GetRetirementChance(int age, int currentAbility)
        {
            if (age >= MandatoryRetirementAge)
            {
                return 1.0;
            }

            if (age < EarliestRetirementAge)
            {
                return 0.0;
            }

            var yearsPast = age - EarliestRetirementAge;

            var chance = BaseRetirementChance + (yearsPast * RetirementChanceGrowth);

            chance -= (PlayerRating.ToAttributePoints(currentAbility) - PlayerAttributes.Average) * QualityReprieve;

            return Math.Max(0.0, Math.Min(1.0, chance));
        }
    }

    public static class ProspectGenerator
    {
        public const int MinimumEntryAge = 18;
        public const int MaximumEntryAge = 22;

        public const int MinimumStartingRating = 7;
        public const int MaximumStartingRating = 13;

        public const int MinimumUpside = 3;
        public const int MaximumUpside = 8;

        public const int MaximumScoutingError = 3;

        private static readonly string[] FirstNames =
        {
            "Liam", "Noah", "Milan", "Lucas", "Arthur", "Finn", "Louis", "Victor",
            "Adam", "Jules", "Mathis", "Elias", "Max", "Thomas", "Nicolas", "Julian",
            "Nathan", "Simon", "Robin", "Alex", "Wout", "Senne", "Vince", "Lars"
        };

        private static readonly string[] LastNames =
        {
            "Peeters", "Janssens", "Maes", "Jacobs", "Mertens", "Willems", "Claes",
            "Goossens", "Wouters", "De Smet", "Vermeulen", "Verhoeven", "Aerts",
            "Hermans", "Dubois", "Lambert", "Michiels", "Segers", "Coppens", "Pauwels"
        };

        public static Player Create(int id, PlayerPosition position, IRandomSource random)
        {
            var firstName = FirstNames[random.NextInt(0, FirstNames.Length)];
            var lastName = LastNames[random.NextInt(0, LastNames.Length)];

            var age = random.NextInt(MinimumEntryAge, MaximumEntryAge + 1);

            var baseRating = random.NextInt(MinimumStartingRating, MaximumStartingRating + 1);
            var upside = random.NextInt(MinimumUpside, MaximumUpside + 1);

            var potential = PlayerRating.FromAttributePoints(baseRating + upside);

            var scoutingError = random.NextInt(-MaximumScoutingError, MaximumScoutingError + 1);
            var scoutedPotential = PlayerRating.ClampAbility(potential + (int)(scoutingError * PlayerRating.AbilityPerAttributePoint));

            var attributes = CreateAttributes(baseRating, position, random);

            return new Player(id, firstName, lastName, position, attributes, age, potential, scoutedPotential);
        }

        private static PlayerAttributes CreateAttributes(int baseRating, PlayerPosition position, IRandomSource random)
        {
            int Roll(int bias)
            {
                var spread = random.NextInt(-2, 3);

                return PlayerAttributes.Clamp(baseRating + bias + spread);
            }

            var guard = position == PlayerPosition.PointGuard || position == PlayerPosition.ShootingGuard;
            var big = position == PlayerPosition.PowerForward || position == PlayerPosition.Center;

            return new PlayerAttributes(
                finishing: Roll(big ? 2 : 0),
                midRange: Roll(0),
                threePoint: Roll(guard ? 2 : -2),
                freeThrow: Roll(guard ? 1 : -1),
                passing: Roll(position == PlayerPosition.PointGuard ? 3 : guard ? 1 : -1),
                ballHandling: Roll(position == PlayerPosition.PointGuard ? 3 : guard ? 1 : -2),
                perimeterDefense: Roll(guard ? 2 : -1),
                interiorDefense: Roll(big ? 2 : -1),
                offensiveRebounding: Roll(big ? 2 : -2),
                defensiveRebounding: Roll(big ? 3 : -2),
                speed: Roll(guard ? 2 : -1),
                strength: Roll(big ? 3 : -1),
                stamina: Roll(0),
                basketballIq: Roll(-2));
        }
    }
}
