using System.Collections.Generic;
using ProBasketballManager.Domain.Clubs;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Demo
{
    public static class DemoSeasonFactory
    {
        public static Season Create(CompetitionRules rules = null)
        {
            return Create(DemoClubFactory.Create(), TeamType.First, rules);
        }

        public static Season Create(IReadOnlyList<Club> clubs, TeamType type, CompetitionRules rules = null)
        {
            var effectiveRules = rules ?? CompetitionRules.Fiba;

            var league = DemoLeagueFactory.CreateFrom(clubs, type);
            var fixtures = RoundRobinScheduleGenerator.Generate(league, effectiveRules);

            return new Season(type == TeamType.First ? 1 : 2, "2026 / 27", league, fixtures, effectiveRules);
        }
    }
}
