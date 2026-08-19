using System;
using System.Linq;
using ProBasketballManager.Persistence;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public sealed class ClubSelectScreenController : ScreenController
    {
        protected override string ScreenElementName => "club-select-screen";

        public event Action<GameDatabase, int> ClubChosen;

        public event Action Cancelled;

        private VisualElement _clubList;
        private Label _databaseLabel;
        private Label _statusLabel;
        private Button _backButton;

        private GameDatabase _database;

        protected override void FindControls(VisualElement documentRoot)
        {
            _clubList = documentRoot.Q<VisualElement>("club-select-list");
            _databaseLabel = documentRoot.Q<Label>("club-select-database");
            _statusLabel = documentRoot.Q<Label>("club-select-status");
            _backButton = documentRoot.Q<Button>("club-select-back-button");
        }

        public void RegisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _backButton.clicked -= RaiseCancelled;
            _backButton.clicked += RaiseCancelled;
        }

        public void UnregisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _backButton.clicked -= RaiseCancelled;
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

            _clubList.Clear();
            _statusLabel.text = string.Empty;
            _databaseLabel.text = string.Empty;

            _database = null;

            try
            {
                _database = GameDatabaseRepository.Load(Application.streamingAssetsPath);
            }
            catch (GameDatabaseException exception)
            {
                _statusLabel.text = exception.Message;

                Debug.LogException(exception);

                return;
            }
            catch (Exception exception)
            {
                _statusLabel.text = $"The database could not be loaded: {exception.Message}";

                Debug.LogException(exception);

                return;
            }

            _databaseLabel.text = _database.Name;

            foreach (var club in _database.Clubs.OrderBy(entry => entry.Name))
            {
                _clubList.Add(CreateClubRow(club));
            }
        }

        private VisualElement CreateClubRow(Domain.Clubs.Club club)
        {
            var team = club.FirstTeam;

            var row = new Button();
            row.AddToClassList("club-select-item");
            row.clicked += () => ChooseClub(team.Id);

            var name = new Label(club.Name);
            name.AddToClassList("club-select-item-name");

            var detail = new Label($"{club.Squad.Count} players");
            detail.AddToClassList("club-select-item-detail");

            row.Add(name);
            row.Add(detail);

            return row;
        }

        private void ChooseClub(int teamId)
        {
            if (_database == null)
            {
                return;
            }

            try
            {
                ClubChosen?.Invoke(_database, teamId);
            }
            catch (ArgumentException exception)
            {
                _statusLabel.text = exception.Message;

                Debug.LogException(exception);
            }
        }
    }
}
