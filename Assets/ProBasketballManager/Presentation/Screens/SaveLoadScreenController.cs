using System;
using ProBasketballManager.Persistence;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    /// <summary>
    /// Saving to a named slot and loading an existing one, from inside a game in
    /// progress.
    ///
    /// This screen does not apply a loaded game itself. Replacing the live session
    /// means every other screen has to be rebound to the new one, and only the root
    /// controller knows about all of them, so this raises LoadRequested and lets it
    /// happen there.
    /// </summary>
    public sealed class SaveLoadScreenController : ScreenController
    {
        protected override string ScreenElementName => "save-load-screen";

        /// <summary>Raised with a loaded game that the root controller should adopt.</summary>
        public event Action<GameSessionSnapshot> LoadRequested;

        private TextField _slotNameField;
        private Button _saveButton;
        private Label _statusLabel;
        private Label _pathLabel;
        private VisualElement _slotList;

        protected override void FindControls(VisualElement documentRoot)
        {
            _slotNameField = documentRoot.Q<TextField>("save-slot-name-field");
            _saveButton = documentRoot.Q<Button>("save-game-button");
            _statusLabel = documentRoot.Q<Label>("save-load-status");
            _pathLabel = documentRoot.Q<Label>("save-load-path");
            _slotList = documentRoot.Q<VisualElement>("save-slot-list");
        }

        public void RegisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _saveButton.clicked -= SaveCurrentGame;
            _saveButton.clicked += SaveCurrentGame;
        }

        public void UnregisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _saveButton.clicked -= SaveCurrentGame;
        }

        public override void Render()
        {
            if (!IsBound)
            {
                return;
            }

            // Suggest a name that says something about the game, so a player who
            // saves without thinking still ends up with a distinguishable slot.
            if (string.IsNullOrWhiteSpace(_slotNameField.value))
            {
                _slotNameField.value = BuildSuggestedName();
            }

            _pathLabel.text = SaveGameRepository.SaveDirectory;

            RefreshSlotList();
        }

        private string BuildSuggestedName()
        {
            var season = Session.Season;

            return season.IsComplete
                ? $"{Session.UserTeam.Name} end of season"
                : $"{Session.UserTeam.Name} round {season.CurrentRoundNumber}";
        }

        private void RefreshSlotList()
        {
            try
            {
                SaveSlotListRenderer.Render(_slotList, SaveGameRepository.ListSaves(), RequestLoad, DeleteSlot);
            }
            catch (Exception exception)
            {
                _statusLabel.text = $"Could not read the saves folder: {exception.Message}";

                Debug.LogException(exception);
            }
        }

        private void SaveCurrentGame()
        {
            try
            {
                var slotName = SaveGameRepository.SanitiseSlotName(_slotNameField.value);

                SaveGameRepository.Save(Session.CreateSnapshot(), slotName);

                _slotNameField.value = slotName;
                _statusLabel.text = $"Saved as '{slotName}'.";

                RefreshSlotList();
            }
            catch (Exception exception)
            {
                _statusLabel.text = $"Save failed: {exception.Message}";

                Debug.LogException(exception);
            }
        }

        private void RequestLoad(SaveSlotInfo slot)
        {
            try
            {
                var snapshot = SaveGameRepository.LoadFromPath(slot.FilePath);

                _statusLabel.text = $"Loaded '{slot.SlotName}'.";

                LoadRequested?.Invoke(snapshot);
            }
            catch (SaveGameException exception)
            {
                // A rejected save is a normal outcome, not a crash: wrong version,
                // corrupt file, or a reference that does not resolve.
                _statusLabel.text = exception.Message;
            }
            catch (Exception exception)
            {
                _statusLabel.text = $"Load failed: {exception.Message}";

                Debug.LogException(exception);
            }
        }

        private void DeleteSlot(SaveSlotInfo slot)
        {
            try
            {
                SaveGameRepository.Delete(slot.SlotName);

                _statusLabel.text = $"Deleted '{slot.SlotName}'.";

                RefreshSlotList();
            }
            catch (Exception exception)
            {
                _statusLabel.text = $"Delete failed: {exception.Message}";

                Debug.LogException(exception);
            }
        }
    }
}