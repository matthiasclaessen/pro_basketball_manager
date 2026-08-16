namespace ProBasketballManager.Domain.Players
{
    public sealed class Player
    {
        public int Id { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public PlayerPosition Position { get; }

        public PlayerAttributes Attributes { get; }

        public string FullName => $"{FirstName} {LastName}";

        public Player(int id, string firstName, string lastName, PlayerPosition position, PlayerAttributes attributes)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Position = position;
            Attributes = attributes;
        }
    }
}