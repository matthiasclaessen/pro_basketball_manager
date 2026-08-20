using System;

namespace ProBasketballManager.Domain.Players
{
    public static class PlayerRating
    {
        public const int MinimumAbility = 1;

        public const int MaximumAbility = 200;

        public const double AverageAbility = 105.0;

        public const double AbilityPerAttributePoint = 10.0;

        public const double CurveExponent = 1.70;

        public static int CalculateCurrentAbility(PlayerPosition position, PlayerAttributes attributes)
        {
            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            var weightedAverage = PositionWeights.GetWeightedAverage(position, attributes);

            return ClampAbility(MaximumAbility * Math.Pow(weightedAverage / PlayerAttributes.Maximum, CurveExponent));
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
            var clamped = Math.Clamp(attributePoints, 0.0, PlayerAttributes.Maximum);

            return ClampAbility(MaximumAbility * Math.Pow(clamped / PlayerAttributes.Maximum, CurveExponent));
        }

        public static double ToAttributePoints(int ability)
        {
            var clamped = Math.Clamp(ability, MinimumAbility, MaximumAbility);

            return PlayerAttributes.Maximum * Math.Pow((double)clamped / MaximumAbility, 1.0 / CurveExponent);
        }
    }
}
