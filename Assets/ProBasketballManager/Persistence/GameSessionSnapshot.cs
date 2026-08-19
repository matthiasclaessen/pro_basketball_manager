using System.Collections.Generic;
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

        public List<int> ManagedTeamIds;

        public Dictionary<int, TeamTactics> Tactics;

        public Dictionary<int, TeamRotation> Rotations;

        public TeamTactics UserTactics;

        public TeamRotation UserRotation;

        public uint NextSeed;

        public bool CurrentFixtureRecorded;
    }
}
