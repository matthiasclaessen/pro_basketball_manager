using ProBasketballManager.Domain.Players;

namespace ProBasketballManager.Domain.Players
{
    public enum AttributeCategory
    {
        Physical,

        Skill,

        Mental
    }

    public static class AttributeCategories
    {
        public static AttributeCategory GetCategory(PlayerAttributeKind attribute)
        {
            switch (attribute)
            {
                case PlayerAttributeKind.Speed:
                case PlayerAttributeKind.Strength:
                case PlayerAttributeKind.Stamina:
                    return AttributeCategory.Physical;

                case PlayerAttributeKind.BasketballIq:
                    return AttributeCategory.Mental;

                default:
                    return AttributeCategory.Skill;
            }
        }
    }

    public enum PlayerAttributeKind
    {
        Finishing,
        MidRange,
        ThreePoint,
        FreeThrow,
        Passing,
        BallHandling,
        PerimeterDefense,
        InteriorDefense,
        OffensiveRebounding,
        DefensiveRebounding,
        Speed,
        Strength,
        Stamina,
        BasketballIq
    }
}