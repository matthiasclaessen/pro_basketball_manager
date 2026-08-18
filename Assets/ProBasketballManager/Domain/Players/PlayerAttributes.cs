using System;

namespace ProBasketballManager.Domain.Players
{
    public sealed class PlayerAttributes
    {
        public const int Minimum = 1;
        public const int Maximum = 20;

        public const double Average = 10.5;

        public int Finishing { get; }
        public int MidRange { get; }
        public int ThreePoint { get; }
        public int FreeThrow { get; }

        public int Passing { get; }
        public int BallHandling { get; }

        public int PerimeterDefense { get; }
        public int InteriorDefense { get; }

        public int OffensiveRebounding { get; }
        public int DefensiveRebounding { get; }

        public int Speed { get; }
        public int Strength { get; }
        public int Stamina { get; }

        public int BasketballIq { get; }

        public PlayerAttributes(int finishing, int midRange, int threePoint, int freeThrow, int passing, int ballHandling, int perimeterDefense, int interiorDefense, int offensiveRebounding, int defensiveRebounding, int speed, int strength, int stamina, int basketballIq)
        {
            Finishing = Validate(finishing, nameof(finishing));
            MidRange = Validate(midRange, nameof(midRange));
            ThreePoint = Validate(threePoint, nameof(threePoint));
            FreeThrow = Validate(freeThrow, nameof(freeThrow));

            Passing = Validate(passing, nameof(passing));
            BallHandling = Validate(ballHandling, nameof(ballHandling));

            PerimeterDefense = Validate(perimeterDefense, nameof(perimeterDefense));
            InteriorDefense = Validate(interiorDefense, nameof(interiorDefense));

            OffensiveRebounding = Validate(offensiveRebounding, nameof(offensiveRebounding));
            DefensiveRebounding = Validate(defensiveRebounding, nameof(defensiveRebounding));

            Speed = Validate(speed, nameof(speed));
            Strength = Validate(strength, nameof(strength));
            Stamina = Validate(stamina, nameof(stamina));

            BasketballIq = Validate(basketballIq, nameof(basketballIq));
        }

        private static int Validate(int value, string parameterName)
        {
            if (value < Minimum || value > Maximum)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, $"Player attributes must be between {Minimum} and {Maximum}.");
            }

            return value;
        }

        public static int Clamp(int value)
        {
            return value < Minimum ? Minimum : value > Maximum ? Maximum : value;
        }

        public static int Clamp(double value)
        {
            return Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero));
        }
    }
}