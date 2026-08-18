using System;

namespace ProBasketballManager.Domain.Players
{
    public sealed class Player
    {
        public const int DefaultAge = 24;

        public int Id { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public PlayerPosition Position { get; }

        public PlayerAttributes Attributes { get; private set; }

        public int Age { get; private set; }

        public int Potential { get; private set; }

        public int ScoutedPotential { get; private set; }

        public string FullName => $"{FirstName} {LastName}";

        public int RemainingUpside => Math.Max(0, Potential - GetOverallRating(Attributes));

        public Player(int id, string firstName, string lastName, PlayerPosition position, PlayerAttributes attributes, int age = DefaultAge, int potential = 0, int scoutedPotential = 0)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Position = position;
            Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));

            Age = age;

            Potential = potential <= 0 ? EstimateCeiling(attributes) : PlayerAttributes.Clamp(potential);
            ScoutedPotential = scoutedPotential <= 0 ? Potential : PlayerAttributes.Clamp(scoutedPotential);
        }

        public void ApplyDevelopment(PlayerAttributes developedAttributes)
        {
            Attributes = developedAttributes ?? throw new ArgumentNullException(nameof(developedAttributes));
        }

        public void AdvanceAge()
        {
            Age++;
        }

        public static int GetOverallRating(PlayerAttributes attributes)
        {
            var total =
                attributes.Finishing + attributes.MidRange + attributes.ThreePoint + attributes.FreeThrow +
                attributes.Passing + attributes.BallHandling +
                attributes.PerimeterDefense + attributes.InteriorDefense +
                attributes.OffensiveRebounding + attributes.DefensiveRebounding +
                attributes.Speed + attributes.Strength + attributes.Stamina +
                attributes.BasketballIq;

            return PlayerAttributes.Clamp(total / 14.0);
        }

        public int Overall => GetOverallRating(Attributes);

        private static int EstimateCeiling(PlayerAttributes attributes)
        {
            return GetOverallRating(attributes);
        }
    }
}