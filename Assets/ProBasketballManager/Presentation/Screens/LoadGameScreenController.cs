using System;
using ProBasketballManager.Persistence;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public sealed class LoadGameScreenController : ScreenController
    {
        protected override string ScreenElementName => "load-game-screen";

        public event Action<GameSessionSnapshot> LoadRequested;

        public event Action Cancelled;

        private VisualElement _slotList;
        private Label _statusLabel;
        private Label _pathLabel;
        private Button _backButton;

        protected override void FindControls(VisualElement documentRoot)
        {
            _slotList = Require<VisualElement>(documentRoot, "load-game-slot-list");
            _statusLabel = Require<Label>(documentRoot, "load-game-status");
            _pathLabel = Require<Label>(documentRoot, "load-game-path");
            _backButton = Require<Button>(documentRoot, "load-game-back-button");
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

            _statusLabel.text = string.Empty;
            _pathLabel.text = SaveGameRepository.SaveDirectory;
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
