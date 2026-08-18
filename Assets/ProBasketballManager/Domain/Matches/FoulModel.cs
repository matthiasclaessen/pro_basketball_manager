using System;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Tactics;

namespace ProBasketballManager.Domain.Matches
{
    public static class FoulModel
    {
        public const double NonShootingFoulChance = 0.105;

        public const double OffensiveFoulChance = 0.020;

        public const double LooseBallFoulChance = 0.030;

        private const double DisciplineInfluence = 0.0022;

        private const double PerimeterPressureFoulInfluence = 0.030;
        private const double ProtectPaintFoulInfluence = 0.034;

        private const double AverageAttributeRating = 10.5;

        public const double FoulTroubleSecondsPerLevel = 760.0;

        public static bool IsDisqualified(int personalFouls, int foulsToDisqualify)
        {
            return personalFouls >= foulsToDisqualify;
        }

        public static int GetFoulTroubleLevel(int personalFouls, int periodNumber, CompetitionRules rules)
        {
            if (rules.IsOvertimePeriod(periodNumber))
            {
                // Overtime. Only outright disqualification matters now.
                return 0;
            }

            var tolerated = periodNumber + (rules.PersonalFoulsToDisqualify - 5);

            return Math.Max(0, personalFouls - tolerated);
        }

        public static double GetSubstitutionCost(int personalFouls, int periodNumber, CompetitionRules rules)
        {
            return GetFoulTroubleLevel(personalFouls, periodNumber, rules) * FoulTroubleSecondsPerLevel;
        }

        public static bool IsInBonus(int teamFoulsThisPeriod, int teamFoulsBeforeBonus)
        {
            return teamFoulsThisPeriod >= teamFoulsBeforeBonus;
        }

        public static double GetNonShootingFoulChance(double averagePerimeterDefense, double averageFatigue, TeamTactics defendingTactics)
        {
            var chance = NonShootingFoulChance;

            chance -= (averagePerimeterDefense - AverageAttributeRating) * DisciplineInfluence;

            var perimeterAggression = Math.Max(0.0, (defendingTactics.PerimeterPressure - 50) / 50.0);
            var paintAggression = Math.Max(0.0, (defendingTactics.ProtectPaint - 50) / 50.0);

            chance += perimeterAggression * PerimeterPressureFoulInfluence;
            chance += paintAggression * ProtectPaintFoulInfluence;

            chance *= FatigueModel.GetFoulMultiplier(averageFatigue);

            return Math.Max(0.0, chance);
        }

        public static double GetOffensiveFoulChance(Player ballHandler, double fatigue)
        {
            var control = (ballHandler.Attributes.BallHandling + ballHandler.Attributes.BasketballIq) / 2.0;

            var chance = OffensiveFoulChance - ((control - AverageAttributeRating) * DisciplineInfluence);

            chance *= FatigueModel.GetFoulMultiplier(fatigue);

            return Math.Max(0.0, chance);
        }

        public static double GetLooseBallFoulChance(double averageFatigue)
        {
            return LooseBallFoulChance * FatigueModel.GetFoulMultiplier(averageFatigue);
        }
    }
}