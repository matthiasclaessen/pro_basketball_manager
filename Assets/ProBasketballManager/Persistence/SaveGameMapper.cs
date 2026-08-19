using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Clubs;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Matches;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Statistics;
using ProBasketballManager.Domain.Tactics;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Persistence
{
    public static class SaveGameMapper
    {
        public const int CurrentSchemaVersion = 10;

        public const int MinimumReadableSchemaVersion = 1;

        public static SaveGameDto ToDto(GameSessionSnapshot snapshot, string saveName)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var season = snapshot.Career.CurrentSeason;

            return new SaveGameDto
            {
                SchemaVersion = CurrentSchemaVersion,
                SaveName = saveName,
                SavedAtUtc = DateTime.UtcNow.ToString("o"),
                Description = BuildDescription(snapshot),
                Clubs = snapshot.Career.Clubs.Select(ToDto).ToList(),
                Leagues = snapshot.Career.Seasons.Select(running => ToDto(running.League)).ToList(),
                Seasons = snapshot.Career.Seasons.Select(ToSeasonDto).ToList(),
                League = ToDto(season.League),
                Season = ToSeasonDto(season),
                CompletedSeasons = snapshot.Career.CompletedSeasons.Select(ToDto).ToList(),
                RetiredPlayers = snapshot.Career.RetiredPlayers.Select(ToDto).ToList(),
                UserClubId = snapshot.UserTeam.ClubId,
                UserTeamId = snapshot.UserTeam.Id,
                ManagedTeamIds = snapshot.ManagedTeamIds ?? new List<int> { snapshot.UserTeam.Id },
                TeamSetups = BuildTeamSetups(snapshot),
                UserTactics = ToDto(snapshot.UserTactics),
                UserRotation = ToDto(snapshot.UserRotation),
                NextSeed = snapshot.NextSeed,
                CurrentDate = snapshot.Career.CurrentDate.ToString("yyyy-MM-dd"),
                CurrentFixtureRecorded = snapshot.CurrentFixtureRecorded
            };
        }

        private static string BuildDescription(GameSessionSnapshot snapshot)
        {
            var season = snapshot.Career.CurrentSeason;

            var standing = season.GetStandings()
                .FirstOrDefault(entry => entry.Team.Id == snapshot.UserTeam.Id);

            var record = standing == null ? string.Empty : $" ({standing.Wins}-{standing.Losses})";

            var careerPrefix = snapshot.Career.SeasonsCompleted == 0 ? string.Empty : $"season {snapshot.Career.SeasonsCompleted + 1}, ";

            return season.IsComplete ? $"{snapshot.UserTeam.Name}{record} - {careerPrefix}season complete" : $"{snapshot.UserTeam.Name}{record} - {careerPrefix}round {season.CurrentRoundNumber} of {season.TotalRounds}";
        }

        private static CompletedSeasonDto ToDto(CompletedSeason season)
        {
            return new CompletedSeasonDto
            {
                Id = season.Id,
                CompetitionId = season.CompetitionId,
                Name = season.Name,
                FinalStandings = season.FinalStandings
                    .Select(standing => new ArchivedStandingDto
                    {
                        Position = standing.Position,
                        TeamId = standing.Team.Id,
                        Played = standing.Played,
                        Wins = standing.Wins,
                        Losses = standing.Losses,
                        PointsFor = standing.PointsFor,
                        PointsAgainst = standing.PointsAgainst
                    })
                    .ToList(),
                PlayerStatistics = season.PlayerStatistics
                    .Select(statistics => new ArchivedPlayerSeasonDto
                    {
                        PlayerId = statistics.Player.Id,
                        GamesPlayed = statistics.GamesPlayed,
                        GamesStarted = statistics.GamesStarted,
                        TotalMinutes = statistics.TotalMinutes,
                        Points = statistics.Points,
                        FieldGoalsMade = statistics.FieldGoalsMade,
                        FieldGoalsAttempted = statistics.FieldGoalsAttempted,
                        ThreePointsMade = statistics.ThreePointsMade,
                        ThreePointsAttempted = statistics.ThreePointsAttempted,
                        FreeThrowsMade = statistics.FreeThrowsMade,
                        FreeThrowsAttempted = statistics.FreeThrowsAttempted,
                        OffensiveRebounds = statistics.OffensiveRebounds,
                        DefensiveRebounds = statistics.DefensiveRebounds,
                        Assists = statistics.Assists,
                        Steals = statistics.Steals,
                        PersonalFouls = statistics.PersonalFouls,
                        Turnovers = statistics.Turnovers
                    })
                    .ToList()
            };
        }

        private static ClubDto ToDto(Club club)
        {
            return new ClubDto
            {
                Id = club.Id,
                Name = club.Name,
                ShortName = club.ShortName,
                City = club.City,
                PrimaryColor = club.PrimaryColor,
                SecondaryColor = club.SecondaryColor,
                TertiaryColor = club.TertiaryColor,
                BadgeTemplate = club.BadgeTemplate,
                Squad = club.Squad.Select(ToDto).ToList(),
                Teams = club.Teams.Select(ToDto).ToList()
            };
        }

        private static LeagueDto ToDto(League league)
        {
            return new LeagueDto
            {
                Id = league.Id,
                Name = league.Name,
                Calendar = ToDto(league.Calendar),
                TeamIds = league.Teams.Select(team => team.Id).ToList()
            };
        }

        private static List<TeamSetupDto> BuildTeamSetups(GameSessionSnapshot snapshot)
        {
            var teamIds = new List<int>();

            if (snapshot.Tactics != null)
            {
                teamIds.AddRange(snapshot.Tactics.Keys);
            }

            if (snapshot.Rotations != null)
            {
                teamIds.AddRange(snapshot.Rotations.Keys);
            }

            return teamIds.Distinct().OrderBy(id => id).Select(id => new TeamSetupDto
            {
                TeamId = id,
                Tactics = snapshot.Tactics != null && snapshot.Tactics.TryGetValue(id, out var tactics) ? ToDto(tactics) : null,
                Rotation = snapshot.Rotations != null && snapshot.Rotations.TryGetValue(id, out var rotation) ? ToDto(rotation) : null
            }).ToList();
        }

        private static CompetitionCalendarDto ToDto(CompetitionCalendar calendar)
        {
            return new CompetitionCalendarDto
            {
                MatchDay = calendar.MatchDay.ToString(),
                DaysBetweenRounds = calendar.DaysBetweenRounds,
                FirstRoundMonth = calendar.FirstRoundMonth,
                FirstRoundDay = calendar.FirstRoundDay
            };
        }

        private static CompetitionCalendar FromDto(CompetitionCalendarDto dto)
        {
            if (dto == null)
            {
                return CompetitionCalendar.Default;
            }

            try
            {
                return new CompetitionCalendar(ParseEnum<DayOfWeek>(dto.MatchDay, "match day"), dto.DaysBetweenRounds, dto.FirstRoundMonth, dto.FirstRoundDay);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new SaveGameException($"The save file contains an invalid competition calendar: {exception.Message}", exception);
            }
        }

        private static TeamDto ToDto(Team team)
        {
            return new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                Type = team.Type.ToString(),
                PlayerIds = team.Players.Select(player => player.Id).ToList()
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
                Nationality = player.Nationality,
                Attributes = ToDto(player.Attributes),
                Age = player.Age,
                Potential = player.Potential,
                ScoutedPotential = player.ScoutedPotential
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
                CompetitionId = season.League.Id,
                Name = season.Name,
                Rules = ToDto(season.Rules),
                Fixtures = season.Fixtures.Select(ToDto).ToList()
            };
        }

        private static CompetitionRulesDto ToDto(CompetitionRules rules)
        {
            return new CompetitionRulesDto
            {
                Name = rules.Name,
                PeriodCount = rules.PeriodCount,
                PeriodLengthSeconds = rules.PeriodLengthSeconds,
                OvertimeLengthSeconds = rules.OvertimeLengthSeconds,
                PlayersOnCourt = rules.PlayersOnCourt,
                PersonalFoulsToDisqualify = rules.PersonalFoulsToDisqualify,
                TeamFoulsBeforeBonus = rules.TeamFoulsBeforeBonus,
                BonusFreeThrows = rules.BonusFreeThrows,
                RosterSize = rules.RosterSize,
                RoundRobinPasses = rules.RoundRobinPasses
            };
        }

        private static CompetitionRules FromDto(CompetitionRulesDto dto)
        {
            // Saves written before rules were data all assumed FIBA.
            if (dto == null)
            {
                return CompetitionRules.Fiba;
            }

            try
            {
                return new CompetitionRules(
                    dto.Name,
                    dto.PeriodCount,
                    dto.PeriodLengthSeconds,
                    dto.OvertimeLengthSeconds,
                    dto.PlayersOnCourt,
                    dto.PersonalFoulsToDisqualify,
                    dto.TeamFoulsBeforeBonus,
                    dto.BonusFreeThrows,
                    dto.RosterSize,
                    dto.RoundRobinPasses);
            }
            catch (ArgumentException exception)
            {
                throw new SaveGameException($"The save file contains an invalid ruleset: {exception.Message}", exception);
            }
        }

        private static FixtureDto ToDto(Fixture fixture)
        {
            return new FixtureDto
            {
                Id = fixture.Id,
                RoundNumber = fixture.RoundNumber,
                Date = fixture.Date.ToString("yyyy-MM-dd"),
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

            if (dto.SchemaVersion > CurrentSchemaVersion || dto.SchemaVersion < MinimumReadableSchemaVersion)
            {
                throw new SaveGameException($"This save was written by a different version of the game (schema {dto.SchemaVersion}, " + $"this build reads schema {MinimumReadableSchemaVersion} to {CurrentSchemaVersion}).");
            }

            if (dto.League == null || dto.Season == null)
            {
                throw new SaveGameException("The save file is missing its league or season.");
            }

            UpgradeAbilityScale(dto);

            var hasClubs = dto.Clubs != null && dto.Clubs.Count > 0;

            if (!hasClubs && (dto.League.Teams == null || dto.League.Teams.Count == 0))
            {
                throw new SaveGameException("The save file contains no clubs.");
            }

            var clubs = hasClubs ? dto.Clubs.Select(FromDto).ToList() : dto.League.Teams.Select(UpgradeLegacyTeam).ToList();

            var teams = clubs.SelectMany(club => club.Teams).ToDictionary(team => team.Id);

            var retiredPlayers = (dto.RetiredPlayers ?? new List<PlayerDto>())
                .Select(FromDto)
                .ToList();

            var players = clubs
                .SelectMany(club => club.Squad)
                .Concat(retiredPlayers)
                .ToDictionary(player => player.Id);

            var leagueDtos = dto.Leagues != null && dto.Leagues.Count > 0 ? dto.Leagues : new List<LeagueDto> { dto.League };

            var leagues = leagueDtos.Select(entry => FromDto(entry, teams, clubs)).ToDictionary(entry => entry.Id);

            var league = leagues.Values.OrderBy(entry => entry.Id).First();

            var season = FromDto(dto.Season, league, teams, players);

            var completed = (dto.CompletedSeasons ?? new List<CompletedSeasonDto>())
                .Select(completedSeason => FromDto(completedSeason, teams, players))
                .ToList();

            var userTeam = Resolve(teams, dto.UserTeamId, "user team");

            var seasonDtos = dto.Seasons != null && dto.Seasons.Count > 0 ? dto.Seasons : new List<SeasonDto> { dto.Season };

            var runningSeasons = seasonDtos.Select(entry => FromDto(entry, ResolveLeagueFor(entry, leagues, league), teams, players)).ToList();

            var career = new Career(clubs, runningSeasons, completed, retiredPlayers);

            if (!string.IsNullOrWhiteSpace(dto.CurrentDate))
            {
                if (!DateTime.TryParse(dto.CurrentDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var currentDate))
                {
                    throw new SaveGameException($"The save file has an unreadable current date: '{dto.CurrentDate}'.");
                }

                if (currentDate.Date > career.CurrentDate)
                {
                    career.SetCurrentDate(currentDate);
                }
            }

            var managedTeamIds = dto.ManagedTeamIds != null && dto.ManagedTeamIds.Count > 0 ? dto.ManagedTeamIds.ToList() : new List<int> { userTeam.Id };

            foreach (var managedTeamId in managedTeamIds)
            {
                if (!teams.ContainsKey(managedTeamId))
                {
                    throw new SaveGameException($"The save file marks an unknown team as managed (id {managedTeamId}).");
                }
            }

            var tactics = new Dictionary<int, TeamTactics>();
            var rotations = new Dictionary<int, TeamRotation>();

            foreach (var setup in dto.TeamSetups ?? new List<TeamSetupDto>())
            {
                var setupTeam = Resolve(teams, setup.TeamId, "team setup");

                var setupRules = career.GetSeasonFor(setupTeam)?.Rules ?? season.Rules;

                if (setup.Tactics != null)
                {
                    tactics[setup.TeamId] = FromDto(setup.Tactics);
                }

                if (setup.Rotation != null)
                {
                    rotations[setup.TeamId] = FromDto(setup.Rotation, teams, players, setupRules);
                }
            }

            return new GameSessionSnapshot
            {
                Career = career,
                UserTeam = userTeam,
                ManagedTeamIds = managedTeamIds,
                Tactics = tactics,
                Rotations = rotations,
                UserTactics = FromDto(dto.UserTactics),
                UserRotation = FromDto(dto.UserRotation, teams, players, career.GetSeasonFor(userTeam)?.Rules ?? season.Rules),
                NextSeed = dto.NextSeed,
                CurrentFixtureRecorded = dto.CurrentFixtureRecorded
            };
        }

        private static CompletedSeason FromDto(CompletedSeasonDto dto, Dictionary<int, Team> teams, Dictionary<int, Player> players)
        {
            var standings = dto.FinalStandings
                .Select(standing => new LeagueStanding(
                    standing.Position,
                    Resolve(teams, standing.TeamId, "archived standing team"),
                    standing.Played,
                    standing.Wins,
                    standing.Losses,
                    standing.PointsFor,
                    standing.PointsAgainst))
                .ToList();

            var statistics = dto.PlayerStatistics
                .Select(entry => new PlayerSeasonStatistics(
                    Resolve(players, entry.PlayerId, "archived player"),
                    entry.GamesPlayed,
                    entry.GamesStarted,
                    entry.TotalMinutes,
                    entry.Points,
                    entry.FieldGoalsMade,
                    entry.FieldGoalsAttempted,
                    entry.ThreePointsMade,
                    entry.ThreePointsAttempted,
                    entry.FreeThrowsMade,
                    entry.FreeThrowsAttempted,
                    entry.OffensiveRebounds,
                    entry.DefensiveRebounds,
                    entry.Assists,
                    entry.Steals,
                    entry.PersonalFouls,
                    entry.Turnovers))
                .ToList();

            return new CompletedSeason(dto.Id, dto.CompetitionId, dto.Name, standings, statistics);
        }

        private static Club FromDto(ClubDto dto)
        {
            if (dto.Squad == null || dto.Squad.Count == 0)
            {
                throw new SaveGameException($"Club '{dto.Name}' has no squad in the save file.");
            }

            var squad = dto.Squad.Select(FromDto).ToList();
            var squadById = squad.ToDictionary(player => player.Id);

            var teams = (dto.Teams ?? new List<TeamDto>()).Select(team => FromDto(team, dto.Id, squadById)).ToList();

            return new Club(dto.Id, dto.Name, squad, teams, dto.ShortName, dto.City, dto.PrimaryColor, dto.SecondaryColor, dto.TertiaryColor, dto.BadgeTemplate);
        }

        private static Team FromDto(TeamDto dto, int clubId, Dictionary<int, Player> squad)
        {
            var players = dto.PlayerIds.Select(id => Resolve(squad, id, $"player on team '{dto.Name}'")).ToList();

            return new Team(dto.Id, dto.Name, players, clubId, ParseEnum<TeamType>(dto.Type, "team type"));
        }

        private static League FromDto(LeagueDto dto, Dictionary<int, Team> teams, List<Club> clubs)
        {
            var teamIds = dto.TeamIds != null && dto.TeamIds.Count > 0 ? dto.TeamIds : clubs.Select(club => club.FirstTeam.Id).ToList();

            var leagueTeams = teamIds.Select(id => Resolve(teams, id, $"team in league '{dto.Name}'")).ToList();

            return new League(dto.Id, dto.Name, leagueTeams, FromDto(dto.Calendar));
        }

        private static Club UpgradeLegacyTeam(LegacyTeamDto dto)
        {
            if (dto.Players == null || dto.Players.Count == 0)
            {
                throw new SaveGameException($"Team '{dto.Name}' has no players in the save file.");
            }

            var squad = dto.Players.Select(FromDto).ToList();

            var team = new Team(dto.Id, dto.Name, squad, dto.Id, TeamType.First);

            return new Club(dto.Id, dto.Name, squad, new[] { team });
        }

        private static Player FromDto(PlayerDto dto)
        {
            return new Player(
                dto.Id,
                dto.FirstName,
                dto.LastName,
                ParseEnum<PlayerPosition>(dto.Position, "player position"),
                FromDto(dto.Attributes),
                dto.Age <= 0 ? Player.DefaultAge : dto.Age,
                dto.Potential,
                dto.ScoutedPotential,
                string.IsNullOrWhiteSpace(dto.Nationality) ? Player.DefaultNationality : dto.Nationality);
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
            var rules = FromDto(dto.Rules);

            var fixtures = new List<Fixture>(dto.Fixtures.Count);

            foreach (var fixtureDto in dto.Fixtures)
            {
                var fixture = new Fixture(
                    fixtureDto.Id,
                    fixtureDto.RoundNumber,
                    Resolve(teams, fixtureDto.HomeTeamId, "fixture home team"),
                    Resolve(teams, fixtureDto.AwayTeamId, "fixture away team"),
                    ParseFixtureDate(fixtureDto, league));

                if (fixtureDto.Result != null)
                {
                    fixture.Complete(FromDto(fixtureDto.Result, teams, players, rules));
                }

                fixtures.Add(fixture);
            }

            return new Season(dto.Id, dto.Name, league, fixtures, rules);
        }

        private static MatchResult FromDto(MatchResultDto dto, Dictionary<int, Team> teams, Dictionary<int, Player> players, CompetitionRules rules)
        {
            return new MatchResult(
                Resolve(teams, dto.HomeTeamId, "result home team"),
                Resolve(teams, dto.AwayTeamId, "result away team"),
                FromDto(dto.HomeRotation, teams, players, rules),
                FromDto(dto.AwayRotation, teams, players, rules),
                dto.HomePeriodScores.ToList(),
                dto.AwayPeriodScores.ToList(),
                dto.Events.Select(matchEvent => FromDto(matchEvent, teams, players)).ToList(),
                dto.HomePlayerStats.Select(box => FromDto(box, players)).ToList(),
                dto.AwayPlayerStats.Select(box => FromDto(box, players)).ToList());
        }

        private static TeamRotation FromDto(TeamRotationDto dto, Dictionary<int, Team> teams, Dictionary<int, Player> players, CompetitionRules rules)
        {
            if (dto == null)
            {
                throw new SaveGameException("The save file is missing a rotation.");
            }

            var starters = dto.Starters.ToDictionary(starter => ParseEnum<PlayerPosition>(starter.Position, "starter position"), starter => Resolve(players, starter.PlayerId, "starter"));

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
                Resolve(players, dto.PrimaryScorerId, "primary scorer"),
                rules);
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
            var secondaryPlayer = dto.SecondaryPlayerId == MatchEventDto.NoPlayer ? null : Resolve(players, dto.SecondaryPlayerId, "secondary player");

            OffensiveActionType? offensiveAction = string.IsNullOrEmpty(dto.OffensiveAction) ? (OffensiveActionType?)null : ParseEnum<OffensiveActionType>(dto.OffensiveAction, "offensive action");

            ShotZone? shotZone = string.IsNullOrEmpty(dto.ShotZone) ? (ShotZone?)null : ParseEnum<ShotZone>(dto.ShotZone, "shot zone");

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

        private static void UpgradeAbilityScale(SaveGameDto dto)
        {
            if (dto.SchemaVersion >= 9)
            {
                return;
            }

            var players = new List<PlayerDto>();

            if (dto.Clubs != null)
            {
                players.AddRange(dto.Clubs.Where(club => club.Squad != null).SelectMany(club => club.Squad));
            }

            if (dto.League != null && dto.League.Teams != null)
            {
                players.AddRange(dto.League.Teams.Where(team => team.Players != null).SelectMany(team => team.Players));
            }

            if (dto.RetiredPlayers != null)
            {
                players.AddRange(dto.RetiredPlayers);
            }

            foreach (var player in players)
            {
                player.Potential = ScaleLegacyAbility(player.Potential);
                player.ScoutedPotential = ScaleLegacyAbility(player.ScoutedPotential);
            }
        }

        private static int ScaleLegacyAbility(int legacyValue)
        {
            return legacyValue <= 0 ? 0 : PlayerRating.ClampAbility(legacyValue * (int)PlayerRating.AbilityPerAttributePoint);
        }

        private static League ResolveLeagueFor(SeasonDto dto, Dictionary<int, League> leagues, League fallback)
        {
            if (dto.CompetitionId == 0)
            {
                return fallback;
            }

            if (!leagues.TryGetValue(dto.CompetitionId, out var league))
            {
                throw new SaveGameException($"Season '{dto.Name}' refers to an unknown competition (id {dto.CompetitionId}).");
            }

            return league;
        }

        private static DateTime ParseFixtureDate(FixtureDto dto, League league)
        {
            if (string.IsNullOrWhiteSpace(dto.Date))
            {
                return league.Calendar.GetRoundDate(CompetitionCalendar.DefaultFirstSeasonYear, dto.RoundNumber);
            }

            if (!DateTime.TryParse(dto.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                throw new SaveGameException($"Fixture {dto.Id} has an unreadable date: '{dto.Date}'.");
            }

            return parsed.Date;
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
