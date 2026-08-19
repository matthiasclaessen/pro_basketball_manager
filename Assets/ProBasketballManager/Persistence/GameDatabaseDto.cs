using System.Collections.Generic;

namespace ProBasketballManager.Persistence
{
    public sealed class GameDatabaseDto
    {
        public int SchemaVersion;
        public string Name;
        public string Description;

        public List<DatabaseClubDto> Clubs = new List<DatabaseClubDto>();
        public List<DatabaseTeamDto> Teams = new List<DatabaseTeamDto>();
        public List<DatabasePlayerDto> Players = new List<DatabasePlayerDto>();
        public List<DatabaseCompetitionDto> Competitions = new List<DatabaseCompetitionDto>();
    }

    public sealed class DatabaseClubDto
    {
        public int Id;
        public string Name;
        public string ShortName;
        public string City;
        public string PrimaryColor;
        public string SecondaryColor;
        public string TertiaryColor;
        public string BadgeTemplate;
    }

    public sealed class DatabaseTeamDto
    {
        public int Id;
        public int ClubId;
        public string Name;
        public string Type;
        public List<int> PlayerIds = new List<int>();
    }

    public sealed class DatabasePlayerDto
    {
        public int Id;
        public int ClubId;
        public string FirstName;
        public string LastName;
        public string Position;
        public string Nationality;
        public int Age;
        public int Potential;
        public int ScoutedPotential;
        public DatabaseAttributesDto Attributes;
    }

    public sealed class DatabaseAttributesDto
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

    public sealed class DatabaseCompetitionDto
    {
        public int Id;
        public string Name;
        public string Rules;
        public List<int> TeamIds = new List<int>();
        public DatabaseCalendarDto Calendar;
    }

    public sealed class DatabaseCalendarDto
    {
        public string MatchDay;
        public int DaysBetweenRounds;
        public int FirstRoundMonth;
        public int FirstRoundDay;
    }
}
