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
        PersonalFoul,
        OffensiveFoul,
        LooseBallFoul,
        FoulOut,

        MadeFreeThrow,
        MissedFreeThrow
    }
}