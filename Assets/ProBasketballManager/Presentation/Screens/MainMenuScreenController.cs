using System;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public sealed class MainMenuScreenController : ScreenController
    {
        protected override string ScreenElementName => "main-menu-screen";

        public event Action NewGameRequested;

        public event Action LoadGameRequested;

        public event Action ExitRequested;

        private Button _newGameButton;
        private Button _loadGameButton;
        private Button _exitGameButton;
        private Label _statusLabel;

        protected override void FindControls(VisualElement documentRoot)
        {
            _newGameButton = Require<Button>(documentRoot, "new-game-button");
            _loadGameButton = Require<Button>(documentRoot, "load-game-button");
            _exitGameButton = Require<Button>(documentRoot, "exit-game-button");
            _statusLabel = Require<Label>(documentRoot, "main-menu-status");
        }

        public void RegisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _newGameButton.clicked -= RaiseNewGame;
            _newGameButton.clicked += RaiseNewGame;

            _loadGameButton.clicked -= RaiseLoadGame;
            _loadGameButton.clicked += RaiseLoadGame;

            _exitGameButton.clicked -= RaiseExit;
            _exitGameButton.clicked += RaiseExit;
        }

        public void UnregisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _newGameButton.clicked -= RaiseNewGame;
            _loadGameButton.clicked -= RaiseLoadGame;
            _exitGameButton.clicked -= RaiseExit;
        }

        public void ShowMessage(string message)
        {
            if (!IsBound)
            {
                return;
            }

            _statusLabel.text = message ?? string.Empty;
        }

        private void RaiseNewGame()
        {
            NewGameRequested?.Invoke();
        }

        private void RaiseLoadGame()
        {
            LoadGameRequested?.Invoke();
        }

        private void RaiseExit()
        {
            ExitRequested?.Invoke();
        }

        public override void Render()
        {
            if (!IsBound)
            {
                return;
            }

            _statusLabel.text = string.Empty;
        }
    }
}
