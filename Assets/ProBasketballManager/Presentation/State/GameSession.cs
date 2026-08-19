using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Clubs;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Demo;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Tactics;
using ProBasketballManager.Domain.Teams;
using ProBasketballManager.Persistence;

namespace ProBasketballManager.Presentation.State
{
    public sealed class GameSession
    {
        public const int MaximumDaysPerContinue = 500;

        private readonly HashSet<int> _managedTeamIds = new HashSet<int>();
        private readonly Dictionary<int, TeamTactics> _tactics = new Dictionary<int, TeamTactics>();
        private readonly Dictionary<int, TeamRotation> _rotations = new Dictionary<int, TeamRotation>();

        public Career Career { get; private set; }

        public Club UserClub => Career.GetClub(UserTeam.ClubId);

        public Team UserTeam { get; private set; }

        public Season Season => Career.GetSeasonFor(UserTeam);

        public DateTime CurrentDate => Career.CurrentDate;

        public IReadOnlyCollection<int> ManagedTeamIds => _managedTeamIds;

        public IReadOnlyList<Team> ManagedTeams => UserClub.Teams.Where(team => _managedTeamIds.Contains(team.Id)).ToList();

        public Fixture CurrentFixture { get; private set; }

        public Team HomeTeam => CurrentFixture?.HomeTeam;

        public Team AwayTeam => CurrentFixture?.AwayTeam;

        public TeamTactics UserTactics { get => GetTactics(UserTeam); set => SetTactics(UserTeam, value); }

        public TeamRotation UserRotation { get => GetRotation(UserTeam); set => SetRotation(UserTeam, value); }

        public MatchResult CurrentMatchResult { get; set; }

        public Player SelectedPlayer { get; set; }

        public uint NextSeed { get; set; } = 12345;

        public uint LastUsedSeed { get; private set; }

        public bool CurrentFixtureRecorded { get; set; }

        public bool CanAdvanceSeason => Career.CanAdvance;

        public event Action Changed;

        private GameSession(Career career, Team userTeam, IEnumerable<int> managedTeamIds = null)
        {
            Career = career ?? throw new ArgumentNullException(nameof(career));
            UserTeam = userTeam ?? throw new ArgumentNullException(nameof(userTeam));

            var requested = managedTeamIds?.ToList() ?? new List<int> { UserClub.FirstTeam.Id };

            foreach (var teamId in requested)
            {
                _managedTeamIds.Add(teamId);
            }

            _managedTeamIds.Add(userTeam.Id);

            RefreshCurrentFixture();
        }

        public TeamTactics GetTactics(Team team)
        {
            if (team == null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            return _tactics.TryGetValue(team.Id, out var tactics) ? tactics : TeamTactics.Default;
        }

        public void SetTactics(Team team, TeamTactics tactics)
        {
            if (team == null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            _tactics[team.Id] = tactics ?? TeamTactics.Default;
        }

        public TeamRotation GetRotation(Team team)
        {
            if (team == null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            if (!_rotations.TryGetValue(team.Id, out var rotation))
            {
                rotation = TeamRotation.CreateDefault(team, GetRulesFor(team));

                _rotations[team.Id] = rotation;
            }

            return rotation;
        }

        public void SetRotation(Team team, TeamRotation rotation)
        {
            if (team == null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            _rotations[team.Id] = rotation ?? TeamRotation.CreateDefault(team, GetRulesFor(team));
        }

        public bool IsManaged(int teamId)
        {
            return _managedTeamIds.Contains(teamId);
        }

        public void SetManaged(int teamId, bool managed)
        {
            var team = UserClub.Teams.FirstOrDefault(candidate => candidate.Id == teamId);

            if (team == null)
            {
                throw new ArgumentException($"Team {teamId} does not belong to {UserClub.Name}.", nameof(teamId));
            }

            if (!managed && teamId == UserTeam.Id)
            {
                throw new InvalidOperationException($"{team.Name} is the selected team and cannot be unmanaged. Select another team first.");
            }

            if (managed)
            {
                _managedTeamIds.Add(teamId);
            }
            else
            {
                _managedTeamIds.Remove(teamId);
            }

            RefreshCurrentFixture();
        }

        public void SelectTeam(Team team)
        {
            if (team == null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            if (team.ClubId != UserTeam.ClubId)
            {
                throw new ArgumentException($"{team.Name} does not belong to {UserClub.Name}.", nameof(team));
            }

            if (!_managedTeamIds.Contains(team.Id))
            {
                throw new InvalidOperationException($"{team.Name} is not managed. Turn management on before selecting it.");
            }

            UserTeam = team;

            RefreshCurrentFixture();
        }

        public ContinueOutcome Continue()
        {
            var daysAdvanced = 0;

            for (var guard = 0; guard < MaximumDaysPerContinue; guard++)
            {
                var due = Career.GetDueFixtures();

                var managed = due.Where(fixture => IsManaged(fixture.HomeTeam.Id) || IsManaged(fixture.AwayTeam.Id)).ToList();

                if (managed.Count > 0)
                {
                    CurrentFixture = managed[0];
                    CurrentMatchResult = null;
                    CurrentFixtureRecorded = false;

                    return ContinueOutcome.MatchDay(Career.CurrentDate, daysAdvanced, managed);
                }

                foreach (var fixture in due)
                {
                    SimulateAiFixture(fixture);
                }

                if (Career.AllSeasonsComplete)
                {
                    return ContinueOutcome.SeasonEnded(Career.CurrentDate, daysAdvanced);
                }

                Career.AdvanceOneDay();

                daysAdvanced++;
            }

            return ContinueOutcome.Idle(Career.CurrentDate, daysAdvanced);
        }

        public void RefreshCurrentFixture()
        {
            CurrentFixture = Career.Seasons
                .SelectMany(season => season.Fixtures)
                .Where(fixture => !fixture.IsPlayed)
                .Where(fixture => IsManaged(fixture.HomeTeam.Id) || IsManaged(fixture.AwayTeam.Id))
                .OrderBy(fixture => fixture.Date)
                .ThenBy(fixture => fixture.Id)
                .FirstOrDefault();
        }

        public MatchResult SimulateCurrentFixture()
        {
            if (CurrentFixture == null || CurrentFixture.IsPlayed)
            {
                return null;
            }

            var seed = NextSeed;
            NextSeed++;

            var rules = GetRulesFor(CurrentFixture.HomeTeam);

            var homeManaged = IsManaged(CurrentFixture.HomeTeam.Id);
            var awayManaged = IsManaged(CurrentFixture.AwayTeam.Id);

            var homeRotation = homeManaged ? GetRotation(CurrentFixture.HomeTeam) : TeamRotation.CreateDefault(CurrentFixture.HomeTeam, rules);
            var awayRotation = awayManaged ? GetRotation(CurrentFixture.AwayTeam) : TeamRotation.CreateDefault(CurrentFixture.AwayTeam, rules);

            var homeTactics = homeManaged ? GetTactics(CurrentFixture.HomeTeam) : TeamTactics.Default;
            var awayTactics = awayManaged ? GetTactics(CurrentFixture.AwayTeam) : TeamTactics.Default;

            var simulator = new MatchSimulator(new XorShiftRandom(seed), rules);

            CurrentMatchResult = simulator.Simulate(CurrentFixture.HomeTeam, CurrentFixture.AwayTeam, homeRotation, awayRotation, homeTactics, awayTactics);

            LastUsedSeed = seed;
            CurrentFixtureRecorded = false;

            return CurrentMatchResult;
        }

        public void CompleteCurrentFixture()
        {
            if (CurrentFixtureRecorded || CurrentFixture == null || CurrentMatchResult == null)
            {
                return;
            }

            Career.GetSeasonFor(CurrentFixture.HomeTeam).RecordResult(CurrentFixture.Id, CurrentMatchResult);

            CurrentFixtureRecorded = true;

            RefreshCurrentFixture();
        }

        public ContinueOutcome CompleteCurrentRound()
        {
            CompleteCurrentFixture();

            return Continue();
        }

        private CompetitionRules GetRulesFor(Team team)
        {
            var season = Career.GetSeasonFor(team);

            return season?.Rules ?? Career.CurrentSeason.Rules;
        }

        private void SimulateAiFixture(Fixture fixture)
        {
            var rules = GetRulesFor(fixture.HomeTeam);

            var simulator = new MatchSimulator(new XorShiftRandom(NextSeed), rules);
            NextSeed++;

            var result = simulator.Simulate(fixture.HomeTeam, fixture.AwayTeam, TeamRotation.CreateDefault(fixture.HomeTeam, rules), TeamRotation.CreateDefault(fixture.AwayTeam, rules), TeamTactics.Default, TeamTactics.Default);

            Career.GetSeasonFor(fixture.HomeTeam).RecordResult(fixture.Id, result);
        }

        public CompletedSeason AdvanceToNextSeason()
        {
            var archived = Career.AdvanceToNextSeason();

            _rotations.Clear();

            CurrentMatchResult = null;
            CurrentFixtureRecorded = false;

            RefreshCurrentFixture();

            return archived;
        }

        public GameSessionSnapshot CreateSnapshot()
        {
            return new GameSessionSnapshot
            {
                Career = Career,
                UserTeam = UserTeam,
                ManagedTeamIds = _managedTeamIds.OrderBy(id => id).ToList(),
                Tactics = UserClub.Teams.Where(team => _tactics.ContainsKey(team.Id)).ToDictionary(team => team.Id, team => _tactics[team.Id]),
                Rotations = UserClub.Teams.Where(team => _rotations.ContainsKey(team.Id)).ToDictionary(team => team.Id, team => _rotations[team.Id]),
                UserTactics = UserTactics,
                UserRotation = UserRotation,
                NextSeed = NextSeed,
                CurrentFixtureRecorded = CurrentFixtureRecorded
            };
        }

        public static GameSession Restore(GameSessionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var session = new GameSession(snapshot.Career, snapshot.UserTeam, snapshot.ManagedTeamIds)
            {
                NextSeed = snapshot.NextSeed,
                CurrentFixtureRecorded = snapshot.CurrentFixtureRecorded
            };

            if (snapshot.Tactics != null)
            {
                foreach (var entry in snapshot.Tactics)
                {
                    session._tactics[entry.Key] = entry.Value;
                }
            }

            if (snapshot.Rotations != null)
            {
                foreach (var entry in snapshot.Rotations)
                {
                    session._rotations[entry.Key] = entry.Value;
                }
            }

            if (snapshot.UserTactics != null)
            {
                session._tactics[snapshot.UserTeam.Id] = snapshot.UserTactics;
            }

            if (snapshot.UserRotation != null)
            {
                session._rotations[snapshot.UserTeam.Id] = snapshot.UserRotation;
            }

            return session;
        }

        public static GameSession CreateNew(GameDatabase database, int userTeamId, int seasonStartYear = CompetitionCalendar.DefaultFirstSeasonYear)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            var seasons = database.Competitions.Select(competition => CreateSeason(competition, database.Rules[competition.Id], seasonStartYear)).ToList();

            var career = new Career(database.Clubs, seasons);

            var userTeam = database.Clubs.SelectMany(club => club.Teams).FirstOrDefault(team => team.Id == userTeamId);

            if (userTeam == null)
            {
                throw new ArgumentException($"No team with id {userTeamId} exists in database '{database.Name}'.", nameof(userTeamId));
            }

            return new GameSession(career, userTeam);
        }

        private static Season CreateSeason(League competition, CompetitionRules rules, int seasonStartYear)
        {
            var fixtures = RoundRobinScheduleGenerator.Generate(competition, rules, seasonStartYear);

            return new Season(competition.Id, $"{seasonStartYear} / {(seasonStartYear + 1) % 100:00}", competition, fixtures, rules);
        }

        public static GameSession CreateDemo()
        {
            var clubs = DemoClubFactory.Create();

            var first = DemoSeasonFactory.Create(clubs, TeamType.First);
            var reserve = DemoSeasonFactory.Create(clubs, TeamType.Reserve);

            var career = new Career(clubs, new[] { first, reserve });

            return new GameSession(career, clubs[0].FirstTeam);
        }

        public void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
