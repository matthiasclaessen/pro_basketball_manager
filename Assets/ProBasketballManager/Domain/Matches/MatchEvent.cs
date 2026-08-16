using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Matches
{
    public sealed class MatchEvent
    {
        public int PeriodNumber { get; }

        public int SecondsRemaining { get; }

        public MatchEventType Type { get; }

        public Team Team { get; }

        public Player Player { get; }

        public Player SecondaryPlayer { get; }

        public OffensiveActionType? OffensiveAction { get; }

        public ShotZone? ShotZone { get; }

        public int HomeScore { get; }

        public int AwayScore { get; }

        public MatchEvent(int periodNumber, int secondsRemaining, MatchEventType type, Team team, Player player, Player secondaryPlayer, int homeScore, int awayScore, OffensiveActionType? offensiveAction = null, ShotZone? shotZone = null)
        {
            PeriodNumber = periodNumber;
            SecondsRemaining = secondsRemaining;
            Type = type;
            Team = team;
            Player = player;
            SecondaryPlayer = secondaryPlayer;
            HomeScore = homeScore;
            AwayScore = awayScore;
            OffensiveAction = offensiveAction;
            ShotZone = shotZone;
        }
    }
}