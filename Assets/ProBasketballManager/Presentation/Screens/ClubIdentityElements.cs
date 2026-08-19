using System;
using ProBasketballManager.Domain.Clubs;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public static class ClubIdentityElements
    {
        public const string FlagResourceFolder = "flags";

        public static VisualElement CreateBadge(Club club, int diameter = 32)
        {
            var badge = new VisualElement();
            badge.AddToClassList("club-badge");

            badge.style.width = diameter;
            badge.style.height = diameter;
            badge.style.borderTopLeftRadius = diameter / 2f;
            badge.style.borderTopRightRadius = diameter / 2f;
            badge.style.borderBottomLeftRadius = diameter / 2f;
            badge.style.borderBottomRightRadius = diameter / 2f;

            if (club == null)
            {
                return badge;
            }

            var texture = ClubBadgeRenderer.GetBadge(club);

            if (texture != null)
            {
                badge.style.backgroundImage = new StyleBackground(texture);

                return badge;
            }

            badge.style.backgroundColor = ParseColor(club.PrimaryColor, new Color32(30, 41, 59, 255));

            var initials = new Label(club.ShortName);
            initials.AddToClassList("club-badge-initials");
            initials.style.color = ParseColor(club.SecondaryColor, new Color32(248, 250, 252, 255));
            initials.style.fontSize = Mathf.Max(8, Mathf.RoundToInt(diameter * 0.36f));

            badge.Add(initials);

            return badge;
        }

        public static VisualElement CreateFlag(string nationality, int width = 20)
        {
            var flag = new VisualElement();
            flag.AddToClassList("player-flag");

            flag.style.width = width;
            flag.style.height = Mathf.RoundToInt(width * 2f / 3f);

            var texture = LoadFlagTexture(nationality);

            if (texture != null)
            {
                flag.style.backgroundImage = new StyleBackground(texture);
            }
            else
            {
                flag.AddToClassList("player-flag-missing");
                flag.tooltip = nationality;
            }

            return flag;
        }

        private static Texture2D LoadFlagTexture(string nationality)
        {
            if (string.IsNullOrWhiteSpace(nationality))
            {
                return null;
            }

            return Resources.Load<Texture2D>($"{FlagResourceFolder}/{nationality.Trim().ToUpperInvariant()}");
        }

        private static Color ParseColor(string hex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                return fallback;
            }

            return ColorUtility.TryParseHtmlString(hex, out var parsed) ? parsed : fallback;
        }
    }
}
