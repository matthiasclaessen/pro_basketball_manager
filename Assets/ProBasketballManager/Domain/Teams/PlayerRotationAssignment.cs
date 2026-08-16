using System;
using ProBasketballManager.Domain.Players;

namespace ProBasketballManager.Domain.Teams
{
    public sealed class PlayerRotationAssignment
    {
        public Player Player { get; }

        public int TargetMinutes { get; }

        public int RotationOrder { get; }

        public PlayerRotationAssignment(Player player, int targetMinutes, int rotationOrder)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));

            if (targetMinutes < 0 || targetMinutes > 40)
            {
                throw new ArgumentOutOfRangeException(nameof(targetMinutes), "Target minutes must be between 0 and 40.");
            }

            if (rotationOrder < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(rotationOrder), "Rotation order must be at least 1.");
            }

            TargetMinutes = targetMinutes;
            RotationOrder = rotationOrder;
        }
    }
}
