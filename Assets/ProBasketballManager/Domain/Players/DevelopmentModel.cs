using System;
using ProBasketballManager.Domain.Matches;

namespace ProBasketballManager.Domain.Players
{
    public static class DevelopmentModel
    {
        public const int GrowthEndsAtAge = 30;

        public const int PeakGrowthAge = 21;

        public const double MaximumGrowthPerSeason = 0.95;

        public const int PhysicalDeclineStartsAtAge = 29;
        public const int SkillDeclineStartsAtAge = 33;

        public const double PhysicalDeclinePerYear = 0.16;
        public const double SkillDeclinePerYear = 0.12;

        public const double MentalGrowthPerSeason = 0.22;

        public const double SeasonVariation = 0.55;

        public static PlayerAttributes Develop(Player player, IRandomSource random)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            var current = player.Attributes;
            var headroom = player.Potential - Player.GetOverallRating(current);

            return new PlayerAttributes(
                Move(current.Finishing, PlayerAttributeKind.Finishing, player, headroom, random),
                Move(current.MidRange, PlayerAttributeKind.MidRange, player, headroom, random),
                Move(current.ThreePoint, PlayerAttributeKind.ThreePoint, player, headroom, random),
                Move(current.FreeThrow, PlayerAttributeKind.FreeThrow, player, headroom, random),
                Move(current.Passing, PlayerAttributeKind.Passing, player, headroom, random),
                Move(current.BallHandling, PlayerAttributeKind.BallHandling, player, headroom, random),
                Move(current.PerimeterDefense, PlayerAttributeKind.PerimeterDefense, player, headroom, random),
                Move(current.InteriorDefense, PlayerAttributeKind.InteriorDefense, player, headroom, random),
                Move(current.OffensiveRebounding, PlayerAttributeKind.OffensiveRebounding, player, headroom, random),
                Move(current.DefensiveRebounding, PlayerAttributeKind.DefensiveRebounding, player, headroom, random),
                Move(current.Speed, PlayerAttributeKind.Speed, player, headroom, random),
                Move(current.Strength, PlayerAttributeKind.Strength, player, headroom, random),
                Move(current.Stamina, PlayerAttributeKind.Stamina, player, headroom, random),
                Move(current.BasketballIq, PlayerAttributeKind.BasketballIq, player, headroom, random));
        }

        private static int Move(int value, PlayerAttributeKind attribute, Player player, int headroom, IRandomSource random)
        {
            var category = AttributeCategories.GetCategory(attribute);

            var change = GetGrowth(player.Age, headroom, category) - GetDecline(player.Age, category);

            change += (random.NextDouble() - 0.5) * SeasonVariation;

            return PlayerAttributes.Clamp(value + RoundStochastically(change, random));
        }

        public static int RoundStochastically(double change, IRandomSource random)
        {
            var whole = Math.Floor(change);
            var fraction = change - whole;

            if (random.NextDouble() < fraction)
            {
                whole += 1;
            }

            return (int)whole;
        }

        public static double GetGrowth(int age, int headroom, AttributeCategory category)
        {
            if (category == AttributeCategory.Mental)
            {
                return MentalGrowthPerSeason;
            }

            if (age >= GrowthEndsAtAge || headroom <= 0)
            {
                return 0.0;
            }

            var ageFactor = age <= PeakGrowthAge ? 1.0 : 1.0 - ((age - PeakGrowthAge) / (double)(GrowthEndsAtAge - PeakGrowthAge));

            var headroomFactor = Math.Min(1.0, headroom / 3.0);

            return MaximumGrowthPerSeason * ageFactor * headroomFactor;
        }

        public static double GetDecline(int age, AttributeCategory category)
        {
            switch (category)
            {
                case AttributeCategory.Physical:
                    return age <= PhysicalDeclineStartsAtAge ? 0.0 : (age - PhysicalDeclineStartsAtAge) * PhysicalDeclinePerYear;

                case AttributeCategory.Skill:
                    return age <= SkillDeclineStartsAtAge ? 0.0 : (age - SkillDeclineStartsAtAge) * SkillDeclinePerYear;

                default:
                    return 0.0;
            }
        }
    }
}