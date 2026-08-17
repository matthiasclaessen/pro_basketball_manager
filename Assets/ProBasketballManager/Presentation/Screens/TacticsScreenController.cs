using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Tactics;
using ProBasketballManager.Domain.Teams;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public sealed class TacticsScreenController : ScreenController
    {
        protected override string ScreenElementName => "tactics-screen";

        public event Action RotationChanged;

        private SliderInt _paceSlider;
        private SliderInt _rimSlider;
        private SliderInt _midRangeSlider;
        private SliderInt _threePointSlider;
        private SliderInt _ballMovementSlider;
        private SliderInt _perimeterPressureSlider;
        private SliderInt _protectPaintSlider;

        private Button _applyButton;
        private Button _resetButton;

        private Label _summaryLabel;
        private Label _saveStateLabel;
        private Label _minutesTotalLabel;

        private VisualElement _rotationList;

        private DropdownField _starterPointGuardDropdown;
        private DropdownField _starterShootingGuardDropdown;
        private DropdownField _starterSmallForwardDropdown;
        private DropdownField _starterPowerForwardDropdown;
        private DropdownField _starterCenterDropdown;
        private DropdownField _primaryBallHandlerDropdown;
        private DropdownField _primaryScorerDropdown;

        private readonly Dictionary<int, IntegerField> _minuteFields = new Dictionary<int, IntegerField>();
        private readonly Dictionary<int, IntegerField> _orderFields = new Dictionary<int, IntegerField>();

        protected override void FindControls(VisualElement documentRoot)
        {
            _paceSlider = documentRoot.Q<SliderInt>("pace-slider");
            _rimSlider = documentRoot.Q<SliderInt>("rim-slider");
            _midRangeSlider = documentRoot.Q<SliderInt>("mid-range-slider");
            _threePointSlider = documentRoot.Q<SliderInt>("three-point-slider");
            _ballMovementSlider = documentRoot.Q<SliderInt>("ball-movement-slider");
            _perimeterPressureSlider = documentRoot.Q<SliderInt>("perimeter-pressure-slider");
            _protectPaintSlider = documentRoot.Q<SliderInt>("protect-paint-slider");

            _applyButton = documentRoot.Q<Button>("apply-tactics-button");
            _resetButton = documentRoot.Q<Button>("reset-tactics-button");

            _summaryLabel = documentRoot.Q<Label>("tactics-summary");
            _saveStateLabel = documentRoot.Q<Label>("tactics-save-state");
            _minutesTotalLabel = documentRoot.Q<Label>("rotation-minutes-total");

            _rotationList = documentRoot.Q<VisualElement>("rotation-list");

            _starterPointGuardDropdown = documentRoot.Q<DropdownField>("starter-pg-dropdown");
            _starterShootingGuardDropdown = documentRoot.Q<DropdownField>("starter-sg-dropdown");
            _starterSmallForwardDropdown = documentRoot.Q<DropdownField>("starter-sf-dropdown");
            _starterPowerForwardDropdown = documentRoot.Q<DropdownField>("starter-pf-dropdown");
            _starterCenterDropdown = documentRoot.Q<DropdownField>("starter-c-dropdown");

            _primaryBallHandlerDropdown = documentRoot.Q<DropdownField>("primary-ball-handler-dropdown");
            _primaryScorerDropdown = documentRoot.Q<DropdownField>("primary-scorer-dropdown");
        }

        public void RegisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _applyButton.clicked -= Apply;
            _applyButton.clicked += Apply;

            _resetButton.clicked -= Reset;
            _resetButton.clicked += Reset;
        }

        public void UnregisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _applyButton.clicked -= Apply;
            _resetButton.clicked -= Reset;
        }

        public override void Render()
        {
            if (!IsBound)
            {
                return;
            }

            var tactics = Session.UserTactics;

            _paceSlider.value = tactics.Pace;
            _rimSlider.value = tactics.RimWeight;
            _midRangeSlider.value = tactics.MidRangeWeight;
            _threePointSlider.value = tactics.ThreePointWeight;
            _ballMovementSlider.value = tactics.BallMovement;
            _perimeterPressureSlider.value = tactics.PerimeterPressure;
            _protectPaintSlider.value = tactics.ProtectPaint;

            RenderSummary();
            RenderRotationControls();
        }

        private void RenderSummary()
        {
            var tactics = Session.UserTactics;

            var rimPercentage = (int)Math.Round(tactics.RimShare * 100);
            var midRangePercentage = (int)Math.Round(tactics.MidRangeShare * 100);
            var threePointPercentage = (int)Math.Round(tactics.ThreePointShare * 100);

            var separator = ScreenFormatting.Separator;

            _summaryLabel.text = $"Pace {tactics.Pace} {separator} Rim {rimPercentage}% {separator} Mid {midRangePercentage}% " + $"{separator} 3PT {threePointPercentage}% {separator} Ball movement {tactics.BallMovement}";
        }

        private void RenderRotationControls()
        {
            ConfigureStarterDropdown(_starterPointGuardDropdown, PlayerPosition.PointGuard);
            ConfigureStarterDropdown(_starterShootingGuardDropdown, PlayerPosition.ShootingGuard);
            ConfigureStarterDropdown(_starterSmallForwardDropdown, PlayerPosition.SmallForward);
            ConfigureStarterDropdown(_starterPowerForwardDropdown, PlayerPosition.PowerForward);
            ConfigureStarterDropdown(_starterCenterDropdown, PlayerPosition.Center);

            var playerNames = Session.UserTeam.Players.Select(player => player.FullName).ToList();

            _primaryBallHandlerDropdown.choices = playerNames;
            _primaryBallHandlerDropdown.value = Session.UserRotation.PrimaryBallHandler.FullName;

            _primaryScorerDropdown.choices = new List<string>(playerNames);
            _primaryScorerDropdown.value = Session.UserRotation.PrimaryScorer.FullName;

            RenderRotationRows();
        }

        private void ConfigureStarterDropdown(DropdownField dropdown, PlayerPosition position)
        {
            var eligiblePlayers = Session.UserTeam.Players
                .Where(player => player.Position == position)
                .ToList();

            dropdown.choices = eligiblePlayers.Select(player => player.FullName).ToList();
            dropdown.value = Session.UserRotation.GetStarter(position).FullName;
        }

        private void RenderRotationRows()
        {
            _rotationList.Clear();
            _minuteFields.Clear();
            _orderFields.Clear();

            foreach (var assignment in Session.UserRotation.Assignments.OrderBy(entry => entry.RotationOrder))
            {
                var row = new VisualElement();
                row.AddToClassList("rotation-row");

                var orderField = new IntegerField { value = assignment.RotationOrder };
                orderField.AddToClassList("rotation-order-field");
                orderField.AddToClassList("rotation-order-column");

                var playerName = ScreenFormatting.CreateLabel(assignment.Player.FullName, "rotation-player-name");
                playerName.AddToClassList("rotation-player-column");

                var position = ScreenFormatting.CreateLabel(
                    ScreenFormatting.GetPositionAbbreviation(assignment.Player.Position),
                    "rotation-position");
                position.AddToClassList("rotation-position-column");

                var minutesField = new IntegerField { value = assignment.TargetMinutes };
                minutesField.AddToClassList("rotation-minutes-field");
                minutesField.AddToClassList("rotation-minutes-column");
                minutesField.RegisterValueChangedCallback(_ => UpdateMinutesTotal());

                _orderFields[assignment.Player.Id] = orderField;
                _minuteFields[assignment.Player.Id] = minutesField;

                row.Add(orderField);
                row.Add(playerName);
                row.Add(position);
                row.Add(minutesField);

                _rotationList.Add(row);
            }

            UpdateMinutesTotal();
        }

        private void UpdateMinutesTotal()
        {
            var totalMinutes = _minuteFields.Values.Sum(field => field.value);

            _minutesTotalLabel.text = $"{totalMinutes} / 200 MIN";

            _minutesTotalLabel.RemoveFromClassList("rotation-total-valid");
            _minutesTotalLabel.RemoveFromClassList("rotation-total-invalid");

            _minutesTotalLabel.AddToClassList(totalMinutes == 200 ? "rotation-total-valid" : "rotation-total-invalid");
        }

        private void Apply()
        {
            var rimWeight = _rimSlider.value;
            var midRangeWeight = _midRangeSlider.value;
            var threePointWeight = _threePointSlider.value;

            if (rimWeight + midRangeWeight + threePointWeight == 0)
            {
                _saveStateLabel.text = "Shot profile cannot be 0 / 0 / 0";

                return;
            }

            try
            {
                var tactics = new TeamTactics(
                    pace: _paceSlider.value,
                    rimWeight: rimWeight,
                    midRangeWeight: midRangeWeight,
                    threePointWeight: threePointWeight,
                    ballMovement: _ballMovementSlider.value,
                    perimeterPressure: _perimeterPressureSlider.value,
                    protectPaint: _protectPaintSlider.value);

                var rotation = BuildRotationFromControls();

                Session.UserTactics = tactics;
                Session.UserRotation = rotation;

                RenderSummary();
                RenderRotationControls();

                _saveStateLabel.text = "Tactics and rotation applied";

                RotationChanged?.Invoke();
            }
            catch (ArgumentException exception)
            {
                _saveStateLabel.text = exception.Message;
            }
        }

        private void Reset()
        {
            Session.UserTactics = TeamTactics.Default;
            Session.UserRotation = TeamRotation.CreateDefault(Session.UserTeam);

            Render();

            _saveStateLabel.text = "Default tactics restored";

            RotationChanged?.Invoke();
        }

        private TeamRotation BuildRotationFromControls()
        {
            var starters = new Dictionary<PlayerPosition, Player>
            {
                [PlayerPosition.PointGuard] = FindPlayer(_starterPointGuardDropdown.value),
                [PlayerPosition.ShootingGuard] = FindPlayer(_starterShootingGuardDropdown.value),
                [PlayerPosition.SmallForward] = FindPlayer(_starterSmallForwardDropdown.value),
                [PlayerPosition.PowerForward] = FindPlayer(_starterPowerForwardDropdown.value),
                [PlayerPosition.Center] = FindPlayer(_starterCenterDropdown.value)
            };

            var assignments = Session.UserTeam.Players
                .Select(player => new PlayerRotationAssignment(
                    player,
                    _minuteFields[player.Id].value,
                    _orderFields[player.Id].value))
                .ToList();

            return new TeamRotation(
                Session.UserTeam,
                starters,
                assignments,
                FindPlayer(_primaryBallHandlerDropdown.value),
                FindPlayer(_primaryScorerDropdown.value));
        }

        private Player FindPlayer(string fullName)
        {
            return Session.UserTeam.Players.Single(player => player.FullName == fullName);
        }
    }
}