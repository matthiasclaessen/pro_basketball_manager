using System;
using System.Linq;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Demo;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Tactics;
using ProBasketballManager.Domain.Teams;
using ProBasketballManager.Persistence;

namespace ProBasketballManager.Presentation.State
{
    public sealed class GameSession
    {
        public Career Career { get; private set; }

        public Season Season => Career.CurrentSeason;

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

        private GameSession(Career career, Team userTeam)
        {
            Career = career ?? throw new ArgumentNullException(nameof(career));
            UserTeam = userTeam ?? throw new ArgumentNullException(nameof(userTeam));

            UserTactics = TeamTactics.Default;
            UserRotation = TeamRotation.CreateDefault(userTeam, career.CurrentSeason.Rules);

            RefreshCurrentFixture();
        }

        public GameSessionSnapshot CreateSnapshot()
        {
            return new GameSessionSnapshot
            {
                Career = Career,
                UserTeam = UserTeam,
                UserTactics = UserTactics,
                UserRotation = UserRotation,
                NextSeed = NextSeed,
                CurrentFixtureRecorded = CurrentFixtureRecorded
            };
        }

        public static GameSession Restore(GameSessionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return new GameSession(snapshot.Career, snapshot.UserTeam)
            {
                UserTactics = snapshot.UserTactics,
                UserRotation = snapshot.UserRotation,
                NextSeed = snapshot.NextSeed,
                CurrentFixtureRecorded = snapshot.CurrentFixtureRecorded
            };
        }

        public static GameSession CreateDemo()
        {
            var season = DemoSeasonFactory.Create();

            var career = new Career(season.League, season);

            return new GameSession(career, career.League.Teams[0]);
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

            var homeRotation = userIsHome ? UserRotation : TeamRotation.CreateDefault(CurrentFixture.HomeTeam, Season.Rules);
            var awayRotation = userIsHome ? TeamRotation.CreateDefault(CurrentFixture.AwayTeam, Season.Rules) : UserRotation;

            var homeTactics = userIsHome ? UserTactics : TeamTactics.Default;
            var awayTactics = userIsHome ? TeamTactics.Default : UserTactics;

            var simulator = new MatchSimulator(new XorShiftRandom(seed), Season.Rules);

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
            var simulator = new MatchSimulator(new XorShiftRandom(NextSeed), Season.Rules);
            NextSeed++;

            var result = simulator.Simulate(
                fixture.HomeTeam,
                fixture.AwayTeam,
                TeamRotation.CreateDefault(fixture.HomeTeam, Season.Rules),
                TeamRotation.CreateDefault(fixture.AwayTeam, Season.Rules),
                TeamTactics.Default,
                TeamTactics.Default);

            Season.RecordResult(fixture.Id, result);
        }

        public CompletedSeason AdvanceToNextSeason()
        {
            var archived = Career.AdvanceToNextSeason();

            UserRotation = TeamRotation.CreateDefault(UserTeam, Season.Rules);

            CurrentMatchResult = null;
            CurrentFixtureRecorded = false;

            RefreshCurrentFixture();

            return archived;
        }

        public bool CanAdvanceSeason => Career.CanAdvance;

        public void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}