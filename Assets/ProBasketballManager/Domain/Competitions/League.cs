using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Competitions
{
    public sealed class League
    {
        private readonly List<Team> _teams;

        public int Id { get; }

        public string Name { get; }

        public IReadOnlyList<Team> Teams => _teams;

        public CompetitionCalendar Calendar { get; }

        public League(int id, string name, IEnumerable<Team> teams, CompetitionCalendar calendar = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A league must have a name.", nameof(name)
                );
            }

            Id = id;
            Name = name;
            Calendar = calendar ?? CompetitionCalendar.Default;

            _teams = teams.ToList();

            if (_teams.Count < 2)
            {
                throw new ArgumentException("A league must contain at least two teams.", nameof(teams)
                );
            }
        }
    }
}
