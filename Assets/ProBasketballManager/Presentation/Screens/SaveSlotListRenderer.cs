using System;
using System.Collections.Generic;
using ProBasketballManager.Persistence;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public static class SaveSlotListRenderer
    {
        public static void Render(VisualElement list, IReadOnlyList<SaveSlotInfo> slots, Action<SaveSlotInfo> onLoad, Action<SaveSlotInfo> onDelete)
        {
            if (list == null)
            {
                return;
            }

            list.Clear();

            if (slots.Count == 0)
            {
                list.Add(ScreenFormatting.CreateLabel("No saved games yet.", "box-score-placeholder"));

                return;
            }

            foreach (var slot in slots)
            {
                list.Add(CreateRow(slot, onLoad, onDelete));
            }
        }

        private static VisualElement CreateRow(SaveSlotInfo slot, Action<SaveSlotInfo> onLoad, Action<SaveSlotInfo> onDelete)
        {
            var row = new VisualElement();
            row.AddToClassList("save-slot-row");

            if (!slot.IsReadable)
            {
                row.AddToClassList("save-slot-row-error");
            }

            var details = new VisualElement();
            details.AddToClassList("save-slot-details");

            details.Add(ScreenFormatting.CreateLabel(slot.SlotName, "save-slot-name"));

            details.Add(ScreenFormatting.CreateLabel(slot.IsReadable ? slot.Description : slot.Error, "save-slot-description"));

            details.Add(ScreenFormatting.CreateLabel(slot.SavedAtUtc == default ? ScreenFormatting.NoValue : slot.SavedAtUtc.ToLocalTime().ToString("d MMM yyyy HH:mm"), "save-slot-timestamp"));

            row.Add(details);

            var actions = new VisualElement();
            actions.AddToClassList("save-slot-actions");

            if (onLoad != null)
            {
                var loadButton = new Button { text = "Load" };
                loadButton.AddToClassList("save-slot-load-button");

                // A file we could not read is a file we cannot load.
                loadButton.SetEnabled(slot.IsReadable);

                loadButton.clicked += () => onLoad(slot);

                actions.Add(loadButton);
            }

            if (onDelete != null)
            {
                var deleteButton = new Button { text = "Delete" };
                deleteButton.AddToClassList("save-slot-delete-button");

                deleteButton.clicked += () => onDelete(slot);

                actions.Add(deleteButton);
            }

            row.Add(actions);

            return row;
        }
    }
}