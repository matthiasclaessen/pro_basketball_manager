using System;

namespace ProBasketballManager.Domain.Players
{
    public static class PositionWeights
    {
        public const int AttributeCount = 14;

        private static readonly double[] PointGuard = { 9, 11, 14, 10, 17, 17, 13, 4, 3, 5, 13, 5, 10, 15 };
        private static readonly double[] ShootingGuard = { 13, 15, 19, 12, 10, 13, 16, 5, 4, 6, 14, 7, 11, 12 };
        private static readonly double[] SmallForward = { 16, 14, 16, 10, 9, 10, 16, 8, 6, 9, 12, 10, 11, 11 };
        private static readonly double[] PowerForward = { 15, 10, 10, 8, 6, 5, 8, 16, 12, 15, 7, 14, 9, 10 };
        private static readonly double[] Center = { 15, 8, 5, 7, 7, 5, 7, 17, 13, 16, 6, 14, 9, 10 };

        public static double[] For(PlayerPosition position)
        {
            switch (position)
            {
                case PlayerPosition.PointGuard: return PointGuard;
                case PlayerPosition.ShootingGuard: return ShootingGuard;
                case PlayerPosition.SmallForward: return SmallForward;
                case PlayerPosition.PowerForward: return PowerForward;
                case PlayerPosition.Center: return Center;
                default: throw new ArgumentOutOfRangeException(nameof(position), position, "No attribute weights are defined for this position.");
            }
        }

        public static double[] ToArray(PlayerAttributes attributes)
        {
            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            return new double[]
            {
                attributes.Finishing, attributes.MidRange, attributes.ThreePoint, attributes.FreeThrow,
                attributes.Passing, attributes.BallHandling,
                attributes.PerimeterDefense, attributes.InteriorDefense,
                attributes.OffensiveRebounding, attributes.DefensiveRebounding,
                attributes.Speed, attributes.Strength, attributes.Stamina, attributes.BasketballIq
            };
        }

        public static double GetWeightedAverage(PlayerPosition position, PlayerAttributes attributes)
        {
            var weights = For(position);
            var values = ToArray(attributes);

            var total = 0.0;
            var weightTotal = 0.0;

            for (var i = 0; i < AttributeCount; i++)
            {
                total += values[i] * weights[i];
                weightTotal += weights[i];
            }

            return total / weightTotal; ;
        }
    }
}
