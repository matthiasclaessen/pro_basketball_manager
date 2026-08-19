using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Teams;
using ProBasketballManager.Domain.Clubs;
using ProBasketballManager.Domain.Statistics;

namespace ProBasketballManager.Domain.Competitions
{
    public sealed class Career
    {
        private readonly List<CompletedSeason> _completedSeasons;
        private readonly List<Player> _retiredPlayers = new List<Player>();
        private readonly List<Club> _clubs;

        public IReadOnlyList<Club> Clubs => _clubs;

        private readonly List<Season> _seasons = new List<Season>();

        public IReadOnlyList<Season> Seasons => _seasons;

        public IReadOnlyList<League> Leagues => _seasons.Select(season => season.League).ToList();

        public League PrimaryLeague => _seasons.OrderBy(season => season.League.Id).First().League;

        public League League => PrimaryLeague;

        public Season CurrentSeason => _seasons.OrderBy(season => season.League.Id).First();

        public DateTime CurrentDate { get; private set; }

        public IReadOnlyList<CompletedSeason> CompletedSeasons => _completedSeasons;

        public int SeasonsCompleted => _completedSeasons.Count(season => season.CompetitionId == PrimaryLeague.Id);

        public bool AllSeasonsComplete => _seasons.All(season => season.IsComplete);

        public bool CanAdvance => AllSeasonsComplete;

        public Career(IEnumerable<Club> clubs, League league, Season currentSeason, IEnumerable<CompletedSeason> completedSeasons = null, IEnumerable<Player> retiredPlayers = null) : this(clubs, new[] { currentSeason ?? throw new ArgumentNullException(nameof(currentSeason)) }, completedSeasons, retiredPlayers)
        {
            if (currentSeason.League != league)
            {
                throw new ArgumentException("The current season must belong to the career's league.", nameof(currentSeason));
            }
        }

        public Career(IEnumerable<Club> clubs, IEnumerable<Season> seasons, IEnumerable<CompletedSeason> completedSeasons = null, IEnumerable<Player> retiredPlayers = null)
        {
            if (clubs == null)
            {
                throw new ArgumentNullException(nameof(clubs));
            }

            _clubs = clubs.ToList();

            if (_clubs.Count == 0)
            {
                throw new ArgumentException("A career must contain at least one club.", nameof(clubs));
            }

            if (seasons == null)
            {
                throw new ArgumentNullException(nameof(seasons));
            }

            _seasons.AddRange(seasons);

            if (_seasons.Count == 0)
            {
                throw new ArgumentException("A career must contain at least one season.", nameof(seasons));
            }

            var duplicateCompetition = _seasons.GroupBy(season => season.League.Id).FirstOrDefault(group => group.Count() > 1);

            if (duplicateCompetition != null)
            {
                throw new ArgumentException($"Competition {duplicateCompetition.Key} has more than one running season.", nameof(seasons));
            }

            _completedSeasons = completedSeasons?.ToList() ?? new List<CompletedSeason>();

            CurrentDate = _seasons.Min(season => season.StartDate);

            if (retiredPlayers != null)
            {
                _retiredPlayers.AddRange(retiredPlayers);
            }

            var clubTeamIds = _clubs.SelectMany(club => club.Teams).Select(team => team.Id).ToHashSet();

            foreach (var season in _seasons)
            {
                var orphan = season.League.Teams.FirstOrDefault(team => !clubTeamIds.Contains(team.Id));

                if (orphan != null)
                {
                    throw new ArgumentException($"{orphan.Name} competes in {season.League.Name} but belongs to no club in this career.", nameof(clubs));
                }
            }
        }

        public Season GetSeasonFor(Team team)
        {
            if (team == null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            return _seasons.FirstOrDefault(season => season.League.Teams.Any(candidate => candidate.Id == team.Id));
        }

        public IReadOnlyList<Fixture> GetFixturesOn(DateTime date)
        {
            return _seasons.SelectMany(season => season.GetFixturesOn(date)).OrderBy(fixture => fixture.Id).ToList();
        }

        public IReadOnlyList<Fixture> GetDueFixtures()
        {
            return GetFixturesOn(CurrentDate).Where(fixture => !fixture.IsPlayed).ToList();
        }

        public DateTime? NextFixtureDate
        {
            get
            {
                var upcoming = _seasons.SelectMany(season => season.Fixtures).Where(fixture => !fixture.IsPlayed).Select(fixture => fixture.Date).ToList();

                return upcoming.Count == 0 ? (DateTime?)null : upcoming.Min();
            }
        }

        public void AdvanceOneDay()
        {
            var due = GetDueFixtures();

            if (due.Count > 0)
            {
                throw new InvalidOperationException($"{due.Count} fixture(s) on {CurrentDate:yyyy-MM-dd} have not been played; the clock cannot move past them.");
            }

            CurrentDate = CurrentDate.AddDays(1);
        }

        public void SetCurrentDate(DateTime date)
        {
            if (date.Date < CurrentDate)
            {
                throw new ArgumentException("The career clock cannot run backwards.", nameof(date));
            }

            CurrentDate = date.Date;
        }

        public Club GetClub(int clubId)
        {
            var club = _clubs.FirstOrDefault(candidate => candidate.Id == clubId);

            if (club == null)
            {
                throw new ArgumentException($"There is no club with id {clubId} in this career.", nameof(clubId));
            }

            return club;
        }

        public Club GetClubFor(Team team)
        {
            if (team == null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            return GetClub(team.ClubId);
        }

        public static Career Start(IEnumerable<Club> clubs, League league, int seasonId, string seasonName, CompetitionRules rules = null)
        {
            var effectiveRules = rules ?? CompetitionRules.Fiba;

            var fixtures = RoundRobinScheduleGenerator.Generate(league, effectiveRules);

            return new Career(clubs, league, new Season(seasonId, seasonName, league, fixtures, effectiveRules));
        }

        public CompletedSeason AdvanceToNextSeason(IRandomSource random = null)
        {
            if (!AllSeasonsComplete)
            {
                throw new InvalidOperationException("The season is not finished yet, so the next one cannot start.");
            }

            var seed = (uint)(CurrentSeason.Id * 7919 + 13);

            var archivedSeasons = _seasons.OrderBy(season => season.League.Id).Select(Archive).ToList();

            _completedSeasons.AddRange(archivedSeasons);

            LastCloseSeason = RunCloseSeason(random ?? new XorShiftRandom(seed));

            var replacements = _seasons
                .Select(season => new Season(season.Id + _seasons.Count, GetNextSeasonName(season.Name), season.League, RoundRobinScheduleGenerator.Generate(season.League, season.Rules, season.StartDate.Year + 1), season.Rules))
                .ToList();

            _seasons.Clear();
            _seasons.AddRange(replacements);

            return archivedSeasons.First(season => season.CompetitionId == PrimaryLeague.Id);
        }

        public IReadOnlyList<Player> RetiredPlayers => _retiredPlayers;

        public CloseSeasonReport LastCloseSeason { get; private set; }

        private CloseSeasonReport RunCloseSeason(IRandomSource random)
        {
            var report = new CloseSeasonReport();

            var nextPlayerId = _clubs.Max(club => club.GetHighestPlayerId()) + 1;

            foreach (var club in _clubs)
            {
                foreach (var player in club.Squad.ToList())
                {
                    var team = club.GetPrimaryTeamFor(player.Id);

                    player.AdvanceAge();

                    if (RetirementModel.ShouldRetire(player, random))
                    {
                        var replacement = ProspectGenerator.Create(nextPlayerId++, player.Position, random);

                        club.ReplacePlayer(player, replacement);

                        _retiredPlayers.Add(player);

                        report.Retirements.Add(new RetirementRecord(team, player, player.Age, replacement));

                        continue;
                    }

                    var before = player.Overall;

                    player.ApplyDevelopment(DevelopmentModel.Develop(player, random));

                    var after = player.Overall;

                    if (after > before)
                    {
                        report.Improvements.Add(new DevelopmentRecord(team, player, before, after));
                    }
                    else if (after < before)
                    {
                        report.Declines.Add(new DevelopmentRecord(team, player, before, after));
                    }
                }
            }

            return report;
        }

        private static CompletedSeason Archive(Season season)
        {
            var statistics = new List<PlayerSeasonStatistics>();

            foreach (var team in season.League.Teams)
            {
                statistics.AddRange(PlayerSeasonStatisticsCalculator.Calculate(season, team));
            }

            return new CompletedSeason(
                season.Id,
                season.League.Id,
                season.Name,
                season.GetStandings(),
                statistics);
        }

        public static string GetNextSeasonName(string currentName)
        {
            if (string.IsNullOrWhiteSpace(currentName))
            {
                return "Season 2";
            }

            var parts = currentName.Split('/');

            if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out var startYear) && int.TryParse(parts[1].Trim(), out var endYear))
            {
                var nextStart = startYear + 1;
                var nextEnd = (endYear + 1) % 100;

                return $"{nextStart} / {nextEnd:00}";
            }

            return currentName + " (next)";
        }

        public IReadOnlyList<CompletedSeason> GetSeasonsFor(int teamId)
        {
            return _completedSeasons
                .Where(season => season.GetStandingFor(teamId) != null)
                .Reverse()
                .ToList();
        }

        public int GetTitleCount(int teamId)
        {
            return _completedSeasons.Count(season => season.Champion != null && season.Champion.Team.Id == teamId);
        }
    }
}
