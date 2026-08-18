using System;

namespace ProBasketballManager.Domain.Competitions
{
    public sealed class CompetitionCalendar
    {
        public const int DefaultFirstSeasonYear = 2026;

        public static readonly CompetitionCalendar Default = new CompetitionCalendar(DayOfWeek.Saturday, 7, 9, 15);

        public DayOfWeek MatchDay { get; }

        public int DaysBetweenRounds { get; }

        public int FirstRoundMonth { get; }

        public int FirstRoundDay { get; }

        public CompetitionCalendar(DayOfWeek matchDay, int daysBetweenRounds, int firstRoundMonth, int firstRoundDay)
        {
            if (daysBetweenRounds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(daysBetweenRounds), "There must be at least one day between rounds.");
            }

            if (firstRoundMonth < 1 || firstRoundMonth > 12)
            {
                throw new ArgumentOutOfRangeException(nameof(firstRoundMonth), "The first round month must be between 1 and 12.");
            }

            if (firstRoundDay < 1 || firstRoundDay > 28)
            {
                throw new ArgumentOutOfRangeException(nameof(firstRoundDay), "The first round day must be between 1 and 28 so it is valid in every month.");
            }

            MatchDay = matchDay;
            DaysBetweenRounds = daysBetweenRounds;
            FirstRoundMonth = firstRoundMonth;
            FirstRoundDay = firstRoundDay;
        }

        public DateTime GetFirstRoundDate(int year)
        {
            if (year < 1 || year > 9999)
            {
                throw new ArgumentOutOfRangeException(nameof(year), "The season year must be a valid calendar year.");
            }

            var earliest = new DateTime(year, FirstRoundMonth, FirstRoundDay);

            var offset = ((int)MatchDay - (int)earliest.DayOfWeek + 7) % 7;

            return earliest.AddDays(offset);
        }

        public DateTime GetRoundDate(int year, int roundNumber)
        {
            if (roundNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber), "Round number must be at least 1.");
            }

            return GetFirstRoundDate(year).AddDays((roundNumber - 1) * DaysBetweenRounds);
        }
    }
}
