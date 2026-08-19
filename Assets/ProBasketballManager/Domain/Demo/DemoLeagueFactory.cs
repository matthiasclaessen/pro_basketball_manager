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

        public static League CreateFrom(IReadOnlyList<Club> clubs, TeamType type)
        {
            var name = type == TeamType.First ? "Belgian Demo League" : "Belgian Demo Reserve League";
            var id = type == TeamType.First ? 1 : 2;

            var calendar = type == TeamType.First ? CompetitionCalendar.Default : new CompetitionCalendar(DayOfWeek.Sunday, 7, 9, 15);

            return new League(id, name, clubs.Where(club => club.HasTeam(type)).Select(club => club.GetTeam(type)), calendar);
        }
    }
}
