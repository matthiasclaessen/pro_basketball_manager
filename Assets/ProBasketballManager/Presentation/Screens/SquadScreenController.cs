using System;
using System.Linq;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Statistics;
using ProBasketballManager.Domain.Teams;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public sealed class SquadScreenController : ScreenController
    {
        protected override string ScreenElementName => "squad-screen";

        public event Action<Player> PlayerSelected;

        private VisualElement _squadList;
        private Label _subtitleLabel;
        private Label _gamesBadgeLabel;
        private Label _rosterValueLabel;
        private Label _recordValueLabel;
        private Label _leadingScorerValueLabel;
        private Label _leadingScorerDetailLabel;

        protected override void FindControls(VisualElement documentRoot)
        {
            _squadList = documentRoot.Q<VisualElement>("squad-list");
            _subtitleLabel = documentRoot.Q<Label>("squad-subtitle");
            _gamesBadgeLabel = documentRoot.Q<Label>("squad-games-badge");
            _rosterValueLabel = documentRoot.Q<Label>("squad-roster-value");
            _recordValueLabel = documentRoot.Q<Label>("squad-record-value");
            _leadingScorerValueLabel = documentRoot.Q<Label>("squad-leading-scorer-value");
            _leadingScorerDetailLabel = documentRoot.Q<Label>("squad-leading-scorer-detail");
        }

        public override void Render()
        {
            if (!IsBound)
            {
                return;
            }

            var season = Session.Season;
            var userTeam = Session.UserTeam;

            _squadList.Clear();

            var userStanding = season.GetStandings().Single(standing => standing.Team.Id == userTeam.Id);

            var statistics = PlayerSeasonStatisticsCalculator
                .Calculate(season, userTeam)
                .ToDictionary(playerStatistics => playerStatistics.Player.Id);

            _subtitleLabel.text = $"{userTeam.Name} {ScreenFormatting.Separator} {season.Name}";

            _gamesBadgeLabel.text = userStanding.Played == 1 ? "1 GAME PLAYED" : $"{userStanding.Played} GAMES PLAYED";

            _rosterValueLabel.text = userTeam.Players.Count.ToString();
            _recordValueLabel.text = $"{userStanding.Wins} - {userStanding.Losses}";

            RenderLeadingScorer(statistics.Values);

            foreach (var assignment in Session.UserRotation.Assignments.OrderBy(entry => entry.RotationOrder))
            {
                _squadList.Add(CreatePlayerRow(statistics[assignment.Player.Id], assignment));
            }
        }

        private static Label CreatePotentialLabel(Player player)
        {
            if (player.ScoutedPotential <= player.Overall)
            {
                var settled = ScreenFormatting.CreateLabel(ScreenFormatting.NoValue, "squad-small-column");
                settled.AddToClassList("squad-potential-settled");

                return settled;
            }

            var label = ScreenFormatting.CreateLabel(player.ScoutedPotential.ToString(), "squad-small-column");

            var upside = player.ScoutedPotential - player.Overall;

            label.AddToClassList(upside >= 4 ? "squad-potential-high" : "squad-potential-modest");

            return label;
        }

        private void RenderLeadingScorer(System.Collections.Generic.IEnumerable<PlayerSeasonStatistics> statistics)
        {
            var leadingScorer = statistics
                .Where(playerStatistics => playerStatistics.GamesPlayed > 0)
                .OrderByDescending(playerStatistics => playerStatistics.PointsPerGame)
                .ThenByDescending(playerStatistics => playerStatistics.Points)
                .FirstOrDefault();

            if (leadingScorer == null)
            {
                _leadingScorerValueLabel.text = "No games yet";
                _leadingScorerDetailLabel.text = $"{ScreenFormatting.NoValue} PPG";

                return;
            }

            _leadingScorerValueLabel.text = leadingScorer.Player.FullName;
            _leadingScorerDetailLabel.text = $"{leadingScorer.PointsPerGame:0.0} PPG";
        }

        private VisualElement CreatePlayerRow(PlayerSeasonStatistics statistics, PlayerRotationAssignment assignment)
        {
            var row = new VisualElement();
            row.AddToClassList("squad-row");

            row.Add(ScreenFormatting.CreateLabel(statistics.Player.FullName, "squad-player-column"));
            row.Add(ScreenFormatting.CreateLabel(ScreenFormatting.GetPositionAbbreviation(statistics.Player.Position), "squad-position-column"));
            row.Add(ScreenFormatting.CreateLabel(statistics.Player.Age.ToString(), "squad-small-column"));
            row.Add(CreatePotentialLabel(statistics.Player));
            row.Add(ScreenFormatting.CreateLabel(statistics.GamesPlayed.ToString(), "squad-small-column"));
            row.Add(ScreenFormatting.CreateLabel(statistics.GamesStarted.ToString(), "squad-small-column"));
            row.Add(ScreenFormatting.CreateLabel(ScreenFormatting.FormatPerGame(statistics.GamesPlayed, statistics.MinutesPerGame), "squad-stat-column"));
            row.Add(ScreenFormatting.CreateLabel(ScreenFormatting.FormatPerGame(statistics.GamesPlayed, statistics.PointsPerGame), "squad-stat-column"));
            row.Add(ScreenFormatting.CreateLabel(ScreenFormatting.FormatPerGame(statistics.GamesPlayed, statistics.ReboundsPerGame), "squad-stat-column"));
            row.Add(ScreenFormatting.CreateLabel(ScreenFormatting.FormatPerGame(statistics.GamesPlayed, statistics.AssistsPerGame), "squad-stat-column"));
            row.Add(ScreenFormatting.CreateLabel(ScreenFormatting.FormatPerGame(statistics.GamesPlayed, statistics.StealsPerGame), "squad-stat-column"));
            row.Add(ScreenFormatting.CreateLabel(ScreenFormatting.FormatPercentage(statistics.FieldGoalsAttempted, statistics.FieldGoalPercentage), "squad-percent-column"));
            row.Add(ScreenFormatting.CreateLabel(ScreenFormatting.FormatPercentage(statistics.ThreePointsAttempted, statistics.ThreePointPercentage), "squad-percent-column"));
            row.Add(ScreenFormatting.CreateLabel(ScreenFormatting.FormatPercentage(statistics.FreeThrowsAttempted, statistics.FreeThrowPercentage), "squad-percent-column"));

            var player = statistics.Player;

            row.RegisterCallback<ClickEvent>(_ => PlayerSelected?.Invoke(player));

            return row;
        }
    }
}
