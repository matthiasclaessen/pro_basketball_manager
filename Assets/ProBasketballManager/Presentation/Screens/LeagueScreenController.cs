using ProBasketballManager.Domain.Competitions;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public sealed class LeagueScreenController : ScreenController
    {
        protected override string ScreenElementName => "league-screen";

        private VisualElement _standingsList;
        private Label _subtitleLabel;
        private Label _progressBadgeLabel;

        protected override void FindControls(VisualElement documentRoot)
        {
            _standingsList = documentRoot.Q<VisualElement>("league-standings-list");
            _subtitleLabel = documentRoot.Q<Label>("league-screen-subtitle");
            _progressBadgeLabel = documentRoot.Q<Label>("league-progress-badge");
        }

        public override void Render()
        {
            if (!IsBound)
            {
                return;
            }

            var season = Session.Season;

            _standingsList.Clear();

            _subtitleLabel.text = $"{season.League.Name} - {season.Name}";

            _progressBadgeLabel.text = season.IsComplete ? "SEASON COMPLETE" : $"ROUND {season.CurrentRoundNumber} / {season.TotalRounds}";

            ApplySeasonBadgeState(_progressBadgeLabel);

            foreach (var standing in season.GetStandings())
            {
                _standingsList.Add(CreateStandingRow(standing));
            }
        }

        private VisualElement CreateStandingRow(LeagueStanding standing)
        {
            var row = new VisualElement();
            row.AddToClassList("standings-row");

            if (standing.Team.Id == Session.UserTeam.Id)
            {
                row.AddToClassList("standings-row-user");
            }

            row.Add(CreateLabel(standing.Position.ToString(), "standings-position"));
            row.Add(CreateLabel(standing.Team.Name, "standings-team"));
            row.Add(CreateLabel(standing.Played.ToString(), "standings-small"));
            row.Add(CreateLabel(standing.Wins.ToString(), "standings-small"));
            row.Add(CreateLabel(standing.Losses.ToString(), "standings-small"));
            row.Add(CreateLabel(standing.PointsFor.ToString(), "standings-medium"));
            row.Add(CreateLabel(standing.PointsAgainst.ToString(), "standings-medium"));
            row.Add(CreateDifferenceLabel(standing.PointDifference));

            return row;
        }

        private static Label CreateDifferenceLabel(int pointDifference)
        {
            var text = pointDifference > 0 ? $"+{pointDifference}" : pointDifference.ToString();

            var label = CreateLabel(text, "standings-medium");

            if (pointDifference > 0)
            {
                label.AddToClassList("standings-difference-positive");
            }
            else if (pointDifference < 0)
            {
                label.AddToClassList("standings-difference-negative");
            }

            return label;
        }

        private static Label CreateLabel(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);

            return label;
        }
    }
}