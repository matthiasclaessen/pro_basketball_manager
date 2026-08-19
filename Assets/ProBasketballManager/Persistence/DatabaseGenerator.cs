using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Players;

namespace ProBasketballManager.Persistence
{
    public static class DatabaseGenerator
    {
        private sealed class ClubSeed
        {
            public int Id;
            public string Name;
            public string ShortName;
            public string City;
            public double Strength;
        }

        private static readonly ClubSeed[] Clubs =
        {
            new ClubSeed { Id = 1, Name = "Oostende", ShortName = "OOS", City = "Oostende", Strength = 2.6 },
            new ClubSeed { Id = 2, Name = "Antwerp Giants", ShortName = "ANT", City = "Antwerp", Strength = 2.3 },
            new ClubSeed { Id = 3, Name = "Kangoeroes Mechelen", ShortName = "MEC", City = "Mechelen", Strength = 1.5 },
            new ClubSeed { Id = 4, Name = "Limburg United", ShortName = "LIM", City = "Hasselt", Strength = 0.8 },
            new ClubSeed { Id = 5, Name = "Leuven Bears", ShortName = "LEU", City = "Leuven", Strength = 0.3 },
            new ClubSeed { Id = 6, Name = "Spirou Charleroi", ShortName = "CHA", City = "Charleroi", Strength = 0.0 },
            new ClubSeed { Id = 7, Name = "Okapi Aalst", ShortName = "AAL", City = "Aalst", Strength = -0.4 },
            new ClubSeed { Id = 8, Name = "Kortrijk Spurs", ShortName = "KOR", City = "Kortrijk", Strength = -0.8 },
            new ClubSeed { Id = 9, Name = "Mons-Hainaut", ShortName = "MON", City = "Mons", Strength = -1.2 },
            new ClubSeed { Id = 10, Name = "Liege Basket", ShortName = "LIE", City = "Liege", Strength = -1.7 },
            new ClubSeed { Id = 11, Name = "Brussels Basketball", ShortName = "BRU", City = "Brussels", Strength = -2.2 }
        };

        private static readonly double[] RoleModifiers = { 4.2, 2.8, 2.0, 1.4, 0.8, 0.1, -0.5, -1.2, -1.9, -2.6, -3.4, -4.2 };

        private static readonly PlayerPosition[] RosterPositions =
        {
            PlayerPosition.PointGuard, PlayerPosition.ShootingGuard, PlayerPosition.SmallForward, PlayerPosition.PowerForward, PlayerPosition.Center,
            PlayerPosition.PointGuard, PlayerPosition.ShootingGuard, PlayerPosition.SmallForward, PlayerPosition.PowerForward, PlayerPosition.Center,
            PlayerPosition.ShootingGuard, PlayerPosition.Center
        };

        private static readonly string[] FirstNames =
        {
            "Liam", "Noah", "Milan", "Lucas", "Arthur", "Finn", "Louis", "Victor", "Adam", "Jules",
            "Mathis", "Elias", "Max", "Thomas", "Nicolas", "Julian", "Nathan", "Simon", "Robin", "Alex",
            "Wout", "Senne", "Vince", "Lars", "Basil", "Ruben", "Jonas", "Seppe", "Ferre", "Tuur",
            "Emile", "Gustave", "Hugo", "Raphael", "Cyriel", "Bram", "Kobe", "Jarne", "Warre", "Stan"
        };

        private static readonly string[] LastNames =
        {
            "Peeters", "Janssens", "Maes", "Jacobs", "Mertens", "Willems", "Claes", "Goossens", "Wouters", "De Smet",
            "Vermeulen", "Verhoeven", "Aerts", "Hermans", "Dubois", "Lambert", "Michiels", "Segers", "Coppens", "Pauwels",
            "De Vos", "Vandamme", "De Clercq", "Van den Berg", "Lemaire", "Dupont", "Martin", "Simon", "Leroy", "Renard",
            "Declercq", "Vandenbossche", "Van Damme", "De Backer", "Cools", "Smets", "Bogaert", "Naessens", "Verlinden", "Storme"
        };

        public static GameDatabaseDto Generate(uint seed = 20260901)
        {
            var random = new XorShiftRandom(seed);

            var database = new GameDatabaseDto
            {
                SchemaVersion = 1,
                Name = "Belgium 2026/27",
                Description = "Belgian first division clubs with generated squads."
            };

            var playerId = 1000;

            foreach (var club in Clubs)
            {
                database.Clubs.Add(new DatabaseClubDto { Id = club.Id, Name = club.Name, ShortName = club.ShortName, City = club.City });

                var squad = new List<DatabasePlayerDto>();

                for (var slot = 0; slot < RosterPositions.Length; slot++)
                {
                    squad.Add(CreatePlayer(playerId++, club, RosterPositions[slot], RoleModifiers[slot], random));
                }

                database.Players.AddRange(squad);

                database.Teams.Add(new DatabaseTeamDto
                {
                    Id = club.Id,
                    ClubId = club.Id,
                    Name = club.Name,
                    Type = "First",
                    PlayerIds = squad.Select(player => player.Id).ToList()
                });
            }

            database.Competitions.Add(new DatabaseCompetitionDto
            {
                Id = 1,
                Name = "Belgian First Division",
                Rules = "Fiba",
                TeamIds = database.Teams.Select(team => team.Id).ToList(),
                Calendar = new DatabaseCalendarDto { MatchDay = "Saturday", DaysBetweenRounds = 7, FirstRoundMonth = 9, FirstRoundDay = 26 }
            });

            return database;
        }

        private static DatabasePlayerDto CreatePlayer(int id, ClubSeed club, PlayerPosition position, double roleModifier, XorShiftRandom random)
        {
            var baseline = 10.5 + club.Strength + roleModifier + ((random.NextDouble() - 0.5) * 2.2);

            var age = PickAge(roleModifier, random);

            var attributes = CreateAttributes(baseline, position, random);

            var currentAbility = PlayerRating.CalculateCurrentAbility(position, attributes);

            var upside = age <= 20 ? 45 : age <= 23 ? 30 : age <= 26 ? 16 : age <= 29 ? 6 : 0;

            var potential = PlayerRating.ClampAbility(currentAbility + upside + (int)((random.NextDouble() - 0.4) * 22));

            var scoutingError = (int)((random.NextDouble() - 0.5) * 26);

            return new DatabasePlayerDto
            {
                Id = id,
                ClubId = club.Id,
                FirstName = FirstNames[random.NextInt(0, FirstNames.Length)],
                LastName = LastNames[random.NextInt(0, LastNames.Length)],
                Position = position.ToString(),
                Age = age,
                Potential = potential,
                ScoutedPotential = PlayerRating.ClampAbility(potential + scoutingError),
                Attributes = ToDto(attributes)
            };
        }

        private static int PickAge(double roleModifier, XorShiftRandom random)
        {
            if (roleModifier <= -2.5)
            {
                return random.NextInt(18, 23);
            }

            if (roleModifier >= 2.5)
            {
                return random.NextInt(25, 33);
            }

            return random.NextInt(21, 34);
        }

        private static PlayerAttributes CreateAttributes(double baseline, PlayerPosition position, XorShiftRandom random)
        {
            int Roll(double bias)
            {
                var noise = (random.NextDouble() - 0.5) * 3.6;

                return PlayerAttributes.Clamp(baseline + bias + noise);
            }

            var guard = position == PlayerPosition.PointGuard || position == PlayerPosition.ShootingGuard;
            var big = position == PlayerPosition.PowerForward || position == PlayerPosition.Center;
            var centre = position == PlayerPosition.Center;

            return new PlayerAttributes(
                finishing: Roll(big ? 2.5 : 0.0),
                midRange: Roll(centre ? -3.0 : 0.5),
                threePoint: Roll(guard ? 2.5 : centre ? -5.5 : -1.0),
                freeThrow: Roll(guard ? 1.5 : -1.5),
                passing: Roll(position == PlayerPosition.PointGuard ? 4.0 : guard ? 1.0 : big ? -2.5 : -0.5),
                ballHandling: Roll(position == PlayerPosition.PointGuard ? 4.5 : guard ? 1.5 : centre ? -4.5 : -2.0),
                perimeterDefense: Roll(guard ? 2.0 : centre ? -3.5 : 0.0),
                interiorDefense: Roll(centre ? 5.0 : big ? 3.0 : guard ? -4.0 : -0.5),
                offensiveRebounding: Roll(centre ? 4.0 : big ? 2.5 : guard ? -4.5 : -1.0),
                defensiveRebounding: Roll(centre ? 4.5 : big ? 3.0 : guard ? -3.5 : 0.0),
                speed: Roll(guard ? 3.0 : centre ? -3.5 : 0.5),
                strength: Roll(centre ? 4.5 : big ? 3.0 : guard ? -3.0 : 0.0),
                stamina: Roll(0.5),
                basketballIq: Roll(position == PlayerPosition.PointGuard ? 2.5 : 0.0));
        }

        private static DatabaseAttributesDto ToDto(PlayerAttributes attributes)
        {
            return new DatabaseAttributesDto
            {
                Finishing = attributes.Finishing,
                MidRange = attributes.MidRange,
                ThreePoint = attributes.ThreePoint,
                FreeThrow = attributes.FreeThrow,
                Passing = attributes.Passing,
                BallHandling = attributes.BallHandling,
                PerimeterDefense = attributes.PerimeterDefense,
                InteriorDefense = attributes.InteriorDefense,
                OffensiveRebounding = attributes.OffensiveRebounding,
                DefensiveRebounding = attributes.DefensiveRebounding,
                Speed = attributes.Speed,
                Strength = attributes.Strength,
                Stamina = attributes.Stamina,
                BasketballIq = attributes.BasketballIq
            };
        }
    }
}
