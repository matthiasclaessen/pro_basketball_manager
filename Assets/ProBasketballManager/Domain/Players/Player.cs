using System;
using System.Linq;

namespace ProBasketballManager.Domain.Players
{
    public sealed class Player
    {
        public const int DefaultAge = 24;

        public int Id { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public PlayerPosition Position { get; }

        public string Nationality { get; }

        public PlayerAttributes Attributes { get; private set; }

        public int Age { get; private set; }

        public int Potential { get; private set; }

        public int ScoutedPotential { get; private set; }

        public string FullName => $"{FirstName} {LastName}";

        public int CurrentAbility => PlayerRating.CalculateCurrentAbility(Position, Attributes);

        public int Overall => PlayerAttributes.Clamp(PlayerRating.ToAttributePoints(CurrentAbility));

        public int RemainingUpside => Math.Max(0, Potential - CurrentAbility);

        public const string DefaultNationality = "BEL";

        public Player(int id, string firstName, string lastName, PlayerPosition position, PlayerAttributes attributes, int age = DefaultAge, int potential = 0, int scoutedPotential = 0, string nationality = DefaultNationality)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Position = position;
            Nationality = NormaliseNationality(nationality);
            Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));

            Age = age;

            Potential = potential <= 0 ? EstimateCeiling(position, attributes) : PlayerRating.ClampAbility(potential);
            ScoutedPotential = scoutedPotential <= 0 ? Potential : PlayerRating.ClampAbility(scoutedPotential);
        }

        public void ApplyDevelopment(PlayerAttributes developedAttributes)
        {
            Attributes = developedAttributes ?? throw new ArgumentNullException(nameof(developedAttributes));
        }

        public void AdvanceAge()
        {
            Age++;
        }

        private static string NormaliseNationality(string nationality)
        {
            if (string.IsNullOrWhiteSpace(nationality))
            {
                return DefaultNationality;
            }

            var trimmed = nationality.Trim().ToUpperInvariant();

            if (trimmed.Length != 3 || !trimmed.All(char.IsLetter))
            {
                throw new ArgumentException($"'{nationality}' is not a three-letter country code.", nameof(nationality));
            }

            return trimmed;
        }

        private static int EstimateCeiling(PlayerPosition position, PlayerAttributes attributes)
        {
            return PlayerRating.CalculateCurrentAbility(position, attributes);
        }
    }
}
