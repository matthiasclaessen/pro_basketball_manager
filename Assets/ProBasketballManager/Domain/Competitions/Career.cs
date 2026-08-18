using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Statistics;

namespace ProBasketballManager.Domain.Competitions
{
    public sealed class Career
    {
        private readonly List<CompletedSeason> _completedSeasons;
        private readonly List<Player> _retiredPlayers = new List<Player>();

        public League League { get; }

        public Season CurrentSeason { get; private set; }

        public IReadOnlyList<CompletedSeason> CompletedSeasons => _completedSeasons;

        public int SeasonsCompleted => _completedSeasons.Count;

        public bool CanAdvance => CurrentSeason.IsComplete;

        public Career(League league, Season currentSeason, IEnumerable<CompletedSeason> completedSeasons = null, IEnumerable<Player> retiredPlayers = null)
        {
            League = league ?? throw new ArgumentNullException(nameof(league));
            CurrentSeason = currentSeason ?? throw new ArgumentNullException(nameof(currentSeason));

            _completedSeasons = completedSeasons?.ToList() ?? new List<CompletedSeason>();

            if (retiredPlayers != null)
            {
                _retiredPlayers.AddRange(retiredPlayers);
            }

            if (currentSeason.League != league)
            {
                throw new ArgumentException("The current season must belong to the career's league.", nameof(currentSeason));
            }
        }

        public static Career Start(League league, int seasonId, string seasonName, CompetitionRules rules = null)
        {
            var effectiveRules = rules ?? CompetitionRules.Fiba;

            var fixtures = RoundRobinScheduleGenerator.Generate(league, effectiveRules);

            return new Career(league, new Season(seasonId, seasonName, league, fixtures, effectiveRules));
        }

        public CompletedSeason AdvanceToNextSeason(IRandomSource random = null)
        {
            if (!CurrentSeason.IsComplete)
            {
                throw new InvalidOperationException("The season is not finished yet, so the next one cannot start.");
            }

            var archived = Archive(CurrentSeason);

            _completedSeasons.Add(archived);

            LastCloseSeason = RunCloseSeason(random ?? new XorShiftRandom((uint)(CurrentSeason.Id * 7919 + 13)));

            var fixtures = RoundRobinScheduleGenerator.Generate(League, CurrentSeason.Rules);

            CurrentSeason = new Season(
                CurrentSeason.Id + 1,
                GetNextSeasonName(CurrentSeason.Name),
                League,
                fixtures,
                CurrentSeason.Rules);

            return archived;
        }

        public IReadOnlyList<Player> RetiredPlayers => _retiredPlayers;

        public CloseSeasonReport LastCloseSeason { get; private set; }

        private CloseSeasonReport RunCloseSeason(IRandomSource random)
        {
            var report = new CloseSeasonReport();

            var nextPlayerId = League.Teams.Max(team => team.GetHighestPlayerId()) + 1;

            foreach (var team in League.Teams)
            {
                foreach (var player in team.Players.ToList())
                {
                    player.AdvanceAge();

                    if (RetirementModel.ShouldRetire(player, random))
                    {
                        var replacement = ProspectGenerator.Create(nextPlayerId++, player.Position, random);

                        team.ReplacePlayer(player, replacement);

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