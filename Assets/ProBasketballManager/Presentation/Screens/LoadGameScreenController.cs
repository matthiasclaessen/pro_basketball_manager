using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        private VisualElement _saveList;
        private Label _statusLabel;
        private Label _selectedLogoLabel;
        private Label _selectedNameLabel;
        private Label _selectedLastPlayedLabel;
        private Label _selectedSeasonLabel;
        private Button _backButton;
        private Button _deleteButton;
        private Button _loadButton;

        private readonly List<Button> _rows = new List<Button>();

        private IReadOnlyList<SaveSlotInfo> _slots = Array.Empty<SaveSlotInfo>();

        private SaveSlotInfo _selected;

        protected override void FindControls(VisualElement documentRoot)
        {
            _saveList = Require<VisualElement>(documentRoot, "save-list");
            _statusLabel = Require<Label>(documentRoot, "load-game-status");
            _selectedLogoLabel = Require<Label>(documentRoot, "selected-save-logo-label");
            _selectedNameLabel = Require<Label>(documentRoot, "selected-name-label");
            _selectedLastPlayedLabel = Require<Label>(documentRoot, "selected-last-played-label");
            _selectedSeasonLabel = Require<Label>(documentRoot, "selected-season-label");
            _backButton = Require<Button>(documentRoot, "load-game-back-button");
            _deleteButton = Require<Button>(documentRoot, "delete-save-button");
            _loadButton = Require<Button>(documentRoot, "load-save-button");
        }

        public void RegisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _backButton.clicked -= RaiseCancelled;
            _backButton.clicked += RaiseCancelled;

            _deleteButton.clicked -= DeleteSelected;
            _deleteButton.clicked += DeleteSelected;

            _loadButton.clicked -= LoadSelected;
            _loadButton.clicked += LoadSelected;
        }

        public void UnregisterCallbacks()
        {
            if (!IsBound)
            {
                return;
            }

            _backButton.clicked -= RaiseCancelled;
            _deleteButton.clicked -= DeleteSelected;
            _loadButton.clicked -= LoadSelected;
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

            _slots = ReadSlots();
            _selected = _slots.FirstOrDefault(slot => slot.IsReadable);

            RebuildRows();
            RenderSelected();
        }

        private IReadOnlyList<SaveSlotInfo> ReadSlots()
        {
            try
            {
                return SaveGameRepository.ListSaves();
            }
            catch (Exception exception)
            {
                _statusLabel.text = $"Could not read the saves folder: {exception.Message}";

                Debug.LogException(exception);

                return Array.Empty<SaveSlotInfo>();
            }
        }

        private void RebuildRows()
        {
            _saveList.Clear();
            _rows.Clear();

            if (_slots.Count == 0)
            {
                _saveList.Add(ScreenFormatting.CreateLabel("No saved careers yet. Start a new game to create one.", "save-list-empty"));

                return;
            }

            foreach (var slot in _slots)
            {
                var row = CreateRow(slot);

                _rows.Add(row);
                _saveList.Add(row);
            }

            ApplySelectionState();
        }

        private Button CreateRow(SaveSlotInfo slot)
        {
            var row = new Button { name = $"save-row-{slot.SlotName}" };
            row.AddToClassList("save-row");

            if (!slot.IsReadable)
            {
                row.AddToClassList("save-row-unreadable");
            }

            var logo = new VisualElement();
            logo.AddToClassList("save-logo");
            logo.Add(ScreenFormatting.CreateLabel(GetInitials(slot.SlotName), "save-logo-placeholder"));

            row.Add(logo);
            row.Add(CreateField("Save", slot.SlotName, "save-field-name", false));
            row.Add(CreateSeparator());
            row.Add(CreateField("Last Played", FormatSavedAt(slot.SavedAtUtc), "save-field-last-played", false));
            row.Add(CreateSeparator());
            row.Add(CreateField("Season", slot.IsReadable ? ScreenFormatting.NoValue : slot.Error, "save-field-season", !slot.IsReadable));

            var check = new VisualElement();
            check.AddToClassList("selected-check");
            check.Add(ScreenFormatting.CreateLabel("\u2713", "selected-check-label"));

            row.Add(check);

            row.clicked += () => Select(slot);

            return row;
        }

        private static VisualElement CreateField(string label, string value, string widthClassName, bool isError)
        {
            var field = new VisualElement();
            field.AddToClassList("save-field");
            field.AddToClassList(widthClassName);
            field.Add(ScreenFormatting.CreateLabel(label, "save-field-label"));

            var valueLabel = ScreenFormatting.CreateLabel(value, "save-field-value");
            valueLabel.EnableInClassList("save-field-value-error", isError);

            field.Add(valueLabel);

            return field;
        }

        private static VisualElement CreateSeparator()
        {
            var separator = new VisualElement();
            separator.AddToClassList("save-separator");

            return separator;
        }

        private void Select(SaveSlotInfo slot)
        {
            _selected = slot;
            _statusLabel.text = slot.IsReadable ? string.Empty : slot.Error;

            ApplySelectionState();
            RenderSelected();
        }

        private void ApplySelectionState()
        {
            for (var index = 0; index < _rows.Count; index++)
            {
                var isSelected = ReferenceEquals(_slots[index], _selected);

                _rows[index].EnableInClassList("save-row-selected", isSelected);
                _rows[index].Q<VisualElement>(className: "selected-check").EnableInClassList("selected-check-hidden", !isSelected);
            }
        }

        private void RenderSelected()
        {
            _selectedLogoLabel.text = _selected == null ? string.Empty : GetInitials(_selected.SlotName);
            _selectedNameLabel.text = _selected == null ? ScreenFormatting.NoValue : _selected.SlotName;
            _selectedLastPlayedLabel.text = _selected == null ? ScreenFormatting.NoValue : FormatSavedAt(_selected.SavedAtUtc);
            _selectedSeasonLabel.text = ScreenFormatting.NoValue;

            _loadButton.SetEnabled(_selected != null && _selected.IsReadable);
            _deleteButton.SetEnabled(_selected != null);
        }

        private void LoadSelected()
        {
            if (_selected == null)
            {
                return;
            }

            if (!_selected.IsReadable)
            {
                _statusLabel.text = _selected.Error;

                return;
            }

            try
            {
                LoadRequested?.Invoke(SaveGameRepository.LoadFromPath(_selected.FilePath));
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

        private void DeleteSelected()
        {
            if (_selected == null)
            {
                return;
            }

            try
            {
                SaveGameRepository.Delete(_selected.SlotName);

                Render();
            }
            catch (Exception exception)
            {
                _statusLabel.text = $"Delete failed: {exception.Message}";

                Debug.LogException(exception);
            }
        }

        private static string GetInitials(string slotName)
        {
            var trimmed = slotName?.Trim();

            return string.IsNullOrEmpty(trimmed) ? "--" : trimmed.Substring(0, Math.Min(2, trimmed.Length)).ToUpperInvariant();
        }

        private static string FormatSavedAt(DateTime savedAtUtc)
        {
            return savedAtUtc == default ? ScreenFormatting.NoValue : savedAtUtc.ToLocalTime().ToString("MMM d, yyyy - HH:mm", CultureInfo.InvariantCulture);
        }
    }
}
