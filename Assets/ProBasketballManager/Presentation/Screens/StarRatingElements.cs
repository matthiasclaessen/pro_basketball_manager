using System;
using ProBasketballManager.Domain.Players;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public static class StarRatingElements
    {
        public const string StarResourceFolder = "stars";

        public const int StarCount = 5;

        public static VisualElement Create(double stars, int size = 13)
        {
            var row = new VisualElement();
            row.AddToClassList("star-rating");

            var clamped = Math.Clamp(stars, 0.0, StarCount);

            for (var i = 0; i < StarCount; i++)
            {
                var remaining = clamped - i;

                var texture = LoadStar(remaining >= 0.75 ? "star-full" : remaining >= 0.25 ? "star-half" : "star-empty");

                var star = new VisualElement();
                star.AddToClassList("star-rating-item");

                star.style.width = size;
                star.style.height = size;

                if (texture != null)
                {
                    star.style.backgroundImage = new StyleBackground(texture);
                }

                row.Add(star);
            }

            row.tooltip = $"{StarRating.Format(clamped)} of {StarCount} stars";

            return row;
        }

        private static Texture2D LoadStar(string name)
        {
            var texture = Resources.Load<Texture2D>($"{StarResourceFolder}/{name}");

            if (texture == null)
            {
                Debug.LogWarning($"Star image '{name}' was not found in Resources/{StarResourceFolder}.");
            }

            return texture;
        }
    }
}
