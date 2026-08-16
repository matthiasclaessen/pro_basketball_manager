using ProBasketballManager.Domain.Competitions;

namespace ProBasketballManager.Domain.Demo
{
    public static class DemoSeasonFactory
    {
        public static Season Create()
        {
            var league = DemoLeagueFactory.Create();
            var fixtures = RoundRobinScheduleGenerator.Generate(league);

            return new Season(id: 1, name: "2026 / 27", league: league, fixtures: fixtures
            );
        }
    }
}