using System;
using System.Linq;
using ProBasketballManager.Domain.Competitions;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public sealed class EndOfSeasonScreenController : ScreenController
    {
        protected override string ScreenElementName => "end-of-season-screen";

        public event Action NextSeasonRequested;

        private Label _seasonLabel;
        private Label _championLabel;
        private Label _championRecordLabel;
        private Label _userFinishLabel;
        private Label _userRecordLabel;
        private Label _leadingScorerLabel;
        private Label _leadingScorerDetailLabel;
        private Label _careerSummaryLabel;
        private VisualElement _standingsList;
        private Button _nextSeasonButton;

        protected override void FindControls(VisualElement documentRoot)
        {
            _seasonLabel = documentRoot.Q<Label>("end-of-season-name");
            _championLabel = documentRoot.Q<Label>("end-of-season-champion");
            _championRecordLabel = documentRoot.Q<Label>("end-of-season-champion-record");
            _userFinishLabel = documentRoot.Q<Label>("end-of-season-user-finish");
            _userRecordLabel = documentRoot.Q<Label>("end-of-season-user-record");
            _leadingScorerLabel = documentRoot.Q<Label>("end-of-season-leading-scorer");
            _leadingScorerDetailLabel = documentRoot.Q<Label>("end-of-season-leading-scorer-detail");
            _careerSummaryLabel = documentRoot.Q<Label>("end-of-season-career-summary");
            _standingsList = documentRoot.Q<VisualElement>("end-of-season-standings");
            _nextSeasonButton = documentRoot.Q<Button>("start-next-season-button");
        }

        public void RegisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _nextSeasonButton.clicked -= RaiseNextSeasonRequested;
            _nextSeasonButton.clicked += RaiseNextSeasonRequested;
        }

        public void UnregisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _nextSeasonButton.clicked -= RaiseNextSeasonRequested;
        }

        private void RaiseNextSeasonRequested()
        {
            NextSeasonRequested?.Invoke();
        }

        public override void Render()
        {
            if (!IsBound || !Session.Season.IsComplete)
            {
                return;
            }

            var season = Session.Season;
            var standings = season.GetStandings();

            _seasonLabel.text = season.Name;

            var champion = standings[0];

            _championLabel.text = champion.Team.Name;
            _championRecordLabel.text = $"{champion.Wins} - {champion.Losses}";

            var userStanding = standings.Single(standing => standing.Team.Id == Session.UserTeam.Id);

            _userFinishLabel.text = $"{FormatOrdinal(userStanding.Position)} place";
            _userRecordLabel.text = $"{userStanding.Wins} - {userStanding.Losses}";

            RenderLeadingScorer(season);
            RenderCareerSummary();
            RenderStandings(standings);

            _nextSeasonButton.text = "Start " + Career.GetNextSeasonName(season.Name);
        }

        private void RenderLeadingScorer(Season season)
        {
            var leaders = season.League.Teams
                .SelectMany(team => Domain.Statistics.PlayerSeasonStatisticsCalculator.Calculate(season, team))
                .Where(statistics => statistics.GamesPlayed > 0)
                .OrderByDescending(statistics => statistics.PointsPerGame)
                .ToList();

            if (leaders.Count == 0)
            {
                _leadingScorerLabel.text = ScreenFormatting.NoValue;
                _leadingScorerDetailLabel.text = string.Empty;

                return;
            }

            var leader = leaders[0];

            _leadingScorerLabel.text = leader.Player.FullName;
            _leadingScorerDetailLabel.text = $"{leader.PointsPerGame:0.0} PPG";
        }

        private void RenderCareerSummary()
        {
            var career = Session.Career;

            var seasonsPlayed = career.SeasonsCompleted + 1;
            var titles = career.GetTitleCount(Session.UserTeam.Id);

            var justWon = Session.Season.GetStandings()[0].Team.Id == Session.UserTeam.Id;

            if (justWon)
            {
                titles++;
            }

            var seasonWord = seasonsPlayed == 1 ? "season" : "seasons";
            var titleWord = titles == 1 ? "title" : "titles";

            _careerSummaryLabel.text = $"{seasonsPlayed} {seasonWord} managed {ScreenFormatting.Separator} {titles} {titleWord}";
        }

        private void RenderStandings(System.Collections.Generic.IReadOnlyList<LeagueStanding> standings)
        {
            _standingsList.Clear();

            foreach (var standing in standings)
            {
                var row = new VisualElement();
                row.AddToClassList("standings-row");

                if (standing.Team.Id == Session.UserTeam.Id)
                {
                    row.AddToClassList("standings-row-user");
                }

                row.Add(ScreenFormatting.CreateLabel(standing.Position.ToString(), "standings-position"));
                row.Add(ScreenFormatting.CreateLabel(standing.Team.Name, "standings-team"));
                row.Add(ScreenFormatting.CreateLabel(standing.Wins.ToString(), "standings-small"));
                row.Add(ScreenFormatting.CreateLabel(standing.Losses.ToString(), "standings-small"));

                var difference = standing.PointDifference;

                var differenceLabel = ScreenFormatting.CreateLabel(difference > 0 ? $"+{difference}" : difference.ToString(), "standings-medium");

                differenceLabel.AddToClassList(difference >= 0 ? "standings-difference-positive" : "standings-difference-negative");

                row.Add(differenceLabel);

                _standingsList.Add(row);
            }
        }

        private static string FormatOrdinal(int position)
        {
            if (position % 100 >= 11 && position % 100 <= 13)
            {
                return position + "th";
            }

            return (position % 10) switch
            {
                1 => position + "st",
                2 => position + "nd",
                3 => position + "rd",
                _ => position + "th"
            };
        }
    }
}