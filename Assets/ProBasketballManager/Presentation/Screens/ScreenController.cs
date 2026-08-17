using ProBasketballManager.Presentation.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public abstract class ScreenController : MonoBehaviour
    {
        protected GameSession Session { get; private set; }

        protected VisualElement ScreenRoot { get; private set; }

        protected abstract string ScreenElementName { get; }

        public bool IsBound { get; private set; }

        public void Bind(GameSession session, VisualElement documentRoot)
        {
            Session = session;
            ScreenRoot = documentRoot.Q<VisualElement>(ScreenElementName);

            if (ScreenRoot == null)
            {
                Debug.LogError(
                    $"{GetType().Name} could not find an element named '{ScreenElementName}' in the UI document. " +
                    "Check that the name matches the UXML.");

                IsBound = false;

                return;
            }

            FindControls(documentRoot);

            IsBound = true;
        }

        protected abstract void FindControls(VisualElement documentRoot);

        public abstract void Render();

        public virtual void Show()
        {
            if (!IsBound)
            {
                return;
            }

            Render();

            ScreenRoot.style.display = DisplayStyle.Flex;
        }

        public virtual void Hide()
        {
            if (!IsBound)
            {
                return;
            }

            ScreenRoot.style.display = DisplayStyle.None;
        }

        protected void ApplySeasonBadgeState(Label badge)
        {
            if (badge == null)
            {
                return;
            }

            badge.RemoveFromClassList("season-complete-badge");

            if (Session.Season.IsComplete)
            {
                badge.AddToClassList("season-complete-badge");
            }
        }
    }
}