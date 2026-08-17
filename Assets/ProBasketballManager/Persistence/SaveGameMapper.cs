using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Tactics;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Persistence
{
    public static class SaveGameMapper
    {
        public const int CurrentSchemaVersion = 1;

        public static SaveGameDto ToDto(GameSessionSnapshot snapshot, string saveName)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var season = snapshot.Season;

            return new SaveGameDto
            {
                SchemaVersion = CurrentSchemaVersion,
                SaveName = saveName,
                SavedAtUtc = DateTime.UtcNow.ToString("o"),
                Description = BuildDescription(snapshot),
                League = ToDto(season.League),
                Season = ToSeasonDto(season),
                UserTeamId = snapshot.UserTeam.Id,
                UserTactics = ToDto(snapshot.UserTactics),
                UserRotation = ToDto(snapshot.UserRotation),
                NextSeed = snapshot.NextSeed,
                CurrentFixtureRecorded = snapshot.CurrentFixtureRecorded
            };
        }

        private static string BuildDescription(GameSessionSnapshot snapshot)
        {
            var season = snapshot.Season;

            var standing = season.GetStandings()
                .FirstOrDefault(entry => entry.Team.Id == snapshot.UserTeam.Id);

            var record = standing == null ? string.Empty : $" ({standing.Wins}-{standing.Losses})";

            return season.IsComplete ? $"{snapshot.UserTeam.Name}{record} - season complete" : $"{snapshot.UserTeam.Name}{record} - round {season.CurrentRoundNumber} of {season.TotalRounds}";
        }

        private static LeagueDto ToDto(League league)
        {
            return new LeagueDto
            {
                Id = league.Id,
                Name = league.Name,
                Teams = league.Teams.Select(ToDto).ToList()
            };
        }

        private static TeamDto ToDto(Team team)
        {
            return new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                Players = team.Players.Select(ToDto).ToList()
            };
        }

        private static PlayerDto ToDto(Player player)
        {
            return new PlayerDto
            {
                Id = player.Id,
                FirstName = player.FirstName,
                LastName = player.LastName,
                Position = player.Position.ToString(),
                Attributes = ToDto(player.Attributes)
            };
        }

        private static PlayerAttributesDto ToDto(PlayerAttributes attributes)
        {
            return new PlayerAttributesDto
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

        private static SeasonDto ToSeasonDto(Season season)
        {
            return new SeasonDto
            {
                Id = season.Id,
                Name = season.Name,
                Fixtures = season.Fixtures.Select(ToDto).ToList()
            };
        }

        private static FixtureDto ToDto(Fixture fixture)
        {
            return new FixtureDto
            {
                Id = fixture.Id,
                RoundNumber = fixture.RoundNumber,
                HomeTeamId = fixture.HomeTeam.Id,
                AwayTeamId = fixture.AwayTeam.Id,
                Result = fixture.IsPlayed ? ToDto(fixture.Result) : null
            };
        }

        private static MatchResultDto ToDto(MatchResult result)
        {
            return new MatchResultDto
            {
                HomeTeamId = result.HomeTeam.Id,
                AwayTeamId = result.AwayTeam.Id,
                HomeRotation = ToDto(result.HomeRotation),
                AwayRotation = ToDto(result.AwayRotation),
                HomePeriodScores = result.HomePeriodScores.ToList(),
                AwayPeriodScores = result.AwayPeriodScores.ToList(),
                HomePlayerStats = result.HomePlayerStats.Select(ToDto).ToList(),
                AwayPlayerStats = result.AwayPlayerStats.Select(ToDto).ToList(),
                Events = result.Events.Select(ToDto).ToList()
            };
        }

        private static TeamRotationDto ToDto(TeamRotation rotation)
        {
            return new TeamRotationDto
            {
                TeamId = rotation.Team.Id,
                Starters = rotation.Starters
                    .Select(entry => new StarterDto
                    {
                        Position = entry.Key.ToString(),
                        PlayerId = entry.Value.Id
                    })
                    .ToList(),
                Assignments = rotation.Assignments
                    .Select(assignment => new RotationAssignmentDto
                    {
                        PlayerId = assignment.Player.Id,
                        TargetMinutes = assignment.TargetMinutes,
                        RotationOrder = assignment.RotationOrder
                    })
                    .ToList(),
                PrimaryBallHandlerId = rotation.PrimaryBallHandler.Id,
                PrimaryScorerId = rotation.PrimaryScorer.Id
            };
        }

        private static TeamTacticsDto ToDto(TeamTactics tactics)
        {
            return new TeamTacticsDto
            {
                Pace = tactics.Pace,
                RimWeight = tactics.RimWeight,
                MidRangeWeight = tactics.MidRangeWeight,
                ThreePointWeight = tactics.ThreePointWeight,
                BallMovement = tactics.BallMovement,
                PerimeterPressure = tactics.PerimeterPressure,
                ProtectPaint = tactics.ProtectPaint
            };
        }

        private static PlayerBoxScoreDto ToDto(PlayerBoxScore box)
        {
            return new PlayerBoxScoreDto
            {
                PlayerId = box.Player.Id,
                IsStarter = box.IsStarter,
                SecondsPlayed = box.SecondsPlayed,
                Points = box.Points,
                FieldGoalsMade = box.FieldGoalsMade,
                FieldGoalsAttempted = box.FieldGoalsAttempted,
                ThreePointsMade = box.ThreePointsMade,
                ThreePointsAttempted = box.ThreePointsAttempted,
                FreeThrowsMade = box.FreeThrowsMade,
                FreeThrowsAttempted = box.FreeThrowsAttempted,
                OffensiveRebounds = box.OffensiveRebounds,
                DefensiveRebounds = box.DefensiveRebounds,
                Assists = box.Assists,
                Steals = box.Steals,
                PersonalFouls = box.PersonalFouls,
                Turnovers = box.Turnovers,
                EndOfMatchFatigue = box.EndOfMatchFatigue,
                PeakFatigue = box.PeakFatigue
            };
        }

        private static MatchEventDto ToDto(MatchEvent matchEvent)
        {
            return new MatchEventDto
            {
                PeriodNumber = matchEvent.PeriodNumber,
                SecondsRemaining = matchEvent.SecondsRemaining,
                Type = matchEvent.Type.ToString(),
                TeamId = matchEvent.Team.Id,
                PlayerId = matchEvent.Player.Id,
                SecondaryPlayerId = matchEvent.SecondaryPlayer?.Id ?? MatchEventDto.NoPlayer,
                OffensiveAction = matchEvent.OffensiveAction?.ToString(),
                ShotZone = matchEvent.ShotZone?.ToString(),
                HomeScore = matchEvent.HomeScore,
                AwayScore = matchEvent.AwayScore
            };
        }

        public static GameSessionSnapshot FromDto(SaveGameDto dto)
        {
            if (dto == null)
            {
                throw new SaveGameException("The save file was empty.");
            }

            if (dto.SchemaVersion != CurrentSchemaVersion)
            {
                throw new SaveGameException(
                    $"This save was written by a different version of the game (schema {dto.SchemaVersion}, " +
                    $"this build reads schema {CurrentSchemaVersion}).");
            }

            if (dto.League == null || dto.Season == null)
            {
                throw new SaveGameException("The save file is missing its league or season.");
            }

            var league = FromDto(dto.League);

            var teams = league.Teams.ToDictionary(team => team.Id);

            var players = league.Teams
                .SelectMany(team => team.Players)
                .ToDictionary(player => player.Id);

            var season = FromDto(dto.Season, league, teams, players);

            var userTeam = Resolve(teams, dto.UserTeamId, "user team");

            return new GameSessionSnapshot
            {
                Season = season,
                UserTeam = userTeam,
                UserTactics = FromDto(dto.UserTactics),
                UserRotation = FromDto(dto.UserRotation, teams, players),
                NextSeed = dto.NextSeed,
                CurrentFixtureRecorded = dto.CurrentFixtureRecorded
            };
        }

        private static League FromDto(LeagueDto dto)
        {
            var teams = dto.Teams.Select(FromDto).ToList();

            return new League(dto.Id, dto.Name, teams);
        }

        private static Team FromDto(TeamDto dto)
        {
            var players = dto.Players.Select(FromDto).ToList();

            return new Team(dto.Id, dto.Name, players);
        }

        private static Player FromDto(PlayerDto dto)
        {
            return new Player(
                dto.Id,
                dto.FirstName,
                dto.LastName,
                ParseEnum<PlayerPosition>(dto.Position, "player position"),
                FromDto(dto.Attributes));
        }

        private static PlayerAttributes FromDto(PlayerAttributesDto dto)
        {
            return new PlayerAttributes(
                dto.Finishing,
                dto.MidRange,
                dto.ThreePoint,
                dto.FreeThrow,
                dto.Passing,
                dto.BallHandling,
                dto.PerimeterDefense,
                dto.InteriorDefense,
                dto.OffensiveRebounding,
                dto.DefensiveRebounding,
                dto.Speed,
                dto.Strength,
                dto.Stamina,
                dto.BasketballIq);
        }

        private static Season FromDto(SeasonDto dto, League league, Dictionary<int, Team> teams, Dictionary<int, Player> players)
        {
            var fixtures = new List<Fixture>(dto.Fixtures.Count);

            foreach (var fixtureDto in dto.Fixtures)
            {
                var fixture = new Fixture(
                    fixtureDto.Id,
                    fixtureDto.RoundNumber,
                    Resolve(teams, fixtureDto.HomeTeamId, "fixture home team"),
                    Resolve(teams, fixtureDto.AwayTeamId, "fixture away team"));

                if (fixtureDto.Result != null)
                {
                    fixture.Complete(FromDto(fixtureDto.Result, teams, players));
                }

                fixtures.Add(fixture);
            }

            return new Season(dto.Id, dto.Name, league, fixtures);
        }

        private static MatchResult FromDto(MatchResultDto dto, Dictionary<int, Team> teams, Dictionary<int, Player> players)
        {
            return new MatchResult(
                Resolve(teams, dto.HomeTeamId, "result home team"),
                Resolve(teams, dto.AwayTeamId, "result away team"),
                FromDto(dto.HomeRotation, teams, players),
                FromDto(dto.AwayRotation, teams, players),
                dto.HomePeriodScores.ToList(),
                dto.AwayPeriodScores.ToList(),
                dto.Events.Select(matchEvent => FromDto(matchEvent, teams, players)).ToList(),
                dto.HomePlayerStats.Select(box => FromDto(box, players)).ToList(),
                dto.AwayPlayerStats.Select(box => FromDto(box, players)).ToList());
        }

        private static TeamRotation FromDto(TeamRotationDto dto, Dictionary<int, Team> teams, Dictionary<int, Player> players)
        {
            if (dto == null)
            {
                throw new SaveGameException("The save file is missing a rotation.");
            }

            var starters = dto.Starters.ToDictionary(
                starter => ParseEnum<PlayerPosition>(starter.Position, "starter position"),
                starter => Resolve(players, starter.PlayerId, "starter"));

            var assignments = dto.Assignments
                .Select(assignment => new PlayerRotationAssignment(
                    Resolve(players, assignment.PlayerId, "rotation assignment"),
                    assignment.TargetMinutes,
                    assignment.RotationOrder))
                .ToList();

            return new TeamRotation(
                Resolve(teams, dto.TeamId, "rotation team"),
                starters,
                assignments,
                Resolve(players, dto.PrimaryBallHandlerId, "primary ball handler"),
                Resolve(players, dto.PrimaryScorerId, "primary scorer"));
        }

        private static TeamTactics FromDto(TeamTacticsDto dto)
        {
            if (dto == null)
            {
                return TeamTactics.Default;
            }

            return new TeamTactics(
                dto.Pace,
                dto.RimWeight,
                dto.MidRangeWeight,
                dto.ThreePointWeight,
                dto.BallMovement,
                dto.PerimeterPressure,
                dto.ProtectPaint);
        }

        private static PlayerBoxScore FromDto(PlayerBoxScoreDto dto, Dictionary<int, Player> players)
        {
            return new PlayerBoxScore(
                Resolve(players, dto.PlayerId, "box score player"),
                dto.IsStarter,
                dto.SecondsPlayed,
                dto.Points,
                dto.FieldGoalsMade,
                dto.FieldGoalsAttempted,
                dto.ThreePointsMade,
                dto.ThreePointsAttempted,
                dto.FreeThrowsMade,
                dto.FreeThrowsAttempted,
                dto.OffensiveRebounds,
                dto.DefensiveRebounds,
                dto.Assists,
                dto.Steals,
                dto.PersonalFouls,
                dto.Turnovers,
                dto.EndOfMatchFatigue,
                dto.PeakFatigue);
        }

        private static MatchEvent FromDto(MatchEventDto dto, Dictionary<int, Team> teams, Dictionary<int, Player> players)
        {
            var secondaryPlayer = dto.SecondaryPlayerId == MatchEventDto.NoPlayer
                ? null
                : Resolve(players, dto.SecondaryPlayerId, "secondary player");

            OffensiveActionType? offensiveAction = string.IsNullOrEmpty(dto.OffensiveAction)
                ? (OffensiveActionType?)null
                : ParseEnum<OffensiveActionType>(dto.OffensiveAction, "offensive action");

            ShotZone? shotZone = string.IsNullOrEmpty(dto.ShotZone)
                ? (ShotZone?)null
                : ParseEnum<ShotZone>(dto.ShotZone, "shot zone");

            return new MatchEvent(
                dto.PeriodNumber,
                dto.SecondsRemaining,
                ParseEnum<MatchEventType>(dto.Type, "match event type"),
                Resolve(teams, dto.TeamId, "event team"),
                Resolve(players, dto.PlayerId, "event player"),
                secondaryPlayer,
                dto.HomeScore,
                dto.AwayScore,
                offensiveAction,
                shotZone);
        }

        private static TValue Resolve<TValue>(Dictionary<int, TValue> lookup, int id, string what)
        {
            if (lookup.TryGetValue(id, out var value))
            {
                return value;
            }

            throw new SaveGameException($"The save file refers to an unknown {what} (id {id}).");
        }

        private static TEnum ParseEnum<TEnum>(string value, string what) where TEnum : struct
        {
            if (Enum.TryParse<TEnum>(value, out var parsed))
            {
                return parsed;
            }

            throw new SaveGameException($"The save file contains an unrecognised {what}: '{value}'.");
        }
    }
}