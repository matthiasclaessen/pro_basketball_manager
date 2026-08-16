using System;

namespace ProBasketballManager.Domain.Tactics
{
    public sealed class TeamTactics
    {
        public int Pace { get; }

        public int RimWeight { get; }

        public int MidRangeWeight { get; }

        public int ThreePointWeight { get; }

        public int BallMovement { get; }

        public int PerimeterPressure { get; }

        public int ProtectPaint { get; }

        public double RimShare => RimWeight / (double)ShotWeightTotal;

        public double MidRangeShare => MidRangeWeight / (double)ShotWeightTotal;

        public double ThreePointShare => ThreePointWeight / (double)ShotWeightTotal;

        private int ShotWeightTotal => RimWeight + MidRangeWeight + ThreePointWeight;

        public static TeamTactics Default => new TeamTactics(
            pace: 50,
            rimWeight: 45,
            midRangeWeight: 20,
            threePointWeight: 35,
            ballMovement: 50,
            perimeterPressure: 50,
            protectPaint: 50
        );

        public TeamTactics(int pace, int rimWeight, int midRangeWeight, int threePointWeight, int ballMovement, int perimeterPressure, int protectPaint)
        {
            Pace = Validate(pace, nameof(pace));
            RimWeight = Validate(rimWeight, nameof(rimWeight));
            MidRangeWeight = Validate(midRangeWeight, nameof(midRangeWeight));
            ThreePointWeight = Validate(threePointWeight, nameof(threePointWeight));
            BallMovement = Validate(ballMovement, nameof(ballMovement));
            PerimeterPressure = Validate(perimeterPressure, nameof(perimeterPressure));
            ProtectPaint = Validate(protectPaint, nameof(protectPaint));

            if (ShotWeightTotal == 0)
            {
                throw new ArgumentException("At least one shot profile weight must be greater than zero.");
            }
        }

        private static int Validate(int value, string parameterName)
        {
            if (value < 0 || value > 100)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Tactical values must be between 0 and 100.");
            }

            return value;
        }
    }
}