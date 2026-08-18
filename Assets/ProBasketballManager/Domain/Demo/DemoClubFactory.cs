using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Clubs;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Demo
{
    public static class DemoClubFactory
    {
        private sealed class ClubBlueprint
        {
            public int Id;
            public string Name;
            public int FirstTeamPlayerId;
            public int ReservePlayerId;
            public int OffenseModifier;
            public int DefenseModifier;
            public int ShootingModifier;
            public int NameOffset;
        }

        private static readonly ClubBlueprint[] Blueprints =
        {
            new ClubBlueprint { Id = 1, Name = "Antwerp Giants", FirstTeamPlayerId = 100, ReservePlayerId = 150, OffenseModifier = 0, DefenseModifier = 0, ShootingModifier = 0, NameOffset = 0 },
            new ClubBlueprint { Id = 2, Name = "Okapi Aalst", FirstTeamPlayerId = 200, ReservePlayerId = 250, OffenseModifier = 1, DefenseModifier = -1, ShootingModifier = 1, NameOffset = 3 },
            new ClubBlueprint { Id = 3, Name = "Leuven Bears", FirstTeamPlayerId = 300, ReservePlayerId = 350, OffenseModifier = -1, DefenseModifier = 2, ShootingModifier = 0, NameOffset = 6 },
            new ClubBlueprint { Id = 4, Name = "Filou Oostende", FirstTeamPlayerId = 400, ReservePlayerId = 450, OffenseModifier = 0, DefenseModifier = 0, ShootingModifier = 2, NameOffset = 9 }
        };

        public static IReadOnlyList<Club> Create(bool withDualRegistration = false)
        {
            return Blueprints.Select(blueprint => CreateClub(blueprint, withDualRegistration)).ToList();
        }

        private static Club CreateClub(ClubBlueprint blueprint, bool withDualRegistration)
        {
            var firstTeamPlayers = CreateRoster(blueprint.FirstTeamPlayerId, blueprint.OffenseModifier, blueprint.DefenseModifier, blueprint.ShootingModifier, blueprint.NameOffset, 0, false).ToList();
            var reservePlayers = CreateRoster(blueprint.ReservePlayerId, blueprint.OffenseModifier, blueprint.DefenseModifier, blueprint.ShootingModifier, blueprint.NameOffset + 5, -4, true).ToList();

            var calledUp = withDualRegistration ? reservePlayers.OrderByDescending(player => player.Potential).Take(2).ToList() : new List<Player>();

            var firstTeam = new Team(blueprint.Id, blueprint.Name + " A", firstTeamPlayers.Concat(calledUp), blueprint.Id, TeamType.First);
            var reserveTeam = new Team(blueprint.Id + 100, blueprint.Name + " B", reservePlayers, blueprint.Id, TeamType.Reserve);

            return new Club(blueprint.Id, blueprint.Name, firstTeamPlayers.Concat(reservePlayers), new[] { firstTeam, reserveTeam });
        }

        private static IEnumerable<Player> CreateRoster(int startingPlayerId, int offenseModifier, int defenseModifier, int shootingModifier, int nameOffset, int qualityModifier, bool youngRoster)
        {
            var positions = new[]
            {
                PlayerPosition.PointGuard,
                PlayerPosition.ShootingGuard,
                PlayerPosition.SmallForward,
                PlayerPosition.PowerForward,
                PlayerPosition.Center,

                PlayerPosition.PointGuard,
                PlayerPosition.ShootingGuard,
                PlayerPosition.SmallForward,
                PlayerPosition.PowerForward,
                PlayerPosition.Center,

                PlayerPosition.ShootingGuard,
                PlayerPosition.Center
            };

            for (var i = 0; i < positions.Length; i++)
            {
                var position = positions[i];

                yield return new Player(
                    id: startingPlayerId + i,
                    firstName: GetFirstName(i + nameOffset),
                    lastName: GetLastName(i + nameOffset),
                    position: position,
                    attributes: CreateAttributes(position, offenseModifier, defenseModifier, shootingModifier, i, qualityModifier),
                    age: GetStartingAge(i, youngRoster),
                    potential: GetStartingPotential(position, offenseModifier, defenseModifier, shootingModifier, i, qualityModifier, youngRoster),
                    scoutedPotential: 0
                );
            }
        }

        private static int GetStartingAge(int rosterIndex, bool youngRoster)
        {
            var ages = youngRoster ? new[] { 21, 19, 22, 20, 18, 23, 19, 21, 20, 18, 19, 22 } : new[] { 29, 27, 24, 31, 22, 26, 33, 20, 25, 23, 19, 28 };

            return ages[rosterIndex % ages.Length];
        }

        private static int GetStartingPotential(PlayerPosition position, int offenseModifier, int defenseModifier, int shootingModifier, int rosterIndex, int qualityModifier, bool youngRoster)
        {
            var current = Player.GetOverallRating(CreateAttributes(position, offenseModifier, defenseModifier, shootingModifier, rosterIndex, qualityModifier));

            var age = GetStartingAge(rosterIndex, youngRoster);

            var upside = age <= 21 ? 6 : age <= 24 ? 4 : age <= 27 ? 2 : 0;

            return PlayerAttributes.Clamp(current + upside);
        }

        private static PlayerAttributes CreateAttributes(PlayerPosition position, int offenseModifier, int defenseModifier, int shootingModifier, int playerIndex, int qualityModifier)
        {
            var variation = (playerIndex % 3) - 1;

            return position switch
            {
                PlayerPosition.PointGuard => new PlayerAttributes(
                    finishing: Attribute(11 + offenseModifier + variation + qualityModifier),
                    midRange: Attribute(12 + offenseModifier + variation + qualityModifier),
                    threePoint: Attribute(13 + shootingModifier + variation + qualityModifier),
                    freeThrow: Attribute(14 + variation + qualityModifier),
                    passing: Attribute(15 + offenseModifier + qualityModifier),
                    ballHandling: Attribute(16 + offenseModifier + qualityModifier),
                    perimeterDefense: Attribute(12 + defenseModifier + qualityModifier),
                    interiorDefense: Attribute(5 + defenseModifier + qualityModifier),
                    offensiveRebounding: Attribute(4 + qualityModifier),
                    defensiveRebounding: Attribute(6 + qualityModifier),
                    speed: Attribute(16 + qualityModifier),
                    strength: Attribute(8 + qualityModifier),
                    stamina: Attribute(14 + qualityModifier),
                    basketballIq: Attribute(15 + qualityModifier)
                ),

                PlayerPosition.ShootingGuard => new PlayerAttributes(
                    finishing: Attribute(13 + offenseModifier + variation + qualityModifier),
                    midRange: Attribute(13 + offenseModifier + qualityModifier),
                    threePoint: Attribute(15 + shootingModifier + variation + qualityModifier),
                    freeThrow: Attribute(14 + qualityModifier),
                    passing: Attribute(11 + offenseModifier + qualityModifier),
                    ballHandling: Attribute(13 + offenseModifier + qualityModifier),
                    perimeterDefense: Attribute(12 + defenseModifier + qualityModifier),
                    interiorDefense: Attribute(6 + defenseModifier + qualityModifier),
                    offensiveRebounding: Attribute(5 + qualityModifier),
                    defensiveRebounding: Attribute(7 + qualityModifier),
                    speed: Attribute(14 + qualityModifier),
                    strength: Attribute(9 + qualityModifier),
                    stamina: Attribute(14 + qualityModifier),
                    basketballIq: Attribute(12 + qualityModifier)
                ),

                PlayerPosition.SmallForward => new PlayerAttributes(
                    finishing: Attribute(13 + offenseModifier + qualityModifier),
                    midRange: Attribute(13 + offenseModifier + variation + qualityModifier),
                    threePoint: Attribute(12 + shootingModifier + qualityModifier),
                    freeThrow: Attribute(12 + qualityModifier),
                    passing: Attribute(11 + qualityModifier),
                    ballHandling: Attribute(11 + qualityModifier),
                    perimeterDefense: Attribute(12 + defenseModifier + qualityModifier),
                    interiorDefense: Attribute(10 + defenseModifier + qualityModifier),
                    offensiveRebounding: Attribute(8 + qualityModifier),
                    defensiveRebounding: Attribute(10 + qualityModifier),
                    speed: Attribute(13 + qualityModifier),
                    strength: Attribute(12 + qualityModifier),
                    stamina: Attribute(13 + qualityModifier),
                    basketballIq: Attribute(12 + qualityModifier)
                ),

                PlayerPosition.PowerForward => new PlayerAttributes(
                    finishing: Attribute(14 + offenseModifier + qualityModifier),
                    midRange: Attribute(11 + offenseModifier + qualityModifier),
                    threePoint: Attribute(9 + shootingModifier + qualityModifier),
                    freeThrow: Attribute(11 + qualityModifier),
                    passing: Attribute(10 + qualityModifier),
                    ballHandling: Attribute(9 + qualityModifier),
                    perimeterDefense: Attribute(10 + defenseModifier + qualityModifier),
                    interiorDefense: Attribute(14 + defenseModifier + qualityModifier),
                    offensiveRebounding: Attribute(12 + qualityModifier),
                    defensiveRebounding: Attribute(13 + qualityModifier),
                    speed: Attribute(10 + qualityModifier),
                    strength: Attribute(15 + qualityModifier),
                    stamina: Attribute(13 + qualityModifier),
                    basketballIq: Attribute(11 + qualityModifier)
                ),

                PlayerPosition.Center => new PlayerAttributes(
                    finishing: Attribute(15 + offenseModifier + variation + qualityModifier),
                    midRange: Attribute(8 + offenseModifier + qualityModifier),
                    threePoint: Attribute(5 + shootingModifier + qualityModifier),
                    freeThrow: Attribute(10 + qualityModifier),
                    passing: Attribute(8 + qualityModifier),
                    ballHandling: Attribute(7 + qualityModifier),
                    perimeterDefense: Attribute(8 + defenseModifier + qualityModifier),
                    interiorDefense: Attribute(16 + defenseModifier + qualityModifier),
                    offensiveRebounding: Attribute(14 + qualityModifier),
                    defensiveRebounding: Attribute(15 + qualityModifier),
                    speed: Attribute(8 + qualityModifier),
                    strength: Attribute(17 + qualityModifier),
                    stamina: Attribute(12 + qualityModifier),
                    basketballIq: Attribute(11 + qualityModifier)
                ),

                _ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
            };
        }

        private static int Attribute(int value)
        {
            return Math.Clamp(value, 1, 20);
        }

        private static string GetFirstName(int index)
        {
            var names = new[] { "Liam", "Noah", "Milan", "Lucas", "Arthur", "Finn", "Louis", "Victor", "Adam", "Jules", "Mathis", "Elias", "Max", "Thomas", "Nicolas", "Julian", "Nathan", "Simon", "Robin", "Alex" };

            return names[index % names.Length];
        }

        private static string GetLastName(int index)
        {
            var names = new[] { "Janssens", "Peeters", "Maes", "Jacobs", "Mertens", "Willems", "Claes", "Goossens", "Wouters", "De Smet", "Vermeulen", "Verhoeven", "Dubois", "Lambert", "Martens", "De Vos", "Vandamme", "De Clercq", "Van den Berg", "Lemaire" };

            return names[index % names.Length];
        }
    }
}
