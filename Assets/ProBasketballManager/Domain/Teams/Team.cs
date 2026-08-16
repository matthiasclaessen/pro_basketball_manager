using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Players;

namespace ProBasketballManager.Domain.Teams
{
    public sealed class Team
    {
        private readonly List<Player> _players;

        public int Id { get; }

        public string Name { get; }

        public IReadOnlyList<Player> Players => _players;

        public Team(int id, string name, IEnumerable<Player> players)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "A team must have a name.",
                    nameof(name)
                );
            }

            Id = id;
            Name = name;

            _players = players.ToList();

            if (_players.Count < 5)
            {
                throw new ArgumentException(
                    "A basketball team must contain at least five players.",
                    nameof(players)
                );
            }
        }
    }
}