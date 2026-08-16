using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Statistics
{
    public sealed class PlayerGameLogEntry
    {
        public Fixture Fixture { get; }

        public Team Team { get; }

        public Team Opponent => Fixture.HomeTeam.Id == Team.Id ? Fixture.AwayTeam : Fixture.HomeTeam;

        public PlayerBoxScore BoxScore { get; }

        public int RoundNumber => Fixture.RoundNumber;

        public bool IsHome => Fixture.HomeTeam.Id == Team.Id;

        public bool DidPlay => BoxScore.MinutesPlayed > 0;

        public bool Won => IsHome ? Fixture.Result.HomeScore > Fixture.Result.AwayScore : Fixture.Result.AwayScore > Fixture.Result.HomeScore;

        public int TeamScore => IsHome ? Fixture.Result.HomeScore : Fixture.Result.AwayScore;

        public int OpponentScore => IsHome ? Fixture.Result.AwayScore : Fixture.Result.HomeScore;

        public PlayerGameLogEntry(Fixture fixture, Team team, PlayerBoxScore boxScore)
        {
            Fixture = fixture;
            Team = team;
            BoxScore = boxScore;
        }
    }
}