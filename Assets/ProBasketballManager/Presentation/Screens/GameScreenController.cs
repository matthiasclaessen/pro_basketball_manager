using System.Linq;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Teams;
using ProBasketballManager.Persistence;
using ProBasketballManager.Presentation.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameScreenController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Pre-game menu: new game, or continue a saved one.")]
        private MainMenuScreenController _mainMenuScreen;

        [SerializeField]
        [Tooltip("Save to a slot, or load an existing save mid-game.")]
        private SaveLoadScreenController _saveLoadScreen;

        [SerializeField]
        [Tooltip("Home screen: club summary, next fixture and mini league table.")]
        private DashboardScreenController _dashboardScreen;

        [SerializeField]
        [Tooltip("Squad list with season averages.")]
        private SquadScreenController _squadScreen;

        [SerializeField]
        [Tooltip("Individual player profile, opened from the squad list.")]
        private PlayerProfileScreenController _playerProfileScreen;

        [SerializeField]
        [Tooltip("Tactics sliders and the rotation plan.")]
        private TacticsScreenController _tacticsScreen;

        [SerializeField]
        [Tooltip("Full season schedule.")]
        private ScheduleScreenController _scheduleScreen;

        [SerializeField]
        [Tooltip("League standings table.")]
        private LeagueScreenController _leagueScreen;

        [SerializeField]
        [Tooltip("Live match replay, play by play and box score.")]
        private MatchCentreScreenController _matchCentreScreen;

        [SerializeField]
        [Tooltip("Season summary shown once the last fixture is played.")]
        private EndOfSeasonScreenController _endOfSeasonScreen;

        [SerializeField]
        [Tooltip("Retirements and development, shown after starting a new season.")]
        private CloseSeasonScreenController _closeSeasonScreen;

        private GameSession _session;

        private Button _navDashboardButton;
        private Button _navSquadButton;
        private Button _navTacticsButton;
        private Button _navScheduleButton;
        private Button _navLeagueButton;
        private Button _navSaveLoadButton;

        private Button _playerProfileBackButton;

        private VisualElement _navigationBar;
        private VisualElement _teamSwitcher;

        private VisualElement _root;

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;

            FindNavigation(_root);

            HideEveryScreenElement();

            BindScreen(_mainMenuScreen, nameof(MainMenuScreenController), _root, null);

            if (_mainMenuScreen != null)
            {
                _mainMenuScreen.RegisterCallbacks();
                _mainMenuScreen.NewGameRequested += StartNewGame;
                _mainMenuScreen.LoadRequested += AdoptLoadedGame;
            }

            ShowMainMenu();
        }

        private void StartNewGame()
        {
            AdoptSession(GameSession.CreateDemo());
        }

        private void AdoptLoadedGame(GameSessionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            AdoptSession(GameSession.Restore(snapshot));
        }

        private void AdoptSession(GameSession session)
        {
            _matchCentreScreen?.StopReplay();

            _session = session;

            BindScreens(_root);
            RegisterCallbacks();

            RenderAllScreens();

            SetNavigationVisible(true);
            SetNavigationEnabled(true);

            if (_session.CanAdvanceSeason)
            {
                ShowEndOfSeason();

                return;
            }

            ShowDashboard();
        }

        private void OnDisable()
        {
            _matchCentreScreen?.StopReplay();

            if (_mainMenuScreen != null)
            {
                _mainMenuScreen.UnregisterCallbacks();
                _mainMenuScreen.NewGameRequested -= StartNewGame;
                _mainMenuScreen.LoadRequested -= AdoptLoadedGame;
            }

            UnregisterCallbacks();
        }

        private void FindNavigation(VisualElement root)
        {
            _navDashboardButton = root.Q<Button>("nav-dashboard-button");
            _navSquadButton = root.Q<Button>("nav-squad-button");
            _navTacticsButton = root.Q<Button>("nav-tactics-button");
            _navScheduleButton = root.Q<Button>("nav-schedule-button");
            _navLeagueButton = root.Q<Button>("nav-league-button");
            _navSaveLoadButton = root.Q<Button>("nav-save-load-button");

            _playerProfileBackButton = root.Q<Button>("player-profile-back-button");

            _navigationBar = root.Q<VisualElement>("navigation-bar");
            _teamSwitcher = root.Q<VisualElement>("team-switcher");
        }

        private void BindScreens(VisualElement root)
        {
            BindScreen(_saveLoadScreen, nameof(SaveLoadScreenController), root);
            BindScreen(_dashboardScreen, nameof(DashboardScreenController), root);
            BindScreen(_squadScreen, nameof(SquadScreenController), root);
            BindScreen(_playerProfileScreen, nameof(PlayerProfileScreenController), root);
            BindScreen(_tacticsScreen, nameof(TacticsScreenController), root);
            BindScreen(_scheduleScreen, nameof(ScheduleScreenController), root);
            BindScreen(_leagueScreen, nameof(LeagueScreenController), root);
            BindScreen(_matchCentreScreen, nameof(MatchCentreScreenController), root);
            BindScreen(_endOfSeasonScreen, nameof(EndOfSeasonScreenController), root);
            BindScreen(_closeSeasonScreen, nameof(CloseSeasonScreenController), root);
        }

        private void BindScreen(ScreenController screen, string screenName, VisualElement root)
        {
            BindScreen(screen, screenName, root, _session);
        }

        private void BindScreen(ScreenController screen, string screenName, VisualElement root, GameSession session)
        {
            if (screen == null)
            {
                Debug.LogError($"GameScreenController has no {screenName} assigned. " + "Add the component and drag it into the matching field in the Inspector.");

                return;
            }

            screen.Bind(session, root);
        }

        private void RegisterCallbacks()
        {
            UnregisterCallbacks();

            _navDashboardButton.clicked += ShowDashboard;
            _navSquadButton.clicked += ShowSquad;
            _navTacticsButton.clicked += ShowTactics;
            _navScheduleButton.clicked += ShowSchedule;
            _navLeagueButton.clicked += ShowLeague;
            _navSaveLoadButton.clicked += ShowSaveLoad;

            _playerProfileBackButton.clicked += ShowSquad;

            if (_saveLoadScreen != null)
            {
                _saveLoadScreen.RegisterCallbacks();
                _saveLoadScreen.LoadRequested += AdoptLoadedGame;
            }

            if (_dashboardScreen != null)
            {
                _dashboardScreen.RegisterCallbacks();
                _dashboardScreen.MatchCentreRequested += StartMatchCentre;
            }

            if (_squadScreen != null)
            {
                _squadScreen.PlayerSelected += ShowPlayerProfile;
            }

            if (_tacticsScreen != null)
            {
                _tacticsScreen.RegisterCallbacks();
                _tacticsScreen.RotationChanged += OnRotationChanged;
            }

            if (_endOfSeasonScreen != null)
            {
                _endOfSeasonScreen.RegisterCallbacks();
                _endOfSeasonScreen.NextSeasonRequested += StartNextSeason;
            }

            if (_closeSeasonScreen != null)
            {
                _closeSeasonScreen.RegisterCallbacks();
                _closeSeasonScreen.ContinueRequested += ShowDashboard;
            }

            if (_matchCentreScreen != null)
            {
                _matchCentreScreen.RegisterCallbacks();
                _matchCentreScreen.ReplayStarted += OnReplayStarted;
                _matchCentreScreen.ReplayFinished += OnReplayFinished;
                _matchCentreScreen.BackRequested += ShowDashboard;
            }
        }

        private void UnregisterCallbacks()
        {
            if (_navDashboardButton != null)
            {
                _navDashboardButton.clicked -= ShowDashboard;
                _navSquadButton.clicked -= ShowSquad;
                _navTacticsButton.clicked -= ShowTactics;
                _navScheduleButton.clicked -= ShowSchedule;
                _navLeagueButton.clicked -= ShowLeague;
                _navSaveLoadButton.clicked -= ShowSaveLoad;

                _playerProfileBackButton.clicked -= ShowSquad;
            }

            if (_saveLoadScreen != null)
            {
                _saveLoadScreen.UnregisterCallbacks();
                _saveLoadScreen.LoadRequested -= AdoptLoadedGame;
            }

            if (_dashboardScreen != null)
            {
                _dashboardScreen.UnregisterCallbacks();
                _dashboardScreen.MatchCentreRequested -= StartMatchCentre;
            }

            if (_squadScreen != null)
            {
                _squadScreen.PlayerSelected -= ShowPlayerProfile;
            }

            if (_tacticsScreen != null)
            {
                _tacticsScreen.UnregisterCallbacks();
                _tacticsScreen.RotationChanged -= OnRotationChanged;
            }

            if (_endOfSeasonScreen != null)
            {
                _endOfSeasonScreen.UnregisterCallbacks();
                _endOfSeasonScreen.NextSeasonRequested -= StartNextSeason;
            }

            if (_closeSeasonScreen != null)
            {
                _closeSeasonScreen.UnregisterCallbacks();
                _closeSeasonScreen.ContinueRequested -= ShowDashboard;
            }

            if (_matchCentreScreen != null)
            {
                _matchCentreScreen.UnregisterCallbacks();
                _matchCentreScreen.ReplayStarted -= OnReplayStarted;
                _matchCentreScreen.ReplayFinished -= OnReplayFinished;
                _matchCentreScreen.BackRequested -= ShowDashboard;
            }
        }

        private void RenderAllScreens()
        {
            RenderTeamSwitcher();

            _dashboardScreen?.Render();
            _saveLoadScreen?.Render();
            _squadScreen?.Render();
            _tacticsScreen?.Render();
            _scheduleScreen?.Render();
            _leagueScreen?.Render();
            _endOfSeasonScreen?.Render();
            _closeSeasonScreen?.Render();
        }

        private void OnRotationChanged()
        {
            _squadScreen?.Render();
            _playerProfileScreen?.Render();
        }

        private void StartMatchCentre()
        {
            if (_matchCentreScreen == null)
            {
                return;
            }

            HideAllScreens();

            _matchCentreScreen.PrepareMatch();

            SetActiveNavigation(null);
        }

        private void OnReplayStarted()
        {
            SetNavigationEnabled(false);
        }

        private void OnReplayFinished(ContinueOutcome outcome)
        {
            RenderAllScreens();

            SetNavigationEnabled(true);

            if (outcome != null && outcome.Stop == ContinueStop.SeasonEnded)
            {
                ShowEndOfSeason();
            }
        }

        private void ShowEndOfSeason()
        {
            HideAllScreens();
            _endOfSeasonScreen?.Show();
            SetActiveNavigation(null);
        }

        private void StartNextSeason()
        {
            _session.AdvanceToNextSeason();

            RenderAllScreens();

            ShowCloseSeason();
        }

        private void ShowCloseSeason()
        {
            HideAllScreens();
            _closeSeasonScreen?.Show();
            SetActiveNavigation(null);
        }

        private void ShowDashboard()
        {
            HideAllScreens();
            _dashboardScreen?.Show();
            SetActiveNavigation(_navDashboardButton);
        }

        private void ShowSquad()
        {
            _session.SelectedPlayer = null;

            HideAllScreens();
            _squadScreen?.Show();
            SetActiveNavigation(_navSquadButton);
        }

        private void ShowPlayerProfile(Player player)
        {
            HideAllScreens();
            _playerProfileScreen?.ShowForPlayer(player);
            SetActiveNavigation(_navSquadButton);
        }

        private void ShowTactics()
        {
            HideAllScreens();
            _tacticsScreen?.Show();
            SetActiveNavigation(_navTacticsButton);
        }

        private void ShowSchedule()
        {
            HideAllScreens();
            _scheduleScreen?.Show();
            SetActiveNavigation(_navScheduleButton);
        }

        private void HideEveryScreenElement()
        {
            if (_root == null)
            {
                return;
            }

            foreach (var screen in _root.Query<VisualElement>(className: "screen").ToList())
            {
                screen.style.display = DisplayStyle.None;
            }
        }

        private void ShowMainMenu()
        {
            HideEveryScreenElement();
            HideAllScreens();

            SetNavigationVisible(false);

            _mainMenuScreen?.Show();
        }

        private void ShowSaveLoad()
        {
            HideAllScreens();
            _saveLoadScreen?.Show();
            SetActiveNavigation(_navSaveLoadButton);
        }

        private void ShowLeague()
        {
            HideAllScreens();
            _leagueScreen?.Show();
            SetActiveNavigation(_navLeagueButton);
        }

        private void HideAllScreens()
        {
            _dashboardScreen?.Hide();
            _squadScreen?.Hide();
            _playerProfileScreen?.Hide();
            _tacticsScreen?.Hide();
            _scheduleScreen?.Hide();
            _leagueScreen?.Hide();
            _matchCentreScreen?.Hide();
            _endOfSeasonScreen?.Hide();
            _closeSeasonScreen?.Hide();
            _saveLoadScreen?.Hide();
            _mainMenuScreen?.Hide();
        }

        private void SetActiveNavigation(Button activeButton)
        {
            _navDashboardButton.RemoveFromClassList("nav-item-active");
            _navSquadButton.RemoveFromClassList("nav-item-active");
            _navTacticsButton.RemoveFromClassList("nav-item-active");
            _navScheduleButton.RemoveFromClassList("nav-item-active");
            _navLeagueButton.RemoveFromClassList("nav-item-active");
            _navSaveLoadButton.RemoveFromClassList("nav-item-active");

            activeButton?.AddToClassList("nav-item-active");
        }

        private void SetNavigationEnabled(bool enabled)
        {
            _navDashboardButton.SetEnabled(enabled);
            _navSquadButton.SetEnabled(enabled);
            _navTacticsButton.SetEnabled(enabled);
            _navScheduleButton.SetEnabled(enabled);
            _navLeagueButton.SetEnabled(enabled);

            _navSaveLoadButton.SetEnabled(enabled);
        }

        private void SetNavigationVisible(bool visible)
        {
            if (_navigationBar == null)
            {
                return;
            }

            _navigationBar.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RenderTeamSwitcher()
        {
            if (_teamSwitcher == null || _session == null)
            {
                return;
            }

            _teamSwitcher.Clear();

            foreach (var team in _session.UserClub.Teams.OrderBy(entry => entry.Type))
            {
                var isManaged = _session.IsManaged(team.Id);
                var isSelected = team.Id == _session.UserTeam.Id;

                var row = new VisualElement();
                row.AddToClassList("team-switcher-row");

                var selectButton = new Button { text = team.Name };
                selectButton.AddToClassList("team-switcher-item");

                if (isSelected)
                {
                    selectButton.AddToClassList("team-switcher-item-active");
                }

                selectButton.SetEnabled(isManaged && !isSelected);
                selectButton.clicked += () => OnTeamSelected(team);

                var toggleButton = new Button { text = isManaged ? "ON" : "OFF" };
                toggleButton.AddToClassList("team-switcher-toggle");

                if (isManaged)
                {
                    toggleButton.AddToClassList("team-switcher-toggle-on");
                }

                toggleButton.SetEnabled(!isSelected);
                toggleButton.clicked += () => OnTeamManagedToggled(team);

                row.Add(selectButton);
                row.Add(toggleButton);

                _teamSwitcher.Add(row);
            }
        }

        private void OnTeamSelected(Team team)
        {
            _session.SelectTeam(team);

            RenderTeamSwitcher();
            RenderAllScreens();

            ShowDashboard();
        }

        private void OnTeamManagedToggled(Team team)
        {
            _session.SetManaged(team.Id, !_session.IsManaged(team.Id));

            RenderTeamSwitcher();
            RenderAllScreens();
        }
    }
}
