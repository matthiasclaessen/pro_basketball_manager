using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Tactics;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Persistence
{
    public sealed class GameSessionSnapshot
    {
        public Career Career;

        public Season Season => Career?.CurrentSeason;

        public Team UserTeam;

        public TeamTactics UserTactics;

        public TeamRotation UserRotation;

        public uint NextSeed;

        public bool CurrentFixtureRecorded;
    }
}