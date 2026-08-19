using System;
using System.Globalization;
using System.Linq;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public sealed class DashboardScreenController : ScreenController
    {
        protected override string ScreenElementName => "dashboard-screen";

        /// <summary>Raised when the manager asks to play the next fixture.</summary>
        public event Action MatchCentreRequested;

        private VisualElement _topbarBadge;

        private Label _clubNameLabel;
        private Label _leagueNameLabel;
        private Label _seasonNameLabel;
        private Label _currentDateLabel;

        private Label _homeTeamNameLabel;
        private Label _awayTeamNameLabel;
        private Label _homeScoreLabel;
        private Label _awayScoreLabel;
        private Label _matchStatusLabel;
        private Label _fixtureInfoLabel;

        private Label _clubPanelNameLabel;
        private Label _rosterCountLabel;
        private Label _threePointRatingLabel;
        private Label _defenseRatingLabel;

        private VisualElement _periodScores;
        private VisualElement _leagueList;

        private Button _simulateMatchButton;

        protected override void FindControls(VisualElement documentRoot)
        {
            _topbarBadge = documentRoot.Q<VisualElement>("topbar-badge");

            _clubNameLabel = documentRoot.Q<Label>("club-name");
            _leagueNameLabel = documentRoot.Q<Label>("league-name");
            _seasonNameLabel = documentRoot.Q<Label>("season-name");
            _currentDateLabel = documentRoot.Q<Label>("current-date");

            _homeTeamNameLabel = documentRoot.Q<Label>("home-team-name");
            _awayTeamNameLabel = documentRoot.Q<Label>("away-team-name");
            _homeScoreLabel = documentRoot.Q<Label>("home-score");
            _awayScoreLabel = documentRoot.Q<Label>("away-score");
            _matchStatusLabel = documentRoot.Q<Label>("match-status");
            _fixtureInfoLabel = documentRoot.Q<Label>("fixture-info");

            _clubPanelNameLabel = documentRoot.Q<Label>("club-panel-name");
            _rosterCountLabel = documentRoot.Q<Label>("roster-count");
            _threePointRatingLabel = documentRoot.Q<Label>("three-point-rating");
            _defenseRatingLabel = documentRoot.Q<Label>("defense-rating");

            _periodScores = documentRoot.Q<VisualElement>("period-scores");
            _leagueList = documentRoot.Q<VisualElement>("league-list");

            _simulateMatchButton = documentRoot.Q<Button>("simulate-match-button");
        }

        public void RegisterCallbacks()
        {
            if (_simulateMatchButton == null)
            {
                return;
            }

            _simulateMatchButton.clicked -= RaiseMatchCentreRequested;
            _simulateMatchButton.clicked += RaiseMatchCentreRequested;
        }

        public void UnregisterCallbacks()
        {
            if (_simulateMatchButton != null)
            {
                _simulateMatchButton.clicked -= RaiseMatchCentreRequested;
            }
        }

        private void RaiseMatchCentreRequested()
        {
            MatchCentreRequested?.Invoke();
        }

        public override void Render()
        {
            if (!IsBound)
            {
                return;
            }

            var season = Session.Season;
            var userTeam = Session.UserTeam;

            if (_topbarBadge != null)
            {
                _topbarBadge.Clear();
                _topbarBadge.Add(ClubIdentityElements.CreateBadge(Session.UserClub, 28));
            }

            _clubNameLabel.text = userTeam.Name;
            _leagueNameLabel.text = season.League.Name;
            _seasonNameLabel.text = season.Name;

            if (_currentDateLabel != null)
            {
                _currentDateLabel.text = Session.CurrentDate.ToString("ddd d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
            }

            RenderClubInformation();
            RenderStandings();

            _periodScores.Clear();
            _periodScores.style.display = DisplayStyle.None;

            if (Session.CurrentFixture == null)
            {
                RenderCompletedSeason();

                return;
            }

            var fixture = Session.CurrentFixture;

            _homeTeamNameLabel.text = fixture.HomeTeam.Name;
            _awayTeamNameLabel.text = fixture.AwayTeam.Name;
            _homeScoreLabel.text = ScreenFormatting.NoValue;
            _awayScoreLabel.text = ScreenFormatting.NoValue;
            _matchStatusLabel.text = $"ROUND {fixture.RoundNumber}";
            _fixtureInfoLabel.text = $"Round {fixture.RoundNumber} of {season.TotalRounds} {ScreenFormatting.Separator} 20:00";

            _simulateMatchButton.text = "Open Match Centre";
            _simulateMatchButton.SetEnabled(true);
        }

        private void RenderCompletedSeason()
        {
            _homeTeamNameLabel.text = Session.UserTeam.Name;
            _awayTeamNameLabel.text = "Season complete";
            _homeScoreLabel.text = ScreenFormatting.NoValue;
            _awayScoreLabel.text = ScreenFormatting.NoValue;
            _matchStatusLabel.text = "FINAL";
            _fixtureInfoLabel.text = $"{Session.Season.Name} completed";

            _simulateMatchButton.text = "Season Complete";
            _simulateMatchButton.SetEnabled(false);
        }

        private void RenderClubInformation()
        {
            var userTeam = Session.UserTeam;

            var averageThreePoint = userTeam.Players.Average(player => player.Attributes.ThreePoint);

            var averageDefense = userTeam.Players.Average(player =>
                (player.Attributes.PerimeterDefense + player.Attributes.InteriorDefense) / 2.0);

            _clubPanelNameLabel.text = userTeam.Name;
            _rosterCountLabel.text = userTeam.Players.Count.ToString();
            _threePointRatingLabel.text = averageThreePoint.ToString("0.0");
            _defenseRatingLabel.text = averageDefense.ToString("0.0");
        }

        private void RenderStandings()
        {
            _leagueList.Clear();

            foreach (var standing in Session.Season.GetStandings())
            {
                var row = new VisualElement();
                row.AddToClassList("league-row");

                if (standing.Team.Id == Session.UserTeam.Id)
                {
                    row.AddToClassList("league-row-current");
                }

                row.Add(ScreenFormatting.CreateLabel(standing.Position.ToString(), "league-position"));
                row.Add(ScreenFormatting.CreateLabel(standing.Team.Name, "league-team-name"));
                row.Add(ScreenFormatting.CreateLabel($"{standing.Wins} - {standing.Losses}", "league-record"));

                _leagueList.Add(row);
            }
        }
    }
}
