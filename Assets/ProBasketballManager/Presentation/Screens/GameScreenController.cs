using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Demo;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Statistics;
using ProBasketballManager.Domain.Tactics;
using ProBasketballManager.Domain.Teams;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameScreenController : MonoBehaviour
    {
        private Season _season;
        private Team _userTeam;
        private Fixture _currentFixture;
        private Team _homeTeam;
        private Team _awayTeam;
        private MatchResult _currentMatchResult;

        private TeamTactics _userTactics;
        private TeamRotation _userRotation;

        private VisualElement _dashboardScreen;
        private VisualElement _squadScreen;
        private VisualElement _tacticsScreen;
        private VisualElement _scheduleScreen;
        private VisualElement _leagueScreen;
        private VisualElement _matchCentreScreen;

        private VisualElement _periodScores;
        private VisualElement _leagueList;
        private VisualElement _playByPlayList;
        private VisualElement _boxScoreList;
        private VisualElement _rotationList;
        private VisualElement _squadList;
        private VisualElement _scheduleList;
        private VisualElement _leagueStandingsList;

        private ScrollView _playByPlayScroll;

        private Label _clubNameLabel;
        private Label _leagueNameLabel;
        private Label _seasonNameLabel;
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

        private Label _matchCentreStatusLabel;
        private Label _matchCentreHomeTeamLabel;
        private Label _matchCentreAwayTeamLabel;
        private Label _matchCentreHomeScoreLabel;
        private Label _matchCentreAwayScoreLabel;
        private Label _matchCentrePeriodLabel;
        private Label _matchCentreClockLabel;
        private Label _matchSeedLabel;
        private Label _matchEventCountLabel;
        private Label _boxScoreStateLabel;

        private Label _tacticsSummaryLabel;
        private Label _tacticsSaveStateLabel;
        private Label _rotationMinutesTotalLabel;

        private Label _squadSubtitleLabel;
        private Label _squadGamesBadgeLabel;
        private Label _squadRosterValueLabel;
        private Label _squadRecordValueLabel;
        private Label _squadLeadingScorerValueLabel;
        private Label _squadLeadingScorerDetailLabel;

        private Label _scheduleRoundBadgeLabel;
        private Label _leagueScreenSubtitleLabel;
        private Label _leagueProgressBadgeLabel;

        private Button _navDashboardButton;
        private Button _navSquadButton;
        private Button _navTacticsButton;
        private Button _navScheduleButton;
        private Button _navLeagueButton;

        private Button _simulateMatchButton;
        private Button _backToDashboardButton;
        private Button _speed1Button;
        private Button _speed2Button;
        private Button _speed4Button;
        private Button _instantResultButton;

        private Button _applyTacticsButton;
        private Button _resetTacticsButton;

        private SliderInt _paceSlider;
        private SliderInt _rimSlider;
        private SliderInt _midRangeSlider;
        private SliderInt _threePointSlider;
        private SliderInt _ballMovementSlider;
        private SliderInt _perimeterPressureSlider;
        private SliderInt _protectPaintSlider;

        private DropdownField _starterPointGuardDropdown;
        private DropdownField _starterShootingGuardDropdown;
        private DropdownField _starterSmallForwardDropdown;
        private DropdownField _starterPowerForwardDropdown;
        private DropdownField _starterCenterDropdown;
        private DropdownField _primaryBallHandlerDropdown;
        private DropdownField _primaryScorerDropdown;

        private readonly Dictionary<int, IntegerField> _rotationMinuteFields = new Dictionary<int, IntegerField>();
        private readonly Dictionary<int, IntegerField> _rotationOrderFields = new Dictionary<int, IntegerField>();

        private Coroutine _replayCoroutine;

        private uint _nextSeed = 12345;
        private int _eventIndex;
        private float _replaySpeed = 1f;
        private bool _currentFixtureRecorded;

        private void OnEnable()
        {
            var document = GetComponent<UIDocument>();
            var root = document.rootVisualElement;

            FindScreens(root);
            FindNavigation(root);
            FindDashboardControls(root);
            FindSquadControls(root);
            FindTacticsControls(root);
            FindSeasonControls(root);
            FindMatchCentreControls(root);

            CreateGameData();
            RenderAllSeasonViews();
            ShowDashboard();

            RegisterCallbacks();
        }

        private void OnDisable()
        {
            StopReplay();
            UnregisterCallbacks();
        }

        private void FindScreens(VisualElement root)
        {
            _dashboardScreen = root.Q<VisualElement>("dashboard-screen");
            _squadScreen = root.Q<VisualElement>("squad-screen");
            _tacticsScreen = root.Q<VisualElement>("tactics-screen");
            _scheduleScreen = root.Q<VisualElement>("schedule-screen");
            _leagueScreen = root.Q<VisualElement>("league-screen");
            _matchCentreScreen = root.Q<VisualElement>("match-centre-screen");
        }

        private void FindNavigation(VisualElement root)
        {
            _navDashboardButton = root.Q<Button>("nav-dashboard-button");
            _navSquadButton = root.Q<Button>("nav-squad-button");
            _navTacticsButton = root.Q<Button>("nav-tactics-button");
            _navScheduleButton = root.Q<Button>("nav-schedule-button");
            _navLeagueButton = root.Q<Button>("nav-league-button");
        }

        private void FindDashboardControls(VisualElement root)
        {
            _clubNameLabel = root.Q<Label>("club-name");
            _leagueNameLabel = root.Q<Label>("league-name");
            _seasonNameLabel = root.Q<Label>("season-name");

            _homeTeamNameLabel = root.Q<Label>("home-team-name");
            _awayTeamNameLabel = root.Q<Label>("away-team-name");
            _homeScoreLabel = root.Q<Label>("home-score");
            _awayScoreLabel = root.Q<Label>("away-score");
            _matchStatusLabel = root.Q<Label>("match-status");
            _fixtureInfoLabel = root.Q<Label>("fixture-info");

            _clubPanelNameLabel = root.Q<Label>("club-panel-name");
            _rosterCountLabel = root.Q<Label>("roster-count");
            _threePointRatingLabel = root.Q<Label>("three-point-rating");
            _defenseRatingLabel = root.Q<Label>("defense-rating");

            _periodScores = root.Q<VisualElement>("period-scores");
            _leagueList = root.Q<VisualElement>("league-list");

            _simulateMatchButton = root.Q<Button>("simulate-match-button");
        }

        private void FindSquadControls(VisualElement root)
        {
            _squadList = root.Q<VisualElement>("squad-list");

            _squadSubtitleLabel = root.Q<Label>("squad-subtitle");
            _squadGamesBadgeLabel = root.Q<Label>("squad-games-badge");
            _squadRosterValueLabel = root.Q<Label>("squad-roster-value");
            _squadRecordValueLabel = root.Q<Label>("squad-record-value");
            _squadLeadingScorerValueLabel = root.Q<Label>("squad-leading-scorer-value");
            _squadLeadingScorerDetailLabel = root.Q<Label>("squad-leading-scorer-detail");
        }

        private void FindTacticsControls(VisualElement root)
        {
            _paceSlider = root.Q<SliderInt>("pace-slider");
            _rimSlider = root.Q<SliderInt>("rim-slider");
            _midRangeSlider = root.Q<SliderInt>("mid-range-slider");
            _threePointSlider = root.Q<SliderInt>("three-point-slider");
            _ballMovementSlider = root.Q<SliderInt>("ball-movement-slider");
            _perimeterPressureSlider = root.Q<SliderInt>("perimeter-pressure-slider");
            _protectPaintSlider = root.Q<SliderInt>("protect-paint-slider");

            _applyTacticsButton = root.Q<Button>("apply-tactics-button");
            _resetTacticsButton = root.Q<Button>("reset-tactics-button");

            _tacticsSummaryLabel = root.Q<Label>("tactics-summary");
            _tacticsSaveStateLabel = root.Q<Label>("tactics-save-state");

            _rotationList = root.Q<VisualElement>("rotation-list");

            _starterPointGuardDropdown = root.Q<DropdownField>("starter-pg-dropdown");
            _starterShootingGuardDropdown = root.Q<DropdownField>("starter-sg-dropdown");
            _starterSmallForwardDropdown = root.Q<DropdownField>("starter-sf-dropdown");
            _starterPowerForwardDropdown = root.Q<DropdownField>("starter-pf-dropdown");
            _starterCenterDropdown = root.Q<DropdownField>("starter-c-dropdown");

            _primaryBallHandlerDropdown = root.Q<DropdownField>("primary-ball-handler-dropdown");
            _primaryScorerDropdown = root.Q<DropdownField>("primary-scorer-dropdown");

            _rotationMinutesTotalLabel = root.Q<Label>("rotation-minutes-total");
        }

        private void FindSeasonControls(VisualElement root)
        {
            _scheduleList = root.Q<VisualElement>("schedule-list");
            _leagueStandingsList = root.Q<VisualElement>("league-standings-list");

            _scheduleRoundBadgeLabel = root.Q<Label>("schedule-round-badge");
            _leagueScreenSubtitleLabel = root.Q<Label>("league-screen-subtitle");
            _leagueProgressBadgeLabel = root.Q<Label>("league-progress-badge");
        }

        private void FindMatchCentreControls(VisualElement root)
        {
            _matchCentreStatusLabel = root.Q<Label>("match-centre-status");
            _matchCentreHomeTeamLabel = root.Q<Label>("match-centre-home-team");
            _matchCentreAwayTeamLabel = root.Q<Label>("match-centre-away-team");
            _matchCentreHomeScoreLabel = root.Q<Label>("match-centre-home-score");
            _matchCentreAwayScoreLabel = root.Q<Label>("match-centre-away-score");
            _matchCentrePeriodLabel = root.Q<Label>("match-centre-period");
            _matchCentreClockLabel = root.Q<Label>("match-centre-clock");

            _matchSeedLabel = root.Q<Label>("match-seed");
            _matchEventCountLabel = root.Q<Label>("match-event-count");
            _boxScoreStateLabel = root.Q<Label>("box-score-state");

            _playByPlayScroll = root.Q<ScrollView>("play-by-play-scroll");
            _playByPlayList = root.Q<VisualElement>("play-by-play-list");
            _boxScoreList = root.Q<VisualElement>("box-score-list");

            _backToDashboardButton = root.Q<Button>("back-to-dashboard-button");
            _speed1Button = root.Q<Button>("speed-1x-button");
            _speed2Button = root.Q<Button>("speed-2x-button");
            _speed4Button = root.Q<Button>("speed-4x-button");
            _instantResultButton = root.Q<Button>("instant-result-button");
        }

        private void RegisterCallbacks()
        {
            _navDashboardButton.clicked += ShowDashboard;
            _navSquadButton.clicked += ShowSquad;
            _navTacticsButton.clicked += ShowTactics;
            _navScheduleButton.clicked += ShowSchedule;
            _navLeagueButton.clicked += ShowLeague;

            _simulateMatchButton.clicked += StartMatchCentre;
            _backToDashboardButton.clicked += ShowDashboard;

            _speed1Button.clicked += SetSpeed1;
            _speed2Button.clicked += SetSpeed2;
            _speed4Button.clicked += SetSpeed4;
            _instantResultButton.clicked += ShowInstantResult;

            _applyTacticsButton.clicked += ApplyTactics;
            _resetTacticsButton.clicked += ResetTactics;
        }

        private void UnregisterCallbacks()
        {
            if (_navDashboardButton == null)
            {
                return;
            }

            _navDashboardButton.clicked -= ShowDashboard;
            _navSquadButton.clicked -= ShowSquad;
            _navTacticsButton.clicked -= ShowTactics;
            _navScheduleButton.clicked -= ShowSchedule;
            _navLeagueButton.clicked -= ShowLeague;

            _simulateMatchButton.clicked -= StartMatchCentre;
            _backToDashboardButton.clicked -= ShowDashboard;

            _speed1Button.clicked -= SetSpeed1;
            _speed2Button.clicked -= SetSpeed2;
            _speed4Button.clicked -= SetSpeed4;
            _instantResultButton.clicked -= ShowInstantResult;

            _applyTacticsButton.clicked -= ApplyTactics;
            _resetTacticsButton.clicked -= ResetTactics;
        }

        private void CreateGameData()
        {
            _season = DemoSeasonFactory.Create();
            _userTeam = _season.League.Teams[0];

            _userTactics = TeamTactics.Default;
            _userRotation = TeamRotation.CreateDefault(_userTeam);

            UpdateCurrentFixture();

            RenderTacticsControls();
            RenderRotationControls();
        }

        private void UpdateCurrentFixture()
        {
            _currentFixture = _season.GetNextFixtureForTeam(_userTeam);

            if (_currentFixture == null)
            {
                _homeTeam = null;
                _awayTeam = null;
                return;
            }

            _homeTeam = _currentFixture.HomeTeam;
            _awayTeam = _currentFixture.AwayTeam;
        }

        private void RenderAllSeasonViews()
        {
            RenderDashboard();
            RenderSquad();
            RenderSchedule();
            RenderLeagueScreen();
        }

        private void RenderDashboard()
        {
            _clubNameLabel.text = _userTeam.Name;
            _leagueNameLabel.text = _season.League.Name;
            _seasonNameLabel.text = _season.Name;

            RenderClubInformation();
            RenderDashboardStandings();

            _periodScores.Clear();
            _periodScores.style.display = DisplayStyle.None;

            if (_currentFixture == null)
            {
                RenderCompletedSeasonDashboard();
                return;
            }

            _homeTeamNameLabel.text = _currentFixture.HomeTeam.Name;
            _awayTeamNameLabel.text = _currentFixture.AwayTeam.Name;
            _homeScoreLabel.text = "—";
            _awayScoreLabel.text = "—";
            _matchStatusLabel.text = $"ROUND {_currentFixture.RoundNumber}";
            _fixtureInfoLabel.text = $"Round {_currentFixture.RoundNumber} of {_season.TotalRounds} · 20:00";

            _simulateMatchButton.text = "Open Match Centre";
            _simulateMatchButton.SetEnabled(true);
        }

        private void RenderCompletedSeasonDashboard()
        {
            _homeTeamNameLabel.text = _userTeam.Name;
            _awayTeamNameLabel.text = "Season complete";
            _homeScoreLabel.text = "—";
            _awayScoreLabel.text = "—";
            _matchStatusLabel.text = "FINAL";
            _fixtureInfoLabel.text = $"{_season.Name} completed";

            _simulateMatchButton.text = "Season Complete";
            _simulateMatchButton.SetEnabled(false);
        }

        private void RenderDashboardStandings()
        {
            _leagueList.Clear();

            foreach (var standing in _season.GetStandings())
            {
                var row = new VisualElement();
                row.AddToClassList("league-row");

                if (standing.Team.Id == _userTeam.Id)
                {
                    row.AddToClassList("league-row-current");
                }

                var position = new Label(standing.Position.ToString());
                position.AddToClassList("league-position");

                var teamName = new Label(standing.Team.Name);
                teamName.AddToClassList("league-team-name");

                var record = new Label($"{standing.Wins} - {standing.Losses}");
                record.AddToClassList("league-record");

                row.Add(position);
                row.Add(teamName);
                row.Add(record);

                _leagueList.Add(row);
            }
        }

        private void RenderClubInformation()
        {
            var averageThreePoint = _userTeam.Players.Average(player => player.Attributes.ThreePoint);
            var averageDefense = _userTeam.Players.Average(player => (player.Attributes.PerimeterDefense + player.Attributes.InteriorDefense) / 2.0);

            _clubPanelNameLabel.text = _userTeam.Name;
            _rosterCountLabel.text = _userTeam.Players.Count.ToString();
            _threePointRatingLabel.text = averageThreePoint.ToString("0.0");
            _defenseRatingLabel.text = averageDefense.ToString("0.0");
        }

        private void RenderSquad()
        {
            _squadList.Clear();

            var standings = _season.GetStandings();
            var userStanding = standings.Single(standing => standing.Team.Id == _userTeam.Id);
            var statistics = PlayerSeasonStatisticsCalculator.Calculate(_season, _userTeam).ToDictionary(statistics => statistics.Player.Id);

            _squadSubtitleLabel.text = $"{_userTeam.Name} · {_season.Name}";
            _squadGamesBadgeLabel.text = userStanding.Played == 1 ? "1 GAME PLAYED" : $"{userStanding.Played} GAMES PLAYED";
            _squadRosterValueLabel.text = _userTeam.Players.Count.ToString();
            _squadRecordValueLabel.text = $"{userStanding.Wins} - {userStanding.Losses}";

            var leadingScorer = statistics.Values
                .Where(playerStatistics => playerStatistics.GamesPlayed > 0)
                .OrderByDescending(playerStatistics => playerStatistics.PointsPerGame)
                .ThenByDescending(playerStatistics => playerStatistics.Points)
                .FirstOrDefault();

            if (leadingScorer == null)
            {
                _squadLeadingScorerValueLabel.text = "No games yet";
                _squadLeadingScorerDetailLabel.text = "— PPG";
            }
            else
            {
                _squadLeadingScorerValueLabel.text = leadingScorer.Player.FullName;
                _squadLeadingScorerDetailLabel.text = $"{leadingScorer.PointsPerGame:0.0} PPG";
            }

            foreach (var assignment in _userRotation.Assignments.OrderBy(assignment => assignment.RotationOrder))
            {
                AddSquadRow(statistics[assignment.Player.Id], assignment);
            }
        }

        private void AddSquadRow(PlayerSeasonStatistics statistics, PlayerRotationAssignment assignment)
        {
            var row = new VisualElement();
            row.AddToClassList("squad-row");

            var role = GetSquadRole(assignment);

            if (role == "STARTER")
            {
                row.AddToClassList("squad-row-starter");
            }
            else if (role == "RESERVE")
            {
                row.AddToClassList("squad-row-reserve");
            }

            var playerName = CreateSquadLabel(statistics.Player.FullName, "squad-player-column");

            var roleBadge = new Label(role);
            roleBadge.AddToClassList("squad-role-column");
            roleBadge.AddToClassList("squad-role-badge");

            if (role == "STARTER")
            {
                roleBadge.AddToClassList("squad-role-starter");
            }
            else if (role == "ROTATION")
            {
                roleBadge.AddToClassList("squad-role-rotation");
            }
            else
            {
                roleBadge.AddToClassList("squad-role-reserve");
            }

            var position = CreateSquadLabel(GetPositionAbbreviation(statistics.Player.Position), "squad-position-column");
            var gamesPlayed = CreateSquadLabel(statistics.GamesPlayed.ToString(), "squad-small-column");
            var gamesStarted = CreateSquadLabel(statistics.GamesStarted.ToString(), "squad-small-column");
            var minutes = CreateSquadLabel(FormatPerGame(statistics.GamesPlayed, statistics.MinutesPerGame), "squad-stat-column");
            var points = CreateSquadLabel(FormatPerGame(statistics.GamesPlayed, statistics.PointsPerGame), "squad-stat-column");
            var rebounds = CreateSquadLabel(FormatPerGame(statistics.GamesPlayed, statistics.ReboundsPerGame), "squad-stat-column");
            var assists = CreateSquadLabel(FormatPerGame(statistics.GamesPlayed, statistics.AssistsPerGame), "squad-stat-column");
            var steals = CreateSquadLabel(FormatPerGame(statistics.GamesPlayed, statistics.StealsPerGame), "squad-stat-column");
            var fieldGoalPercentage = CreateSquadLabel(FormatPercentage(statistics.FieldGoalsAttempted, statistics.FieldGoalPercentage), "squad-percent-column");
            var threePointPercentage = CreateSquadLabel(FormatPercentage(statistics.ThreePointsAttempted, statistics.ThreePointPercentage), "squad-percent-column");
            var freeThrowPercentage = CreateSquadLabel(FormatPercentage(statistics.FreeThrowsAttempted, statistics.FreeThrowPercentage), "squad-percent-column");

            row.Add(playerName);
            row.Add(roleBadge);
            row.Add(position);
            row.Add(gamesPlayed);
            row.Add(gamesStarted);
            row.Add(minutes);
            row.Add(points);
            row.Add(rebounds);
            row.Add(assists);
            row.Add(steals);
            row.Add(fieldGoalPercentage);
            row.Add(threePointPercentage);
            row.Add(freeThrowPercentage);

            _squadList.Add(row);
        }

        private string GetSquadRole(PlayerRotationAssignment assignment)
        {
            if (_userRotation.IsStarter(assignment.Player.Id))
            {
                return "STARTER";
            }

            return assignment.TargetMinutes > 0 ? "ROTATION" : "RESERVE";
        }

        private static Label CreateSquadLabel(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);

            return label;
        }

        private static string FormatPerGame(int gamesPlayed, double value)
        {
            return gamesPlayed == 0 ? "—" : value.ToString("0.0");
        }

        private static string FormatPercentage(int attempts, double percentage)
        {
            return attempts == 0 ? "—" : $"{percentage:0.0}%";
        }

        private void RenderSchedule()
        {
            _scheduleList.Clear();

            _scheduleRoundBadgeLabel.text = _season.IsComplete
                ? "SEASON COMPLETE"
                : $"ROUND {_season.CurrentRoundNumber} / {_season.TotalRounds}";

            SetSeasonBadgeState(_scheduleRoundBadgeLabel);

            for (var roundNumber = 1; roundNumber <= _season.TotalRounds; roundNumber++)
            {
                AddScheduleRound(roundNumber);
            }
        }

        private void AddScheduleRound(int roundNumber)
        {
            var fixtures = _season.GetFixturesForRound(roundNumber);

            var roundHeader = new VisualElement();
            roundHeader.AddToClassList("schedule-round-header");

            var title = new Label($"Round {roundNumber}");
            title.AddToClassList("schedule-round-title");

            var allPlayed = fixtures.All(fixture => fixture.IsPlayed);
            var roundStatus = new Label(allPlayed ? "COMPLETED" : roundNumber == _season.CurrentRoundNumber ? "CURRENT" : "UPCOMING");
            roundStatus.AddToClassList("schedule-round-status");

            roundHeader.Add(title);
            roundHeader.Add(roundStatus);

            _scheduleList.Add(roundHeader);

            foreach (var fixture in fixtures)
            {
                AddScheduleFixture(fixture);
            }
        }

        private void AddScheduleFixture(Fixture fixture)
        {
            var row = new VisualElement();
            row.AddToClassList("schedule-fixture-row");

            if (FixtureContainsTeam(fixture, _userTeam))
            {
                row.AddToClassList("schedule-fixture-user");
            }

            var homeTeam = new Label(fixture.HomeTeam.Name);
            homeTeam.AddToClassList("schedule-fixture-home");

            var result = new Label(fixture.IsPlayed ? $"{fixture.Result.HomeScore} - {fixture.Result.AwayScore}" : "vs");
            result.AddToClassList("schedule-fixture-result");

            var awayTeam = new Label(fixture.AwayTeam.Name);
            awayTeam.AddToClassList("schedule-fixture-away");

            var status = new Label(fixture.IsPlayed ? "FINAL" : fixture.Id == _currentFixture?.Id ? "NEXT" : "UPCOMING");
            status.AddToClassList("schedule-fixture-status");

            row.Add(homeTeam);
            row.Add(result);
            row.Add(awayTeam);
            row.Add(status);

            _scheduleList.Add(row);
        }

        private void RenderLeagueScreen()
        {
            _leagueStandingsList.Clear();

            _leagueScreenSubtitleLabel.text = $"{_season.League.Name} · {_season.Name}";
            _leagueProgressBadgeLabel.text = _season.IsComplete
                ? "SEASON COMPLETE"
                : $"ROUND {_season.CurrentRoundNumber} / {_season.TotalRounds}";

            SetSeasonBadgeState(_leagueProgressBadgeLabel);

            foreach (var standing in _season.GetStandings())
            {
                AddStandingRow(standing);
            }
        }

        private void AddStandingRow(LeagueStanding standing)
        {
            var row = new VisualElement();
            row.AddToClassList("standings-row");

            if (standing.Team.Id == _userTeam.Id)
            {
                row.AddToClassList("standings-row-user");
            }

            var position = CreateStandingLabel(standing.Position.ToString(), "standings-position");
            var team = CreateStandingLabel(standing.Team.Name, "standings-team");
            var played = CreateStandingLabel(standing.Played.ToString(), "standings-small");
            var wins = CreateStandingLabel(standing.Wins.ToString(), "standings-small");
            var losses = CreateStandingLabel(standing.Losses.ToString(), "standings-small");
            var pointsFor = CreateStandingLabel(standing.PointsFor.ToString(), "standings-medium");
            var pointsAgainst = CreateStandingLabel(standing.PointsAgainst.ToString(), "standings-medium");

            var differenceText = standing.PointDifference > 0 ? $"+{standing.PointDifference}" : standing.PointDifference.ToString();
            var difference = CreateStandingLabel(differenceText, "standings-medium");

            if (standing.PointDifference > 0)
            {
                difference.AddToClassList("standings-difference-positive");
            }
            else if (standing.PointDifference < 0)
            {
                difference.AddToClassList("standings-difference-negative");
            }

            row.Add(position);
            row.Add(team);
            row.Add(played);
            row.Add(wins);
            row.Add(losses);
            row.Add(pointsFor);
            row.Add(pointsAgainst);
            row.Add(difference);

            _leagueStandingsList.Add(row);
        }

        private static Label CreateStandingLabel(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);

            return label;
        }

        private void SetSeasonBadgeState(Label badge)
        {
            badge.RemoveFromClassList("season-complete-badge");

            if (_season.IsComplete)
            {
                badge.AddToClassList("season-complete-badge");
            }
        }

        private void StartMatchCentre()
        {
            if (_currentFixture == null || _currentFixture.IsPlayed)
            {
                return;
            }

            StopReplay();

            var seed = _nextSeed;
            var simulator = new MatchSimulator(new XorShiftRandom(seed));

            var userIsHome = _currentFixture.HomeTeam.Id == _userTeam.Id;

            var homeRotation = userIsHome ? _userRotation : TeamRotation.CreateDefault(_currentFixture.HomeTeam);
            var awayRotation = userIsHome ? TeamRotation.CreateDefault(_currentFixture.AwayTeam) : _userRotation;

            var homeTactics = userIsHome ? _userTactics : TeamTactics.Default;
            var awayTactics = userIsHome ? TeamTactics.Default : _userTactics;

            _currentMatchResult = simulator.Simulate(_currentFixture.HomeTeam, _currentFixture.AwayTeam, homeRotation, awayRotation, homeTactics, awayTactics);

            _nextSeed++;
            _eventIndex = 0;
            _replaySpeed = 1f;
            _currentFixtureRecorded = false;

            _playByPlayList.Clear();
            _boxScoreList.Clear();

            _boxScoreStateLabel.text = "Available after final buzzer";

            var placeholder = new Label("The final player box score will appear here.");
            placeholder.AddToClassList("box-score-placeholder");
            _boxScoreList.Add(placeholder);

            _matchCentreStatusLabel.text = "LIVE";
            _matchCentreHomeTeamLabel.text = _currentFixture.HomeTeam.Name;
            _matchCentreAwayTeamLabel.text = _currentFixture.AwayTeam.Name;
            _matchCentreHomeScoreLabel.text = "0";
            _matchCentreAwayScoreLabel.text = "0";
            _matchCentrePeriodLabel.text = "Q1";
            _matchCentreClockLabel.text = "10:00";

            _matchSeedLabel.text = seed.ToString();
            _matchEventCountLabel.text = _currentMatchResult.Events.Count.ToString();

            _backToDashboardButton.SetEnabled(false);
            _instantResultButton.SetEnabled(true);

            SetNavigationEnabled(false);
            UpdateSpeedButtons();
            ShowMatchCentre();

            _replayCoroutine = StartCoroutine(ReplayMatch());
        }

        private IEnumerator ReplayMatch()
        {
            while (_eventIndex < _currentMatchResult.Events.Count)
            {
                var matchEvent = _currentMatchResult.Events[_eventIndex];

                ApplyEvent(matchEvent);
                _eventIndex++;

                yield return new WaitForSecondsRealtime(0.45f / _replaySpeed);
            }

            FinishReplay();
        }

        private void ApplyEvent(MatchEvent matchEvent)
        {
            _matchCentreHomeScoreLabel.text = matchEvent.HomeScore.ToString();
            _matchCentreAwayScoreLabel.text = matchEvent.AwayScore.ToString();
            _matchCentrePeriodLabel.text = GetPeriodName(matchEvent.PeriodNumber);
            _matchCentreClockLabel.text = FormatClock(matchEvent.SecondsRemaining);

            AddPlayByPlayEvent(matchEvent);
        }

        private void AddPlayByPlayEvent(MatchEvent matchEvent)
        {
            var row = new VisualElement();
            row.AddToClassList("play-by-play-row");

            var time = new Label($"{GetPeriodName(matchEvent.PeriodNumber)} {FormatClock(matchEvent.SecondsRemaining)}");
            time.AddToClassList("play-by-play-time");

            var description = new Label(FormatEvent(matchEvent));
            description.AddToClassList("play-by-play-description");

            var score = new Label($"{matchEvent.HomeScore}-{matchEvent.AwayScore}");
            score.AddToClassList("play-by-play-score");

            row.Add(time);
            row.Add(description);
            row.Add(score);

            _playByPlayList.Add(row);
            _playByPlayScroll.ScrollTo(row);
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
                MatchEventType.MadeFreeThrow => $"{matchEvent.Player.FullName} makes the free throw.",
                MatchEventType.MissedFreeThrow => $"{matchEvent.Player.FullName} misses the free throw.",
                _ => matchEvent.Player.FullName
            };
        }

        private static string FormatMadeShot(MatchEvent matchEvent)
        {
            var description = GetShotDescription(matchEvent);

            if (matchEvent.SecondaryPlayer == null)
            {
                return $"{matchEvent.Player.FullName} {description} and scores.";
            }

            return $"{matchEvent.Player.FullName} {description} and scores, assisted by {matchEvent.SecondaryPlayer.FullName}.";
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

        private void FinishReplay()
        {
            _replayCoroutine = null;

            _matchCentreStatusLabel.text = "FINAL";
            _matchCentreClockLabel.text = "00:00";

            _backToDashboardButton.SetEnabled(true);
            _instantResultButton.SetEnabled(false);

            RenderBoxScore();
            CompleteCurrentRound();

            RenderAllSeasonViews();
            SetNavigationEnabled(true);
        }

        private void ShowInstantResult()
        {
            if (_currentMatchResult == null)
            {
                return;
            }

            StopReplay();

            while (_eventIndex < _currentMatchResult.Events.Count)
            {
                ApplyEvent(_currentMatchResult.Events[_eventIndex]);
                _eventIndex++;
            }

            FinishReplay();
        }

        private void CompleteCurrentRound()
        {
            if (_currentFixtureRecorded || _currentFixture == null || _currentMatchResult == null)
            {
                return;
            }

            var completedRound = _currentFixture.RoundNumber;

            _season.RecordResult(_currentFixture.Id, _currentMatchResult);

            foreach (var fixture in _season.GetFixturesForRound(completedRound).Where(fixture => !fixture.IsPlayed))
            {
                SimulateAiFixture(fixture);
            }

            _currentFixtureRecorded = true;
            UpdateCurrentFixture();
        }

        private void SimulateAiFixture(Fixture fixture)
        {
            var simulator = new MatchSimulator(new XorShiftRandom(_nextSeed));
            _nextSeed++;

            var result = simulator.Simulate(
                fixture.HomeTeam,
                fixture.AwayTeam,
                TeamRotation.CreateDefault(fixture.HomeTeam),
                TeamRotation.CreateDefault(fixture.AwayTeam),
                TeamTactics.Default,
                TeamTactics.Default
            );

            _season.RecordResult(fixture.Id, result);
        }

        private void RenderBoxScore()
        {
            _boxScoreList.Clear();
            _boxScoreStateLabel.text = "FINAL";

            AddTeamBoxScore(_currentMatchResult.HomeTeam.Name, _currentMatchResult.HomePlayerStats);
            AddTeamBoxScore(_currentMatchResult.AwayTeam.Name, _currentMatchResult.AwayPlayerStats);
        }

        private void AddTeamBoxScore(string teamName, IReadOnlyList<PlayerBoxScore> playerStats)
        {
            var teamHeader = new VisualElement();
            teamHeader.AddToClassList("box-score-team-header");

            var teamNameLabel = new Label(teamName);
            teamNameLabel.AddToClassList("box-score-team-name");

            teamHeader.Add(teamNameLabel);
            _boxScoreList.Add(teamHeader);

            var orderedStats = playerStats.OrderByDescending(stats => stats.IsStarter).ThenByDescending(stats => stats.MinutesPlayed);

            foreach (var stats in orderedStats)
            {
                AddPlayerBoxScore(stats);
            }
        }

        private void AddPlayerBoxScore(PlayerBoxScore stats)
        {
            var row = new VisualElement();
            row.AddToClassList("box-score-row");

            if (stats.IsStarter)
            {
                row.AddToClassList("box-score-starter");
            }

            var starterIndicator = stats.IsStarter ? " •" : "";

            var playerName = CreateBoxScoreLabel($"{stats.Player.FullName}{starterIndicator}", "box-score-player");
            var minutes = CreateBoxScoreLabel(stats.MinutesPlayed > 0 ? stats.MinutesPlayed.ToString("0.0") : "-", "box-score-minutes");
            var points = CreateBoxScoreLabel(stats.Points.ToString(), "box-score-number");
            var fieldGoals = CreateBoxScoreLabel($"{stats.FieldGoalsMade}/{stats.FieldGoalsAttempted}", "box-score-shooting");
            var threePoints = CreateBoxScoreLabel($"{stats.ThreePointsMade}/{stats.ThreePointsAttempted}", "box-score-shooting");
            var freeThrows = CreateBoxScoreLabel($"{stats.FreeThrowsMade}/{stats.FreeThrowsAttempted}", "box-score-shooting");
            var rebounds = CreateBoxScoreLabel(stats.Rebounds.ToString(), "box-score-number");
            var assists = CreateBoxScoreLabel(stats.Assists.ToString(), "box-score-number");
            var steals = CreateBoxScoreLabel(stats.Steals.ToString(), "box-score-number");
            var fouls = CreateBoxScoreLabel(stats.PersonalFouls.ToString(), "box-score-number");
            var turnovers = CreateBoxScoreLabel(stats.Turnovers.ToString(), "box-score-number");

            row.Add(playerName);
            row.Add(minutes);
            row.Add(points);
            row.Add(fieldGoals);
            row.Add(threePoints);
            row.Add(freeThrows);
            row.Add(rebounds);
            row.Add(assists);
            row.Add(steals);
            row.Add(fouls);
            row.Add(turnovers);

            _boxScoreList.Add(row);
        }

        private static Label CreateBoxScoreLabel(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);

            return label;
        }

        private void ApplyTactics()
        {
            var rimWeight = _rimSlider.value;
            var midRangeWeight = _midRangeSlider.value;
            var threePointWeight = _threePointSlider.value;

            if (rimWeight + midRangeWeight + threePointWeight == 0)
            {
                _tacticsSaveStateLabel.text = "Shot profile cannot be 0 / 0 / 0";
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
                    protectPaint: _protectPaintSlider.value
                );

                var rotation = CreateRotationFromControls();

                _userTactics = tactics;
                _userRotation = rotation;

                RenderTacticsSummary();
                RenderRotationControls();

                _tacticsSaveStateLabel.text = "Tactics and rotation applied";
            }
            catch (ArgumentException exception)
            {
                _tacticsSaveStateLabel.text = exception.Message;
            }
        }

        private void ResetTactics()
        {
            _userTactics = TeamTactics.Default;
            _userRotation = TeamRotation.CreateDefault(_userTeam);

            RenderTacticsControls();
            RenderRotationControls();

            _tacticsSaveStateLabel.text = "Default tactics restored";
        }

        private void RenderTacticsControls()
        {
            _paceSlider.value = _userTactics.Pace;
            _rimSlider.value = _userTactics.RimWeight;
            _midRangeSlider.value = _userTactics.MidRangeWeight;
            _threePointSlider.value = _userTactics.ThreePointWeight;
            _ballMovementSlider.value = _userTactics.BallMovement;
            _perimeterPressureSlider.value = _userTactics.PerimeterPressure;
            _protectPaintSlider.value = _userTactics.ProtectPaint;

            RenderTacticsSummary();
        }

        private void RenderTacticsSummary()
        {
            var rimPercentage = (int)Math.Round(_userTactics.RimShare * 100);
            var midRangePercentage = (int)Math.Round(_userTactics.MidRangeShare * 100);
            var threePointPercentage = (int)Math.Round(_userTactics.ThreePointShare * 100);

            _tacticsSummaryLabel.text = $"Pace {_userTactics.Pace} · Rim {rimPercentage}% · Mid {midRangePercentage}% · 3PT {threePointPercentage}% · Ball movement {_userTactics.BallMovement}";
        }

        private void RenderRotationControls()
        {
            ConfigureStarterDropdown(_starterPointGuardDropdown, PlayerPosition.PointGuard);
            ConfigureStarterDropdown(_starterShootingGuardDropdown, PlayerPosition.ShootingGuard);
            ConfigureStarterDropdown(_starterSmallForwardDropdown, PlayerPosition.SmallForward);
            ConfigureStarterDropdown(_starterPowerForwardDropdown, PlayerPosition.PowerForward);
            ConfigureStarterDropdown(_starterCenterDropdown, PlayerPosition.Center);

            var playerNames = _userTeam.Players.Select(player => player.FullName).ToList();

            _primaryBallHandlerDropdown.choices = playerNames;
            _primaryBallHandlerDropdown.value = _userRotation.PrimaryBallHandler.FullName;

            _primaryScorerDropdown.choices = playerNames;
            _primaryScorerDropdown.value = _userRotation.PrimaryScorer.FullName;

            RenderRotationRows();
        }

        private void ConfigureStarterDropdown(DropdownField dropdown, PlayerPosition position)
        {
            var eligiblePlayers = _userTeam.Players.Where(player => player.Position == position).ToList();

            dropdown.choices = eligiblePlayers.Select(player => player.FullName).ToList();
            dropdown.value = _userRotation.GetStarter(position).FullName;
        }

        private void RenderRotationRows()
        {
            _rotationList.Clear();
            _rotationMinuteFields.Clear();
            _rotationOrderFields.Clear();

            foreach (var assignment in _userRotation.Assignments.OrderBy(assignment => assignment.RotationOrder))
            {
                var row = new VisualElement();
                row.AddToClassList("rotation-row");

                var orderField = new IntegerField();
                orderField.value = assignment.RotationOrder;
                orderField.AddToClassList("rotation-order-field");
                orderField.AddToClassList("rotation-order-column");

                var playerName = new Label(assignment.Player.FullName);
                playerName.AddToClassList("rotation-player-name");
                playerName.AddToClassList("rotation-player-column");

                var position = new Label(GetPositionAbbreviation(assignment.Player.Position));
                position.AddToClassList("rotation-position");
                position.AddToClassList("rotation-position-column");

                var minutesField = new IntegerField();
                minutesField.value = assignment.TargetMinutes;
                minutesField.AddToClassList("rotation-minutes-field");
                minutesField.AddToClassList("rotation-minutes-column");
                minutesField.RegisterValueChangedCallback(_ => UpdateRotationMinutesTotal());

                _rotationOrderFields[assignment.Player.Id] = orderField;
                _rotationMinuteFields[assignment.Player.Id] = minutesField;

                row.Add(orderField);
                row.Add(playerName);
                row.Add(position);
                row.Add(minutesField);

                _rotationList.Add(row);
            }

            UpdateRotationMinutesTotal();
        }

        private static string GetPositionAbbreviation(PlayerPosition position)
        {
            return position switch
            {
                PlayerPosition.PointGuard => "PG",
                PlayerPosition.ShootingGuard => "SG",
                PlayerPosition.SmallForward => "SF",
                PlayerPosition.PowerForward => "PF",
                PlayerPosition.Center => "C",
                _ => "-"
            };
        }

        private void UpdateRotationMinutesTotal()
        {
            var totalMinutes = _rotationMinuteFields.Values.Sum(field => field.value);

            _rotationMinutesTotalLabel.text = $"{totalMinutes} / 200 MIN";

            _rotationMinutesTotalLabel.RemoveFromClassList("rotation-total-valid");
            _rotationMinutesTotalLabel.RemoveFromClassList("rotation-total-invalid");
            _rotationMinutesTotalLabel.AddToClassList(totalMinutes == 200 ? "rotation-total-valid" : "rotation-total-invalid");
        }

        private TeamRotation CreateRotationFromControls()
        {
            var starters = new Dictionary<PlayerPosition, Player>
            {
                [PlayerPosition.PointGuard] = FindPlayer(_starterPointGuardDropdown.value),
                [PlayerPosition.ShootingGuard] = FindPlayer(_starterShootingGuardDropdown.value),
                [PlayerPosition.SmallForward] = FindPlayer(_starterSmallForwardDropdown.value),
                [PlayerPosition.PowerForward] = FindPlayer(_starterPowerForwardDropdown.value),
                [PlayerPosition.Center] = FindPlayer(_starterCenterDropdown.value)
            };

            var assignments = _userTeam.Players.Select(player => new PlayerRotationAssignment(player, _rotationMinuteFields[player.Id].value, _rotationOrderFields[player.Id].value)).ToList();

            var primaryBallHandler = FindPlayer(_primaryBallHandlerDropdown.value);
            var primaryScorer = FindPlayer(_primaryScorerDropdown.value);

            return new TeamRotation(_userTeam, starters, assignments, primaryBallHandler, primaryScorer);
        }

        private Player FindPlayer(string fullName)
        {
            return _userTeam.Players.Single(player => player.FullName == fullName);
        }

        private void ShowDashboard()
        {
            HideAllScreens();
            _dashboardScreen.style.display = DisplayStyle.Flex;
            SetActiveNavigation(_navDashboardButton);
        }

        private void ShowSquad()
        {
            RenderSquad();
            HideAllScreens();
            _squadScreen.style.display = DisplayStyle.Flex;
            SetActiveNavigation(_navSquadButton);
        }

        private void ShowTactics()
        {
            HideAllScreens();
            _tacticsScreen.style.display = DisplayStyle.Flex;
            SetActiveNavigation(_navTacticsButton);
        }

        private void ShowSchedule()
        {
            RenderSchedule();
            HideAllScreens();
            _scheduleScreen.style.display = DisplayStyle.Flex;
            SetActiveNavigation(_navScheduleButton);
        }

        private void ShowLeague()
        {
            RenderLeagueScreen();
            HideAllScreens();
            _leagueScreen.style.display = DisplayStyle.Flex;
            SetActiveNavigation(_navLeagueButton);
        }

        private void ShowMatchCentre()
        {
            HideAllScreens();
            _matchCentreScreen.style.display = DisplayStyle.Flex;
            SetActiveNavigation(null);
        }

        private void HideAllScreens()
        {
            _dashboardScreen.style.display = DisplayStyle.None;
            _squadScreen.style.display = DisplayStyle.None;
            _tacticsScreen.style.display = DisplayStyle.None;
            _scheduleScreen.style.display = DisplayStyle.None;
            _leagueScreen.style.display = DisplayStyle.None;
            _matchCentreScreen.style.display = DisplayStyle.None;
        }

        private void SetActiveNavigation(Button activeButton)
        {
            _navDashboardButton.RemoveFromClassList("nav-item-active");
            _navSquadButton.RemoveFromClassList("nav-item-active");
            _navTacticsButton.RemoveFromClassList("nav-item-active");
            _navScheduleButton.RemoveFromClassList("nav-item-active");
            _navLeagueButton.RemoveFromClassList("nav-item-active");

            activeButton?.AddToClassList("nav-item-active");
        }

        private void SetNavigationEnabled(bool enabled)
        {
            _navDashboardButton.SetEnabled(enabled);
            _navSquadButton.SetEnabled(enabled);
            _navTacticsButton.SetEnabled(enabled);
            _navScheduleButton.SetEnabled(enabled);
            _navLeagueButton.SetEnabled(enabled);
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

        private void UpdateSpeedButtons()
        {
            _speed1Button.RemoveFromClassList("speed-button-active");
            _speed2Button.RemoveFromClassList("speed-button-active");
            _speed4Button.RemoveFromClassList("speed-button-active");

            if (_replaySpeed == 1f)
            {
                _speed1Button.AddToClassList("speed-button-active");
            }
            else if (_replaySpeed == 2f)
            {
                _speed2Button.AddToClassList("speed-button-active");
            }
            else
            {
                _speed4Button.AddToClassList("speed-button-active");
            }
        }

        private void StopReplay()
        {
            if (_replayCoroutine == null)
            {
                return;
            }

            StopCoroutine(_replayCoroutine);
            _replayCoroutine = null;
        }

        private static bool FixtureContainsTeam(Fixture fixture, Team team)
        {
            return fixture.HomeTeam.Id == team.Id || fixture.AwayTeam.Id == team.Id;
        }
    }
}