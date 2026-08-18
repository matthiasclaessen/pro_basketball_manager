using System;

namespace ProBasketballManager.Domain.Competitions
{
    public sealed class CompetitionRules
    {
        public string Name { get; }

        public int PeriodCount { get; }

        public int PeriodLengthSeconds { get; }

        public int OvertimeLengthSeconds { get; }

        public int PlayersOnCourt { get; }

        public int PersonalFoulsToDisqualify { get; }

        public int TeamFoulsBeforeBonus { get; }

        public int BonusFreeThrows { get; }

        public int RosterSize { get; }

        public int RoundRobinPasses { get; }

        public int RegulationSeconds => PeriodCount * PeriodLengthSeconds;

        public double RegulationMinutes => RegulationSeconds / 60.0;

        public double TotalPlayerMinutesPerGame => PlayersOnCourt * RegulationMinutes;

        public CompetitionRules(string name, int periodCount, int periodLengthSeconds, int overtimeLengthSeconds, int playersOnCourt, int personalFoulsToDisqualify, int teamFoulsBeforeBonus, int bonusFreeThrows, int rosterSize, int roundRobinPasses)
        {
            Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("A ruleset needs a name.", nameof(name)) : name;

            PeriodCount = Require(periodCount, 1, 8, nameof(periodCount));
            PeriodLengthSeconds = Require(periodLengthSeconds, 60, 1800, nameof(periodLengthSeconds));
            OvertimeLengthSeconds = Require(overtimeLengthSeconds, 30, 1800, nameof(overtimeLengthSeconds));
            PlayersOnCourt = Require(playersOnCourt, 3, 6, nameof(playersOnCourt));
            PersonalFoulsToDisqualify = Require(personalFoulsToDisqualify, 2, 12, nameof(personalFoulsToDisqualify));
            TeamFoulsBeforeBonus = Require(teamFoulsBeforeBonus, 1, 12, nameof(teamFoulsBeforeBonus));
            BonusFreeThrows = Require(bonusFreeThrows, 1, 3, nameof(bonusFreeThrows));
            RosterSize = Require(rosterSize, playersOnCourt, 30, nameof(rosterSize));
            RoundRobinPasses = Require(roundRobinPasses, 1, 4, nameof(roundRobinPasses));
        }

        public static CompetitionRules Fiba => new CompetitionRules(
            name: "FIBA",
            periodCount: 4,
            periodLengthSeconds: 600,
            overtimeLengthSeconds: 300,
            playersOnCourt: 5,
            personalFoulsToDisqualify: 5,
            teamFoulsBeforeBonus: 4,
            bonusFreeThrows: 2,
            rosterSize: 12,
            roundRobinPasses: 2);

        public static CompetitionRules Nba => new CompetitionRules(
            name: "NBA",
            periodCount: 4,
            periodLengthSeconds: 720,
            overtimeLengthSeconds: 300,
            playersOnCourt: 5,
            personalFoulsToDisqualify: 6,
            teamFoulsBeforeBonus: 4,
            bonusFreeThrows: 2,
            rosterSize: 15,
            roundRobinPasses: 2);

        public bool IsOvertimePeriod(int periodNumber)
        {
            return periodNumber > PeriodCount;
        }

        public int GetPeriodLengthSeconds(int periodNumber)
        {
            return IsOvertimePeriod(periodNumber) ? OvertimeLengthSeconds : PeriodLengthSeconds;
        }

        public override string ToString()
        {
            return $"{Name}: {PeriodCount}x{PeriodLengthSeconds / 60}min, " + $"{PersonalFoulsToDisqualify} fouls, {RosterSize} player squads";
        }

        private static int Require(int value, int minimum, int maximum, string parameterName)
        {
            if (value < minimum || value > maximum)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} must be between {minimum} and {maximum}.");
            }

            return value;
        }
    }
}