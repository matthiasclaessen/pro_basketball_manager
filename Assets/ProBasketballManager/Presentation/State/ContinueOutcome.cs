using System;
using System.Collections.Generic;
using ProBasketballManager.Domain.Competitions;

namespace ProBasketballManager.Presentation.State
{
    public enum ContinueStop
    {
        MatchDay,
        SeasonEnded,
        Idle
    }

    public sealed class ContinueOutcome
    {
        public ContinueStop Stop { get; }

        public DateTime Date { get; }

        public IReadOnlyList<Fixture> ManagedFixtures { get; }

        public int DaysAdvanced { get; }

        private ContinueOutcome(ContinueStop stop, DateTime date, int daysAdvanced, IReadOnlyList<Fixture> managedFixtures)
        {
            Stop = stop;
            Date = date;
            DaysAdvanced = daysAdvanced;
            ManagedFixtures = managedFixtures ?? new List<Fixture>();
        }

        public static ContinueOutcome MatchDay(DateTime date, int daysAdvanced, IReadOnlyList<Fixture> managedFixtures)
        {
            return new ContinueOutcome(ContinueStop.MatchDay, date, daysAdvanced, managedFixtures);
        }

        public static ContinueOutcome SeasonEnded(DateTime date, int daysAdvanced)
        {
            return new ContinueOutcome(ContinueStop.SeasonEnded, date, daysAdvanced, null);
        }

        public static ContinueOutcome Idle(DateTime date, int daysAdvanced)
        {
            return new ContinueOutcome(ContinueStop.Idle, date, daysAdvanced, null);
        }
    }
}
