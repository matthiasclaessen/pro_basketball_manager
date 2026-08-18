using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Clubs
{
    public sealed class Club
    {
        private readonly List<Player> _squad;
        private readonly List<Team> _teams;

        public int Id { get; }

        public string Name { get; }

        public IReadOnlyList<Player> Squad => _squad;

        public IReadOnlyList<Team> Teams => _teams;

        public Team FirstTeam => GetTeam(TeamType.First);

        public Club(int id, string name, IEnumerable<Player> squad, IEnumerable<Team> teams)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A club must have a name.", nameof(name));
            }

            if (squad == null)
            {
                throw new ArgumentNullException(nameof(squad));
            }

            if (teams == null)
            {
                throw new ArgumentNullException(nameof(teams));
            }

            Id = id;
            Name = name;

            _squad = squad.ToList();
            _teams = teams.ToList();

            Validate();
        }

        public Team GetTeam(TeamType type)
        {
            var team = _teams.FirstOrDefault(candidate => candidate.Type == type);

            if (team == null)
            {
                throw new InvalidOperationException($"{Name} does not field a {type} team.");
            }

            return team;
        }

        public bool HasTeam(TeamType type)
        {
            return _teams.Any(candidate => candidate.Type == type);
        }

        public IReadOnlyList<Team> GetTeamsFor(int playerId)
        {
            return _teams.Where(team => team.Players.Any(player => player.Id == playerId)).ToList();
        }

        public Team GetPrimaryTeamFor(int playerId)
        {
            return _teams.Where(team => team.Players.Any(player => player.Id == playerId)).OrderBy(team => team.Type).FirstOrDefault();
        }

        public bool IsDualRegistered(int playerId)
        {
            return GetTeamsFor(playerId).Count > 1;
        }

        public Player GetPlayer(int playerId)
        {
            var player = _squad.FirstOrDefault(candidate => candidate.Id == playerId);

            if (player == null)
            {
                throw new ArgumentException($"Player {playerId} is not at {Name}.", nameof(playerId));
            }

            return player;
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

            var index = _squad.FindIndex(player => player.Id == leaving.Id);

            if (index < 0)
            {
                throw new ArgumentException($"{leaving.FullName} is not at {Name}.", nameof(leaving));
            }

            _squad[index] = arriving;

            foreach (var team in _teams.Where(team => team.Players.Any(player => player.Id == leaving.Id)))
            {
                team.ReplacePlayer(leaving, arriving);
            }
        }

        public int GetHighestPlayerId()
        {
            return _squad.Max(player => player.Id);
        }

        private void Validate()
        {
            if (_squad.Count == 0)
            {
                throw new ArgumentException($"{Name} has no players.");
            }

            var duplicateSquadIds = _squad.GroupBy(player => player.Id).Where(group => group.Count() > 1).Select(group => group.Key).ToList();

            if (duplicateSquadIds.Count > 0)
            {
                throw new ArgumentException($"{Name} lists player {duplicateSquadIds[0]} more than once in its squad.");
            }

            if (_teams.Count == 0)
            {
                throw new ArgumentException($"{Name} does not field any teams.");
            }

            if (_teams.Count(team => team.Type == TeamType.First) != 1)
            {
                throw new ArgumentException($"{Name} must field exactly one first team.");
            }

            var duplicateTeamIds = _teams.GroupBy(team => team.Id).Where(group => group.Count() > 1).Select(group => group.Key).ToList();

            if (duplicateTeamIds.Count > 0)
            {
                throw new ArgumentException($"{Name} lists team {duplicateTeamIds[0]} more than once.");
            }

            var squadById = _squad.ToDictionary(player => player.Id);

            foreach (var team in _teams)
            {
                if (team.ClubId != Id)
                {
                    throw new ArgumentException($"{team.Name} belongs to club {team.ClubId}, not to {Name}.");
                }

                foreach (var player in team.Players)
                {
                    if (!squadById.TryGetValue(player.Id, out var squadPlayer))
                    {
                        throw new ArgumentException($"{player.FullName} plays for {team.Name} but is not in the {Name} squad.");
                    }

                    if (!ReferenceEquals(player, squadPlayer))
                    {
                        throw new ArgumentException($"{player.FullName} is a duplicate instance on {team.Name}; teams must share the squad's player objects.");
                    }
                }
            }

            var unregistered = _squad.Where(player => GetTeamsFor(player.Id).Count == 0).ToList();

            if (unregistered.Count > 0)
            {
                throw new ArgumentException($"{unregistered[0].FullName} is at {Name} but is not registered to any team.");
            }
        }
    }
}
