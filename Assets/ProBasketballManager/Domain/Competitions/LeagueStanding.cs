using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Competitions
{
    public sealed class LeagueStanding
    {
        public int Position { get; }

        public Team Team { get; }

        public int Played { get; }

        public int Wins { get; }

        public int Losses { get; }

        public int PointsFor { get; }

        public int PointsAgainst { get; }

        public int PointDifference => PointsFor - PointsAgainst;

        public double WinPercentage => Played == 0 ? 0 : Wins / (double)Played;

        public LeagueStanding(int position, Team team, int played, int wins, int losses, int pointsFor, int pointsAgainst)
        {
            Position = position;
            Team = team;
            Played = played;
            Wins = wins;
            Losses = losses;
            PointsFor = pointsFor;
            PointsAgainst = pointsAgainst;
        }
    }
}