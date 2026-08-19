using System;

namespace ProBasketballManager.Domain.Players
{
    public static class PlayerRating
    {
        public const int MinimumAbility = 1;

        public const int MaximumAbility = 200;

        public const double AverageAbility = 105.0;

        public const double AbilityPerAttributePoint = 10.0;

        public static int CalculateCurrentAbility(PlayerPosition position, PlayerAttributes attributes)
        {
            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            var total =
                attributes.Finishing + attributes.MidRange + attributes.ThreePoint + attributes.FreeThrow +
                attributes.Passing + attributes.BallHandling +
                attributes.PerimeterDefense + attributes.InteriorDefense +
                attributes.OffensiveRebounding + attributes.DefensiveRebounding +
                attributes.Speed + attributes.Strength + attributes.Stamina +
                attributes.BasketballIq;

            return ClampAbility(total / 14.0 * AbilityPerAttributePoint);
        }

        public static int ClampAbility(int value)
        {
            return value < MinimumAbility ? MinimumAbility : value > MaximumAbility ? MaximumAbility : value;
        }

        public static int ClampAbility(double value)
        {
            return ClampAbility((int)Math.Round(value, MidpointRounding.AwayFromZero));
        }

        public static int FromAttributePoints(double attributePoints)
        {
            return ClampAbility(attributePoints * AbilityPerAttributePoint);
        }

        public static double ToAttributePoints(int ability)
        {
            return ability / AbilityPerAttributePoint;
        }
    }
}
