using ProBasketballManager.Domain.Competitions;

namespace ProBasketballManager.Domain.Demo
{
    public static class DemoSeasonFactory
    {
        public static Season Create(CompetitionRules rules = null)
        {
            var effectiveRules = rules ?? CompetitionRules.Fiba;

            var league = DemoLeagueFactory.Create();
            var fixtures = RoundRobinScheduleGenerator.Generate(league, effectiveRules);

            return new Season(
                id: 1,
                name: "2026 / 27",
                league: league,
                fixtures: fixtures,
                rules: effectiveRules);
        }
    }
}