using System;
using ProBasketballManager.Persistence;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public sealed class MainMenuScreenController : ScreenController
    {
        protected override string ScreenElementName => "main-menu-screen";

        public event Action NewGameRequested;

        public event Action<GameSessionSnapshot> LoadRequested;

        private Button _newGameButton;
        private Label _statusLabel;
        private VisualElement _slotList;

        protected override void FindControls(VisualElement documentRoot)
        {
            _newGameButton = documentRoot.Q<Button>("new-game-button");
            _statusLabel = documentRoot.Q<Label>("main-menu-status");
            _slotList = documentRoot.Q<VisualElement>("main-menu-save-list");
        }

        public void RegisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _newGameButton.clicked -= RaiseNewGameRequested;
            _newGameButton.clicked += RaiseNewGameRequested;
        }

        public void UnregisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _newGameButton.clicked -= RaiseNewGameRequested;
        }

        private void RaiseNewGameRequested()
        {
            NewGameRequested?.Invoke();
        }

        public override void Render()
        {
            if (!IsBound)
            {
                return;
            }

            _statusLabel.text = string.Empty;

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

        private void RequestLoad(SaveSlotInfo slot)
        {
            try
            {
                LoadRequested?.Invoke(SaveGameRepository.LoadFromPath(slot.FilePath));
            }
            catch (SaveGameException exception)
            {
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

                Render();
            }
            catch (Exception exception)
            {
                _statusLabel.text = $"Delete failed: {exception.Message}";

                Debug.LogException(exception);
            }
        }
    }
}