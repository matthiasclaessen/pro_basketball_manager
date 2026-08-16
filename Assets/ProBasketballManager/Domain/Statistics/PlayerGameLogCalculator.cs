using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Statistics
{
    public static class PlayerGameLogCalculator
    {
        public static IReadOnlyList<PlayerGameLogEntry> Calculate(Season season, Team team, Player player)
        {
            if (season == null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            if (team == null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (!season.League.Teams.Any(leagueTeam => leagueTeam.Id == team.Id))
            {
                throw new ArgumentException("The team does not belong to this season's league.", nameof(team));
            }

            if (!team.Players.Any(teamPlayer => teamPlayer.Id == player.Id))
            {
                throw new ArgumentException("The player does not belong to this team.", nameof(player));
            }

            var entries = new List<PlayerGameLogEntry>();

            foreach (var fixture in season.GetFixturesForTeam(team).Where(fixture => fixture.IsPlayed).OrderBy(fixture => fixture.RoundNumber))
            {
                var playerStats = fixture.HomeTeam.Id == team.Id ? fixture.Result.HomePlayerStats : fixture.Result.AwayPlayerStats;
                var boxScore = playerStats.Single(statistics => statistics.Player.Id == player.Id);

                entries.Add(new PlayerGameLogEntry(fixture, team, boxScore));
            }

            return entries;
        }
    }
}