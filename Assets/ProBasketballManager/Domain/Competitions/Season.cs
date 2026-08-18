using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Competitions
{
    public sealed class Season
    {
        private readonly List<Fixture> _fixtures;

        public int Id { get; }

        public CompetitionRules Rules { get; }

        public string Name { get; }

        public League League { get; }

        public IReadOnlyList<Fixture> Fixtures => _fixtures;

        public int TotalRounds => _fixtures.Count == 0 ? 0 : _fixtures.Max(fixture => fixture.RoundNumber);

        public bool IsComplete => _fixtures.Count > 0 && _fixtures.All(fixture => fixture.IsPlayed);

        public int CurrentRoundNumber
        {
            get
            {
                var nextFixture = _fixtures
                    .Where(fixture => !fixture.IsPlayed)
                    .OrderBy(fixture => fixture.RoundNumber)
                    .ThenBy(fixture => fixture.Id)
                    .FirstOrDefault();

                return nextFixture?.RoundNumber ?? TotalRounds;
            }
        }

        public Season(int id, string name, League league, IEnumerable<Fixture> fixtures, CompetitionRules rules = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A season must have a name.", nameof(name));
            }

            Id = id;
            Rules = rules ?? CompetitionRules.Fiba;
            Name = name;
            League = league ?? throw new ArgumentNullException(nameof(league));

            _fixtures = fixtures?.ToList() ?? throw new ArgumentNullException(nameof(fixtures));

            ValidateFixtures();
        }

        public IReadOnlyList<Fixture> GetFixturesForRound(int roundNumber)
        {
            return _fixtures
                .Where(fixture => fixture.RoundNumber == roundNumber)
                .OrderBy(fixture => fixture.Id)
                .ToList();
        }

        public IReadOnlyList<Fixture> GetCurrentRoundFixtures()
        {
            return GetFixturesForRound(CurrentRoundNumber);
        }

        public Fixture GetNextFixtureForTeam(Team team)
        {
            return _fixtures
                .Where(fixture => !fixture.IsPlayed)
                .Where(fixture => fixture.HomeTeam.Id == team.Id || fixture.AwayTeam.Id == team.Id)
                .OrderBy(fixture => fixture.RoundNumber)
                .ThenBy(fixture => fixture.Id)
                .FirstOrDefault();
        }

        public IReadOnlyList<Fixture> GetFixturesForTeam(Team team)
        {
            return _fixtures
                .Where(fixture => fixture.HomeTeam.Id == team.Id || fixture.AwayTeam.Id == team.Id)
                .OrderBy(fixture => fixture.RoundNumber)
                .ThenBy(fixture => fixture.Id)
                .ToList();
        }

        public IReadOnlyList<LeagueStanding> GetStandings()
        {
            return LeagueStandingsCalculator.Calculate(this);
        }

        public void RecordResult(int fixtureId, MatchResult result)
        {
            var fixture = _fixtures.SingleOrDefault(fixture => fixture.Id == fixtureId);

            if (fixture == null)
            {
                throw new ArgumentException($"Fixture {fixtureId} does not exist.", nameof(fixtureId));
            }

            fixture.Complete(result);
        }

        private void ValidateFixtures()
        {
            var leagueTeamIds = League.Teams.Select(team => team.Id).ToHashSet();

            foreach (var fixture in _fixtures)
            {
                if (!leagueTeamIds.Contains(fixture.HomeTeam.Id) || !leagueTeamIds.Contains(fixture.AwayTeam.Id))
                {
                    throw new ArgumentException("Every fixture team must belong to the season's league.");
                }
            }

            if (_fixtures.Select(fixture => fixture.Id).Distinct().Count() != _fixtures.Count)
            {
                throw new ArgumentException("Every fixture must have a unique ID.");
            }
        }
    }
}