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
        [Tooltip("Load game: pick a save file from the welcome screen.")]
        private LoadGameScreenController _loadGameScreen;

        [SerializeField]
        [Tooltip("Home screen: club summary, next fixture and mini league table.")]
        private DashboardScreenController _dashboardScreen;

        [SerializeField]
        [Tooltip("Squad list with season averages.")]
        private SquadScreenController _squadScreen;

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
                _mainMenuScreen.LoadGameRequested += ShowLoadGame;
                _mainMenuScreen.ExitRequested += ExitGame;
            }



            BindScreen(_loadGameScreen, nameof(LoadGameScreenController), _root, null);

            if (_loadGameScreen != null)
            {
                _loadGameScreen.RegisterCallbacks();
                _loadGameScreen.LoadRequested += AdoptLoadedGame;
                _loadGameScreen.Cancelled += ShowMainMenu;
            }

            ShowMainMenu();
        }

        private void StartNewGame()
        {
            ShowClubSelect();
        }

        private void ShowLoadGame()
        {
            HideEveryScreenElement();
            HideAllScreens();

            SetNavigationVisible(false);

            _loadGameScreen?.Show();
            _loadGameScreen?.Render();
        }

        private void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowClubSelect()
        {
            HideEveryScreenElement();
            HideAllScreens();

            SetNavigationVisible(false);
        }

        private void AdoptNewGame(GameDatabase database, int teamId)
        {
            AdoptSession(GameSession.CreateNew(database, teamId));
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
            ClubBadgeRenderer.ClearCache();

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
            if (_mainMenuScreen != null)
            {
                _mainMenuScreen.UnregisterCallbacks();
                _mainMenuScreen.NewGameRequested -= StartNewGame;
                _mainMenuScreen.LoadGameRequested -= ShowLoadGame;
                _mainMenuScreen.ExitRequested -= ExitGame;
            }

            if (_loadGameScreen != null)
            {
                _loadGameScreen.UnregisterCallbacks();
                _loadGameScreen.LoadRequested -= AdoptLoadedGame;
                _loadGameScreen.Cancelled -= ShowMainMenu;
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
            BindScreen(_dashboardScreen, nameof(DashboardScreenController), root);
            BindScreen(_squadScreen, nameof(SquadScreenController), root);
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

          
            if (_dashboardScreen != null)
            {
                _dashboardScreen.RegisterCallbacks();
                _dashboardScreen.MatchCentreRequested += StartMatchCentre;
            }
        }

        private void UnregisterCallbacks()
        {
            if (_navDashboardButton != null)
            {
                _navDashboardButton.clicked -= ShowDashboard;
                _navSquadButton.clicked -= ShowSquad;

                _playerProfileBackButton.clicked -= ShowSquad;
            }


            if (_dashboardScreen != null)
            {
                _dashboardScreen.UnregisterCallbacks();
                _dashboardScreen.MatchCentreRequested -= StartMatchCentre;
            }
        }

        private void RenderAllScreens()
        {
            RenderTeamSwitcher();

            _dashboardScreen?.Render();
            _squadScreen?.Render();
        }

        private void OnRotationChanged()
        {
            _squadScreen?.Render();
        }

        private void StartMatchCentre()
        {

            HideAllScreens();

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

        private void HideAllScreens()
        {
            _dashboardScreen?.Hide();
            _squadScreen?.Hide();
            _mainMenuScreen?.Hide();
            _loadGameScreen?.Hide();
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
