using System.Collections.Generic;
using ProBasketballManager.Domain.Clubs;
using UnityEngine;

namespace ProBasketballManager.Presentation.Screens
{
    public static class ClubBadgeRenderer
    {
        public const string TemplateResourceFolder = "badges";

        private static readonly string[] FallbackTemplates =
        {
            "heater-pale", "heater-fess", "heater-bend", "heater-cross",
            "heater-saltire", "heater-chevron", "heater-quarterly", "heater-chief-mullets",
            "heater-barry", "heater-paly", "heater-bendy-shaded", "heater-lozenge",
            "roundel-annulet", "roundel-gyronny", "oval-pale-mullet"
        };

        private static readonly Dictionary<int, Texture2D> Cache = new Dictionary<int, Texture2D>();

        public static void ClearCache()
        {
            foreach (var texture in Cache.Values)
            {
                if (texture != null)
                {
                    Object.Destroy(texture);
                }
            }

            Cache.Clear();
        }

        public static Texture2D GetBadge(Club club)
        {
            if (club == null)
            {
                return null;
            }

            if (Cache.TryGetValue(club.Id, out var cached) && cached != null)
            {
                return cached;
            }

            var template = LoadTemplate(club);

            if (template == null)
            {
                return null;
            }

            var badge = Recolour(template, ParseColor(club.PrimaryColor, new Color32(30, 41, 59, 255)), ParseColor(club.SecondaryColor, new Color32(248, 250, 252, 255)), ParseColor(club.TertiaryColor, new Color32(10, 10, 10, 255)));

            Cache[club.Id] = badge;

            return badge;
        }

        public static string GetTemplateName(Club club)
        {
            if (club != null && !string.IsNullOrWhiteSpace(club.BadgeTemplate))
            {
                return club.BadgeTemplate;
            }

            var index = club == null ? 0 : Mathf.Abs(club.Id) % FallbackTemplates.Length;

            return FallbackTemplates[index];
        }

        private static Texture2D LoadTemplate(Club club)
        {
            var name = GetTemplateName(club);

            var template = Resources.Load<Texture2D>($"{TemplateResourceFolder}/{name}");

            if (template == null)
            {
                Debug.LogWarning($"Badge template '{name}' was not found for {club.Name}; falling back to a plain badge.");
            }

            return template;
        }

        private static Texture2D Recolour(Texture2D template, Color32 primary, Color32 secondary, Color32 tertiary)
        {
            Color32[] source;

            try
            {
                source = template.GetPixels32();
            }
            catch (UnityException exception)
            {
                Debug.LogError($"Badge template '{template.name}' is not readable. Enable Read/Write in its import settings. {exception.Message}");

                return null;
            }

            var result = new Color32[source.Length];

            for (var i = 0; i < source.Length; i++)
            {
                var pixel = source[i];

                var total = pixel.r + pixel.g + pixel.b;

                if (total == 0)
                {
                    result[i] = new Color32(primary.r, primary.g, primary.b, pixel.a);

                    continue;
                }

                result[i] = new Color32(
                    (byte)((pixel.r * primary.r + pixel.g * secondary.r + pixel.b * tertiary.r) / total),
                    (byte)((pixel.r * primary.g + pixel.g * secondary.g + pixel.b * tertiary.g) / total),
                    (byte)((pixel.r * primary.b + pixel.g * secondary.b + pixel.b * tertiary.b) / total),
                    pixel.a);
            }

            var badge = new Texture2D(template.width, template.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };

            badge.SetPixels32(result);
            badge.Apply(false, false);

            return badge;
        }

        private static Color32 ParseColor(string hex, Color32 fallback)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                return fallback;
            }

            return ColorUtility.TryParseHtmlString(hex, out var parsed) ? (Color32)parsed : fallback;
        }
    }
}
