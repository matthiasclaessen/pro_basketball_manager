using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ProBasketballManager.Domain.Clubs;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Persistence
{
    public sealed class GameDatabaseException : Exception
    {
        public GameDatabaseException(string message) : base(message) { }

        public GameDatabaseException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class GameDatabase
    {
        public string Name { get; }

        public IReadOnlyList<Club> Clubs { get; }

        public IReadOnlyList<League> Competitions { get; }

        public IReadOnlyDictionary<int, CompetitionRules> Rules { get; }

        public GameDatabase(string name, IReadOnlyList<Club> clubs, IReadOnlyList<League> competitions, IReadOnlyDictionary<int, CompetitionRules> rules)
        {
            Name = name;
            Clubs = clubs;
            Competitions = competitions;
            Rules = rules;
        }
    }

    public static class GameDatabaseRepository
    {
        public const int SupportedSchemaVersion = 1;

        public const string DefaultDatabaseName = "default";

        public static string GetDatabaseDirectory(string streamingAssetsPath, string databaseName)
        {
            return Path.Combine(streamingAssetsPath, "databases", databaseName);
        }

        public static IReadOnlyList<string> ListDatabases(string streamingAssetsPath)
        {
            var root = Path.Combine(streamingAssetsPath, "databases");

            if (!Directory.Exists(root))
            {
                return new List<string>();
            }

            return Directory.GetDirectories(root).Select(Path.GetFileName).OrderBy(name => name).ToList();
        }

        public static GameDatabase Load(string streamingAssetsPath, string databaseName = DefaultDatabaseName)
        {
            var path = Path.Combine(GetDatabaseDirectory(streamingAssetsPath, databaseName), "database.json");

            if (!File.Exists(path))
            {
                throw new GameDatabaseException($"No database file found at '{path}'.");
            }

            string json;

            try
            {
                json = File.ReadAllText(path);
            }
            catch (IOException exception)
            {
                throw new GameDatabaseException($"The database at '{path}' could not be read.", exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                throw new GameDatabaseException($"The database at '{path}' could not be read.", exception);
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new GameDatabaseException($"The database at '{path}' is empty.");
            }

            GameDatabaseDto dto;

            try
            {
                dto = JsonConvert.DeserializeObject<GameDatabaseDto>(json);
            }
            catch (JsonException exception)
            {
                throw new GameDatabaseException($"The database at '{path}' is not valid JSON.", exception);
            }

            if (dto == null)
            {
                throw new GameDatabaseException($"The database at '{path}' contained no data.");
            }

            return FromDto(dto);
        }

        public static GameDatabase FromDto(GameDatabaseDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (dto.SchemaVersion != SupportedSchemaVersion)
            {
                throw new GameDatabaseException($"This database uses schema {dto.SchemaVersion}; this build reads schema {SupportedSchemaVersion}.");
            }

            RequireUniqueIds(dto.Clubs?.Select(club => club.Id), "club");
            RequireUniqueIds(dto.Teams?.Select(team => team.Id), "team");
            RequireUniqueIds(dto.Players?.Select(player => player.Id), "player");
            RequireUniqueIds(dto.Competitions?.Select(competition => competition.Id), "competition");

            var playersById = (dto.Players ?? new List<DatabasePlayerDto>()).ToDictionary(player => player.Id, BuildPlayer);

            var clubs = new List<Club>();
            var teamsById = new Dictionary<int, Team>();

            foreach (var clubDto in dto.Clubs ?? new List<DatabaseClubDto>())
            {
                var squad = (dto.Players ?? new List<DatabasePlayerDto>()).Where(player => player.ClubId == clubDto.Id).Select(player => playersById[player.Id]).ToList();

                if (squad.Count == 0)
                {
                    throw new GameDatabaseException($"Club '{clubDto.Name}' has no players.");
                }

                var teamDtos = (dto.Teams ?? new List<DatabaseTeamDto>()).Where(team => team.ClubId == clubDto.Id).ToList();

                if (teamDtos.Count == 0)
                {
                    throw new GameDatabaseException($"Club '{clubDto.Name}' fields no teams.");
                }

                var teams = teamDtos.Select(team => BuildTeam(team, clubDto, playersById)).ToList();

                foreach (var team in teams)
                {
                    teamsById[team.Id] = team;
                }

                try
                {
                    clubs.Add(new Club(clubDto.Id, clubDto.Name, squad, teams, clubDto.ShortName, clubDto.City, clubDto.PrimaryColor, clubDto.SecondaryColor));
                }
                catch (ArgumentException exception)
                {
                    throw new GameDatabaseException($"Club '{clubDto.Name}' is not valid: {exception.Message}", exception);
                }
            }

            var competitions = new List<League>();
            var rules = new Dictionary<int, CompetitionRules>();

            foreach (var competitionDto in dto.Competitions ?? new List<DatabaseCompetitionDto>())
            {
                var teams = competitionDto.TeamIds.Select(id => ResolveTeam(teamsById, id, competitionDto.Name)).ToList();

                if (teams.Count < 2)
                {
                    throw new GameDatabaseException($"Competition '{competitionDto.Name}' needs at least two teams.");
                }

                competitions.Add(new League(competitionDto.Id, competitionDto.Name, teams, BuildCalendar(competitionDto)));

                rules[competitionDto.Id] = BuildRules(competitionDto);
            }

            if (competitions.Count == 0)
            {
                throw new GameDatabaseException("The database contains no competitions.");
            }

            var unassigned = teamsById.Values.Where(team => competitions.All(competition => competition.Teams.All(entry => entry.Id != team.Id))).ToList();

            if (unassigned.Count > 0)
            {
                throw new GameDatabaseException($"Team '{unassigned[0].Name}' is not entered in any competition.");
            }

            return new GameDatabase(dto.Name, clubs, competitions, rules);
        }

        private static Team BuildTeam(DatabaseTeamDto dto, DatabaseClubDto club, Dictionary<int, Player> playersById)
        {
            var players = new List<Player>();

            foreach (var playerId in dto.PlayerIds ?? new List<int>())
            {
                if (!playersById.TryGetValue(playerId, out var player))
                {
                    throw new GameDatabaseException($"Team '{dto.Name}' registers unknown player {playerId}.");
                }

                players.Add(player);
            }

            try
            {
                return new Team(dto.Id, dto.Name, players, club.Id, ParseTeamType(dto));
            }
            catch (ArgumentException exception)
            {
                throw new GameDatabaseException($"Team '{dto.Name}' is not valid: {exception.Message}", exception);
            }
        }

        private static TeamType ParseTeamType(DatabaseTeamDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Type))
            {
                return TeamType.First;
            }

            if (!Enum.TryParse<TeamType>(dto.Type, false, out var parsed) || !Enum.IsDefined(typeof(TeamType), parsed))
            {
                throw new GameDatabaseException($"Team '{dto.Name}' has an unrecognised type '{dto.Type}'.");
            }

            return parsed;
        }

        private static Player BuildPlayer(DatabasePlayerDto dto)
        {
            if (!Enum.TryParse<PlayerPosition>(dto.Position, false, out var position) || !Enum.IsDefined(typeof(PlayerPosition), position))
            {
                throw new GameDatabaseException($"Player {dto.Id} has an unrecognised position '{dto.Position}'.");
            }

            if (dto.Attributes == null)
            {
                throw new GameDatabaseException($"Player {dto.Id} has no attributes.");
            }

            try
            {
                var attributes = new PlayerAttributes(
                    dto.Attributes.Finishing, dto.Attributes.MidRange, dto.Attributes.ThreePoint, dto.Attributes.FreeThrow,
                    dto.Attributes.Passing, dto.Attributes.BallHandling,
                    dto.Attributes.PerimeterDefense, dto.Attributes.InteriorDefense,
                    dto.Attributes.OffensiveRebounding, dto.Attributes.DefensiveRebounding,
                    dto.Attributes.Speed, dto.Attributes.Strength, dto.Attributes.Stamina, dto.Attributes.BasketballIq);

                return new Player(dto.Id, dto.FirstName, dto.LastName, position, attributes, dto.Age <= 0 ? Player.DefaultAge : dto.Age, dto.Potential, dto.ScoutedPotential, dto.Nationality);
            }
            catch (ArgumentException exception)
            {
                throw new GameDatabaseException($"Player {dto.Id} ('{dto.FirstName} {dto.LastName}') is not valid: {exception.Message}", exception);
            }
        }

        private static CompetitionCalendar BuildCalendar(DatabaseCompetitionDto dto)
        {
            if (dto.Calendar == null)
            {
                return CompetitionCalendar.Default;
            }

            if (!Enum.TryParse<DayOfWeek>(dto.Calendar.MatchDay, false, out var matchDay))
            {
                throw new GameDatabaseException($"Competition '{dto.Name}' has an unrecognised match day '{dto.Calendar.MatchDay}'.");
            }

            try
            {
                return new CompetitionCalendar(matchDay, dto.Calendar.DaysBetweenRounds, dto.Calendar.FirstRoundMonth, dto.Calendar.FirstRoundDay);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new GameDatabaseException($"Competition '{dto.Name}' has an invalid calendar: {exception.Message}", exception);
            }
        }

        private static CompetitionRules BuildRules(DatabaseCompetitionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Rules) || string.Equals(dto.Rules, "Fiba", StringComparison.OrdinalIgnoreCase))
            {
                return CompetitionRules.Fiba;
            }

            if (string.Equals(dto.Rules, "Nba", StringComparison.OrdinalIgnoreCase))
            {
                return CompetitionRules.Nba;
            }

            throw new GameDatabaseException($"Competition '{dto.Name}' uses unknown rules '{dto.Rules}'.");
        }

        private static Team ResolveTeam(Dictionary<int, Team> teams, int id, string competitionName)
        {
            if (!teams.TryGetValue(id, out var team))
            {
                throw new GameDatabaseException($"Competition '{competitionName}' includes unknown team {id}.");
            }

            return team;
        }

        private static void RequireUniqueIds(IEnumerable<int> ids, string what)
        {
            if (ids == null)
            {
                return;
            }

            var duplicate = ids.GroupBy(id => id).FirstOrDefault(group => group.Count() > 1);

            if (duplicate != null)
            {
                throw new GameDatabaseException($"The database lists {what} {duplicate.Key} more than once.");
            }
        }
    }
}
