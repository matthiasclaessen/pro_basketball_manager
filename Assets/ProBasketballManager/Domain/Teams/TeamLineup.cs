using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Players;

namespace ProBasketballManager.Domain.Teams
{
    public sealed class TeamLineup
    {
        public Team Team { get; }

        public IReadOnlyList<Player> Starters { get; }

        public IReadOnlyList<Player> SecondUnit { get; }

        public IReadOnlyList<Player> Bench { get; }

        private TeamLineup(Team team, IReadOnlyList<Player> starters, IReadOnlyList<Player> secondUnit, IReadOnlyList<Player> bench)
        {
            Team = team;
            Starters = starters;
            SecondUnit = secondUnit;
            Bench = bench;
        }

        public static TeamLineup CreateDefault(Team team)
        {
            var remainingPlayers = team.Players.ToList();
            var starters = new List<Player>();

            foreach (PlayerPosition position in Enum.GetValues(typeof(PlayerPosition)))
            {
                var player = remainingPlayers.FirstOrDefault(candidate => candidate.Position == position);

                if (player == null && remainingPlayers.Count > 0)
                {
                    player = remainingPlayers[0];
                }

                if (player == null)
                {
                    throw new InvalidOperationException($"Could not create a starting five for {team.Name}.");
                }

                starters.Add(player);
                remainingPlayers.Remove(player);
            }

            var bench = remainingPlayers.ToList();
            var secondUnit = CreateSecondUnit(starters, bench);

            return new TeamLineup(team, starters, secondUnit, bench);
        }

        public IReadOnlyList<Player> GetOnCourtPlayers(bool useSecondUnit)
        {
            return useSecondUnit ? SecondUnit : Starters;
        }

        public bool IsStarter(int playerId)
        {
            return Starters.Any(player => player.Id == playerId);
        }

        private static IReadOnlyList<Player> CreateSecondUnit(IReadOnlyList<Player> starters, IReadOnlyList<Player> bench)
        {
            var availableBenchPlayers = bench.ToList();
            var secondUnit = new List<Player>();

            foreach (var starter in starters)
            {
                var replacement = availableBenchPlayers.FirstOrDefault(player => player.Position == starter.Position);

                if (replacement == null && availableBenchPlayers.Count > 0)
                {
                    replacement = availableBenchPlayers[0];
                }

                if (replacement != null)
                {
                    secondUnit.Add(replacement);
                    availableBenchPlayers.Remove(replacement);
                }
                else
                {
                    secondUnit.Add(starter);
                }
            }

            return secondUnit;
        }
    }
}