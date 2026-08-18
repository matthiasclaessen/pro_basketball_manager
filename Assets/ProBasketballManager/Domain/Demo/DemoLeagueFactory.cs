using System;
using System.Collections.Generic;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Demo
{
    public static class DemoLeagueFactory
    {
        public static League Create()
        {
            var teams = new List<Team>
            {
                new Team(
                    id: 1,
                    name: "Antwerp Giants",
                    players: CreateRoster(
                        startingPlayerId: 100,
                        offenseModifier: 0,
                        defenseModifier: 0,
                        shootingModifier: 0,
                        nameOffset: 0
                    )
                ),

                new Team(
                    id: 2,
                    name: "Okapi Aalst",
                    players: CreateRoster(
                        startingPlayerId: 200,
                        offenseModifier: 1,
                        defenseModifier: -1,
                        shootingModifier: 1,
                        nameOffset: 3
                    )
                ),

                new Team(
                    id: 3,
                    name: "Leuven Bears",
                    players: CreateRoster(
                        startingPlayerId: 300,
                        offenseModifier: -1,
                        defenseModifier: 2,
                        shootingModifier: 0,
                        nameOffset: 6
                    )
                ),

                new Team(
                    id: 4,
                    name: "Filou Oostende",
                    players: CreateRoster(
                        startingPlayerId: 400,
                        offenseModifier: 0,
                        defenseModifier: 0,
                        shootingModifier: 2,
                        nameOffset: 9
                    )
                )
            };

            return new League(
                id: 1,
                name: "Belgian Demo League",
                teams: teams
            );
        }

        private static IEnumerable<Player> CreateRoster(int startingPlayerId,int offenseModifier,int defenseModifier,int shootingModifier,int nameOffset)
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
                    attributes: CreateAttributes(
                        position,
                        offenseModifier,
                        defenseModifier,
                        shootingModifier,
                        i
                    ),
                    age: GetStartingAge(i),
                    potential: GetStartingPotential(position, offenseModifier, defenseModifier, shootingModifier, i),
                    scoutedPotential: 0
                );
            }
        }

        private static int GetStartingAge(int rosterIndex)
        {
            var ages = new[] { 29, 27, 24, 31, 22, 26, 33, 20, 25, 23, 19, 28 };

            return ages[rosterIndex % ages.Length];
        }

        private static int GetStartingPotential(PlayerPosition position,int offenseModifier,int defenseModifier,int shootingModifier,int rosterIndex)
        {
            var current = Player.GetOverallRating(
                CreateAttributes(position, offenseModifier, defenseModifier, shootingModifier, rosterIndex));

            var age = GetStartingAge(rosterIndex);

            var upside = age <= 21 ? 6: age <= 24 ? 4: age <= 27 ? 2: 0;

            return PlayerAttributes.Clamp(current + upside);
        }

        private static PlayerAttributes CreateAttributes(PlayerPosition position,int offenseModifier,int defenseModifier,int shootingModifier,int playerIndex)
        {
            var variation = (playerIndex % 3) - 1;

            return position switch
            {
                PlayerPosition.PointGuard => new PlayerAttributes(
                    finishing: Attribute(11 + offenseModifier + variation),
                    midRange: Attribute(12 + offenseModifier + variation),
                    threePoint: Attribute(13 + shootingModifier + variation),
                    freeThrow: Attribute(14 + variation),
                    passing: Attribute(15 + offenseModifier),
                    ballHandling: Attribute(16 + offenseModifier),
                    perimeterDefense: Attribute(12 + defenseModifier),
                    interiorDefense: Attribute(5 + defenseModifier),
                    offensiveRebounding: Attribute(4),
                    defensiveRebounding: Attribute(6),
                    speed: Attribute(16),
                    strength: Attribute(8),
                    stamina: Attribute(14),
                    basketballIq: Attribute(15)
                ),

                PlayerPosition.ShootingGuard => new PlayerAttributes(
                    finishing: Attribute(13 + offenseModifier + variation),
                    midRange: Attribute(13 + offenseModifier),
                    threePoint: Attribute(15 + shootingModifier + variation),
                    freeThrow: Attribute(14),
                    passing: Attribute(11 + offenseModifier),
                    ballHandling: Attribute(13 + offenseModifier),
                    perimeterDefense: Attribute(12 + defenseModifier),
                    interiorDefense: Attribute(6 + defenseModifier),
                    offensiveRebounding: Attribute(5),
                    defensiveRebounding: Attribute(7),
                    speed: Attribute(14),
                    strength: Attribute(9),
                    stamina: Attribute(14),
                    basketballIq: Attribute(12)
                ),

                PlayerPosition.SmallForward => new PlayerAttributes(
                    finishing: Attribute(13 + offenseModifier),
                    midRange: Attribute(13 + offenseModifier + variation),
                    threePoint: Attribute(12 + shootingModifier),
                    freeThrow: Attribute(12),
                    passing: Attribute(11),
                    ballHandling: Attribute(11),
                    perimeterDefense: Attribute(12 + defenseModifier),
                    interiorDefense: Attribute(10 + defenseModifier),
                    offensiveRebounding: Attribute(8),
                    defensiveRebounding: Attribute(10),
                    speed: Attribute(13),
                    strength: Attribute(12),
                    stamina: Attribute(13),
                    basketballIq: Attribute(12)
                ),

                PlayerPosition.PowerForward => new PlayerAttributes(
                    finishing: Attribute(14 + offenseModifier),
                    midRange: Attribute(11 + offenseModifier),
                    threePoint: Attribute(9 + shootingModifier),
                    freeThrow: Attribute(11),
                    passing: Attribute(10),
                    ballHandling: Attribute(9),
                    perimeterDefense: Attribute(10 + defenseModifier),
                    interiorDefense: Attribute(14 + defenseModifier),
                    offensiveRebounding: Attribute(12),
                    defensiveRebounding: Attribute(13),
                    speed: Attribute(10),
                    strength: Attribute(15),
                    stamina: Attribute(13),
                    basketballIq: Attribute(11)
                ),

                PlayerPosition.Center => new PlayerAttributes(
                    finishing: Attribute(15 + offenseModifier + variation),
                    midRange: Attribute(8 + offenseModifier),
                    threePoint: Attribute(5 + shootingModifier),
                    freeThrow: Attribute(10),
                    passing: Attribute(8),
                    ballHandling: Attribute(7),
                    perimeterDefense: Attribute(8 + defenseModifier),
                    interiorDefense: Attribute(16 + defenseModifier),
                    offensiveRebounding: Attribute(14),
                    defensiveRebounding: Attribute(15),
                    speed: Attribute(8),
                    strength: Attribute(17),
                    stamina: Attribute(12),
                    basketballIq: Attribute(11)
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
            var names = new[]
            {
                "Liam",
                "Noah",
                "Milan",
                "Lucas",
                "Arthur",
                "Finn",
                "Louis",
                "Victor",
                "Adam",
                "Jules",
                "Mathis",
                "Elias",
                "Max",
                "Thomas",
                "Nicolas",
                "Julian",
                "Nathan",
                "Simon",
                "Robin",
                "Alex"
            };

            return names[index % names.Length];
        }

        private static string GetLastName(int index)
        {
            var names = new[]
            {
                "Janssens",
                "Peeters",
                "Maes",
                "Jacobs",
                "Mertens",
                "Willems",
                "Claes",
                "Goossens",
                "Wouters",
                "De Smet",
                "Vermeulen",
                "Verhoeven",
                "Dubois",
                "Lambert",
                "Martens",
                "De Vos",
                "Vandamme",
                "De Clercq",
                "Van den Berg",
                "Lemaire"
            };

            return names[index % names.Length];
        }
    }
}