using System.Linq;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Statistics;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public sealed class PlayerProfileScreenController : ScreenController
    {
        protected override string ScreenElementName => "player-profile-screen";

        private Label _nameLabel;
        private Label _metaLabel;
        private Label _roleLabel;
        private Label _seasonLabel;

        private VisualElement _overallContainer;
        private VisualElement _potentialContainer;

        private Label _gamesPlayedLabel;
        private Label _gamesStartedLabel;
        private Label _minutesLabel;
        private Label _pointsLabel;
        private Label _reboundsLabel;
        private Label _assistsLabel;
        private Label _stealsLabel;
        private Label _fieldGoalLabel;
        private Label _threePointLabel;
        private Label _freeThrowLabel;
        private Label _turnoversLabel;
        private Label _foulsLabel;

        private VisualElement _scoringAttributes;
        private VisualElement _playmakingAttributes;
        private VisualElement _defenseAttributes;
        private VisualElement _physicalAttributes;

        private VisualElement _gameLogList;
        private Label _gameLogCountLabel;

        protected override void FindControls(VisualElement documentRoot)
        {
            _nameLabel = documentRoot.Q<Label>("player-profile-name");
            _metaLabel = documentRoot.Q<Label>("player-profile-meta");
            _roleLabel = documentRoot.Q<Label>("player-profile-role");
            _seasonLabel = documentRoot.Q<Label>("player-profile-season");

            _overallContainer = documentRoot.Q<VisualElement>("squad-stars-column");
            _potentialContainer = documentRoot.Q<VisualElement>("squad-stars-column");

            _gamesPlayedLabel = documentRoot.Q<Label>("player-profile-gp");
            _gamesStartedLabel = documentRoot.Q<Label>("player-profile-gs");
            _minutesLabel = documentRoot.Q<Label>("player-profile-min");
            _pointsLabel = documentRoot.Q<Label>("player-profile-pts");
            _reboundsLabel = documentRoot.Q<Label>("player-profile-reb");
            _assistsLabel = documentRoot.Q<Label>("player-profile-ast");
            _stealsLabel = documentRoot.Q<Label>("player-profile-stl");
            _fieldGoalLabel = documentRoot.Q<Label>("player-profile-fg");
            _threePointLabel = documentRoot.Q<Label>("player-profile-3pt");
            _freeThrowLabel = documentRoot.Q<Label>("player-profile-ft");
            _turnoversLabel = documentRoot.Q<Label>("player-profile-to");
            _foulsLabel = documentRoot.Q<Label>("player-profile-pf");

            _scoringAttributes = documentRoot.Q<VisualElement>("player-profile-scoring-attributes");
            _playmakingAttributes = documentRoot.Q<VisualElement>("player-profile-playmaking-attributes");
            _defenseAttributes = documentRoot.Q<VisualElement>("player-profile-defense-attributes");
            _physicalAttributes = documentRoot.Q<VisualElement>("player-profile-physical-attributes");

            _gameLogList = documentRoot.Q<VisualElement>("player-profile-game-log-list");
            _gameLogCountLabel = documentRoot.Q<Label>("player-profile-game-log-count");
        }

        /// <summary>Selects a player and reveals the screen.</summary>
        public void ShowForPlayer(Player player)
        {
            Session.SelectedPlayer = player;

            Show();
        }

        public override void Render()
        {
            if (!IsBound || Session.SelectedPlayer == null)
            {
                return;
            }

            var player = Session.SelectedPlayer;

            var statistics = PlayerSeasonStatisticsCalculator
                .Calculate(Session.Season, Session.UserTeam)
                .Single(playerStatistics => playerStatistics.Player.Id == player.Id);

            var assignment = Session.UserRotation.GetAssignment(player.Id);
            var role = ScreenFormatting.GetSquadRole(Session.UserRotation, assignment);

            _nameLabel.text = player.FullName;
            _metaLabel.text = $"{ScreenFormatting.GetPositionAbbreviation(player.Position)} {ScreenFormatting.Separator} " + $"{player.Age} years {ScreenFormatting.Separator} {Session.UserTeam.Name}";

            RenderDevelopment(player);
            _seasonLabel.text = Session.Season.Name;

            _roleLabel.text = role;
            ScreenFormatting.ApplyRoleClass(_roleLabel, role);

            RenderStatistics(statistics);
            RenderAttributes(player);
            RenderGameLog(player);
        }

        private void RenderDevelopment(Player player)
        {
            _overallContainer.Clear();
            _overallContainer.Add(StarRatingElements.Create(Session.GetAbilityStars(player), 16));

            _potentialContainer.Clear();
            _potentialContainer.Add(StarRatingElements.Create(Session.GetPotentialStars(player), 16));
        }

        private void RenderStatistics(PlayerSeasonStatistics statistics)
        {
            _gamesPlayedLabel.text = statistics.GamesPlayed.ToString();
            _gamesStartedLabel.text = statistics.GamesStarted.ToString();

            _minutesLabel.text = ScreenFormatting.FormatPerGame(statistics.GamesPlayed, statistics.MinutesPerGame);
            _pointsLabel.text = ScreenFormatting.FormatPerGame(statistics.GamesPlayed, statistics.PointsPerGame);
            _reboundsLabel.text = ScreenFormatting.FormatPerGame(statistics.GamesPlayed, statistics.ReboundsPerGame);
            _assistsLabel.text = ScreenFormatting.FormatPerGame(statistics.GamesPlayed, statistics.AssistsPerGame);
            _stealsLabel.text = ScreenFormatting.FormatPerGame(statistics.GamesPlayed, statistics.StealsPerGame);
            _turnoversLabel.text = ScreenFormatting.FormatPerGame(statistics.GamesPlayed, statistics.TurnoversPerGame);
            _foulsLabel.text = ScreenFormatting.FormatPerGame(statistics.GamesPlayed, statistics.PersonalFoulsPerGame);

            _fieldGoalLabel.text = ScreenFormatting.FormatPercentage(statistics.FieldGoalsAttempted, statistics.FieldGoalPercentage);
            _threePointLabel.text = ScreenFormatting.FormatPercentage(statistics.ThreePointsAttempted, statistics.ThreePointPercentage);
            _freeThrowLabel.text = ScreenFormatting.FormatPercentage(statistics.FreeThrowsAttempted, statistics.FreeThrowPercentage);
        }

        private void RenderAttributes(Player player)
        {
            _scoringAttributes.Clear();
            _playmakingAttributes.Clear();
            _defenseAttributes.Clear();
            _physicalAttributes.Clear();

            var attributes = player.Attributes;

            AddAttributeRow(_scoringAttributes, "Finishing", attributes.Finishing);
            AddAttributeRow(_scoringAttributes, "Mid Range", attributes.MidRange);
            AddAttributeRow(_scoringAttributes, "Three Point", attributes.ThreePoint);
            AddAttributeRow(_scoringAttributes, "Free Throw", attributes.FreeThrow);

            AddAttributeRow(_playmakingAttributes, "Passing", attributes.Passing);
            AddAttributeRow(_playmakingAttributes, "Ball Handling", attributes.BallHandling);
            AddAttributeRow(_playmakingAttributes, "Basketball IQ", attributes.BasketballIq);

            AddAttributeRow(_defenseAttributes, "Perimeter Defense", attributes.PerimeterDefense);
            AddAttributeRow(_defenseAttributes, "Interior Defense", attributes.InteriorDefense);
            AddAttributeRow(_defenseAttributes, "Offensive Rebounding", attributes.OffensiveRebounding);
            AddAttributeRow(_defenseAttributes, "Defensive Rebounding", attributes.DefensiveRebounding);

            AddAttributeRow(_physicalAttributes, "Speed", attributes.Speed);
            AddAttributeRow(_physicalAttributes, "Strength", attributes.Strength);
            AddAttributeRow(_physicalAttributes, "Stamina", attributes.Stamina);
        }

        private static void AddAttributeRow(VisualElement container, string attributeName, int value)
        {
            var row = new VisualElement();
            row.AddToClassList("player-attribute-row");

            row.Add(ScreenFormatting.CreateLabel(attributeName, "player-attribute-label"));

            var valueLabel = ScreenFormatting.CreateLabel(value.ToString(), "player-attribute-value");

            if (value >= 16)
            {
                valueLabel.AddToClassList("player-attribute-value-elite");
            }
            else if (value >= 13)
            {
                valueLabel.AddToClassList("player-attribute-value-strong");
            }
            else if (value <= 7)
            {
                valueLabel.AddToClassList("player-attribute-value-weak");
            }

            row.Add(valueLabel);

            container.Add(row);
        }

        private void RenderGameLog(Player player)
        {
            _gameLogList.Clear();

            var entries = PlayerGameLogCalculator.Calculate(Session.Season, Session.UserTeam, player);

            _gameLogCountLabel.text = entries.Count == 1 ? "1 FIXTURE" : $"{entries.Count} FIXTURES";

            if (entries.Count == 0)
            {
                _gameLogList.Add(ScreenFormatting.CreateLabel("No games have been played yet.", "box-score-placeholder"));

                return;
            }

            foreach (var entry in entries)
            {
                _gameLogList.Add(CreateGameLogRow(entry));
            }
        }

        private static VisualElement CreateGameLogRow(PlayerGameLogEntry entry)
        {
            var row = new VisualElement();
            row.AddToClassList("player-game-log-row");

            row.Add(ScreenFormatting.CreateLabel(entry.RoundNumber.ToString(), "player-game-log-round"));
            row.Add(ScreenFormatting.CreateLabel(entry.Opponent.Name, "player-game-log-opponent"));
            row.Add(ScreenFormatting.CreateLabel(entry.IsHome ? "H" : "A", "player-game-log-side"));

            var resultText = $"{(entry.Won ? "W" : "L")} {entry.TeamScore}-{entry.OpponentScore}";
            var result = ScreenFormatting.CreateLabel(resultText, "player-game-log-result");
            result.AddToClassList(entry.Won ? "player-game-log-win" : "player-game-log-loss");
            row.Add(result);

            if (!entry.DidPlay)
            {
                var dnp = ScreenFormatting.CreateLabel("DNP", "player-game-log-stat");
                dnp.AddToClassList("player-game-log-dnp");
                row.Add(dnp);

                for (var column = 0; column < 4; column++)
                {
                    row.Add(ScreenFormatting.CreateLabel(ScreenFormatting.NoValue, "player-game-log-stat"));
                }

                row.Add(ScreenFormatting.CreateLabel(ScreenFormatting.NoValue, "player-game-log-shooting"));
                row.Add(ScreenFormatting.CreateLabel(ScreenFormatting.NoValue, "player-game-log-shooting"));

                return row;
            }

            var boxScore = entry.BoxScore;

            row.Add(ScreenFormatting.CreateLabel(boxScore.MinutesPlayed.ToString("0.0"), "player-game-log-stat"));
            row.Add(ScreenFormatting.CreateLabel(boxScore.Points.ToString(), "player-game-log-stat"));
            row.Add(ScreenFormatting.CreateLabel(boxScore.Rebounds.ToString(), "player-game-log-stat"));
            row.Add(ScreenFormatting.CreateLabel(boxScore.Assists.ToString(), "player-game-log-stat"));
            row.Add(ScreenFormatting.CreateLabel(boxScore.Steals.ToString(), "player-game-log-stat"));
            row.Add(ScreenFormatting.CreateLabel($"{boxScore.FieldGoalsMade}/{boxScore.FieldGoalsAttempted}", "player-game-log-shooting"));
            row.Add(ScreenFormatting.CreateLabel($"{boxScore.ThreePointsMade}/{boxScore.ThreePointsAttempted}", "player-game-log-shooting"));

            return row;
        }
    }
}
