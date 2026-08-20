using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Competitions;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public sealed class CloseSeasonScreenController : ScreenController
    {
        protected override string ScreenElementName => "close-season-screen";

        public event Action ContinueRequested;

        private const int MaximumMoversShown = 6;

        private Label _seasonLabel;
        private Label _summaryLabel;
        private VisualElement _retirementList;
        private VisualElement _improvementList;
        private VisualElement _declineList;
        private Button _continueButton;

        protected override void FindControls(VisualElement documentRoot)
        {
            _seasonLabel = documentRoot.Q<Label>("close-season-name");
            _summaryLabel = documentRoot.Q<Label>("close-season-summary");
            _retirementList = documentRoot.Q<VisualElement>("close-season-retirements");
            _improvementList = documentRoot.Q<VisualElement>("close-season-improvements");
            _declineList = documentRoot.Q<VisualElement>("close-season-declines");
            _continueButton = documentRoot.Q<Button>("close-season-continue-button");
        }

        public void RegisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _continueButton.clicked -= RaiseContinueRequested;
            _continueButton.clicked += RaiseContinueRequested;
        }

        public void UnregisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _continueButton.clicked -= RaiseContinueRequested;
        }

        private void RaiseContinueRequested()
        {
            ContinueRequested?.Invoke();
        }

        public override void Render()
        {
            if (!IsBound)
            {
                return;
            }

            var report = Session.Career.LastCloseSeason;

            _seasonLabel.text = Session.Season.Name;

            if (report == null)
            {
                _summaryLabel.text = "Nothing to report.";

                _retirementList.Clear();
                _improvementList.Clear();
                _declineList.Clear();

                return;
            }

            var userTeamId = Session.UserTeam.Id;

            var retirements = report.Retirements
                .Where(record => record.Team.Id == userTeamId)
                .ToList();

            var improvements = report.Improvements
                .Where(record => record.Team.Id == userTeamId)
                .OrderByDescending(record => record.Change)
                .ThenByDescending(record => record.OverallAfter)
                .Take(MaximumMoversShown)
                .ToList();

            var declines = report.Declines
                .Where(record => record.Team.Id == userTeamId)
                .OrderBy(record => record.Change)
                .ThenByDescending(record => record.OverallAfter)
                .Take(MaximumMoversShown)
                .ToList();

            _summaryLabel.text = BuildSummary(retirements.Count, improvements.Count, declines.Count, report);

            RenderRetirements(retirements);
            RenderMovers(_improvementList, improvements, "No players improved this close season.");
            RenderMovers(_declineList, declines, "Nobody went backwards this close season.");
        }

        private string BuildSummary(int retirements, int improvements, int declines, CloseSeasonReport report)
        {
            var parts = new List<string>();

            parts.Add(retirements == 1 ? "1 retirement" : $"{retirements} retirements");
            parts.Add($"{improvements} improved");
            parts.Add($"{declines} declined");

            var elsewhere = report.TotalRetirements - retirements;

            if (elsewhere > 0)
            {
                parts.Add(elsewhere == 1 ? "1 retirement elsewhere in the league" : $"{elsewhere} retirements elsewhere in the league");
            }

            return string.Join($" {ScreenFormatting.Separator} ", parts);
        }

        private void RenderRetirements(IReadOnlyList<RetirementRecord> retirements)
        {
            _retirementList.Clear();

            if (retirements.Count == 0)
            {
                _retirementList.Add(ScreenFormatting.CreateLabel("Nobody retired from your squad.", "box-score-placeholder"));

                return;
            }

            foreach (var record in retirements)
            {
                var row = new VisualElement();
                row.AddToClassList("close-season-row");

                var details = new VisualElement();
                details.AddToClassList("close-season-details");

                details.Add(ScreenFormatting.CreateLabel($"{record.Retired.FullName} retired at {record.RetiredAtAge}", "close-season-headline"));

                var replacement = record.Replacement;

                var upside = replacement.ScoutedPotential > replacement.CurrentAbility ? $", scouted ceiling {replacement.ScoutedPotential}" : string.Empty;

                details.Add(ScreenFormatting.CreateLabel(
                    $"Replaced by {replacement.FullName}, {replacement.Age}, " +
                    $"{ScreenFormatting.GetPositionAbbreviation(replacement.Position)}, " +
                    $"rated {replacement.CurrentAbility}{upside}",
                    "close-season-detail"));

                row.Add(details);

                _retirementList.Add(row);
            }
        }

        private static void RenderMovers(VisualElement list, IReadOnlyList<DevelopmentRecord> records, string emptyMessage)
        {
            list.Clear();

            if (records.Count == 0)
            {
                list.Add(ScreenFormatting.CreateLabel(emptyMessage, "box-score-placeholder"));

                return;
            }

            foreach (var record in records)
            {
                var row = new VisualElement();
                row.AddToClassList("close-season-row");

                row.Add(ScreenFormatting.CreateLabel($"{record.Player.FullName} ({record.Player.Age})", "close-season-mover-name"));

                row.Add(ScreenFormatting.CreateLabel($"{record.OverallBefore} to {record.OverallAfter}", "close-season-mover-range"));

                var change = ScreenFormatting.CreateLabel(record.Change > 0 ? $"+{record.Change}" : record.Change.ToString(), "close-season-mover-change");

                change.AddToClassList(record.Change > 0 ? "standings-difference-positive" : "standings-difference-negative");

                row.Add(change);

                list.Add(row);
            }
        }
    }
}
