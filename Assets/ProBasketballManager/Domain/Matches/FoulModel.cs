using System;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Tactics;

namespace ProBasketballManager.Domain.Matches
{
    /// <summary>
    /// Models fouls beyond the shooting foul the simulator already handled.
    ///
    /// Three new kinds are covered:
    ///
    ///   Non-shooting defensive fouls — a reach or a hold away from the shot. Once
    ///   the defending team is in the bonus these send the attacker to the line.
    ///
    ///   Offensive fouls — a charge. Counts as a turnover against the attacker and
    ///   never produces free throws.
    ///
    ///   Loose-ball fouls — contact on the glass, charged to whoever lost the
    ///   rebounding battle.
    ///
    /// Together these lift personal fouls from around 8 per game to a realistic
    /// figure, which no amount of tuning the shooting foul rate could achieve: only
    /// a fraction of real fouls happen on a shot attempt.
    ///
    /// Fouls also matter because they remove players. Under FIBA rules a fifth
    /// personal foul disqualifies, and a manager rests anyone approaching that limit
    /// long before it arrives. That gives bench depth a second reason to exist
    /// alongside fatigue.
    /// </summary>
    public static class FoulModel
    {
        // ---- Rules ------------------------------------------------------
        // These are FIBA rules, not tuning knobs. Changing them changes the sport.

        /// <summary>Personal fouls that disqualify a player.</summary>
        public const int DisqualificationLimit = 5;

        /// <summary>
        /// Team fouls allowed per period before the opponent shoots free throws on
        /// every subsequent defensive foul. The fifth team foul of a period is the
        /// first to send the attacker to the line.
        /// </summary>
        public const int TeamFoulsBeforeBonus = 4;

        /// <summary>Free throws awarded for a foul committed in the bonus.</summary>
        public const int BonusFreeThrows = 2;

        // ---- Rates ------------------------------------------------------

        /// <summary>
        /// Chance per possession of a defensive foul away from the shot. This is the
        /// largest single contributor to the personal foul count, because most real
        /// fouls are not committed on a shot attempt.
        /// </summary>
        public const double NonShootingFoulChance = 0.105;

        /// <summary>
        /// Chance per possession that the attacker commits an offensive foul. Charges
        /// are rare but they cost a possession as well as a foul.
        /// </summary>
        public const double OffensiveFoulChance = 0.020;

        /// <summary>Chance that a rebound is decided by a loose-ball foul.</summary>
        public const double LooseBallFoulChance = 0.030;

        /// <summary>
        /// How much each point of defensive rating above average reduces the chance
        /// of fouling. A disciplined defender reaches less.
        /// </summary>
        private const double DisciplineInfluence = 0.0022;

        /// <summary>
        /// How much an aggressive defensive tactic raises the foul rate. This is the
        /// cost that perimeter pressure and paint protection previously lacked.
        /// </summary>
        private const double PerimeterPressureFoulInfluence = 0.030;
        private const double ProtectPaintFoulInfluence = 0.034;

        private const double AverageAttributeRating = 10.5;

        // ---- Foul trouble -----------------------------------------------

        /// <summary>
        /// Seconds of substitution priority lost per level of foul trouble. Expressed
        /// in seconds so it combines with the fatigue cost and the minutes deficit on
        /// one scale.
        /// </summary>
        public const double FoulTroubleSecondsPerLevel = 760.0;

        /// <summary>
        /// Whether a player has fouled out and cannot return.
        /// </summary>
        public static bool IsDisqualified(int personalFouls)
        {
            return personalFouls >= DisqualificationLimit;
        }

        /// <summary>
        /// How deep in foul trouble a player is, given the period. The usual coaching
        /// rule of thumb is that carrying more fouls than the period number is
        /// dangerous: two in the first, three in the second, four in the third. In
        /// the closing period the limit relaxes, because there is no later game to
        /// save a player for.
        /// </summary>
        public static int GetFoulTroubleLevel(int personalFouls, int periodNumber)
        {
            if (periodNumber > 4)
            {
                // Overtime. Only outright disqualification matters now.
                return 0;
            }

            var tolerated = periodNumber;

            return Math.Max(0, personalFouls - tolerated);
        }

        /// <summary>
        /// Substitution cost in seconds for a player in foul trouble, so the rotation
        /// rests them before they foul out rather than after.
        /// </summary>
        public static double GetSubstitutionCost(int personalFouls, int periodNumber)
        {
            return GetFoulTroubleLevel(personalFouls, periodNumber) * FoulTroubleSecondsPerLevel;
        }

        /// <summary>
        /// Whether the defending team's next foul sends the attacker to the line.
        /// </summary>
        public static bool IsInBonus(int teamFoulsThisPeriod)
        {
            return teamFoulsThisPeriod >= TeamFoulsBeforeBonus;
        }

        /// <summary>
        /// Chance that the defence commits a foul away from the shot, adjusted for
        /// the defenders' discipline, their fatigue and how aggressive the tactics are.
        /// </summary>
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

        /// <summary>
        /// Chance the ball handler commits an offensive foul. Better ball handlers and
        /// smarter players charge into defenders less often.
        /// </summary>
        public static double GetOffensiveFoulChance(Player ballHandler, double fatigue)
        {
            var control = (ballHandler.Attributes.BallHandling + ballHandler.Attributes.BasketballIq) / 2.0;

            var chance = OffensiveFoulChance - ((control - AverageAttributeRating) * DisciplineInfluence);

            chance *= FatigueModel.GetFoulMultiplier(fatigue);

            return Math.Max(0.0, chance);
        }

        /// <summary>Chance a contested rebound produces a loose-ball foul.</summary>
        public static double GetLooseBallFoulChance(double averageFatigue)
        {
            return LooseBallFoulChance * FatigueModel.GetFoulMultiplier(averageFatigue);
        }
    }
}