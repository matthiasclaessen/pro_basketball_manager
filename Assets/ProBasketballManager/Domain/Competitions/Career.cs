using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Statistics;

namespace ProBasketballManager.Domain.Competitions
{
    /// <summary>
    /// A league played across many seasons: the one in progress, plus the record of
    /// every season already finished.
    ///
    /// This is what turns a single season into a game. Before it existed the season
    /// ended and there was nowhere to go; now finishing one archives it and generates
    /// the next.
    ///
    /// Rosters are deliberately not touched here. Teams and Players carry over
    /// untouched, so a career is currently the same squads playing again. Ageing and
    /// development belong in this class later, as a step inside AdvanceToNextSeason,
    /// but they are a separate concern from the rollover itself.
    /// </summary>
    public sealed class Career
    {
        private readonly List<CompletedSeason> _completedSeasons;

        public League League { get; }

        /// <summary>The season being played.</summary>
        public Season CurrentSeason { get; private set; }

        /// <summary>Finished seasons, oldest first.</summary>
        public IReadOnlyList<CompletedSeason> CompletedSeasons => _completedSeasons;

        /// <summary>How many seasons have been played to a finish.</summary>
        public int SeasonsCompleted => _completedSeasons.Count;

        /// <summary>Whether the current season is over and the next one can begin.</summary>
        public bool CanAdvance => CurrentSeason.IsComplete;

        public Career(League league, Season currentSeason, IEnumerable<CompletedSeason> completedSeasons = null)
        {
            League = league ?? throw new ArgumentNullException(nameof(league));
            CurrentSeason = currentSeason ?? throw new ArgumentNullException(nameof(currentSeason));

            _completedSeasons = completedSeasons?.ToList() ?? new List<CompletedSeason>();

            if (currentSeason.League != league)
            {
                throw new ArgumentException("The current season must belong to the career's league.", nameof(currentSeason));
            }
        }

        /// <summary>Starts a career on a freshly generated season.</summary>
        public static Career Start(League league, int seasonId, string seasonName)
        {
            var fixtures = RoundRobinScheduleGenerator.Generate(league);

            return new Career(league, new Season(seasonId, seasonName, league, fixtures));
        }

        /// <summary>
        /// Archives the finished season and generates the next one.
        ///
        /// Throws rather than doing nothing if the season is still running, because a
        /// silent no-op here would look to the player like a button that does not
        /// work.
        /// </summary>
        public CompletedSeason AdvanceToNextSeason()
        {
            if (!CurrentSeason.IsComplete)
            {
                throw new InvalidOperationException(
                    "The season is not finished yet, so the next one cannot start.");
            }

            var archived = Archive(CurrentSeason);

            _completedSeasons.Add(archived);

            var fixtures = RoundRobinScheduleGenerator.Generate(League);

            CurrentSeason = new Season(
                CurrentSeason.Id + 1,
                GetNextSeasonName(CurrentSeason.Name),
                League,
                fixtures);

            return archived;
        }

        /// <summary>Captures the final table and every player's totals.</summary>
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

        /// <summary>
        /// Turns "2026 / 27" into "2027 / 28". Falls back to appending a number when
        /// the name is not in that shape, so an unusual name still advances instead of
        /// repeating forever.
        /// </summary>
        public static string GetNextSeasonName(string currentName)
        {
            if (string.IsNullOrWhiteSpace(currentName))
            {
                return "Season 2";
            }

            var parts = currentName.Split('/');

            if (parts.Length == 2
                && int.TryParse(parts[0].Trim(), out var startYear)
                && int.TryParse(parts[1].Trim(), out var endYear))
            {
                var nextStart = startYear + 1;
                var nextEnd = (endYear + 1) % 100;

                return $"{nextStart} / {nextEnd:00}";
            }

            return currentName + " (next)";
        }

        /// <summary>
        /// Every finish a team has recorded, newest first. The basis of a club history
        /// view, and of any future logic that cares about recent form across seasons.
        /// </summary>
        public IReadOnlyList<CompletedSeason> GetSeasonsFor(int teamId)
        {
            return _completedSeasons
                .Where(season => season.GetStandingFor(teamId) != null)
                .Reverse()
                .ToList();
        }

        /// <summary>How many titles a team has won in this career.</summary>
        public int GetTitleCount(int teamId)
        {
            return _completedSeasons.Count(season => season.Champion != null && season.Champion.Team.Id == teamId);
        }
    }
}