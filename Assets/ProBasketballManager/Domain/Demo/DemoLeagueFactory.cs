using System.Collections.Generic;
using System.Linq;
using System;
using ProBasketballManager.Domain.Clubs;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Demo
{
    public static class DemoLeagueFactory
    {
        public static League Create()
        {
            return CreateFrom(DemoClubFactory.Create(), TeamType.First);
        }

        public static League CreateFrom(IReadOnlyList<Club> clubs, TeamType level)
        {
            var name = level == TeamType.First ? "Belgian Demo League" : "Belgian Demo Reserve League";
            var id = level == TeamType.First ? 1 : 2;

            var calendar = level == TeamType.First ? CompetitionCalendar.Default : new CompetitionCalendar(DayOfWeek.Sunday, 7, 9, 15);

            return new League(id, name, clubs.Where(club => club.HasTeam(level)).Select(club => club.GetTeam(level)), calendar);
        }
    }
}
