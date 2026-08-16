namespace ProBasketballManager.Domain.Matches
{
    public enum MatchEventType
    {
        Turnover,
        Steal,

        MadeTwoPointer,
        MissedTwoPointer,

        MadeThreePointer,
        MissedThreePointer,

        OffensiveRebound,
        DefensiveRebound,

        ShootingFoul,

        MadeFreeThrow,
        MissedFreeThrow
    }
}