using System;

namespace ProBasketballManager.Domain.Players
{
    public sealed class PlayerAttributes
    {
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
            Finishing = finishing;
            MidRange = midRange;
            ThreePoint = threePoint;
            FreeThrow = freeThrow;

            Passing = passing;
            BallHandling = ballHandling;

            PerimeterDefense = perimeterDefense;
            InteriorDefense = interiorDefense;

            OffensiveRebounding = offensiveRebounding;
            DefensiveRebounding = defensiveRebounding;

            Speed = speed;
            Strength = strength;
            Stamina = stamina;

            BasketballIq = basketballIq;
        }

        private static int Validate(int value)
        {
            if (value < 1 || value > 20)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Player attributes must be between 1 and 20.");
            }

            return value;
        }
    }
}