using System;
using ProBasketballManager.Domain.Players;

namespace ProBasketballManager.Domain.Matches
{
    /// <summary>
    /// Models how tired a player becomes over the course of a single match.
    ///
    /// Fatigue runs from 0.0 (completely fresh) to 1.0 (exhausted). It builds while
    /// a player is on court and recovers while they sit, both at a rate set by the
    /// player's Stamina attribute. Nothing carries between matches: every player
    /// starts each game at zero.
    ///
    /// Fatigue reaches the simulation in two separate ways, and both matter:
    ///
    ///   1. It degrades performance. A tired player shoots worse, turns the ball
    ///      over more, defends and rebounds less effectively, and fouls more.
    ///   2. It drives substitutions. RotationRuntime subtracts a fatigue cost from
    ///      each player's minutes deficit, so tired players get pulled ahead of
    ///      schedule and rested ones come on early.
    ///
    /// The second is what turns the rotation screen into a real decision. Without
    /// it, playing five starters for the whole game is free.
    /// </summary>
    public static class FatigueModel
    {
        /// <summary>
        /// Fatigue added per second on court for a player of average stamina.
        /// Tuned so that a full period of continuous play leaves an average player
        /// around 55 percent fatigued.
        /// </summary>
        private const double BaseDrainPerSecond = 0.00092;

        /// <summary>
        /// Fatigue removed per second on the bench for a player of average stamina.
        /// Recovery is faster than drain, which is what makes short rests useful and
        /// keeps a well managed rotation ahead of one that rides its starters.
        /// </summary>
        private const double BaseRecoveryPerSecond = 0.00155;

        /// <summary>
        /// How much each point of Stamina above or below average changes the rate.
        /// A player at 20 stamina tires roughly a third slower than one at 1.
        /// </summary>
        private const double StaminaDrainInfluence = 0.040;
        private const double StaminaRecoveryInfluence = 0.030;

        private const double AverageStamina = 10.5;

        // ---- Performance penalties at maximum fatigue -----------------------
        // Each of these is the full penalty applied at fatigue 1.0, scaled linearly
        // down to zero at fatigue 0.0. They are deliberately modest: fatigue should
        // shape rotation decisions, not make exhausted players unable to function.

        /// <summary>Percentage points removed from shot chance when exhausted.</summary>
        public const double MaximumShootingPenalty = 0.070;

        /// <summary>Added to the chance a possession ends in a turnover.</summary>
        public const double MaximumTurnoverPenalty = 0.045;

        /// <summary>Fraction of defensive effectiveness lost when exhausted.</summary>
        public const double MaximumDefensivePenalty = 0.180;

        /// <summary>Fraction of rebounding weight lost when exhausted.</summary>
        public const double MaximumReboundingPenalty = 0.220;

        /// <summary>Multiplier applied to foul chance when exhausted.</summary>
        public const double MaximumFoulPenalty = 0.300;

        public const double Fresh = 0.0;
        public const double Exhausted = 1.0;

        /// <summary>
        /// Fatigue gained by a player after a spell on court. Pace is passed in so a
        /// team running a fast tempo tires faster, which gives the pace slider a real
        /// cost rather than being a free source of extra possessions.
        /// </summary>
        public static double ApplyExertion(double currentFatigue, Player player, double seconds, double paceMultiplier, double conditionFactor)
        {
            var staminaFactor = 1.0 + ((AverageStamina - player.Attributes.Stamina) * StaminaDrainInfluence);

            var drain = BaseDrainPerSecond * seconds * staminaFactor * paceMultiplier * conditionFactor;

            return Clamp(currentFatigue + drain);
        }

        /// <summary>
        /// Smallest and largest per match condition multiplier. Each player is rolled
        /// a value in this band once per game, representing whether they turned up
        /// sharp or heavy legged. Without it every match produces an identical
        /// substitution pattern, because fatigue would otherwise be a fixed function
        /// of time on court.
        /// </summary>
        public const double MinimumConditionFactor = 0.78;
        public const double MaximumConditionFactor = 1.22;

        /// <summary>
        /// Rolls a player's condition for a single match. Higher stamina narrows the
        /// band slightly, so reliable players are also more predictable.
        /// </summary>
        public static double RollConditionFactor(Player player, double roll)
        {
            var spread = (MaximumConditionFactor - MinimumConditionFactor) / 2.0;
            var reliability = 1.0 - ((player.Attributes.Stamina - AverageStamina) * 0.012);
            var centre = (MaximumConditionFactor + MinimumConditionFactor) / 2.0;

            return centre + ((roll - 0.5) * 2.0 * spread * reliability);
        }

        /// <summary>
        /// Fatigue recovered by a player after a spell on the bench.
        /// </summary>
        public static double ApplyRecovery(double currentFatigue, Player player, double seconds)
        {
            var staminaFactor = 1.0 + ((player.Attributes.Stamina - AverageStamina) * StaminaRecoveryInfluence);

            var recovery = BaseRecoveryPerSecond * seconds * staminaFactor;

            return Clamp(currentFatigue - recovery);
        }

        /// <summary>Points of shot chance lost at the given fatigue level.</summary>
        public static double GetShootingPenalty(double fatigue)
        {
            return fatigue * MaximumShootingPenalty;
        }

        /// <summary>Extra turnover chance at the given fatigue level.</summary>
        public static double GetTurnoverPenalty(double fatigue)
        {
            return fatigue * MaximumTurnoverPenalty;
        }

        /// <summary>
        /// Multiplier applied to a defender's contribution. Returns 1.0 when fresh
        /// and falls as fatigue rises.
        /// </summary>
        public static double GetDefensiveMultiplier(double fatigue)
        {
            return 1.0 - (fatigue * MaximumDefensivePenalty);
        }

        /// <summary>
        /// Multiplier applied to a player's rebounding weight. Returns 1.0 when
        /// fresh, so a tired big man loses boards to a fresher opponent.
        /// </summary>
        public static double GetReboundingMultiplier(double fatigue)
        {
            return 1.0 - (fatigue * MaximumReboundingPenalty);
        }

        /// <summary>
        /// Multiplier applied to foul chance. Returns 1.0 when fresh and rises with
        /// fatigue, because tired defenders reach rather than move their feet.
        /// </summary>
        public static double GetFoulMultiplier(double fatigue)
        {
            return 1.0 + (fatigue * MaximumFoulPenalty);
        }

        /// <summary>
        /// Converts fatigue into an equivalent number of seconds of playing time, so
        /// the substitution logic can weigh "this player is tired" against "this
        /// player is behind on minutes" on a single scale. A fully exhausted player
        /// is treated as though they were this many seconds ahead of schedule.
        /// </summary>
        public const double SubstitutionSecondsPerFatiguePoint = 620.0;

        public static double GetSubstitutionCost(double fatigue)
        {
            return fatigue * SubstitutionSecondsPerFatiguePoint;
        }

        private static double Clamp(double value)
        {
            return Math.Max(Fresh, Math.Min(Exhausted, value));
        }
    }
}