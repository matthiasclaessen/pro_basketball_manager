using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Presentation.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public sealed class MatchCentreScreenController : ScreenController
    {
        protected override string ScreenElementName => "match-centre-screen";

        public event Action ReplayStarted;

        public event Action<ContinueOutcome> ReplayFinished;

        public event Action BackRequested;

        private const float SecondsPerEvent = 0.45f;

        private Label _statusLabel;
        private Label _homeTeamLabel;
        private Label _awayTeamLabel;
        private Label _homeScoreLabel;
        private Label _awayScoreLabel;
        private Label _periodLabel;
        private Label _clockLabel;
        private Label _seedLabel;
        private Label _eventCountLabel;
        private Label _boxScoreStateLabel;

        private ScrollView _playByPlayScroll;
        private VisualElement _playByPlayList;
        private VisualElement _boxScoreList;

        private Button _startMatchButton;
        private Button _backButton;
        private Button _speed1Button;
        private Button _speed2Button;
        private Button _speed4Button;
        private Button _instantResultButton;

        private Coroutine _replayCoroutine;
        private int _eventIndex;
        private bool _matchStarted;
        private float _replaySpeed = 1f;

        protected override void FindControls(VisualElement documentRoot)
        {
            _statusLabel = documentRoot.Q<Label>("match-centre-status");
            _homeTeamLabel = documentRoot.Q<Label>("match-centre-home-team");
            _awayTeamLabel = documentRoot.Q<Label>("match-centre-away-team");
            _homeScoreLabel = documentRoot.Q<Label>("match-centre-home-score");
            _awayScoreLabel = documentRoot.Q<Label>("match-centre-away-score");
            _periodLabel = documentRoot.Q<Label>("match-centre-period");
            _clockLabel = documentRoot.Q<Label>("match-centre-clock");

            _seedLabel = documentRoot.Q<Label>("match-seed");
            _eventCountLabel = documentRoot.Q<Label>("match-event-count");
            _boxScoreStateLabel = documentRoot.Q<Label>("box-score-state");

            _playByPlayScroll = documentRoot.Q<ScrollView>("play-by-play-scroll");
            _playByPlayList = documentRoot.Q<VisualElement>("play-by-play-list");
            _boxScoreList = documentRoot.Q<VisualElement>("box-score-list");

            _startMatchButton = documentRoot.Q<Button>("start-match-button");
            _backButton = documentRoot.Q<Button>("back-to-dashboard-button");
            _speed1Button = documentRoot.Q<Button>("speed-1x-button");
            _speed2Button = documentRoot.Q<Button>("speed-2x-button");
            _speed4Button = documentRoot.Q<Button>("speed-4x-button");
            _instantResultButton = documentRoot.Q<Button>("instant-result-button");
        }

        public void RegisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            UnregisterCallbacks();

            _startMatchButton.clicked += StartMatch;
            _backButton.clicked += RaiseBackRequested;
            _speed1Button.clicked += SetSpeed1;
            _speed2Button.clicked += SetSpeed2;
            _speed4Button.clicked += SetSpeed4;
            _instantResultButton.clicked += ShowInstantResult;
        }

        public void UnregisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _startMatchButton.clicked -= StartMatch;
            _backButton.clicked -= RaiseBackRequested;
            _speed1Button.clicked -= SetSpeed1;
            _speed2Button.clicked -= SetSpeed2;
            _speed4Button.clicked -= SetSpeed4;
            _instantResultButton.clicked -= ShowInstantResult;
        }

        private void RaiseBackRequested()
        {
            BackRequested?.Invoke();
        }

        public override void Render()
        {
        }

        public void PrepareMatch()
        {
            if (!IsBound || Session.CurrentFixture == null || Session.CurrentFixture.IsPlayed)
            {
                return;
            }

            StopReplay();

            _matchStarted = false;
            _eventIndex = 0;
            _replaySpeed = 1f;

            _playByPlayList.Clear();
            _boxScoreList.Clear();

            _boxScoreStateLabel.text = "Available after final buzzer";
            _boxScoreList.Add(ScreenFormatting.CreateLabel(
                "The final player box score will appear here.",
                "box-score-placeholder"));

            _playByPlayList.Add(ScreenFormatting.CreateLabel(
                "Press Start Match to watch the game unfold, or Instant Result to skip to the final score.",
                "box-score-placeholder"));

            _statusLabel.text = "PRE-MATCH";
            _homeTeamLabel.text = Session.CurrentFixture.HomeTeam.Name;
            _awayTeamLabel.text = Session.CurrentFixture.AwayTeam.Name;
            _homeScoreLabel.text = "0";
            _awayScoreLabel.text = "0";
            _periodLabel.text = "Q1";
            _clockLabel.text = "10:00";

            _seedLabel.text = ScreenFormatting.NoValue;
            _eventCountLabel.text = ScreenFormatting.NoValue;

            _startMatchButton.SetEnabled(true);
            _instantResultButton.SetEnabled(true);
            _backButton.SetEnabled(true);

            SetSpeedButtonsEnabled(false);
            UpdateSpeedButtons();

            Show();
        }

        public void StartMatch()
        {
            if (!IsBound || _matchStarted)
            {
                return;
            }

            if (!EnsureMatchSimulated())
            {
                return;
            }

            _playByPlayList.Clear();

            _statusLabel.text = "LIVE";

            _startMatchButton.SetEnabled(false);
            _backButton.SetEnabled(false);
            _instantResultButton.SetEnabled(true);

            SetSpeedButtonsEnabled(true);
            UpdateSpeedButtons();

            ReplayStarted?.Invoke();

            _replayCoroutine = StartCoroutine(ReplayMatch());
        }

        private bool EnsureMatchSimulated()
        {
            if (_matchStarted)
            {
                return true;
            }

            var result = Session.SimulateCurrentFixture();

            if (result == null)
            {
                return false;
            }

            _matchStarted = true;
            _eventIndex = 0;

            _seedLabel.text = Session.LastUsedSeed.ToString();
            _eventCountLabel.text = result.Events.Count.ToString();

            return true;
        }

        public void StopReplay()
        {
            if (_replayCoroutine == null)
            {
                return;
            }

            StopCoroutine(_replayCoroutine);

            _replayCoroutine = null;
        }

        private IEnumerator ReplayMatch()
        {
            var events = Session.CurrentMatchResult.Events;

            while (_eventIndex < events.Count)
            {
                ApplyEvent(events[_eventIndex]);

                _eventIndex++;

                yield return new WaitForSecondsRealtime(SecondsPerEvent / _replaySpeed);
            }

            FinishReplay();
        }

        private void ShowInstantResult()
        {
            if (!IsBound)
            {
                return;
            }

            StopReplay();

            if (!EnsureMatchSimulated())
            {
                return;
            }

            _playByPlayList.Clear();

            ReplayStarted?.Invoke();

            var events = Session.CurrentMatchResult.Events;

            while (_eventIndex < events.Count)
            {
                ApplyEvent(events[_eventIndex]);

                _eventIndex++;
            }

            FinishReplay();
        }

        private void FinishReplay()
        {
            _replayCoroutine = null;

            _statusLabel.text = "FINAL";
            _clockLabel.text = "00:00";

            _startMatchButton.SetEnabled(false);
            _backButton.SetEnabled(true);
            _instantResultButton.SetEnabled(false);

            SetSpeedButtonsEnabled(false);

            RenderBoxScore();

            Session.CompleteCurrentFixture();

            var outcome = Session.Continue();

            ReplayFinished?.Invoke(outcome);
        }

        private void ApplyEvent(MatchEvent matchEvent)
        {
            _homeScoreLabel.text = matchEvent.HomeScore.ToString();
            _awayScoreLabel.text = matchEvent.AwayScore.ToString();
            _periodLabel.text = GetPeriodName(matchEvent.PeriodNumber);
            _clockLabel.text = FormatClock(matchEvent.SecondsRemaining);

            AddPlayByPlayRow(matchEvent);
        }

        private void AddPlayByPlayRow(MatchEvent matchEvent)
        {
            var row = new VisualElement();
            row.AddToClassList("play-by-play-row");

            var timeText = $"{GetPeriodName(matchEvent.PeriodNumber)} {FormatClock(matchEvent.SecondsRemaining)}";

            row.Add(ScreenFormatting.CreateLabel(timeText, "play-by-play-time"));
            row.Add(ScreenFormatting.CreateLabel(FormatEvent(matchEvent), "play-by-play-description"));
            row.Add(ScreenFormatting.CreateLabel($"{matchEvent.HomeScore}-{matchEvent.AwayScore}", "play-by-play-score"));

            _playByPlayList.Add(row);
            _playByPlayScroll.ScrollTo(row);
        }

        private void SetSpeed1()
        {
            _replaySpeed = 1f;

            UpdateSpeedButtons();
        }

        private void SetSpeed2()
        {
            _replaySpeed = 2f;

            UpdateSpeedButtons();
        }

        private void SetSpeed4()
        {
            _replaySpeed = 4f;

            UpdateSpeedButtons();
        }

        private void SetSpeedButtonsEnabled(bool enabled)
        {
            _speed1Button.SetEnabled(enabled);
            _speed2Button.SetEnabled(enabled);
            _speed4Button.SetEnabled(enabled);
        }

        private void UpdateSpeedButtons()
        {
            _speed1Button.RemoveFromClassList("speed-button-active");
            _speed2Button.RemoveFromClassList("speed-button-active");
            _speed4Button.RemoveFromClassList("speed-button-active");

            if (Math.Abs(_replaySpeed - 1f) < 0.01f)
            {
                _speed1Button.AddToClassList("speed-button-active");
            }
            else if (Math.Abs(_replaySpeed - 2f) < 0.01f)
            {
                _speed2Button.AddToClassList("speed-button-active");
            }
            else
            {
                _speed4Button.AddToClassList("speed-button-active");
            }
        }

        private void RenderBoxScore()
        {
            _boxScoreList.Clear();

            _boxScoreStateLabel.text = "FINAL";

            var result = Session.CurrentMatchResult;

            AddTeamBoxScore(result.HomeTeam.Name, result.HomePlayerStats);
            AddTeamBoxScore(result.AwayTeam.Name, result.AwayPlayerStats);
        }

        private void AddTeamBoxScore(string teamName, IReadOnlyList<PlayerBoxScore> playerStats)
        {
            var header = new VisualElement();
            header.AddToClassList("box-score-team-header");
            header.Add(ScreenFormatting.CreateLabel(teamName, "box-score-team-name"));

            _boxScoreList.Add(header);

            var ordered = playerStats
                .OrderByDescending(stats => stats.IsStarter)
                .ThenByDescending(stats => stats.MinutesPlayed);

            foreach (var stats in ordered)
            {
                _boxScoreList.Add(CreatePlayerBoxScoreRow(stats));
            }
        }

        private static VisualElement CreatePlayerBoxScoreRow(PlayerBoxScore stats)
        {
            var row = new VisualElement();
            row.AddToClassList("box-score-row");

            if (stats.IsStarter)
            {
                row.AddToClassList("box-score-starter");
            }

            var starterIndicator = stats.IsStarter ? " \u2022" : string.Empty;

            var minutes = stats.MinutesPlayed > 0 ? stats.MinutesPlayed.ToString("0.0") : "-";

            row.Add(ScreenFormatting.CreateLabel($"{stats.Player.FullName}{starterIndicator}", "box-score-player"));
            row.Add(ScreenFormatting.CreateLabel(minutes, "box-score-minutes"));
            row.Add(ScreenFormatting.CreateLabel(stats.Points.ToString(), "box-score-number"));
            row.Add(ScreenFormatting.CreateLabel($"{stats.FieldGoalsMade}/{stats.FieldGoalsAttempted}", "box-score-shooting"));
            row.Add(ScreenFormatting.CreateLabel($"{stats.ThreePointsMade}/{stats.ThreePointsAttempted}", "box-score-shooting"));
            row.Add(ScreenFormatting.CreateLabel($"{stats.FreeThrowsMade}/{stats.FreeThrowsAttempted}", "box-score-shooting"));
            row.Add(ScreenFormatting.CreateLabel(stats.Rebounds.ToString(), "box-score-number"));
            row.Add(ScreenFormatting.CreateLabel(stats.Assists.ToString(), "box-score-number"));
            row.Add(ScreenFormatting.CreateLabel(stats.Steals.ToString(), "box-score-number"));
            row.Add(ScreenFormatting.CreateLabel(stats.PersonalFouls.ToString(), "box-score-number"));
            row.Add(ScreenFormatting.CreateLabel(stats.Turnovers.ToString(), "box-score-number"));
            row.Add(CreateConditionLabel(stats));

            return row;
        }

        private static Label CreateConditionLabel(PlayerBoxScore stats)
        {
            if (stats.SecondsPlayed <= 0.0)
            {
                return ScreenFormatting.CreateLabel(ScreenFormatting.NoValue, "box-score-number");
            }

            var lowestCondition = (1.0 - stats.PeakFatigue) * 100.0;

            var label = ScreenFormatting.CreateLabel($"{lowestCondition:0}%", "box-score-number");

            if (lowestCondition < 55.0)
            {
                label.AddToClassList("box-score-condition-low");
            }
            else if (lowestCondition < 75.0)
            {
                label.AddToClassList("box-score-condition-medium");
            }
            else
            {
                label.AddToClassList("box-score-condition-high");
            }

            return label;
        }

        private static string GetPeriodName(int periodNumber)
        {
            return periodNumber <= 4 ? $"Q{periodNumber}" : $"OT{periodNumber - 4}";
        }

        private static string FormatClock(int seconds)
        {
            var minutes = seconds / 60;
            var remainingSeconds = seconds % 60;

            return $"{minutes:00}:{remainingSeconds:00}";
        }

        private static string FormatEvent(MatchEvent matchEvent)
        {
            return matchEvent.Type switch
            {
                MatchEventType.Turnover => $"{matchEvent.Player.FullName} commits a turnover.",
                MatchEventType.Steal => $"{matchEvent.Player.FullName} steals the ball from {matchEvent.SecondaryPlayer.FullName}.",
                MatchEventType.MadeTwoPointer => FormatMadeShot(matchEvent),
                MatchEventType.MissedTwoPointer => FormatMissedShot(matchEvent),
                MatchEventType.MadeThreePointer => FormatMadeShot(matchEvent),
                MatchEventType.MissedThreePointer => FormatMissedShot(matchEvent),
                MatchEventType.OffensiveRebound => $"{matchEvent.Player.FullName} grabs the offensive rebound.",
                MatchEventType.DefensiveRebound => $"{matchEvent.Player.FullName} grabs the defensive rebound.",
                MatchEventType.ShootingFoul => $"{matchEvent.Player.FullName} fouls {matchEvent.SecondaryPlayer.FullName} on the shot.",
                MatchEventType.PersonalFoul => $"{matchEvent.Player.FullName} fouls {matchEvent.SecondaryPlayer.FullName}.",
                MatchEventType.OffensiveFoul => $"{matchEvent.Player.FullName} is called for an offensive foul.",
                MatchEventType.LooseBallFoul => $"{matchEvent.Player.FullName} is called for a loose ball foul.",
                MatchEventType.FoulOut => $"{matchEvent.Player.FullName} fouls out.",
                MatchEventType.MadeFreeThrow => $"{matchEvent.Player.FullName} makes the free throw.",
                MatchEventType.MissedFreeThrow => $"{matchEvent.Player.FullName} misses the free throw.",
                _ => matchEvent.Player.FullName
            };
        }

        private static string FormatMadeShot(MatchEvent matchEvent)
        {
            var description = GetShotDescription(matchEvent);

            return matchEvent.SecondaryPlayer == null ? $"{matchEvent.Player.FullName} {description} and scores." : $"{matchEvent.Player.FullName} {description} and scores, assisted by {matchEvent.SecondaryPlayer.FullName}.";
        }

        private static string FormatMissedShot(MatchEvent matchEvent)
        {
            return $"{matchEvent.Player.FullName} {GetShotDescription(matchEvent)} and misses.";
        }

        private static string GetShotDescription(MatchEvent matchEvent)
        {
            if (!matchEvent.OffensiveAction.HasValue || !matchEvent.ShotZone.HasValue)
            {
                return "takes a shot";
            }

            var action = matchEvent.OffensiveAction.Value;
            var zone = matchEvent.ShotZone.Value;

            return action switch
            {
                OffensiveActionType.Drive => zone == ShotZone.AtRim ? "drives to the rim" : "drives into the paint",
                OffensiveActionType.PostUp => zone == ShotZone.AtRim ? "posts up and attacks the rim" : "shoots from the post",
                OffensiveActionType.PullUp => zone == ShotZone.MidRange ? "pulls up from mid-range" : "pulls up from three",
                OffensiveActionType.SpotUp => zone == ShotZone.CornerThree ? "takes a corner three" : "takes a three-pointer",
                _ => "takes a shot"
            };
        }
    }
}
