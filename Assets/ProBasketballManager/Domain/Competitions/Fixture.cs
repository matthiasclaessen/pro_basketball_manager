using System;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Competitions
{
    public sealed class Fixture
    {
        public int Id { get; }

        public int RoundNumber { get; }

        public Team HomeTeam { get; }

        public Team AwayTeam { get; }

        public DateTime Date { get; }

        public MatchResult Result { get; private set; }

        public bool IsPlayed => Result != null;

        public Fixture(int id, int roundNumber, Team homeTeam, Team awayTeam, DateTime date)
        {
            if (homeTeam.Id == awayTeam.Id)
            {
                throw new ArgumentException("A team cannot play against itself.");
            }

            if (roundNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber), "Round number must be at least 1.");
            }

            if (date == default)
            {
                throw new ArgumentException("A fixture must be scheduled on a real date.", nameof(date));
            }

            Id = id;
            RoundNumber = roundNumber;
            Date = date.Date;
            HomeTeam = homeTeam;
            AwayTeam = awayTeam;
        }

        public void Complete(MatchResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (IsPlayed)
            {
                throw new InvalidOperationException("This fixture has already been played.");
            }

            if (result.HomeTeam.Id != HomeTeam.Id || result.AwayTeam.Id != AwayTeam.Id)
            {
                throw new ArgumentException("The match result does not belong to this fixture.", nameof(result));
            }

            if (result.HomeScore == result.AwayScore)
            {
                throw new ArgumentException("A basketball fixture cannot finish tied.", nameof(result));
            }

            Result = result;
        }
    }
}
