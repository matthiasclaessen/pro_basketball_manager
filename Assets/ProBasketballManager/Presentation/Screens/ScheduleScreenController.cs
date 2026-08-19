using System.Globalization;
using System.Linq;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Teams;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public sealed class ScheduleScreenController : ScreenController
    {
        protected override string ScreenElementName => "schedule-screen";

        private VisualElement _scheduleList;
        private Label _roundBadgeLabel;

        protected override void FindControls(VisualElement documentRoot)
        {
            _scheduleList = documentRoot.Q<VisualElement>("schedule-list");
            _roundBadgeLabel = documentRoot.Q<Label>("schedule-round-badge");
        }

        public override void Render()
        {
            if (!IsBound)
            {
                return;
            }

            var season = Session.Season;

            _scheduleList.Clear();

            _roundBadgeLabel.text = season.IsComplete ? "SEASON COMPLETE" : $"ROUND {season.CurrentRoundNumber} / {season.TotalRounds}";

            ApplySeasonBadgeState(_roundBadgeLabel);

            for (var roundNumber = 1; roundNumber <= season.TotalRounds; roundNumber++)
            {
                AddRound(roundNumber);
            }
        }

        private void AddRound(int roundNumber)
        {
            var season = Session.Season;
            var fixtures = season.GetFixturesForRound(roundNumber);

            var header = new VisualElement();
            header.AddToClassList("schedule-round-header");

            header.Add(ScreenFormatting.CreateLabel($"Round {roundNumber}", "schedule-round-title"));

            var allPlayed = fixtures.All(fixture => fixture.IsPlayed);

            var statusText = allPlayed ? "COMPLETED" : roundNumber == season.CurrentRoundNumber ? "CURRENT" : "UPCOMING";

            header.Add(ScreenFormatting.CreateLabel(statusText, "schedule-round-status"));

            _scheduleList.Add(header);

            foreach (var fixture in fixtures)
            {
                _scheduleList.Add(CreateFixtureRow(fixture));
            }
        }

        private VisualElement CreateFixtureRow(Fixture fixture)
        {
            var row = new VisualElement();
            row.AddToClassList("schedule-fixture-row");

            if (ContainsTeam(fixture, Session.UserTeam))
            {
                row.AddToClassList("schedule-fixture-user");
            }

            var resultText = fixture.IsPlayed ? $"{fixture.Result.HomeScore} - {fixture.Result.AwayScore}" : "vs";

            var statusText = fixture.IsPlayed ? "FINAL" : fixture.Id == Session.CurrentFixture?.Id ? "NEXT" : "UPCOMING";

            row.Add(ScreenFormatting.CreateLabel(fixture.Date.ToString("d MMM", CultureInfo.InvariantCulture), "schedule-fixture-date"));

            row.Add(ScreenFormatting.CreateLabel(fixture.HomeTeam.Name, "schedule-fixture-home"));
            row.Add(ScreenFormatting.CreateLabel(resultText, "schedule-fixture-result"));
            row.Add(ScreenFormatting.CreateLabel(fixture.AwayTeam.Name, "schedule-fixture-away"));
            row.Add(ScreenFormatting.CreateLabel(statusText, "schedule-fixture-status"));

            return row;
        }

        private static bool ContainsTeam(Fixture fixture, Team team)
        {
            return fixture.HomeTeam.Id == team.Id || fixture.AwayTeam.Id == team.Id;
        }
    }
}
