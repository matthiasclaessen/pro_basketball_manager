using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Teams;
using UnityEngine.UIElements;

namespace ProBasketballManager.Presentation.Screens
{
    public static class ScreenFormatting
    {
        public const string NoValue = "\u2014";

        public const string Separator = "\u00b7";

        public const string StarterRole = "STARTER";
        public const string RotationRole = "ROTATION";
        public const string ReserveRole = "RESERVE";

        public static string GetPositionAbbreviation(PlayerPosition position)
        {
            return position switch
            {
                PlayerPosition.PointGuard => "PG",
                PlayerPosition.ShootingGuard => "SG",
                PlayerPosition.SmallForward => "SF",
                PlayerPosition.PowerForward => "PF",
                PlayerPosition.Center => "C",
                _ => "-"
            };
        }

        public static string GetSquadRole(TeamRotation rotation, PlayerRotationAssignment assignment)
        {
            if (rotation.IsStarter(assignment.Player.Id))
            {
                return StarterRole;
            }

            return assignment.TargetMinutes > 0 ? RotationRole : ReserveRole;
        }

        public static string FormatPerGame(int gamesPlayed, double value)
        {
            return gamesPlayed == 0 ? NoValue : value.ToString("0.0");
        }

        public static string FormatPercentage(int attempts, double percentage)
        {
            return attempts == 0 ? NoValue : $"{percentage:0.0}%";
        }

        public static Label CreateLabel(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);

            return label;
        }

        public static void ApplyRoleClass(VisualElement element, string role)
        {
            element.RemoveFromClassList("squad-role-starter");
            element.RemoveFromClassList("squad-role-rotation");
            element.RemoveFromClassList("squad-role-reserve");

            if (role == StarterRole)
            {
                element.AddToClassList("squad-role-starter");
            }
            else if (role == RotationRole)
            {
                element.AddToClassList("squad-role-rotation");
            }
            else
            {
                element.AddToClassList("squad-role-reserve");
            }
        }
    }
}