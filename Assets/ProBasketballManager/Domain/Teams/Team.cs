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

        public int ClubId { get; }

        public TeamType Type { get; }

        public IReadOnlyList<Player> Players => _players;

        public Team(int id, string name, IEnumerable<Player> players, int clubId = 0, TeamType type = TeamType.First)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A team must have a name.", nameof(name));
            }

            if (players == null)
            {
                throw new ArgumentNullException(nameof(players));
            }

            Id = id;
            Name = name;
            ClubId = clubId;
            Type = type;

            _players = players.ToList();

            if (_players.Count < 5)
            {
                throw new ArgumentException("A basketball team must contain at least five players.", nameof(players));
            }

            var duplicates = _players.GroupBy(player => player.Id).Where(group => group.Count() > 1).Select(group => group.Key).ToList();

            if (duplicates.Count > 0)
            {
                throw new ArgumentException($"{name} lists player {duplicates[0]} more than once.", nameof(players));
            }
        }

        public bool HasPlayer(int playerId)
        {
            return _players.Any(player => player.Id == playerId);
        }

        public void ReplacePlayer(Player leaving, Player arriving)
        {
            if (leaving == null)
            {
                throw new ArgumentNullException(nameof(leaving));
            }

            if (arriving == null)
            {
                throw new ArgumentNullException(nameof(arriving));
            }

            var index = _players.FindIndex(player => player.Id == leaving.Id);

            if (index < 0)
            {
                throw new ArgumentException($"{leaving.FullName} is not on {Name}.", nameof(leaving));
            }

            _players[index] = arriving;
        }

        public int GetHighestPlayerId()
        {
            return _players.Max(player => player.Id);
        }
    }
}
