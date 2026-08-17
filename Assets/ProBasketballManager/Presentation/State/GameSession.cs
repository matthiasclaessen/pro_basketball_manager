using System;
using System.Linq;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Demo;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Tactics;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Presentation.State
{
    public sealed class GameSession
    {
        public Season Season { get; private set; }

        public Team UserTeam { get; private set; }

        public Fixture CurrentFixture { get; private set; }

        public Team HomeTeam => CurrentFixture?.HomeTeam;

        public Team AwayTeam => CurrentFixture?.AwayTeam;

        public TeamTactics UserTactics { get; set; }

        public TeamRotation UserRotation { get; set; }

        public MatchResult CurrentMatchResult { get; set; }

        public Player SelectedPlayer { get; set; }

        public uint NextSeed { get; set; } = 12345;

        public bool CurrentFixtureRecorded { get; set; }

        public event Action Changed;

        private GameSession(Season season, Team userTeam)
        {
            Season = season ?? throw new ArgumentNullException(nameof(season));
            UserTeam = userTeam ?? throw new ArgumentNullException(nameof(userTeam));

            UserTactics = TeamTactics.Default;
            UserRotation = TeamRotation.CreateDefault(userTeam);

            RefreshCurrentFixture();
        }

        public static GameSession CreateDemo()
        {
            var season = DemoSeasonFactory.Create();

            return new GameSession(season, season.League.Teams[0]);
        }

        public void RefreshCurrentFixture()
        {
            CurrentFixture = Season.GetNextFixtureForTeam(UserTeam);
        }

        public MatchResult SimulateCurrentFixture()
        {
            if (CurrentFixture == null || CurrentFixture.IsPlayed)
            {
                return null;
            }

            var seed = NextSeed;
            NextSeed++;

            var userIsHome = CurrentFixture.HomeTeam.Id == UserTeam.Id;

            var homeRotation = userIsHome ? UserRotation : TeamRotation.CreateDefault(CurrentFixture.HomeTeam);
            var awayRotation = userIsHome ? TeamRotation.CreateDefault(CurrentFixture.AwayTeam) : UserRotation;

            var homeTactics = userIsHome ? UserTactics : TeamTactics.Default;
            var awayTactics = userIsHome ? TeamTactics.Default : UserTactics;

            var simulator = new MatchSimulator(new XorShiftRandom(seed));

            CurrentMatchResult = simulator.Simulate(
                CurrentFixture.HomeTeam,
                CurrentFixture.AwayTeam,
                homeRotation,
                awayRotation,
                homeTactics,
                awayTactics);

            LastUsedSeed = seed;
            CurrentFixtureRecorded = false;

            return CurrentMatchResult;
        }

        public uint LastUsedSeed { get; private set; }

        public void CompleteCurrentRound()
        {
            if (CurrentFixtureRecorded || CurrentFixture == null || CurrentMatchResult == null)
            {
                return;
            }

            var completedRound = CurrentFixture.RoundNumber;

            Season.RecordResult(CurrentFixture.Id, CurrentMatchResult);

            var remaining = Season.GetFixturesForRound(completedRound)
                .Where(fixture => !fixture.IsPlayed)
                .ToList();

            foreach (var fixture in remaining)
            {
                SimulateAiFixture(fixture);
            }

            CurrentFixtureRecorded = true;

            RefreshCurrentFixture();
        }

        private void SimulateAiFixture(Fixture fixture)
        {
            var simulator = new MatchSimulator(new XorShiftRandom(NextSeed));
            NextSeed++;

            var result = simulator.Simulate(
                fixture.HomeTeam,
                fixture.AwayTeam,
                TeamRotation.CreateDefault(fixture.HomeTeam),
                TeamRotation.CreateDefault(fixture.AwayTeam),
                TeamTactics.Default,
                TeamTactics.Default);

            Season.RecordResult(fixture.Id, result);
        }

        public void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}