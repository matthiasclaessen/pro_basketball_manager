using System;
using System.Collections.Generic;

namespace ProBasketballManager.Persistence
{
    public sealed class SaveGameDto
    {
        public int SchemaVersion;

        public string SaveName;
        public string SavedAtUtc;

        public string Description;

        public LeagueDto League;
        public SeasonDto Season;

        public int UserTeamId;

        public TeamTacticsDto UserTactics;
        public TeamRotationDto UserRotation;

        public uint NextSeed;

        public bool CurrentFixtureRecorded;
    }

    public sealed class LeagueDto
    {
        public int Id;
        public string Name;
        public List<TeamDto> Teams = new List<TeamDto>();
    }

    public sealed class TeamDto
    {
        public int Id;
        public string Name;
        public List<PlayerDto> Players = new List<PlayerDto>();
    }

    public sealed class PlayerDto
    {
        public int Id;
        public string FirstName;
        public string LastName;

        public string Position;

        public PlayerAttributesDto Attributes;
    }

    public sealed class PlayerAttributesDto
    {
        public int Finishing;
        public int MidRange;
        public int ThreePoint;
        public int FreeThrow;
        public int Passing;
        public int BallHandling;
        public int PerimeterDefense;
        public int InteriorDefense;
        public int OffensiveRebounding;
        public int DefensiveRebounding;
        public int Speed;
        public int Strength;
        public int Stamina;
        public int BasketballIq;
    }

    public sealed class SeasonDto
    {
        public int Id;
        public string Name;
        public List<FixtureDto> Fixtures = new List<FixtureDto>();
    }

    public sealed class FixtureDto
    {
        public int Id;
        public int RoundNumber;
        public int HomeTeamId;
        public int AwayTeamId;

        public MatchResultDto Result;
    }

    public sealed class MatchResultDto
    {
        public int HomeTeamId;
        public int AwayTeamId;

        public TeamRotationDto HomeRotation;
        public TeamRotationDto AwayRotation;

        public List<int> HomePeriodScores = new List<int>();
        public List<int> AwayPeriodScores = new List<int>();

        public List<PlayerBoxScoreDto> HomePlayerStats = new List<PlayerBoxScoreDto>();
        public List<PlayerBoxScoreDto> AwayPlayerStats = new List<PlayerBoxScoreDto>();

        public List<MatchEventDto> Events = new List<MatchEventDto>();
    }

    public sealed class TeamRotationDto
    {
        public int TeamId;

        public List<StarterDto> Starters = new List<StarterDto>();

        public List<RotationAssignmentDto> Assignments = new List<RotationAssignmentDto>();

        public int PrimaryBallHandlerId;
        public int PrimaryScorerId;
    }

    public sealed class StarterDto
    {
        public string Position;
        public int PlayerId;
    }

    public sealed class RotationAssignmentDto
    {
        public int PlayerId;
        public int TargetMinutes;
        public int RotationOrder;
    }

    public sealed class TeamTacticsDto
    {
        public int Pace;
        public int RimWeight;
        public int MidRangeWeight;
        public int ThreePointWeight;
        public int BallMovement;
        public int PerimeterPressure;
        public int ProtectPaint;
    }

    public sealed class PlayerBoxScoreDto
    {
        public int PlayerId;
        public bool IsStarter;
        public double SecondsPlayed;
        public int Points;
        public int FieldGoalsMade;
        public int FieldGoalsAttempted;
        public int ThreePointsMade;
        public int ThreePointsAttempted;
        public int FreeThrowsMade;
        public int FreeThrowsAttempted;
        public int OffensiveRebounds;
        public int DefensiveRebounds;
        public int Assists;
        public int Steals;
        public int PersonalFouls;
        public int Turnovers;
        public double EndOfMatchFatigue;
        public double PeakFatigue;
    }

    public sealed class MatchEventDto
    {
        public int PeriodNumber;
        public int SecondsRemaining;
        public string Type;
        public int TeamId;
        public int PlayerId;

        public int SecondaryPlayerId = NoPlayer;

        public string OffensiveAction;
        public string ShotZone;

        public int HomeScore;
        public int AwayScore;

        public const int NoPlayer = -1;
    }

    public sealed class SaveGameException : Exception
    {
        public SaveGameException(string message) : base(message)
        {
        }

        public SaveGameException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}