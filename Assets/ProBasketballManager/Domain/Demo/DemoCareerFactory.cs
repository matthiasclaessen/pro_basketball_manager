using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Demo
{
    public static class DemoCareerFactory
    {
        public static Career Create(CompetitionRules rules = null, bool withDualRegistration = false)
        {
            var clubs = DemoClubFactory.Create(withDualRegistration);

            var season = DemoSeasonFactory.Create(clubs, TeamType.First, rules);

            return new Career(clubs, season.League, season);
        }
    }
}
