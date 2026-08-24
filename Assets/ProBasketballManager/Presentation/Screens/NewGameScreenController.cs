using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Clubs;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Persistence;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public sealed class NewGameScreenController : ScreenController
    {
        protected override string ScreenElementName => "new-game-screen";

        public event Action<GameDatabase, int> StartRequested;

        public event Action Cancelled;

        private DropdownField _leagueDropdown;
        private TextField _searchField;
        private VisualElement _clubList;
        private Label _statusLabel;
        private Label _seasonValue;
        private Label _summarySeason;
        private Label _summaryLeague;
        private Label _summaryClub;
        private Label _summarySquad;
        private Button _backButton;
        private Button _startButton;

        private readonly List<Club> _visibleClubs = new List<Club>();
        private readonly List<Button> _rows = new List<Button>();

        private Dictionary<int, League> _leagueByClubId = new Dictionary<int, League>();

        private GameDatabase _database;
        private League _selectedLeague;
        private Club _selectedClub;
        private string _search = string.Empty;

        protected override void FindControls(VisualElement documentRoot)
        {
            _leagueDropdown = Require<DropdownField>(documentRoot, "new-game-league-dropdown");
            _searchField = Require<TextField>(documentRoot, "new-game-search-field");
            _clubList = Require<VisualElement>(documentRoot, "new-game-club-list");
            _statusLabel = Require<Label>(documentRoot, "new-game-status");
            _seasonValue = Require<Label>(documentRoot, "new-game-season-value");
            _summarySeason = Require<Label>(documentRoot, "summary-season");
            _summaryLeague = Require<Label>(documentRoot, "summary-league");
            _summaryClub = Require<Label>(documentRoot, "summary-club");
            _summarySquad = Require<Label>(documentRoot, "summary-squad");
            _backButton = Require<Button>(documentRoot, "new-game-back-button");
            _startButton = Require<Button>(documentRoot, "start-career-button");
        }

        public void RegisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _backButton.clicked -= RaiseCancelled;
            _backButton.clicked += RaiseCancelled;

            _startButton.clicked -= RaiseStart;
            _startButton.clicked += RaiseStart;

            _leagueDropdown.UnregisterValueChangedCallback(OnLeagueChanged);
            _leagueDropdown.RegisterValueChangedCallback(OnLeagueChanged);

            _searchField.UnregisterValueChangedCallback(OnSearchChanged);
            _searchField.RegisterValueChangedCallback(OnSearchChanged);
        }

        public void UnregisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _backButton.clicked -= RaiseCancelled;
            _startButton.clicked -= RaiseStart;

            _leagueDropdown.UnregisterValueChangedCallback(OnLeagueChanged);
            _searchField.UnregisterValueChangedCallback(OnSearchChanged);
        }

        private void RaiseCancelled()
        {
            Cancelled?.Invoke();
        }

        public override void Render()
        {
            if (!IsBound)
            {
                return;
            }

            _statusLabel.text = string.Empty;
            _seasonValue.text = FormatSeason(CompetitionCalendar.DefaultFirstSeasonYear);

            if (!EnsureDatabase())
            {
                RebuildRows();
                RenderSummary();

                return;
            }

            PopulateLeagues();
            RebuildRows();
            RenderSummary();
        }

        private bool EnsureDatabase()
        {
            if (_database != null)
            {
                return true;
            }

            try
            {
                _database = GameDatabaseRepository.Load(Application.streamingAssetsPath);
            }
            catch (GameDatabaseException exception)
            {
                _statusLabel.text = exception.Message;

                return false;
            }
            catch (Exception exception)
            {
                _statusLabel.text = $"Could not load the database: {exception.Message}";

                Debug.LogException(exception);

                return false;
            }

            BuildLeagueIndex();

            return true;
        }

        private void BuildLeagueIndex()
        {
            _leagueByClubId = new Dictionary<int, League>();

            foreach (var league in _database.Competitions)
            {
                foreach (var team in league.Teams)
                {
                    if (!_leagueByClubId.ContainsKey(team.ClubId))
                    {
                        _leagueByClubId[team.ClubId] = league;
                    }
                }
            }
        }

        private void PopulateLeagues()
        {
            var names = _database.Competitions.Select(league => league.Name).ToList();

            _leagueDropdown.choices = names;

            if (names.Count == 0)
            {
                _statusLabel.text = "This database contains no competitions.";

                return;
            }

            _selectedLeague ??= _database.Competitions[0];

            _leagueDropdown.SetValueWithoutNotify(_selectedLeague.Name);
        }

        private void OnLeagueChanged(ChangeEvent<string> evt)
        {
            _selectedLeague = _database?.Competitions.FirstOrDefault(league => league.Name == evt.newValue);
            _selectedClub = null;

            RebuildRows();
            RenderSummary();
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            _search = evt.newValue?.Trim() ?? string.Empty;

            _leagueDropdown.EnableInClassList("league-dropdown-dimmed", _search.Length > 0);
            _leagueDropdown.SetEnabled(_search.Length == 0);

            RebuildRows();
        }

        private void CollectVisibleClubs()
        {
            _visibleClubs.Clear();

            if (_database == null)
            {
                return;
            }

            if (_search.Length > 0)
            {
                _visibleClubs.AddRange(_database.Clubs.Where(Matches).OrderBy(club => club.Name));

                return;
            }

            if (_selectedLeague == null)
            {
                return;
            }

            var clubIds = _selectedLeague.Teams.Select(team => team.ClubId).Distinct().ToHashSet();

            _visibleClubs.AddRange(_database.Clubs.Where(club => clubIds.Contains(club.Id)).OrderByDescending(club => club.Reputation));
        }

        private bool Matches(Club club)
        {
            return Contains(club.Name) || Contains(club.ShortName) || Contains(club.City);
        }

        private bool Contains(string value)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void RebuildRows()
        {
            CollectVisibleClubs();

            _clubList.Clear();
            _rows.Clear();

            if (_visibleClubs.Count == 0)
            {
                _clubList.Add(ScreenFormatting.CreateLabel(_search.Length > 0 ? "No clubs match that search." : "No clubs in this league.", "club-list-empty"));

                return;
            }

            foreach (var club in _visibleClubs)
            {
                var row = CreateRow(club);

                _rows.Add(row);
                _clubList.Add(row);
            }

            ApplySelectionState();
        }

        private Button CreateRow(Club club)
        {
            var row = new Button { name = $"club-row-{club.Id}" };
            row.AddToClassList("club-row");

            var badge = ClubIdentityElements.CreateBadge(club, 42);
            badge.AddToClassList("club-row-badge");

            row.Add(badge);

            var info = new VisualElement();
            info.AddToClassList("club-row-info");
            info.Add(ScreenFormatting.CreateLabel(club.Name, "club-row-name"));
            info.Add(ScreenFormatting.CreateLabel(BuildMeta(club), "club-row-meta"));

            row.Add(info);

            var reputation = new VisualElement();
            reputation.AddToClassList("club-row-reputation");
            reputation.Add(ScreenFormatting.CreateLabel("REPUTATION", "club-row-reputation-label"));
            reputation.Add(ScreenFormatting.CreateLabel(club.Reputation.ToString(), "club-row-reputation-value"));

            row.Add(reputation);

            var check = new VisualElement();
            check.AddToClassList("club-check");
            check.Add(ScreenFormatting.CreateLabel("\u2713", "club-check-label"));

            row.Add(check);

            row.clicked += () => Select(club);

            return row;
        }

        private string BuildMeta(Club club)
        {
            var city = string.IsNullOrWhiteSpace(club.City) ? ScreenFormatting.NoValue : club.City;

            if (_search.Length == 0)
            {
                return city;
            }

            return _leagueByClubId.TryGetValue(club.Id, out var league) ? $"{city}  ·  {league.Name}" : city;
        }

        private void Select(Club club)
        {
            _selectedClub = club;

            ApplySelectionState();
            RenderSummary();
        }

        private void ApplySelectionState()
        {
            for (var index = 0; index < _rows.Count; index++)
            {
                var isSelected = _selectedClub != null && _visibleClubs[index].Id == _selectedClub.Id;

                _rows[index].EnableInClassList("club-row-selected", isSelected);
                _rows[index].Q<VisualElement>(className: "club-check").EnableInClassList("club-check-hidden", !isSelected);
            }
        }

        private void RenderSummary()
        {
            _summarySeason.text = FormatSeason(CompetitionCalendar.DefaultFirstSeasonYear);
            _summaryClub.text = _selectedClub == null ? ScreenFormatting.NoValue : _selectedClub.Name;
            _summarySquad.text = _selectedClub == null ? ScreenFormatting.NoValue : _selectedClub.Squad.Count.ToString();

            var league = _selectedClub != null && _leagueByClubId.TryGetValue(_selectedClub.Id, out var found) ? found : _selectedLeague;

            _summaryLeague.text = league == null ? ScreenFormatting.NoValue : league.Name;

            _startButton.SetEnabled(_selectedClub != null && _selectedClub.HasTeam(Domain.Teams.TeamType.First));
        }

        private void RaiseStart()
        {
            if (_database == null || _selectedClub == null)
            {
                return;
            }

            var firstTeam = _selectedClub.FirstTeam;

            if (firstTeam == null)
            {
                _statusLabel.text = $"{_selectedClub.Name} has no first team in this database.";

                return;
            }

            StartRequested?.Invoke(_database, firstTeam.Id);
        }

        private static string FormatSeason(int startYear)
        {
            return $"{startYear}/{(startYear + 1) % 100:D2} (Current Season)";
        }
    }
}
